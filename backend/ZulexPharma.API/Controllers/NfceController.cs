using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NfceController : ControllerBase
{
    private readonly NfceService _service;
    private readonly AppDbContext _db;

    public NfceController(NfceService service, AppDbContext db) { _service = service; _db = db; }

    /// <summary>Emite NFC-e para uma venda finalizada.</summary>
    [HttpPost("emitir/{vendaId:long}")]
    public async Task<IActionResult> Emitir(long vendaId)
    {
        try
        {
            var resultado = await _service.EmitirAsync(vendaId);
            if (resultado.Autorizada)
                return Ok(new { success = true, data = resultado, message = $"NFC-e autorizada. Protocolo: {resultado.Protocolo}" });
            return Ok(new { success = false, data = resultado, message = $"NFC-e rejeitada: {resultado.MotivoStatus} (código {resultado.CodigoStatus})" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao emitir NFC-e para venda {VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = $"Erro ao emitir NFC-e: {ex.Message}" });
        }
    }

    /// <summary>Gera o DANFE NFC-e em HTML para impressão.</summary>
    [HttpGet("danfe/{nfceId:long}")]
    [AllowAnonymous] // Permite abrir em nova aba sem token
    public async Task<IActionResult> Danfe(long nfceId)
    {
        try
        {
            var nfce = await _db.Nfces
                .Include(n => n.Filial)
                .Include(n => n.Venda).ThenInclude(v => v.Itens)
                .Include(n => n.Venda).ThenInclude(v => v.Pagamentos).ThenInclude(p => p.TipoPagamento)
                .Include(n => n.Venda).ThenInclude(v => v.Cliente).ThenInclude(c => c!.Pessoa)
                .FirstOrDefaultAsync(n => n.Id == nfceId);

            if (nfce == null) return NotFound("NFC-e não encontrada.");

            var html = GerarDanfeHtml(nfce);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar DANFE NFC-e {NfceId}", nfceId);
            return StatusCode(500, "Erro ao gerar DANFE.");
        }
    }

    private static string GerarDanfeHtml(Domain.Entities.Nfce nfce)
    {
        var v = nfce.Venda;
        var f = nfce.Filial!;

        // Extrair vTotTrib do XML
        decimal vTotTrib = 0;
        if (!string.IsNullOrEmpty(nfce.XmlEnvio))
        {
            var match = System.Text.RegularExpressions.Regex.Match(nfce.XmlEnvio, @"<vTotTrib>([\d.]+)</vTotTrib>");
            // Pegar o último match (o do ICMSTot, não dos itens)
            var matches = System.Text.RegularExpressions.Regex.Matches(nfce.XmlEnvio, @"<vTotTrib>([\d.]+)</vTotTrib>");
            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                decimal.TryParse(lastMatch.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out vTotTrib);
            }
        }
        var amb = nfce.Ambiente == 2 ? "<div style='background:#e74c3c;color:#fff;text-align:center;padding:4px;font-weight:bold;font-size:11px'>SEM VALOR FISCAL - HOMOLOGAÇÃO</div>" : "";

        var itensHtml = new System.Text.StringBuilder();
        var temDesconto = v.Itens.Any(i => i.ValorDesconto > 0);
        foreach (var item in v.Itens)
        {
            var descNome = nfce.Ambiente == 2 ? "HOMOLOGACAO" : item.ProdutoNome;
            var descontoCol = temDesconto ? $"<td style='font-size:10px;padding:2px 4px;text-align:right;color:#c0392b'>{(item.ValorDesconto > 0 ? $"-{item.ValorDesconto:N2}" : "")}</td>" : "";
            itensHtml.Append($@"
                <tr>
                    <td style='font-size:10px;padding:2px 4px'>{item.ProdutoCodigo}</td>
                    <td style='font-size:10px;padding:2px 4px'>{descNome}</td>
                    <td style='font-size:10px;padding:2px 4px;text-align:right'>{item.Quantidade}</td>
                    <td style='font-size:10px;padding:2px 4px;text-align:right'>{item.PrecoVenda:N2}</td>
                    {descontoCol}
                    <td style='font-size:10px;padding:2px 4px;text-align:right'>{item.Total:N2}</td>
                </tr>");
        }

        var pagHtml = new System.Text.StringBuilder();
        foreach (var pag in v.Pagamentos.Where(p => p.Valor > 0))
        {
            pagHtml.Append($@"
                <tr>
                    <td style='font-size:10px;padding:2px 4px'>{pag.TipoPagamento?.Nome ?? "Outros"}</td>
                    <td style='font-size:10px;padding:2px 4px;text-align:right'>{pag.Valor:N2}</td>
                </tr>");
        }
        var troco = v.Pagamentos.Sum(p => p.Troco);
        if (troco > 0) pagHtml.Append($"<tr><td style='font-size:10px;padding:2px 4px'><b>TROCO</b></td><td style='font-size:10px;padding:2px 4px;text-align:right'><b>{troco:N2}</b></td></tr>");

        var clienteNome = v.Cliente?.Pessoa?.Nome ?? "CONSUMIDOR NÃO IDENTIFICADO";
        var clienteCpf = v.Cliente?.Pessoa?.CpfCnpj ?? "";
        var thDesconto = temDesconto ? "<th style='text-align:right'>DESC</th>" : "";

        var qrCodeData = $"http://www.fazenda.pr.gov.br/nfce/qrcode?p={nfce.ChaveAcesso}|3|{nfce.Ambiente}";

        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>DANFE NFC-e #{nfce.Numero}</title>
<style>
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  body {{ font-family: 'Courier New', monospace; width: 302px; margin: 0 auto; padding: 8px; background: #fff; color: #000; }}
  .center {{ text-align: center; }}
  .bold {{ font-weight: bold; }}
  .line {{ border-top: 1px dashed #000; margin: 6px 0; }}
  .empresa {{ font-size: 12px; font-weight: bold; text-align: center; margin-bottom: 2px; }}
  .endereco {{ font-size: 9px; text-align: center; color: #333; }}
  .titulo {{ font-size: 11px; font-weight: bold; text-align: center; margin: 4px 0; }}
  table {{ width: 100%; border-collapse: collapse; }}
  th {{ font-size: 9px; text-align: left; padding: 2px 4px; border-bottom: 1px solid #000; }}
  .totais td {{ font-size: 11px; padding: 2px 4px; }}
  .qrcode {{ text-align: center; margin: 8px 0; }}
  .chave {{ font-size: 8px; text-align: center; word-break: break-all; }}
  .protocolo {{ font-size: 8px; text-align: center; }}
  .info {{ font-size: 9px; }}
  @media print {{
    body {{ width: 80mm; margin: 0; padding: 4px; }}
    .no-print {{ display: none; }}
  }}
</style>
</head>
<body>
{amb}
<div class='empresa'>{f.NomeFantasia}</div>
<div class='endereco'>
  {f.RazaoSocial}<br>
  CNPJ: {f.Cnpj} IE: {f.InscricaoEstadual}<br>
  {f.Rua}, {f.Numero} - {f.Bairro}<br>
  {f.Cidade}/{f.Uf} CEP: {f.Cep}
</div>

<div class='line'></div>
<div class='titulo'>DANFE NFC-e - DOCUMENTO AUXILIAR DA NOTA FISCAL DE CONSUMIDOR ELETRÔNICA</div>
<div class='line'></div>

<table>
  <thead>
    <tr><th>CÓD</th><th>DESCRIÇÃO</th><th style='text-align:right'>QTD</th><th style='text-align:right'>VL.UN</th>{thDesconto}<th style='text-align:right'>TOTAL</th></tr>
  </thead>
  <tbody>
    {itensHtml}
  </tbody>
</table>

<div class='line'></div>
<table class='totais'>
  <tr><td>Qtde. itens</td><td style='text-align:right'><b>{v.TotalItens}</b></td></tr>
  <tr><td>Subtotal</td><td style='text-align:right'>{v.TotalBruto:N2}</td></tr>
  <tr><td>Desconto</td><td style='text-align:right'>-{v.TotalDesconto:N2}</td></tr>
  <tr><td style='font-size:14px'><b>TOTAL</b></td><td style='text-align:right;font-size:14px'><b>R$ {v.TotalLiquido:N2}</b></td></tr>
</table>
{(vTotTrib > 0 ? $"<div style='font-size:9px;text-align:center;padding:4px 0;color:#555'>Val. aprox. tributos R$ {vTotTrib:N2} ({(vTotTrib / (v.TotalLiquido > 0 ? v.TotalLiquido : 1) * 100):N1}%) Fonte: IBPT</div>" : "")}

<div class='line'></div>
<div class='titulo'>FORMA DE PAGAMENTO</div>
<table>
  {pagHtml}
</table>

<div class='line'></div>
<div class='info'>
  <b>Consumidor:</b> {clienteNome}<br>
  {(string.IsNullOrEmpty(clienteCpf) ? "" : $"CPF/CNPJ: {clienteCpf}<br>")}
</div>

<div class='line'></div>
<div class='titulo'>CONSULTE PELA CHAVE DE ACESSO</div>
<div class='chave'>{nfce.ChaveAcesso}</div>

<div class='line'></div>
<div class='qrcode'>
  <img src='https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={System.Uri.EscapeDataString(qrCodeData)}' width='200' height='200' alt='QR Code' />
</div>

<div class='protocolo'>
  NFC-e nº {nfce.Numero} Série {nfce.Serie}<br>
  Protocolo: {nfce.Protocolo}<br>
  Data: {nfce.DataAutorizacao:dd/MM/yyyy HH:mm:ss}
</div>

<div class='line'></div>
<div style='text-align:center;font-size:8px;margin-top:4px'>ZulexPharma ERP</div>


</body>
</html>";
    }
}
