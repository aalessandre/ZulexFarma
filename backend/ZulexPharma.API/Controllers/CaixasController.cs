using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Caixa;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CaixasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICaixaMovimentoService _movimentoService;

    public CaixasController(AppDbContext db, ICaixaMovimentoService movimentoService)
    {
        _db = db;
        _movimentoService = movimentoService;
    }

    private long UsuarioId => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    /// <summary>Lista caixas da filial do usuário.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? status = null)
    {
        try
        {
            var usuario = await _db.Usuarios.FindAsync(UsuarioId);
            if (usuario == null) return BadRequest(new { success = false, message = "Usuário não encontrado." });

            var query = _db.Caixas
                .Include(c => c.Colaborador).ThenInclude(c => c.Pessoa)
                .Where(c => c.FilialId == usuario.FilialId);

            if (status == "fechado") query = query.Where(c => c.Status == CaixaStatus.Fechado);
            else if (status == "aberto") query = query.Where(c => c.Status == CaixaStatus.Aberto);
            else if (status == "conferido") query = query.Where(c => c.Status == CaixaStatus.Conferido);

            var caixas = await query.OrderByDescending(c => c.DataAbertura).Take(100).ToListAsync();
            return Ok(new { success = true, data = caixas.Select(MapearCaixa).ToList() });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixasController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar caixas." }); }
    }

    [HttpGet("aberto")]
    public async Task<IActionResult> ObterAberto()
    {
        try
        {
            var caixa = await _db.Caixas
                .Include(c => c.Colaborador).ThenInclude(c => c.Pessoa)
                .Where(c => c.UsuarioId == UsuarioId && c.Status == CaixaStatus.Aberto)
                .OrderByDescending(c => c.DataAbertura)
                .FirstOrDefaultAsync();

            if (caixa == null) return Ok(new { success = true, data = (object?)null });
            return Ok(new { success = true, data = MapearCaixa(caixa) });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ObterAberto"); return StatusCode(500, new { success = false, message = "Erro ao buscar caixa aberto." }); }
    }

    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirCaixaRequest request)
    {
        try
        {
            var usuario = await _db.Usuarios.Include(u => u.Colaborador).ThenInclude(c => c!.Pessoa).FirstOrDefaultAsync(u => u.Id == UsuarioId);
            if (usuario == null) return BadRequest(new { success = false, message = "Usuário não encontrado." });

            var caixaAberto = await _db.Caixas.AnyAsync(c => c.UsuarioId == UsuarioId && c.Status == CaixaStatus.Aberto);
            if (caixaAberto) return BadRequest(new { success = false, message = "Já existe um caixa aberto para este usuário." });

            var filialId = usuario.FilialId;
            var filial = await _db.Filiais.FirstOrDefaultAsync(f => f.Id == filialId);
            if (filial == null) return BadRequest(new { success = false, message = "Filial não encontrada." });
            if (filial.ContaCofreId == null)
                return BadRequest(new { success = false, message = "Conta Cofre não configurada para esta filial. Configure em Filiais antes de abrir o caixa." });

            var colaboradorId = usuario.ColaboradorId ?? 0L;
            if (colaboradorId == 0) return BadRequest(new { success = false, message = "Usuário não possui colaborador vinculado." });

            // Snapshot do modelo de fechamento
            var cfgModelo = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "caixa.modelo.fechamento");
            var modelo = string.IsNullOrWhiteSpace(cfgModelo?.Valor) ? "confirmacao_posse" : cfgModelo.Valor;

            var agora = DataHoraHelper.Agora();
            var caixa = new Caixa
            {
                FilialId = filialId,
                ColaboradorId = colaboradorId,
                UsuarioId = UsuarioId,
                DataAbertura = agora,
                ValorAbertura = request.ValorAbertura,
                Status = CaixaStatus.Aberto,
                ModeloFechamento = modelo
            };

            _db.Caixas.Add(caixa);
            await _db.SaveChangesAsync();

            // Movimento de abertura
            var tipoDinheiro = await _db.Set<TipoPagamento>()
                .Where(t => t.Modalidade == ModalidadePagamento.VendaVista && t.Ativo)
                .OrderBy(t => t.Ordem).FirstOrDefaultAsync();

            _db.CaixaMovimentos.Add(new CaixaMovimento
            {
                CaixaId = caixa.Id,
                Tipo = TipoMovimentoCaixa.Abertura,
                DataMovimento = agora,
                Valor = request.ValorAbertura,
                TipoPagamentoId = tipoDinheiro?.Id,
                Descricao = $"Abertura de caixa — fundo R$ {request.ValorAbertura:N2}",
                UsuarioId = UsuarioId,
                StatusConferencia = StatusConferenciaMovimento.Conferido,
                DataConferencia = agora,
                ConferidoPorUsuarioId = UsuarioId
            });
            await _db.SaveChangesAsync();

            caixa = await _db.Caixas
                .Include(c => c.Colaborador).ThenInclude(c => c.Pessoa)
                .FirstAsync(c => c.Id == caixa.Id);

            return Created(string.Empty, new { success = true, data = MapearCaixa(caixa) });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em Abrir"); return StatusCode(500, new { success = false, message = "Erro ao abrir caixa." }); }
    }

    [HttpPost("{id:long}/fechar")]
    public async Task<IActionResult> Fechar(long id, [FromBody] FechamentoFormDto? dto = null)
    {
        try
        {
            var caixa = await _db.Caixas.Include(c => c.Declarados).FirstOrDefaultAsync(c => c.Id == id);
            if (caixa == null) return NotFound(new { success = false, message = "Caixa não encontrado." });
            if (caixa.Status != CaixaStatus.Aberto) return BadRequest(new { success = false, message = "O caixa não está aberto." });

            var agora = DataHoraHelper.Agora();

            // No modo "conferência simples" os valores declarados são obrigatórios
            if (caixa.ModeloFechamento == "conferencia_simples" && dto?.Declarados != null)
            {
                _db.Set<CaixaFechamentoDeclarado>().RemoveRange(caixa.Declarados);
                foreach (var d in dto.Declarados)
                {
                    _db.Set<CaixaFechamentoDeclarado>().Add(new CaixaFechamentoDeclarado
                    {
                        CaixaId = caixa.Id,
                        TipoPagamentoId = d.TipoPagamentoId,
                        ValorDeclarado = d.ValorDeclarado
                    });
                }
            }

            caixa.Status = CaixaStatus.Fechado;
            caixa.DataFechamento = agora;
            if (!string.IsNullOrWhiteSpace(dto?.Observacao))
                caixa.Observacao = (caixa.Observacao + "\n" + dto.Observacao).Trim();

            _db.CaixaMovimentos.Add(new CaixaMovimento
            {
                CaixaId = caixa.Id,
                Tipo = TipoMovimentoCaixa.Fechamento,
                DataMovimento = agora,
                Valor = 0,
                Descricao = "Fechamento de caixa",
                UsuarioId = UsuarioId,
                StatusConferencia = StatusConferenciaMovimento.Conferido,
                DataConferencia = agora,
                ConferidoPorUsuarioId = UsuarioId
            });

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em Fechar"); return StatusCode(500, new { success = false, message = "Erro ao fechar caixa." }); }
    }

    /// <summary>Dados da conferência agrupados por forma de pagamento.</summary>
    [HttpGet("{id:long}/conferencia")]
    public async Task<IActionResult> Conferencia(long id)
    {
        try { return Ok(new { success = true, data = await _movimentoService.ObterConferenciaAsync(id) }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Conferencia"); return StatusCode(500, new { success = false, message = "Erro ao obter conferência." }); }
    }

    /// <summary>Finaliza a conferência do caixa.</summary>
    [HttpPost("{id:long}/conferir")]
    public async Task<IActionResult> Conferir(long id, [FromBody] ConferirCaixaFormDto dto)
    {
        try
        {
            await _movimentoService.ConferirCaixaAsync(id, dto, UsuarioId);
            return Ok(new { success = true });
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Conferir"); return StatusCode(500, new { success = false, message = "Erro ao conferir caixa." }); }
    }

    private static object MapearCaixa(Caixa c) => new
    {
        id = c.Id,
        codigo = c.Codigo,
        colaboradorNome = c.Colaborador?.Pessoa?.Nome ?? "",
        dataAbertura = c.DataAbertura,
        dataFechamento = c.DataFechamento,
        dataConferencia = c.DataConferencia,
        valorAbertura = c.ValorAbertura,
        status = (int)c.Status,
        modeloFechamento = c.ModeloFechamento
    };

    public class AbrirCaixaRequest
    {
        public decimal ValorAbertura { get; set; }
    }
}
