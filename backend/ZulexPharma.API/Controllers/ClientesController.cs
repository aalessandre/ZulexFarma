using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Clientes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public ClientesController(IClienteService service, ILogAcaoService log, AppDbContext db) { _service = service; _log = log; _db = db; }

    /// <summary>Pesquisa clientes por código, CPF/CNPJ ou nome. Retorna dados para seleção na pré-venda.</summary>
    [HttpGet("pesquisar")]
    public async Task<IActionResult> Pesquisar([FromQuery] string termo, [FromQuery] string status = "ativos", [FromQuery] int limit = 30)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
                return Ok(new { success = true, data = Array.Empty<object>() });

            var termoNorm = termo.Trim().ToUpper();
            var termoDigitos = CpfCnpjHelper.SomenteDigitos(termo);

            var query = _db.Set<Domain.Entities.Cliente>()
                .Include(c => c.Pessoa)
                .Include(c => c.Convenios).ThenInclude(cv => cv.Convenio).ThenInclude(cv => cv.Pessoa)
                .AsQueryable();

            // Filtro ativo
            if (status == "ativos") query = query.Where(c => c.Ativo);
            else if (status == "inativos") query = query.Where(c => !c.Ativo);

            // Busca por código, CPF/CNPJ, nome, matrícula ou cartão de convênio
            query = query.Where(c =>
                (c.Codigo != null && c.Codigo.Contains(termoNorm)) ||
                (termoDigitos.Length > 0 && c.Pessoa.CpfCnpj.Contains(termoDigitos)) ||
                c.Pessoa.Nome.ToUpper().Contains(termoNorm) ||
                c.Convenios.Any(cv => (cv.Matricula != null && cv.Matricula.Contains(termoNorm)) ||
                                      (cv.Cartao != null && cv.Cartao.Contains(termoNorm)))
            );

            var clientes = await query.OrderBy(c => c.Pessoa.Nome).Take(limit)
                .Select(c => new
                {
                    clienteId = c.Id,
                    codigo = c.Codigo,
                    nome = c.Pessoa.Nome,
                    cpfCnpj = c.Pessoa.CpfCnpj,
                    convenios = c.Convenios.Select(cv => new { id = cv.ConvenioId, nome = cv.Convenio.Pessoa.Nome, matricula = cv.Matricula, cartao = cv.Cartao }).ToList(),
                    ativo = c.Ativo
                })
                .ToListAsync();

            return Ok(new { success = true, data = clientes });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Pesquisar"); return StatusCode(500, new { success = false, message = "Erro ao pesquisar clientes." }); }
    }

    [HttpGet]
    [Permissao("clientes", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar clientes." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("clientes", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Cliente não encontrado." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Obter | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao obter cliente." }); }
    }

    [HttpPost]
    [Permissao("clientes", "i")]
    public async Task<IActionResult> Criar([FromBody] ClienteFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar cliente." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("clientes", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ClienteFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Cliente não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar cliente." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("clientes", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Cliente não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir cliente." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("clientes", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Cliente", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
