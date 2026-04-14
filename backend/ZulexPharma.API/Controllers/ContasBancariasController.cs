using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.ContasBancarias;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContasBancariasController : ControllerBase
{
    private readonly IContaBancariaService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public ContasBancariasController(IContaBancariaService service, ILogAcaoService log, AppDbContext db)
    {
        _service = service;
        _log = log;
        _db = db;
    }

    private long UsuarioId => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    [Permissao("contas-bancarias", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar contas bancárias." }); }
    }

    [HttpPost]
    [Permissao("contas-bancarias", "i")]
    public async Task<IActionResult> Criar([FromBody] ContaBancariaFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar conta bancária." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("contas-bancarias", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ContaBancariaFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta bancária não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar conta bancária." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("contas-bancarias", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta bancária não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir conta bancária." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("contas-bancarias", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("ContaBancaria", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }

    // ═══ Controle Bancário ═══════════════════════════════════════════

    /// <summary>Extrato: lista movimentos de uma conta bancária num período.</summary>
    [HttpGet("{id:long}/extrato")]
    [Permissao("contas-bancarias", "c")]
    public async Task<IActionResult> Extrato(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var query = _db.MovimentosContaBancaria
                .Include(m => m.Caixa)
                .Include(m => m.CaixaMovimento)
                .Include(m => m.Usuario).ThenInclude(u => u!.Colaborador).ThenInclude(c => c!.Pessoa)
                .Where(m => m.ContaBancariaId == id);

            if (dataInicio.HasValue) query = query.Where(m => m.DataMovimento >= dataInicio.Value);
            if (dataFim.HasValue) query = query.Where(m => m.DataMovimento < dataFim.Value.Date.AddDays(1));

            var lista = await query
                .OrderByDescending(m => m.DataMovimento)
                .Select(m => new MovimentoContaBancariaListDto
                {
                    Id = m.Id,
                    DataMovimento = m.DataMovimento,
                    Tipo = (int)m.Tipo,
                    TipoDescricao = m.Tipo == TipoMovimentoBancario.Entrada ? "Entrada" : "Saída",
                    Valor = m.Valor,
                    Descricao = m.Descricao,
                    CaixaId = m.CaixaId,
                    CaixaCodigo = m.Caixa != null ? m.Caixa.Codigo : null,
                    CaixaMovimentoId = m.CaixaMovimentoId,
                    CaixaMovimentoCodigo = m.CaixaMovimento != null ? m.CaixaMovimento.Codigo : null,
                    CaixaMovimentoTipo = m.CaixaMovimento != null ? (int)m.CaixaMovimento.Tipo : (int?)null,
                    UsuarioNome = m.Usuario != null && m.Usuario.Colaborador != null && m.Usuario.Colaborador.Pessoa != null ? m.Usuario.Colaborador.Pessoa.Nome : null,
                    Manual = m.CaixaMovimentoId == null && m.CaixaId == null
                })
                .ToListAsync();

            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em Extrato | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao carregar extrato." }); }
    }

    /// <summary>Saldo atual + totalizadores do período.</summary>
    [HttpGet("{id:long}/saldo")]
    [Permissao("contas-bancarias", "c")]
    public async Task<IActionResult> Saldo(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var conta = await _db.Set<ContaBancaria>().FirstOrDefaultAsync(c => c.Id == id);
            if (conta == null) return NotFound(new { success = false, message = "Conta bancária não encontrada." });

            var usuario = await _db.Usuarios.FindAsync(UsuarioId);
            var ehCofre = usuario != null && await _db.Filiais.AnyAsync(f => f.Id == usuario.FilialId && f.ContaCofreId == id);

            // Saldo atual = saldo inicial + entradas - saídas até agora (todas)
            var todasEntradas = await _db.MovimentosContaBancaria
                .Where(m => m.ContaBancariaId == id && m.Tipo == TipoMovimentoBancario.Entrada)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var todasSaidas = await _db.MovimentosContaBancaria
                .Where(m => m.ContaBancariaId == id && m.Tipo == TipoMovimentoBancario.Saida)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var saldoAtual = conta.SaldoInicial + todasEntradas - todasSaidas;

            // Totalizadores do período filtrado
            var queryPeriodo = _db.MovimentosContaBancaria.Where(m => m.ContaBancariaId == id);
            if (dataInicio.HasValue) queryPeriodo = queryPeriodo.Where(m => m.DataMovimento >= dataInicio.Value);
            if (dataFim.HasValue) queryPeriodo = queryPeriodo.Where(m => m.DataMovimento < dataFim.Value.Date.AddDays(1));

            var entradasPeriodo = await queryPeriodo
                .Where(m => m.Tipo == TipoMovimentoBancario.Entrada)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;
            var saidasPeriodo = await queryPeriodo
                .Where(m => m.Tipo == TipoMovimentoBancario.Saida)
                .SumAsync(m => (decimal?)m.Valor) ?? 0;

            return Ok(new { success = true, data = new ContaBancariaSaldoDto
            {
                ContaBancariaId = conta.Id,
                ContaBancariaNome = conta.Descricao,
                TipoConta = (int)conta.TipoConta,
                EhCofre = ehCofre,
                SaldoInicial = conta.SaldoInicial,
                DataSaldoInicial = conta.DataSaldoInicial,
                SaldoAtual = saldoAtual,
                TotalEntradasPeriodo = entradasPeriodo,
                TotalSaidasPeriodo = saidasPeriodo
            }});
        }
        catch (Exception ex) { Log.Error(ex, "Erro em Saldo | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao carregar saldo." }); }
    }

    /// <summary>Lançamento manual de entrada/saída numa conta bancária.</summary>
    [HttpPost("{id:long}/ajuste-manual")]
    [Permissao("contas-bancarias", "a")]
    public async Task<IActionResult> AjusteManual(long id, [FromBody] AjusteManualFormDto dto)
    {
        try
        {
            var conta = await _db.Set<ContaBancaria>().FirstOrDefaultAsync(c => c.Id == id);
            if (conta == null) return NotFound(new { success = false, message = "Conta bancária não encontrada." });
            if (dto.Valor <= 0) return BadRequest(new { success = false, message = "Valor deve ser maior que zero." });
            if (string.IsNullOrWhiteSpace(dto.Descricao)) return BadRequest(new { success = false, message = "Informe a descrição/motivo." });
            if (dto.Tipo != 1 && dto.Tipo != 2) return BadRequest(new { success = false, message = "Tipo inválido (1=Entrada, 2=Saída)." });

            var mov = new MovimentoContaBancaria
            {
                ContaBancariaId = id,
                DataMovimento = DataHoraHelper.Agora(),
                Tipo = (TipoMovimentoBancario)dto.Tipo,
                Valor = dto.Valor,
                Descricao = dto.Descricao.Trim(),
                UsuarioId = UsuarioId
            };
            _db.MovimentosContaBancaria.Add(mov);
            await _db.SaveChangesAsync();

            await _log.RegistrarAsync("ContasBancarias", "AJUSTE MANUAL", "MovimentoContaBancaria", mov.Id,
                novo: new() {
                    ["ContaBancariaId"] = id.ToString(),
                    ["Tipo"] = dto.Tipo == 1 ? "Entrada" : "Saída",
                    ["Valor"] = dto.Valor.ToString("N2"),
                    ["Descricao"] = dto.Descricao
                });

            return Ok(new { success = true, data = new { id = mov.Id } });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em AjusteManual | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao registrar ajuste." }); }
    }
}
