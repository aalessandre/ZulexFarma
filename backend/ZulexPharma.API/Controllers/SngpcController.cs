using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

// ══ Inventário SNGPC ═══════════════════════════════════════════════
[Authorize]
[ApiController]
[Route("api/sngpc/inventarios")]
public class InventariosSngpcController : ControllerBase
{
    private readonly IInventarioSngpcService _service;
    public InventariosSngpcController(IInventarioSngpcService s) { _service = s; }
    private long? UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro Inventarios.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(id) }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Inventarios.Obter"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost]
    [Permissao("sngpc", "i")]
    public async Task<IActionResult> Criar([FromBody] InventarioSngpcFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto, UsuarioId) }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Inventarios.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("sngpc", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] InventarioSngpcFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Inventarios.Atualizar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost("{id:long}/finalizar")]
    [Permissao("sngpc", "a")]
    public async Task<IActionResult> Finalizar(long id)
    {
        try
        {
            var n = await _service.FinalizarAsync(id, UsuarioId);
            return Ok(new { success = true, lotesCriados = n });
        }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Inventarios.Finalizar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("sngpc", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { await _service.ExcluirAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Inventarios.Excluir"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }
}

// ══ Perdas ═════════════════════════════════════════════════════════
[Authorize]
[ApiController]
[Route("api/sngpc/perdas")]
public class PerdasController : ControllerBase
{
    private readonly IPerdaService _service;
    public PerdasController(IPerdaService s) { _service = s; }
    private long? UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro Perdas.Listar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost]
    [Permissao("sngpc", "i")]
    public async Task<IActionResult> Criar([FromBody] PerdaFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto, UsuarioId) }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Perdas.Criar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("sngpc", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { await _service.ExcluirAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
    }
}

// ══ Estoque SNGPC ══════════════════════════════════════════════════
[Authorize]
[ApiController]
[Route("api/sngpc/estoque")]
public class EstoqueSngpcController : ControllerBase
{
    private readonly IEstoqueSngpcService _service;
    public EstoqueSngpcController(IEstoqueSngpcService s) { _service = s; }

    [HttpGet]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null, [FromQuery] bool incluirVencidos = true)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId, incluirVencidos) }); }
        catch (Exception ex) { Log.Error(ex, "Erro EstoqueSngpc.Listar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }
}

// ══ Compras e Transferências SNGPC ════════════════════════════════
[Authorize]
[ApiController]
[Route("api/sngpc/compras-transferencias")]
public class ComprasSngpcController : ControllerBase
{
    private readonly ICompraSngpcService _service;
    public ComprasSngpcController(ICompraSngpcService s) { _service = s; }
    private long? UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro ComprasSngpc.Listar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpGet("{compraId:long}")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> Obter(long compraId)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(compraId) }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
    }

    [HttpPost("{compraId:long}/lancar-retroativo")]
    [Permissao("sngpc", "i")]
    public async Task<IActionResult> LancarRetroativo(long compraId)
    {
        try
        {
            var n = await _service.LancarRetroativoAsync(compraId, UsuarioId);
            return Ok(new { success = true, lotesCriados = n });
        }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
    }
}

