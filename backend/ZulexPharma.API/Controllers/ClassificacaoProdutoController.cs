using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

public abstract class ClassificacaoProdutoControllerBase<T> : ControllerBase where T : ClassificacaoProdutoBase, new()
{
    protected readonly ClassificacaoProdutoService<T> _service;
    protected readonly ILogAcaoService _log;
    protected readonly string _entidade;

    protected ClassificacaoProdutoControllerBase(ClassificacaoProdutoService<T> service, ILogAcaoService log, string entidade)
    {
        _service = service;
        _log = log;
        _entidade = entidade;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao listar {Ent}", _entidade); return StatusCode(500, new { success = false, message = $"Erro ao listar." }); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(id) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao obter {Ent} {Id}", _entidade, id); return StatusCode(500, new { success = false, message = "Erro ao obter." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ClassificacaoFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao criar {Ent}", _entidade); return StatusCode(500, new { success = false, message = "Erro ao criar." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ClassificacaoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao atualizar {Ent} {Id}", _entidade, id); return StatusCode(500, new { success = false, message = "Erro ao atualizar." }); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao excluir {Ent} {Id}", _entidade, id); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }

    [HttpGet("{id:long}/log")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync(_entidade, id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao buscar log {Ent} {Id}", _entidade, id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}

// ── Concrete controllers ──────────────────────────────────────────

[Authorize] [ApiController] [Route("api/grupos-principais")]
public class GruposPrincipaisController : ClassificacaoProdutoControllerBase<GrupoPrincipal>
{
    public GruposPrincipaisController(ClassificacaoProdutoService<GrupoPrincipal> s, ILogAcaoService l) : base(s, l, "GrupoPrincipal") {}
}

[Authorize] [ApiController] [Route("api/grupos-produtos")]
public class GruposProdutosController : ClassificacaoProdutoControllerBase<GrupoProduto>
{
    public GruposProdutosController(ClassificacaoProdutoService<GrupoProduto> s, ILogAcaoService l) : base(s, l, "GrupoProduto") {}
}

[Authorize] [ApiController] [Route("api/sub-grupos")]
public class SubGruposController : ClassificacaoProdutoControllerBase<SubGrupo>
{
    public SubGruposController(ClassificacaoProdutoService<SubGrupo> s, ILogAcaoService l) : base(s, l, "SubGrupo") {}
}

[Authorize] [ApiController] [Route("api/secoes")]
public class SecoesController : ClassificacaoProdutoControllerBase<Secao>
{
    public SecoesController(ClassificacaoProdutoService<Secao> s, ILogAcaoService l) : base(s, l, "Secao") {}
}
