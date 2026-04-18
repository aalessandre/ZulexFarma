using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

/// <summary>
/// Emissão fiscal unificada (NFe modelo 55 + NFC-e modelo 65).
/// Substitui NfeController e NfceController. Opera sobre Venda + VendaFiscal +
/// VendaItem + VendaItemFiscal via <see cref="IVendaFiscalService"/>.
/// </summary>
[Authorize]
[ApiController]
[Route("api/venda-fiscal")]
public class VendaFiscalController : ControllerBase
{
    private readonly IVendaFiscalService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public VendaFiscalController(IVendaFiscalService service, ILogAcaoService log, AppDbContext db)
    {
        _service = service;
        _log = log;
        _db = db;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Listagem / leitura
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] long? filialId)
    {
        try
        {
            var lista = await _service.ListarAsync(filialId);
            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar documentos fiscais");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            return Ok(new { success = true, data = dto });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter documento fiscal {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NFe modelo 55 — rascunho → emitir
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("rascunho-nfe")]
    public async Task<IActionResult> CriarRascunhoNfe([FromBody] VendaFiscalFormDto dto)
    {
        try
        {
            var result = await _service.CriarRascunhoNfeAsync(dto);
            await _log.RegistrarAsync("venda-fiscal", "criou-rascunho-nfe", "VendaFiscal", result.Id);
            return Ok(new { success = true, data = result, message = "Rascunho de NF-e criado com sucesso." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar rascunho NF-e");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPut("rascunho-nfe/{vendaId:long}")]
    public async Task<IActionResult> AtualizarRascunhoNfe(long vendaId, [FromBody] VendaFiscalFormDto dto)
    {
        try
        {
            await _service.AtualizarRascunhoNfeAsync(vendaId, dto);
            await _log.RegistrarAsync("venda-fiscal", "atualizou-rascunho-nfe", "Venda", vendaId);
            return Ok(new { success = true, message = "Rascunho de NF-e atualizado." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar rascunho NF-e {VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("rascunho-nfe/{vendaId:long}")]
    public async Task<IActionResult> ExcluirRascunhoNfe(long vendaId)
    {
        try
        {
            var msg = await _service.ExcluirRascunhoNfeAsync(vendaId);
            await _log.RegistrarAsync("venda-fiscal", "excluiu-rascunho-nfe", "Venda", vendaId);
            return Ok(new { success = true, message = msg });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao excluir rascunho NF-e {VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("emitir-nfe/{vendaId:long}")]
    public async Task<IActionResult> EmitirNfe(long vendaId)
    {
        try
        {
            var resultado = await _service.EmitirNfeAsync(vendaId);
            await _log.RegistrarAsync("venda-fiscal", "emitiu-nfe", "VendaFiscal", resultado.VendaFiscalId);
            if (resultado.Autorizada)
                return Ok(new { success = true, data = resultado, message = $"NF-e autorizada. Protocolo: {resultado.Protocolo}" });
            return Ok(new { success = false, data = resultado, message = $"NF-e rejeitada: {resultado.MotivoStatus} (código {resultado.CodigoStatus})" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao emitir NF-e da venda {VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = $"Erro ao emitir NF-e: {ex.Message}" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NFC-e modelo 65 — emite direto a partir da venda finalizada
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("emitir-nfce/{vendaId:long}")]
    public async Task<IActionResult> EmitirNfce(long vendaId)
    {
        try
        {
            var resultado = await _service.EmitirNfceAsync(vendaId);
            await _log.RegistrarAsync("venda-fiscal", "emitiu-nfce", "VendaFiscal", resultado.VendaFiscalId);
            if (resultado.Autorizada)
                return Ok(new { success = true, data = resultado, message = $"NFC-e autorizada. Protocolo: {resultado.Protocolo}" });
            return Ok(new { success = false, data = resultado, message = $"NFC-e rejeitada: {resultado.MotivoStatus} (código {resultado.CodigoStatus})" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao emitir NFC-e da venda {VendaId}", vendaId);
            return StatusCode(500, new { success = false, message = $"Erro ao emitir NFC-e: {ex.Message}" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Eventos (cancelamento, CC-e, inutilização)
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("{vendaId:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long vendaId, [FromBody] CancelamentoRequest request)
    {
        try
        {
            var resultado = await _service.CancelarAsync(vendaId, request.Justificativa);
            await _log.RegistrarAsync("venda-fiscal", "cancelou", "VendaFiscal", vendaId);
            if (resultado.Sucesso)
                return Ok(new { success = true, data = resultado, message = "Documento fiscal cancelado com sucesso." });
            return Ok(new { success = false, data = resultado, message = $"Cancelamento rejeitado: {resultado.MotivoStatus}" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao cancelar documento fiscal {Id}", vendaId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{vendaId:long}/carta-correcao")]
    public async Task<IActionResult> CartaCorrecao(long vendaId, [FromBody] CartaCorrecaoRequest request)
    {
        try
        {
            var resultado = await _service.CartaCorrecaoAsync(vendaId, request.TextoCorrecao);
            await _log.RegistrarAsync("venda-fiscal", "carta-correcao", "VendaFiscal", vendaId);
            if (resultado.Sucesso)
                return Ok(new { success = true, data = resultado, message = "Carta de correção registrada com sucesso." });
            return Ok(new { success = false, data = resultado, message = $"CC-e rejeitada: {resultado.MotivoStatus}" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao enviar CC-e para documento fiscal {Id}", vendaId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("inutilizar")]
    public async Task<IActionResult> Inutilizar([FromBody] InutilizacaoRequest request)
    {
        try
        {
            var resultado = await _service.InutilizarAsync(
                request.FilialId, request.Serie, request.NumInicial, request.NumFinal, request.Justificativa);
            await _log.RegistrarAsync("venda-fiscal", "inutilizou", "VendaFiscal", 0);
            if (resultado.Sucesso)
                return Ok(new { success = true, data = resultado, message = "Numeração inutilizada com sucesso." });
            return Ok(new { success = false, data = resultado, message = $"Inutilização rejeitada: {resultado.MotivoStatus}" });
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao inutilizar numeração fiscal");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Log de auditoria
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("{vendaId:long}/log")]
    public async Task<IActionResult> LogAuditoria(long vendaId)
    {
        try
        {
            var logs = await _log.ListarPorRegistroAsync("VendaFiscal", vendaId);
            return Ok(new { success = true, data = logs });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar logs do documento fiscal {Id}", vendaId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DANFE (HTML) — A4 para NFe 55, térmico 80mm para NFC-e 65
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gera o DANFE em HTML. Modelo 55 → layout A4 retrato; Modelo 65 → layout térmico 80mm.
    /// [AllowAnonymous] para permitir abrir em nova aba sem token.
    /// </summary>
    [HttpGet("{vendaId:long}/danfe")]
    [AllowAnonymous]
    public async Task<IActionResult> Danfe(long vendaId)
    {
        try
        {
            var vendaFiscal = await _db.VendaFiscais
                .Include(vf => vf.Venda).ThenInclude(v => v.Filial)
                .Include(vf => vf.Venda).ThenInclude(v => v.Itens).ThenInclude(i => i.Fiscal)
                .Include(vf => vf.Venda).ThenInclude(v => v.Itens).ThenInclude(i => i.Produto)
                .Include(vf => vf.Venda).ThenInclude(v => v.Pagamentos).ThenInclude(p => p.TipoPagamento)
                .Include(vf => vf.Venda).ThenInclude(v => v.Cliente).ThenInclude(c => c!.Pessoa)
                .Include(vf => vf.Venda).ThenInclude(v => v.DestinatarioPessoa).ThenInclude(p => p!.Enderecos)
                .Include(vf => vf.TransportadoraPessoa).ThenInclude(p => p!.Enderecos)
                .FirstOrDefaultAsync(vf => vf.VendaId == vendaId);

            if (vendaFiscal == null) return NotFound("Documento fiscal não encontrado.");

            string html;
            if (vendaFiscal.Modelo == ModeloDocumento.Nfe)
            {
                var parcelas = await _db.ContasReceber
                    .Where(cr => cr.VendaId == vendaFiscal.VendaId)
                    .OrderBy(cr => cr.DataVencimento)
                    .ToListAsync();
                html = GerarDanfeA4Html(vendaFiscal, parcelas);
            }
            else if (vendaFiscal.Modelo == ModeloDocumento.Nfce)
            {
                html = GerarDanfeTermicoHtml(vendaFiscal);
            }
            else
            {
                return BadRequest($"Modelo de documento não suportado para DANFE: {vendaFiscal.Modelo}.");
            }

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar DANFE do documento fiscal {Id}", vendaId);
            return StatusCode(500, "Erro ao gerar DANFE.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DANFE A4 HTML (NFe modelo 55)
    // ═══════════════════════════════════════════════════════════════════

    private static string GerarDanfeA4Html(VendaFiscal vf, List<ContaReceber> parcelas)
    {
        var f = vf.Venda.Filial!;
        var dest = vf.Venda.DestinatarioPessoa;
        var destEnd = dest?.Enderecos?.FirstOrDefault(e => e.Principal) ?? dest?.Enderecos?.FirstOrDefault();
        var transp = vf.TransportadoraPessoa;

        // Format chave in groups of 4
        var chaveFormatada = string.Empty;
        if (!string.IsNullOrEmpty(vf.ChaveAcesso))
        {
            for (int i = 0; i < vf.ChaveAcesso.Length; i += 4)
            {
                if (i > 0) chaveFormatada += " ";
                chaveFormatada += vf.ChaveAcesso.Substring(i, Math.Min(4, vf.ChaveAcesso.Length - i));
            }
        }

        var amb = vf.Ambiente == 2
            ? "<div class='homolog'>SEM VALOR FISCAL - AMBIENTE DE HOMOLOGACAO</div>"
            : "";

        var statusLabel = vf.Venda.StatusFiscal switch
        {
            StatusFiscal.Autorizado => "",
            StatusFiscal.Cancelado => "<div class='cancelada'>NF-e CANCELADA</div>",
            _ => $"<div class='homolog'>Status: {vf.Venda.StatusFiscal}</div>"
        };

        // Items table
        var itensHtml = new System.Text.StringBuilder();
        foreach (var item in vf.Venda.Itens.OrderBy(i => i.Fiscal?.NumeroItem ?? i.Ordem))
        {
            var fi = item.Fiscal;
            var codigo = fi?.CodigoProduto ?? item.ProdutoCodigo;
            var descricao = vf.Ambiente == 2 ? "HOMOLOGACAO" : (fi?.DescricaoProduto ?? item.ProdutoNome);
            var ncm = fi?.Ncm ?? "";
            var cst = fi != null ? (fi.CstIcms ?? fi.Csosn ?? "") : "";
            var cfop = fi?.Cfop ?? "";
            var unid = fi?.Unidade ?? "UN";
            var baseIcms = fi?.BaseIcms ?? 0m;
            var valorIcms = fi?.ValorIcms ?? 0m;
            var aliqIcms = fi?.AliquotaIcms ?? 0m;
            var valorIpi = fi?.ValorIpi ?? 0m;

            itensHtml.Append($@"
                <tr>
                    <td>{codigo}</td>
                    <td class='left'>{descricao}</td>
                    <td>{ncm}</td>
                    <td>{cst}</td>
                    <td>{cfop}</td>
                    <td>{unid}</td>
                    <td class='right'>{item.Quantidade:N4}</td>
                    <td class='right'>{item.PrecoUnitario:N4}</td>
                    <td class='right'>{item.Total:N2}</td>
                    <td class='right'>{baseIcms:N2}</td>
                    <td class='right'>{valorIcms:N2}</td>
                    <td class='right'>{aliqIcms:N2}</td>
                    <td class='right'>{valorIpi:N2}</td>
                </tr>");
        }

        // Duplicatas (ContaReceber — uma linha por parcela)
        var dupHtml = new System.Text.StringBuilder();
        foreach (var dup in parcelas.OrderBy(p => p.NumParcela))
        {
            dupHtml.Append($"<span class='dup'>{dup.NumParcela} - {dup.DataVencimento:dd/MM/yyyy} - R$ {dup.Valor:N2}</span>");
        }

        var tipoNfLabel = vf.TipoNf == 0 ? "ENTRADA" : "SAIDA";
        var totalPaginas = 1;

        var transpNome = transp?.Nome ?? "";
        var transpCpfCnpj = transp != null ? Domain.Helpers.CpfCnpjHelper.SomenteDigitos(transp.CpfCnpj) : "";
        var transpEnd = transp?.Enderecos?.FirstOrDefault(e => e.Principal) ?? transp?.Enderecos?.FirstOrDefault();

        var modFreteLabel = vf.ModFrete switch
        {
            0 => "0-Emitente",
            1 => "1-Destinatario",
            2 => "2-Terceiros",
            9 => "9-Sem Frete",
            _ => vf.ModFrete.ToString()
        };

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<title>DANFE NF-e {vf.Numero}</title>
<style>
  @media print {{ @page {{ size: A4 portrait; margin: 10mm; }} }}
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  body {{ font-family: Arial, Helvetica, sans-serif; font-size: 9px; color: #000; background: #fff; max-width: 210mm; margin: 0 auto; padding: 10mm; }}
  .border {{ border: 1px solid #000; }}
  .grid {{ display: grid; }}
  .row {{ display: flex; }}
  .cell {{ border: 1px solid #000; padding: 2px 4px; }}
  .cell label {{ display: block; font-size: 7px; color: #555; text-transform: uppercase; }}
  .cell span {{ display: block; font-size: 9px; font-weight: bold; }}
  .header {{ display: grid; grid-template-columns: 1fr 200px 1fr; border: 1px solid #000; }}
  .header-left {{ padding: 4px; border-right: 1px solid #000; text-align: center; }}
  .header-center {{ padding: 4px; border-right: 1px solid #000; text-align: center; display: flex; flex-direction: column; justify-content: center; }}
  .header-right {{ padding: 4px; }}
  .header-center .danfe {{ font-size: 14px; font-weight: bold; }}
  .header-center .desc {{ font-size: 7px; }}
  .header-center .tipo {{ font-size: 10px; font-weight: bold; margin-top: 4px; }}
  .chave {{ font-size: 10px; font-weight: bold; letter-spacing: 1px; text-align: center; padding: 4px; }}
  .section {{ margin-top: -1px; }}
  .section-title {{ background: #ddd; font-size: 8px; font-weight: bold; padding: 2px 4px; border: 1px solid #000; }}
  .fields {{ display: grid; }}
  .fields-2 {{ grid-template-columns: 1fr 1fr; }}
  .fields-3 {{ grid-template-columns: 1fr 1fr 1fr; }}
  .fields-4 {{ grid-template-columns: 1fr 1fr 1fr 1fr; }}
  .fields-5 {{ grid-template-columns: 1fr 1fr 1fr 1fr 1fr; }}
  .fields-emit {{ grid-template-columns: 2fr 1fr 1fr; }}
  .fields-dest {{ grid-template-columns: 2fr 1fr 1fr; }}
  .fields-tot {{ grid-template-columns: repeat(6, 1fr); }}
  .fields-tot2 {{ grid-template-columns: repeat(6, 1fr); }}
  .fields-transp {{ grid-template-columns: 1fr 1fr 1fr 1fr; }}
  table.itens {{ width: 100%; border-collapse: collapse; font-size: 8px; margin-top: -1px; }}
  table.itens th {{ background: #eee; border: 1px solid #000; padding: 2px; text-align: center; font-size: 7px; }}
  table.itens td {{ border: 1px solid #000; padding: 1px 3px; text-align: center; }}
  table.itens td.left {{ text-align: left; }}
  table.itens td.right {{ text-align: right; }}
  .infadic {{ border: 1px solid #000; padding: 4px; min-height: 40px; font-size: 8px; margin-top: -1px; }}
  .homolog {{ background: #e74c3c; color: #fff; text-align: center; padding: 4px; font-weight: bold; font-size: 12px; margin-bottom: 4px; }}
  .cancelada {{ background: #c0392b; color: #fff; text-align: center; padding: 6px; font-weight: bold; font-size: 14px; margin-bottom: 4px; }}
  .dup {{ display: inline-block; border: 1px solid #000; padding: 2px 6px; margin: 2px; font-size: 8px; }}
  .proto {{ font-size: 9px; text-align: center; padding: 2px; }}
  .footer {{ text-align: center; font-size: 7px; margin-top: 8px; color: #888; }}
</style>
</head>
<body>
{amb}
{statusLabel}

<!-- HEADER -->
<div class='header'>
  <div class='header-left'>
    <div style='font-size:12px;font-weight:bold'>{f.NomeFantasia}</div>
    <div style='font-size:9px'>{f.RazaoSocial}</div>
    <div style='font-size:8px'>
      {f.Rua}, {f.Numero} - {f.Bairro}<br>
      {f.Cidade}/{f.Uf} - CEP: {f.Cep}<br>
      Fone: {f.Telefone}
    </div>
  </div>
  <div class='header-center'>
    <div class='danfe'>DANFE</div>
    <div class='desc'>DOCUMENTO AUXILIAR DA<br>NOTA FISCAL ELETRONICA</div>
    <div class='tipo'>{tipoNfLabel}</div>
    <div style='font-size:9px'>N.:{vf.Numero:D9}  Serie:{vf.Serie:D3}  Folha 1/{totalPaginas}</div>
  </div>
  <div class='header-right'>
    <div class='chave'>{chaveFormatada}</div>
    <div class='proto'>
      Protocolo de autorização: {vf.Protocolo}<br>
      Data: {vf.DataAutorizacao:dd/MM/yyyy HH:mm:ss}
    </div>
  </div>
</div>

<!-- NATUREZA / PROTOCOLO -->
<div class='section'>
  <div class='fields fields-2'>
    <div class='cell'><label>NATUREZA DA OPERACAO</label><span>{vf.NatOp}</span></div>
    <div class='cell'><label>CNPJ</label><span>{f.Cnpj}</span></div>
  </div>
  <div class='fields fields-3'>
    <div class='cell'><label>INSCRICAO ESTADUAL</label><span>{f.InscricaoEstadual}</span></div>
    <div class='cell'><label>DATA DE EMISSAO</label><span>{vf.DataEmissao:dd/MM/yyyy HH:mm}</span></div>
    <div class='cell'><label>DATA SAIDA/ENTRADA</label><span>{vf.DataSaidaEntrada:dd/MM/yyyy HH:mm}</span></div>
  </div>
</div>

<!-- DESTINATARIO -->
<div class='section'>
  <div class='section-title'>DESTINATARIO / REMETENTE</div>
  <div class='fields fields-dest'>
    <div class='cell'><label>NOME / RAZAO SOCIAL</label><span>{dest?.RazaoSocial ?? dest?.Nome}</span></div>
    <div class='cell'><label>CNPJ/CPF</label><span>{dest?.CpfCnpj}</span></div>
    <div class='cell'><label>DATA EMISSAO</label><span>{vf.DataEmissao:dd/MM/yyyy}</span></div>
  </div>
  <div class='fields fields-4'>
    <div class='cell'><label>ENDERECO</label><span>{destEnd?.Rua}, {destEnd?.Numero}{(string.IsNullOrEmpty(destEnd?.Complemento) ? "" : $" - {destEnd?.Complemento}")}</span></div>
    <div class='cell'><label>BAIRRO</label><span>{destEnd?.Bairro}</span></div>
    <div class='cell'><label>CEP</label><span>{destEnd?.Cep}</span></div>
    <div class='cell'><label>DATA SAIDA/ENTRADA</label><span>{vf.DataSaidaEntrada:dd/MM/yyyy}</span></div>
  </div>
  <div class='fields fields-4'>
    <div class='cell'><label>MUNICIPIO</label><span>{destEnd?.Cidade}</span></div>
    <div class='cell'><label>UF</label><span>{destEnd?.Uf}</span></div>
    <div class='cell'><label>INSCRICAO ESTADUAL</label><span>{dest?.InscricaoEstadual}</span></div>
    <div class='cell'><label>HORA SAIDA/ENTRADA</label><span>{vf.DataSaidaEntrada:HH:mm:ss}</span></div>
  </div>
</div>

<!-- CALCULO DO IMPOSTO -->
<div class='section'>
  <div class='section-title'>CALCULO DO IMPOSTO</div>
  <div class='fields fields-tot'>
    <div class='cell'><label>BASE CALC. ICMS</label><span>{vf.ValorIcms:N2}</span></div>
    <div class='cell'><label>VALOR ICMS</label><span>{vf.ValorIcms:N2}</span></div>
    <div class='cell'><label>BASE CALC. ICMS ST</label><span>{vf.ValorIcmsSt:N2}</span></div>
    <div class='cell'><label>VALOR ICMS ST</label><span>{vf.ValorIcmsSt:N2}</span></div>
    <div class='cell'><label>VALOR PRODUTOS</label><span>{vf.ValorProdutos:N2}</span></div>
    <div class='cell'><label>VALOR TOTAL NF-e</label><span>{vf.ValorNota:N2}</span></div>
  </div>
  <div class='fields fields-tot2'>
    <div class='cell'><label>VALOR FRETE</label><span>{vf.ValorFrete:N2}</span></div>
    <div class='cell'><label>VALOR SEGURO</label><span>{vf.ValorSeguro:N2}</span></div>
    <div class='cell'><label>DESCONTO</label><span>{vf.ValorDesconto:N2}</span></div>
    <div class='cell'><label>OUTRAS DESP.</label><span>{vf.ValorOutros:N2}</span></div>
    <div class='cell'><label>VALOR IPI</label><span>{vf.ValorIpi:N2}</span></div>
    <div class='cell'><label>V. APROX. TRIBUTOS</label><span>{vf.ValorTotalTributos:N2}</span></div>
  </div>
</div>

<!-- TRANSPORTADORA -->
<div class='section'>
  <div class='section-title'>TRANSPORTADOR / VOLUMES TRANSPORTADOS</div>
  <div class='fields fields-transp'>
    <div class='cell'><label>RAZAO SOCIAL</label><span>{transpNome}</span></div>
    <div class='cell'><label>FRETE POR CONTA</label><span>{modFreteLabel}</span></div>
    <div class='cell'><label>CNPJ/CPF</label><span>{transpCpfCnpj}</span></div>
    <div class='cell'><label>PLACA</label><span>{vf.PlacaVeiculo} {vf.UfVeiculo}</span></div>
  </div>
  <div class='fields fields-5'>
    <div class='cell'><label>ENDERECO</label><span>{transpEnd?.Rua}</span></div>
    <div class='cell'><label>MUNICIPIO</label><span>{transpEnd?.Cidade}</span></div>
    <div class='cell'><label>UF</label><span>{transpEnd?.Uf}</span></div>
    <div class='cell'><label>QUANTIDADE</label><span>{vf.VolumeQuantidade}</span></div>
    <div class='cell'><label>ESPECIE</label><span>{vf.VolumeEspecie}</span></div>
  </div>
  <div class='fields fields-4'>
    <div class='cell'><label>PESO LIQUIDO</label><span>{vf.VolumePesoLiquido:N3}</span></div>
    <div class='cell'><label>PESO BRUTO</label><span>{vf.VolumePesoBruto:N3}</span></div>
    <div class='cell' style='grid-column: span 2'><label>IE</label><span>{transp?.InscricaoEstadual}</span></div>
  </div>
</div>

<!-- ITENS -->
<div class='section'>
  <div class='section-title'>DADOS DOS PRODUTOS / SERVICOS</div>
</div>
<table class='itens'>
  <thead>
    <tr>
      <th>CODIGO</th>
      <th>DESCRICAO DO PRODUTO/SERVICO</th>
      <th>NCM</th>
      <th>CST</th>
      <th>CFOP</th>
      <th>UN</th>
      <th>QTD</th>
      <th>VL.UNIT</th>
      <th>VL.TOTAL</th>
      <th>BC ICMS</th>
      <th>VL.ICMS</th>
      <th>%ICMS</th>
      <th>VL.IPI</th>
    </tr>
  </thead>
  <tbody>
    {itensHtml}
  </tbody>
</table>

<!-- DUPLICATAS -->
{(parcelas.Any() ? $@"
<div class='section'>
  <div class='section-title'>FATURA / DUPLICATAS</div>
  <div style='border:1px solid #000;padding:4px;margin-top:-1px'>
    {dupHtml}
  </div>
</div>" : "")}

<!-- INFORMACOES ADICIONAIS -->
<div class='section'>
  <div class='section-title'>DADOS ADICIONAIS</div>
  <div class='infadic'>
    {vf.Observacao ?? ""}
    {(vf.ChaveNfeReferenciada != null ? $"<br>NF-e Referenciada: {vf.ChaveNfeReferenciada}" : "")}
  </div>
</div>

<div class='footer'>Documento gerado por ZulexPharma ERP</div>

</body>
</html>";
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DANFE Térmico 80mm (NFC-e modelo 65)
    // ═══════════════════════════════════════════════════════════════════

    private static string GerarDanfeTermicoHtml(VendaFiscal vf)
    {
        var v = vf.Venda;
        var f = vf.Venda.Filial!;

        // Extrair vTotTrib do XML
        decimal vTotTrib = 0;
        if (!string.IsNullOrEmpty(vf.XmlEnvio))
        {
            // Pegar o último match (o do ICMSTot, não dos itens)
            var matches = System.Text.RegularExpressions.Regex.Matches(vf.XmlEnvio, @"<vTotTrib>([\d.]+)</vTotTrib>");
            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                decimal.TryParse(lastMatch.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out vTotTrib);
            }
        }
        var amb = vf.Ambiente == 2 ? "<div style='background:#e74c3c;color:#fff;text-align:center;padding:4px;font-weight:bold;font-size:11px'>SEM VALOR FISCAL - HOMOLOGAÇÃO</div>" : "";

        var itensHtml = new System.Text.StringBuilder();
        var temDesconto = v.Itens.Any(i => i.ValorDesconto > 0);
        foreach (var item in v.Itens)
        {
            var descNome = vf.Ambiente == 2 ? "HOMOLOGACAO" : item.ProdutoNome;
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

        var qrCodeData = $"http://www.fazenda.pr.gov.br/nfce/qrcode?p={vf.ChaveAcesso}|3|{vf.Ambiente}";

        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>DANFE NFC-e #{vf.Numero}</title>
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
<div class='chave'>{vf.ChaveAcesso}</div>

<div class='line'></div>
<div class='qrcode'>
  <img src='https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={System.Uri.EscapeDataString(qrCodeData)}' width='200' height='200' alt='QR Code' />
</div>

<div class='protocolo'>
  NFC-e nº {vf.Numero} Série {vf.Serie}<br>
  Protocolo: {vf.Protocolo}<br>
  Data: {vf.DataAutorizacao:dd/MM/yyyy HH:mm:ss}
</div>

<div class='line'></div>
<div style='text-align:center;font-size:8px;margin-top:4px'>ZulexPharma ERP</div>


</body>
</html>";
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Request DTOs
    // ═══════════════════════════════════════════════════════════════════

    public class CancelamentoRequest
    {
        public string Justificativa { get; set; } = string.Empty;
    }

    public class CartaCorrecaoRequest
    {
        public string TextoCorrecao { get; set; } = string.Empty;
    }

    public class InutilizacaoRequest
    {
        public long FilialId { get; set; }
        public int Serie { get; set; }
        public int NumInicial { get; set; }
        public int NumFinal { get; set; }
        public string Justificativa { get; set; } = string.Empty;
    }
}
