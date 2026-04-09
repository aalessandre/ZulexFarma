using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Colaboradores;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ColaboradoresController : ControllerBase
{
    private readonly IColaboradorService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public ColaboradoresController(IColaboradorService service, ILogAcaoService log, AppDbContext db)
    {
        _service = service;
        _log = log;
        _db = db;
    }

    /// <summary>Pesquisa colaboradores ativos por código ou nome. Retorna dados para seleção na pré-venda.</summary>
    [HttpGet("pesquisar")]
    public async Task<IActionResult> Pesquisar([FromQuery] string termo, [FromQuery] string status = "ativos", [FromQuery] int limit = 30)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
                return Ok(new { success = true, data = Array.Empty<object>() });

            var termoNorm = termo.Trim().ToUpper();

            var query = _db.Set<Domain.Entities.Colaborador>()
                .Include(c => c.Pessoa)
                .AsQueryable();

            if (status == "ativos") query = query.Where(c => c.Ativo);

            var lista = await query
                .Where(c =>
                    (c.Codigo != null && c.Codigo.Contains(termoNorm)) ||
                    c.Pessoa.Nome.ToUpper().Contains(termoNorm)
                )
                .OrderBy(c => c.Pessoa.Nome)
                .Take(limit)
                .Select(c => new
                {
                    id = c.Id,
                    codigo = c.Codigo,
                    nome = c.Pessoa.Nome
                })
                .ToListAsync();

            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ColaboradoresController.Pesquisar"); return StatusCode(500, new { success = false, message = "Erro ao pesquisar colaboradores." }); }
    }

    [HttpGet]
    [Permissao("colaboradores", "c")]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _service.ListarAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ColaboradoresController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar colaboradores." });
        }
    }

    [HttpGet("{id:long}")]
    [Permissao("colaboradores", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var data = await _service.ObterAsync(id);
            return Ok(new { success = true, data });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Colaborador não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ColaboradoresController.Obter | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao obter colaborador." });
        }
    }

    [HttpPost]
    [Permissao("colaboradores", "i")]
    public async Task<IActionResult> Criar([FromBody] ColaboradorFormDto dto)
    {
        try
        {
            var data = await _service.CriarAsync(dto);
            return Created(string.Empty, new { success = true, data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ColaboradoresController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar colaborador." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("colaboradores", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ColaboradorFormDto dto)
    {
        try
        {
            await _service.AtualizarAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Colaborador não encontrado." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ColaboradoresController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar colaborador." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("colaboradores", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var resultado = await _service.ExcluirAsync(id);
            return Ok(new { success = true, resultado });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Colaborador não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ColaboradoresController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir colaborador." });
        }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("colaboradores", "c")]
    public async Task<IActionResult> ObterLog(long id,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var data = await _log.ListarPorRegistroAsync("Colaborador", id, dataInicio, dataFim);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ColaboradoresController.ObterLog | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." });
        }
    }
}
