using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.SelfCheckout;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/self-checkout")]
public class SelfCheckoutController : ControllerBase
{
    private readonly IErpConnectorFactory _factory;
    private readonly ISelfCheckoutVendaService _venda;

    public SelfCheckoutController(IErpConnectorFactory factory, ISelfCheckoutVendaService venda)
    {
        _factory = factory;
        _venda = venda;
    }

    /// <summary>
    /// Testa a conexão com o banco do ERP origem usando parâmetros ad-hoc
    /// (sem precisar persistir a configuração antes).
    /// </summary>
    [HttpPost("testar-conexao")]
    [Permissao("self-checkout", "a")]
    public async Task<IActionResult> TestarConexao([FromBody] ConfiguracaoConexaoErpDto config, CancellationToken ct)
    {
        try
        {
            await using var connector = _factory.CriarTransiente(config);
            var resultado = await connector.TestarConexaoAsync(ct);
            return Ok(new { success = true, data = resultado });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.TestarConexao");
            return StatusCode(500, new { success = false, message = "Erro ao testar conexão." });
        }
    }

    /// <summary>Busca produto por EAN na filial atualmente configurada.</summary>
    [HttpGet("filial/{filialId:long}/produto/ean/{ean}")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> BuscarPorEan(long filialId, string ean, CancellationToken ct)
    {
        try
        {
            await using var connector = await _factory.CriarParaFilialAsync(filialId, ct);
            if (connector == null)
                return BadRequest(new { success = false, message = "Self-Checkout não configurado para esta filial." });

            var produto = await connector.BuscarProdutoPorEanAsync(ean, ct);
            if (produto == null)
                return NotFound(new { success = false, message = "Produto não localizado." });

            return Ok(new { success = true, data = produto });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.BuscarPorEan ean={Ean}", ean);
            return StatusCode(502, new { success = false, message = "Falha ao consultar o ERP origem." });
        }
    }

    /// <summary>
    /// Lista as naturezas de operação de saída cadastradas no ERP origem.
    /// Usado pelo accordion Self-Checkout para popular o dropdown de natureza.
    /// </summary>
    [HttpGet("filial/{filialId:long}/naturezas-operacao")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> ListarNaturezas(long filialId, CancellationToken ct)
    {
        try
        {
            await using var connector = await _factory.CriarParaFilialAsync(filialId, ct);
            if (connector == null)
                return BadRequest(new { success = false, message = "Self-Checkout não configurado para esta filial." });

            var lista = await connector.ListarNaturezasOperacaoSaidaAsync(ct);
            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.ListarNaturezas filialId={FilialId}", filialId);
            return StatusCode(502, new { success = false, message = "Falha ao consultar o ERP origem." });
        }
    }

    /// <summary>Busca produtos por nome na filial atualmente configurada.</summary>
    [HttpGet("filial/{filialId:long}/produto/busca")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> BuscarPorNome(long filialId, [FromQuery] string q, [FromQuery] int top = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new { success = true, data = Array.Empty<object>() });

        try
        {
            await using var connector = await _factory.CriarParaFilialAsync(filialId, ct);
            if (connector == null)
                return BadRequest(new { success = false, message = "Self-Checkout não configurado para esta filial." });

            var produtos = await connector.BuscarProdutosPorNomeAsync(q, Math.Clamp(top, 1, 50), ct);
            return Ok(new { success = true, data = produtos });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.BuscarPorNome termo={Termo}", q);
            return StatusCode(502, new { success = false, message = "Falha ao consultar o ERP origem." });
        }
    }

    // ── Ciclo de venda kiosk ─────────────────────────────────────────

    /// <summary>Cria a venda kiosk com itens e snapshot fiscal pré-resolvido.</summary>
    [HttpPost("filial/{filialId:long}/venda")]
    [Permissao("self-checkout", "i")]
    public async Task<IActionResult> IniciarVenda(long filialId, [FromBody] IniciarVendaKioskDto input, CancellationToken ct)
    {
        try
        {
            var resultado = await _venda.IniciarAsync(filialId, input, ct);
            return Ok(new { success = true, data = resultado });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.IniciarVenda filialId={FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao iniciar venda." });
        }
    }

    /// <summary>Cliente kiosk informa a forma escolhida (PIX/Cartão).</summary>
    [HttpPost("venda/{vendaId:long}/pagamento")]
    [Permissao("self-checkout", "i")]
    public async Task<IActionResult> RegistrarPagamento(long vendaId, [FromBody] RegistrarPagamentoKioskDto input, CancellationToken ct)
    {
        try
        {
            await _venda.RegistrarPagamentoAsync(vendaId, input, ct);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.RegistrarPagamento vendaId={VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = "Erro ao registrar pagamento." });
        }
    }

    /// <summary>Atendente confirma o recebimento e dispara emissão da NFC-e.</summary>
    [HttpPost("venda/{vendaId:long}/confirmar")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> ConfirmarVenda(long vendaId, CancellationToken ct)
    {
        try
        {
            var resultado = await _venda.ConfirmarPagamentoAsync(vendaId, ct);
            return Ok(new { success = true, data = resultado });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.ConfirmarVenda vendaId={VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = "Erro ao confirmar venda." });
        }
    }

    /// <summary>Cancela venda kiosk pendente (cliente desiste ou atendente recusa).</summary>
    [HttpPost("venda/{vendaId:long}/cancelar")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> CancelarVenda(long vendaId, [FromBody] CancelarVendaDto? input, CancellationToken ct)
    {
        try
        {
            await _venda.CancelarAsync(vendaId, input?.Motivo, ct);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.CancelarVenda vendaId={VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = "Erro ao cancelar venda." });
        }
    }

    public class CancelarVendaDto { public string? Motivo { get; set; } }

    /// <summary>Status consumível pelo terminal kiosk (polling 2s).</summary>
    [HttpGet("venda/{vendaId:long}/status-kiosk")]
    [Permissao("self-checkout", "i")]
    public async Task<IActionResult> StatusKiosk(long vendaId, CancellationToken ct)
    {
        try
        {
            var st = await _venda.ObterStatusKioskAsync(vendaId, ct);
            if (st == null) return NotFound(new { success = false, message = "Venda não encontrada." });
            return Ok(new { success = true, data = st });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.StatusKiosk vendaId={VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = "Erro ao obter status." });
        }
    }

    /// <summary>Lista terminais ativos da filial (consumido pela tela de seleção do kiosk).</summary>
    [HttpGet("filial/{filialId:long}/terminais")]
    [Permissao("self-checkout", "i")]
    public async Task<IActionResult> ListarTerminaisAtivos(long filialId, [FromServices] ISelfCheckoutConfiguracaoService cfg, CancellationToken ct)
    {
        try
        {
            var todos = await cfg.ListarTerminaisAsync(filialId, ct);
            return Ok(new { success = true, data = todos.Where(t => t.Ativo).ToList() });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.ListarTerminaisAtivos filialId={FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao listar terminais." });
        }
    }

    /// <summary>Lista vendas kiosk com pagamento aguardando confirmação manual.</summary>
    [HttpGet("filial/{filialId:long}/pagamentos-pendentes")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> ListarPagamentosPendentes(long filialId, CancellationToken ct)
    {
        try
        {
            var lista = await _venda.ListarPagamentosPendentesAsync(filialId, ct);
            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SelfCheckoutController.ListarPagamentosPendentes filialId={FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao listar pendentes." });
        }
    }
}
