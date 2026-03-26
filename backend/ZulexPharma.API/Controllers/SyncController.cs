using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly SyncService _sync;

    public SyncController(SyncService sync) => _sync = sync;

    /// <summary>
    /// Lista as tabelas disponíveis para sincronização.
    /// </summary>
    [HttpGet("tabelas")]
    public IActionResult ListarTabelas()
    {
        return Ok(new { success = true, data = SyncService.TabelasSyncaveis });
    }

    /// <summary>
    /// Obtém alterações de uma tabela desde uma versão específica.
    /// Usado pela filial para RECEBER dados do servidor central.
    /// </summary>
    [HttpGet("receber")]
    public async Task<IActionResult> Receber(
        [FromQuery] string tabela,
        [FromQuery] long versaoDesde = 0,
        [FromQuery] long? filialId = null,
        [FromQuery] int limite = 500)
    {
        try
        {
            var pacote = await _sync.ObterAlteracoes(tabela, versaoDesde, filialId, limite);
            return Ok(new { success = true, data = pacote });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Receber | Tabela: {Tabela}", tabela);
            return StatusCode(500, new { success = false, message = "Erro ao obter alterações." });
        }
    }

    /// <summary>
    /// Recebe alterações de uma filial e aplica no banco central.
    /// Usado pela filial para ENVIAR dados para o servidor central.
    /// </summary>
    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] EnviarSyncDto dto)
    {
        try
        {
            var resultado = await _sync.AplicarAlteracoes(dto.Tabela, dto.Registros);

            // Update sync control
            if (dto.FilialId > 0 && dto.VersaoAte > 0)
                await _sync.AtualizarControle(dto.FilialId, dto.Tabela, versaoRecebida: dto.VersaoAte);

            return Ok(new { success = true, data = resultado });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Enviar | Tabela: {Tabela}", dto.Tabela);
            return StatusCode(500, new { success = false, message = "Erro ao aplicar alterações." });
        }
    }

    /// <summary>
    /// Obtém o status de sincronização de todas as tabelas para uma filial.
    /// </summary>
    [HttpGet("status/{filialId:long}")]
    public async Task<IActionResult> Status(long filialId)
    {
        try
        {
            var data = await _sync.ObterStatus(filialId);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Status | FilialId: {FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao obter status." });
        }
    }

    /// <summary>
    /// Executa uma sincronização completa para uma filial (todas as tabelas).
    /// </summary>
    [HttpPost("executar/{filialId:long}")]
    public async Task<IActionResult> Executar(long filialId)
    {
        try
        {
            var resultados = new List<object>();
            foreach (var tabela in SyncService.TabelasSyncaveis)
            {
                try
                {
                    var pacote = await _sync.ObterAlteracoes(tabela, 0, filialId);
                    await _sync.AtualizarControle(filialId, tabela, versaoEnviada: pacote.VersaoAte);
                    resultados.Add(new { tabela, registros = pacote.TotalRegistros, status = "OK" });
                }
                catch (Exception ex)
                {
                    await _sync.AtualizarControle(filialId, tabela, status: "ERRO", erro: ex.Message);
                    resultados.Add(new { tabela, registros = 0, status = "ERRO", erro = ex.Message });
                }
            }
            return Ok(new { success = true, data = resultados });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Executar | FilialId: {FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao executar sincronização." });
        }
    }
}

public record EnviarSyncDto(string Tabela, long FilialId, long VersaoAte, List<string> Registros);
