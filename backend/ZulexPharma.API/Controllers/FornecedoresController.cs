using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fornecedores;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FornecedoresController : ControllerBase
{
    private readonly IFornecedorService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public FornecedoresController(IFornecedorService service, ILogAcaoService log, AppDbContext db)
    {
        _service = service;
        _log = log;
        _db = db;
    }

    [HttpGet("pesquisar")]
    public async Task<IActionResult> Pesquisar([FromQuery] string termo, [FromQuery] int limit = 30)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
                return Ok(new { success = true, data = Array.Empty<object>() });

            var termoNorm = termo.Trim().ToUpper();
            var termoDigitos = new string(termo.Where(char.IsDigit).ToArray());

            var query = _db.Set<Domain.Entities.Fornecedor>()
                .Include(f => f.Pessoa)
                .Where(f => f.Ativo);

            query = query.Where(f =>
                (f.Codigo != null && f.Codigo.Contains(termoNorm)) ||
                (termoDigitos.Length > 0 && f.Pessoa.CpfCnpj.Contains(termoDigitos)) ||
                f.Pessoa.Nome.ToUpper().Contains(termoNorm) ||
                (f.Pessoa.RazaoSocial != null && f.Pessoa.RazaoSocial.ToUpper().Contains(termoNorm))
            );

            var resultados = await query.OrderBy(f => f.Pessoa.Nome).Take(limit)
                .Select(f => new
                {
                    id = f.Id,
                    pessoaId = f.PessoaId,
                    codigo = f.Codigo,
                    nome = f.Pessoa.Nome,
                    razaoSocial = f.Pessoa.RazaoSocial,
                    cpfCnpj = f.Pessoa.CpfCnpj,
                    ativo = f.Ativo
                })
                .ToListAsync();

            return Ok(new { success = true, data = resultados });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em FornecedoresController.Pesquisar"); return StatusCode(500, new { success = false, message = "Erro ao pesquisar fornecedores." }); }
    }

    [HttpGet]
    [Permissao("fornecedores", "c")]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _service.ListarAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FornecedoresController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar fornecedores." });
        }
    }

    [HttpGet("{id:long}")]
    [Permissao("fornecedores", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var data = await _service.ObterAsync(id);
            return Ok(new { success = true, data });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Fornecedor não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FornecedoresController.Obter | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao obter fornecedor." });
        }
    }

    [HttpPost]
    [Permissao("fornecedores", "i")]
    public async Task<IActionResult> Criar([FromBody] FornecedorFormDto dto)
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
            Log.Error(ex, "Erro em FornecedoresController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar fornecedor." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("fornecedores", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] FornecedorFormDto dto)
    {
        try
        {
            await _service.AtualizarAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Fornecedor não encontrado." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FornecedoresController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar fornecedor." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("fornecedores", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var resultado = await _service.ExcluirAsync(id);
            return Ok(new { success = true, resultado });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Fornecedor não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FornecedoresController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir fornecedor." });
        }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("fornecedores", "c")]
    public async Task<IActionResult> ObterLog(long id,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var data = await _log.ListarPorRegistroAsync("Fornecedor", id, dataInicio, dataFim);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FornecedoresController.ObterLog | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." });
        }
    }
}
