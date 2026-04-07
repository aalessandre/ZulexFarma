using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.PlanosContas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlanosContasController : ControllerBase
{
    private readonly IPlanoContaService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public PlanosContasController(IPlanoContaService service, ILogAcaoService log, AppDbContext db) { _service = service; _log = log; _db = db; }

    /// <summary>
    /// Pesquisa planos de contas (nível PlanoConta apenas) por descrição ou código hierárquico.
    /// Mínimo 2 caracteres. Limite 30 resultados.
    /// </summary>
    [HttpGet("pesquisar")]
    public async Task<IActionResult> Pesquisar([FromQuery] string termo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
                return Ok(new { success = true, data = Array.Empty<object>() });

            // Busca apenas nível PlanoConta (3), ativos, direto no banco
            var termoNorm = termo.Trim().ToUpper();
            var resultados = await _db.PlanosContas
                .Include(p => p.ContaPai).ThenInclude(s => s!.ContaPai)
                .Where(p => p.Nivel == NivelConta.PlanoConta && p.Ativo &&
                    (p.Descricao.ToUpper().Contains(termoNorm)))
                .OrderBy(p => p.Descricao)
                .Take(30)
                .ToListAsync();

            var lista = resultados.Select(p =>
            {
                // Monta código hierárquico: grupo.subgrupo.plano
                var codigoHier = $"{p.Ordem:D2}";
                if (p.ContaPai != null)
                {
                    codigoHier = $"{p.ContaPai.Ordem}.{p.Ordem:D2}";
                    if (p.ContaPai.ContaPai != null)
                        codigoHier = $"{p.ContaPai.ContaPai.Ordem}.{p.ContaPai.Ordem}.{p.Ordem:D2}";
                }
                return new { id = p.Id, descricao = p.Descricao, codigoHierarquico = codigoHier };
            }).ToList();

            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PlanosContasController.Pesquisar"); return StatusCode(500, new { success = false, message = "Erro ao pesquisar." }); }
    }

    [HttpGet]
    [Permissao("plano-contas", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PlanosContasController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar plano de contas." }); }
    }

    [HttpPost]
    [Permissao("plano-contas", "i")]
    public async Task<IActionResult> Criar([FromBody] PlanoContaFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PlanosContasController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar plano de contas." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("plano-contas", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] PlanoContaFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Plano de contas não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PlanosContasController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar plano de contas." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("plano-contas", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Plano de contas não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PlanosContasController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir plano de contas." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("plano-contas", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("PlanoConta", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PlanosContasController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
