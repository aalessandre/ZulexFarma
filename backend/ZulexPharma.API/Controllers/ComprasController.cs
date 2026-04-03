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
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _service.ListarAsync();
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
}

public class ImportarXmlRequest
{
    public string XmlConteudo { get; set; } = string.Empty;
    public long FilialId { get; set; }
}