// ══ Mapas SNGPC ════════════════════════════════════════════════════
[Authorize]
[ApiController]
[Route("api/sngpc/mapas")]
public class SngpcMapasController : ControllerBase
{
    private readonly ISngpcMapaService _service;
    public SngpcMapasController(ISngpcMapaService s) { _service = s; }
    private long? UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null, [FromQuery] int? ano = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId, ano) }); }
        catch (Exception ex) { Log.Error(ex, "Erro Mapas.Listar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost("gerar")]
    [Permissao("sngpc", "i")]
    public async Task<IActionResult> Gerar([FromBody] GerarMapaSngpcRequest req)
    {
        try { return Ok(new { success = true, data = await _service.GerarAsync(req, UsuarioId) }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Mapas.Gerar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpGet("{id:long}/xml")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> BaixarXml(long id)
    {
        try
        {
            var xml = await _service.ObterXmlAsync(id);
            return File(System.Text.Encoding.UTF8.GetBytes(xml), "application/xml", $"sngpc-mapa-{id}.xml");
        }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
    }

    [HttpPost("{id:long}/enviar")]
    [Permissao("sngpc", "a")]
    public async Task<IActionResult> Enviar(long id, [FromBody] EnviarMapaRequest req)
    {
        try { await _service.MarcarEnviadoAsync(id, req.Protocolo, UsuarioId); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("sngpc", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { await _service.ExcluirAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
    }
}

public class EnviarMapaRequest
{
    public string? Protocolo { get; set; }
}

// ══ Vendas SNGPC (receitas vinculadas + pendentes) ═════════════════
[Authorize]
[ApiController]
[Route("api/sngpc/vendas")]
public class SngpcVendasController : ControllerBase
{
    private readonly IVendaReceitaService _service;
    public SngpcVendasController(IVendaReceitaService s) { _service = s; }
    private long? UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>Itens controlados de uma venda com lotes disponíveis — usado pela modal SNGPC.</summary>
    [HttpGet("{vendaId:long}/itens-controlados")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> ListarItensControlados(long vendaId)
    {
        try { return Ok(new { success = true, data = await _service.ListarItensControladosAsync(vendaId) }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.ListarItensControlados"); return StatusCode(500, new { success = false, message = "Erro ao listar itens." }); }
    }

    /// <summary>
    /// Preview dos itens controlados antes da venda existir no banco — usado na nova tela de receitas
    /// (Avançar) que é mostrada antes da finalização.
    /// </summary>
    [HttpPost("itens-controlados-preview")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> ItensControladosPreview([FromBody] ItensControladosPreviewRequest request)
    {
        try { return Ok(new { success = true, data = await _service.ListarItensControladosPreviewAsync(request) }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.ItensControladosPreview"); return StatusCode(500, new { success = false, message = "Erro ao obter preview." }); }
    }

    /// <summary>Registra (ou lança retroativamente) as receitas de uma venda.</summary>
    [HttpPost("{vendaId:long}/receitas")]
    [Permissao("sngpc", "i")]
    public async Task<IActionResult> RegistrarReceitas(long vendaId, [FromBody] List<VendaReceitaFormDto> receitas)
    {
        try { await _service.RegistrarReceitasAsync(vendaId, receitas, UsuarioId); return Ok(new { success = true }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.RegistrarReceitas"); return StatusCode(500, new { success = false, message = "Erro ao registrar receitas." }); }
    }

    /// <summary>Lista unificada de vendas SNGPC (pendentes/lançadas/todas).</summary>
    [HttpGet("receitas")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> ListarVendasSngpc(
        [FromQuery] string? filtro = null,
        [FromQuery] long? filialId = null,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarVendasSngpcAsync(filtro, filialId, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.ListarVendasSngpc"); return StatusCode(500, new { success = false, message = "Erro ao listar vendas." }); }
    }

    /// <summary>Lista receitas já gravadas de uma venda.</summary>
    [HttpGet("{vendaId:long}/receitas")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> ListarReceitas(long vendaId)
    {
        try { return Ok(new { success = true, data = await _service.ListarReceitasAsync(vendaId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.ListarReceitas"); return StatusCode(500, new { success = false, message = "Erro ao listar receitas." }); }
    }

    /// <summary>Pesquisa produtos SNGPC (para modal de receita manual).</summary>
    [HttpGet("produtos-sngpc")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> PesquisarProdutosSngpc([FromQuery] string termo, [FromQuery] long filialId)
    {
        try { return Ok(new { success = true, data = await _service.PesquisarProdutosSngpcAsync(termo, filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.PesquisarProdutosSngpc"); return StatusCode(500, new { success = false, message = "Erro ao pesquisar." }); }
    }

    /// <summary>Detalhes (produto + lote) de uma linha da tela Receitas — usado na expansão inline.</summary>
    [HttpGet("detalhes")]
    [Permissao("sngpc", "c")]
    public async Task<IActionResult> ObterDetalhes([FromQuery] long? vendaId = null, [FromQuery] long? receitaId = null)
    {
        try { return Ok(new { success = true, data = await _service.ObterDetalhesAsync(vendaId, receitaId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.ObterDetalhes"); return StatusCode(500, new { success = false, message = "Erro ao obter detalhes." }); }
    }

    /// <summary>Registra uma receita manual (sem venda).</summary>
    [HttpPost("receita-manual")]
    [Permissao("sngpc", "i")]
    public async Task<IActionResult> RegistrarReceitaManual([FromBody] RegistrarReceitaManualRequest request)
    {
        try
        {
            var id = await _service.RegistrarReceitaManualAsync(request.Receita, request.FilialId, UsuarioId);
            return Created("", new { success = true, data = new { id } });
        }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro SngpcVendas.RegistrarReceitaManual"); return StatusCode(500, new { success = false, message = "Erro ao registrar receita manual." }); }
    }
}

public class RegistrarReceitaManualRequest
{
    public long FilialId { get; set; }
    public VendaReceitaFormDto Receita { get; set; } = new();
}
