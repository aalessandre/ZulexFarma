using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Compras;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _service;
    private readonly ILogAcaoService _log;

    public ComprasController(ICompraService service, ILogAcaoService log)
    {
        _service = service;
        _log = log;
    }

    [HttpGet]
    [Permissao("compras", "c")]
    public async Task<IActionResult> Listar(
        [FromQuery] long? filialId = null, [FromQuery] string? status = null,
        [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null,
        [FromQuery] string? filtroData = null)
    {
        try
        {
            var data = await _service.ListarAsync(filialId, status, dataInicio, dataFim, filtroData);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar compras." });
        }
    }

    [HttpGet("{id:long}")]
    [Permissao("compras", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var data = await _service.ObterAsync(id);
            return Ok(new { success = true, data });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Compra não encontrada." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.Obter | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao obter compra." });
        }
    }

    [HttpPost("importar-xml")]
    [Permissao("compras", "i")]
    public async Task<IActionResult> ImportarXml([FromBody] ImportarXmlRequest request)
    {
        try
        {
            var data = await _service.ImportarXmlAsync(request.XmlConteudo, request.FilialId);
            return Created(string.Empty, new { success = true, data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.ImportarXml");
            return StatusCode(500, new { success = false, message = "Erro ao importar XML." });
        }
    }

    [HttpPost("vincular")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> VincularProduto([FromBody] VincularProdutoDto dto)
    {
        try
        {
            var data = await _service.VincularProdutoAsync(dto);
            return Ok(new { success = true, data });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.VincularProduto");
            return StatusCode(500, new { success = false, message = "Erro ao vincular produto." });
        }
    }

    [HttpPost("desvincular/{compraProdutoId:long}")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> DesvincularProduto(long compraProdutoId)
    {
        try
        {
            var data = await _service.DesvincularProdutoAsync(compraProdutoId);
            return Ok(new { success = true, data });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.DesvincularProduto");
            return StatusCode(500, new { success = false, message = "Erro ao desvincular produto." });
        }
    }

    [HttpPost("{id:long}/re-vincular")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> ReVincular(long id)
    {
        try
        {
            var data = await _service.ReVincularAsync(id);
            return Ok(new { success = true, data });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Compra não encontrada." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.ReVincular | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao re-vincular." });
        }
    }

    [HttpPost("precificacao")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> GerarPrecificacao([FromBody] PrecificacaoRequest request)
    {
        try { return Ok(new { success = true, data = await _service.GerarPrecificacaoAsync(request) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Precificacao"); return StatusCode(500, new { success = false, message = "Erro ao gerar precificação." }); }
    }

    [HttpPost("aplicar-precificacao")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> AplicarPrecificacao([FromBody] AplicarPrecificacaoRequest request)
    {
        try
        {
            var alterados = await _service.AplicarPrecificacaoAsync(request);
            return Ok(new { success = true, data = new { alterados } });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em AplicarPrecificacao"); return StatusCode(500, new { success = false, message = "Erro ao aplicar precificação." }); }
    }

    [HttpPost("bipar")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> Bipar([FromBody] BiparRequest request)
    {
        try { return Ok(new { success = true, data = await _service.BiparAsync(request) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Bipar"); return StatusCode(500, new { success = false, message = "Erro ao bipar." }); }
    }

    [HttpPost("atualizar-qtde-conf/{compraProdutoId:long}")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> AtualizarQtdeConf(long compraProdutoId, [FromBody] AtualizarQtdeConfRequest request)
    {
        try { return Ok(new { success = true, data = await _service.AtualizarQtdeConfAsync(compraProdutoId, request.QtdeConferida) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Item não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em AtualizarQtdeConf"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost("atualizar-fracao/{compraProdutoId:long}")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> AtualizarFracao(long compraProdutoId, [FromBody] AtualizarFracaoRequest request)
    {
        try
        {
            var cp = await _service.AtualizarFracaoAsync(compraProdutoId, request.Fracao);
            return Ok(new { success = true, data = cp });
        }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Item não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em AtualizarFracao"); return StatusCode(500, new { success = false, message = "Erro ao atualizar fração." }); }
    }

    [HttpPost("salvar-sugestoes")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> SalvarSugestoes([FromBody] SalvarSugestaoRequest request)
    {
        try
        {
            var salvos = await _service.SalvarSugestoesAsync(request);
            return Ok(new { success = true, data = new { salvos } });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em SalvarSugestoes"); return StatusCode(500, new { success = false, message = "Erro ao salvar sugestões." }); }
    }

    [HttpGet("{id:long}/dados-finalizacao")]
    [Permissao("compras", "c")]
    public async Task<IActionResult> DadosFinalizacao(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterDadosFinalizacaoAsync(id) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Compra não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em DadosFinalizacao"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost("finalizar")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> Finalizar([FromBody] FinalizarCompraRequest request)
    {
        try { return Ok(new { success = true, data = await _service.FinalizarAsync(request) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Compra não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Finalizar"); return StatusCode(500, new { success = false, message = "Erro ao finalizar." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("compras", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var resultado = await _service.ExcluirAsync(id);
            return Ok(new { success = true, resultado });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Compra não encontrada." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir compra." });
        }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("compras", "c")]
    public async Task<IActionResult> ObterLog(long id,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var data = await _log.ListarPorRegistroAsync("Compra", id, dataInicio, dataFim);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.ObterLog | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." });
        }
    }

    // ══ Conferência de Lotes ═══════════════════════════════════════════
    [HttpGet("{id:long}/conferencia-lotes")]
    [Permissao("compras", "c")]
    public async Task<IActionResult> ObterConferenciaLotes(long id)
    {
        try
        {
            var data = await _service.ObterConferenciaLotesAsync(id);
            return Ok(new { success = true, data });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.ObterConferenciaLotes | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao obter conferência de lotes." });
        }
    }

    [HttpPost("{id:long}/conferencia-lotes")]
    [Permissao("compras", "a")]
    public async Task<IActionResult> SalvarConferenciaLotes(long id, [FromBody] SalvarConferenciaLotesDto dto)
    {
        try
        {
            var usuarioId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _service.SalvarConferenciaLotesAsync(id, usuarioId, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ComprasController.SalvarConferenciaLotes | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao salvar conferência de lotes." });
        }
    }
}

public class ImportarXmlRequest
{
    public string XmlConteudo { get; set; } = string.Empty;
    public long FilialId { get; set; }
}
