using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Nfe;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/nfe")]
public class NfeController : ControllerBase
{
    private readonly INfeService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public NfeController(INfeService service, ILogAcaoService log, AppDbContext db)
    {
        _service = service;
        _log = log;
        _db = db;
    }

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
            Log.Error(ex, "Erro ao listar NF-e");
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
            Log.Error(ex, "Erro ao obter NF-e {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] NfeFormDto dto)
    {
        try
        {
            var result = await _service.CriarRascunhoAsync(dto);
            await _log.RegistrarAsync("nfe", "criou", "Nfe", result.Id);
            return Ok(new { success = true, data = result, message = "Rascunho de NF-e criado com sucesso." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar rascunho NF-e");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] NfeFormDto dto)
    {
        try
        {
            await _service.AtualizarRascunhoAsync(id, dto);
            await _log.RegistrarAsync("nfe", "atualizou", "Nfe", id);
            return Ok(new { success = true, message = "Rascunho de NF-e atualizado." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar NF-e {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var msg = await _service.ExcluirAsync(id);
            await _log.RegistrarAsync("nfe", "excluiu", "Nfe", id);
            return Ok(new { success = true, message = msg });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao excluir NF-e {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:long}/emitir")]
    public async Task<IActionResult> Emitir(long id)
    {
        try
        {
            var resultado = await _service.EmitirAsync(id);
            await _log.RegistrarAsync("nfe", "emitiu", "Nfe", id);
            if (resultado.Autorizada)
                return Ok(new { success = true, data = resultado, message = $"NF-e autorizada. Protocolo: {resultado.Protocolo}" });
            return Ok(new { success = false, data = resultado, message = $"NF-e rejeitada: {resultado.MotivoStatus} (código {resultado.CodigoStatus})" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao emitir NF-e {Id}", id);
            return StatusCode(500, new { success = false, message = $"Erro ao emitir NF-e: {ex.Message}" });
        }
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long id, [FromBody] CancelamentoRequest request)
    {
        try
        {
            var resultado = await _service.CancelarAsync(id, request.Justificativa);
            await _log.RegistrarAsync("nfe", "cancelou", "Nfe", id);
            if (resultado.Sucesso)
                return Ok(new { success = true, data = resultado, message = "NF-e cancelada com sucesso." });
            return Ok(new { success = false, data = resultado, message = $"Cancelamento rejeitado: {resultado.MotivoStatus}" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao cancelar NF-e {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:long}/carta-correcao")]
    public async Task<IActionResult> CartaCorrecao(long id, [FromBody] CartaCorrecaoRequest request)
    {
        try
        {
            var resultado = await _service.CartaCorrecaoAsync(id, request.TextoCorrecao);
            await _log.RegistrarAsync("nfe", "carta-correcao", "Nfe", id);
            if (resultado.Sucesso)
                return Ok(new { success = true, data = resultado, message = "Carta de correção registrada com sucesso." });
            return Ok(new { success = false, data = resultado, message = $"CC-e rejeitada: {resultado.MotivoStatus}" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao enviar CC-e para NF-e {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("inutilizar")]
    public async Task<IActionResult> Inutilizar([FromBody] InutilizacaoRequest request)
    {
        try
        {
            var resultado = await _service.InutilizarAsync(request.FilialId, request.Serie, request.NumInicial, request.NumFinal, request.Justificativa);
            await _log.RegistrarAsync("nfe", "inutilizou", "Nfe", 0);
            if (resultado.Sucesso)
                return Ok(new { success = true, data = resultado, message = "Numeração inutilizada com sucesso." });
            return Ok(new { success = false, data = resultado, message = $"Inutilização rejeitada: {resultado.MotivoStatus}" });
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao inutilizar NF-e");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Gera o DANFE A4 em HTML para impressão.</summary>
    [HttpGet("{id:long}/danfe")]
    [AllowAnonymous]
    public async Task<IActionResult> Danfe(long id)
    {
        try
        {
            var nfe = await _db.Nfes
                .Include(n => n.Filial)
                .Include(n => n.DestinatarioPessoa).ThenInclude(p => p!.Enderecos)
                .Include(n => n.TransportadoraPessoa)
                .Include(n => n.Itens)
                .Include(n => n.Parcelas)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nfe == null) return NotFound("NF-e não encontrada.");

            var html = GerarDanfeA4Html(nfe);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar DANFE NF-e {Id}", id);
            return StatusCode(500, "Erro ao gerar DANFE.");
        }
    }

    /// <summary>Log de auditoria de uma NF-e.</summary>
    [HttpGet("{id:long}/log")]
    public async Task<IActionResult> LogAuditoria(long id)
    {
        try
        {
            var logs = await _log.ListarPorRegistroAsync("Nfe", id);
            return Ok(new { success = true, data = logs });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar logs NF-e {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DANFE A4 HTML
    // ═══════════════════════════════════════════════════════════════════

    private static string GerarDanfeA4Html(Domain.Entities.Nfe nfe)
    {
        var f = nfe.Filial!;
        var dest = nfe.DestinatarioPessoa;
        var destEnd = dest?.Enderecos?.FirstOrDefault(e => e.Principal) ?? dest?.Enderecos?.FirstOrDefault();
        var transp = nfe.TransportadoraPessoa;

        // Format chave in groups of 4
        var chaveFormatada = string.Empty;
        if (!string.IsNullOrEmpty(nfe.ChaveAcesso))
        {
            for (int i = 0; i < nfe.ChaveAcesso.Length; i += 4)
            {
                if (i > 0) chaveFormatada += " ";
                chaveFormatada += nfe.ChaveAcesso.Substring(i, Math.Min(4, nfe.ChaveAcesso.Length - i));
            }
        }

        var amb = nfe.Ambiente == 2
            ? "<div class='homolog'>SEM VALOR FISCAL - AMBIENTE DE HOMOLOGACAO</div>"
            : "";

        var statusLabel = nfe.Status switch
        {
            Domain.Enums.NfeStatus.Autorizada => "",
            Domain.Enums.NfeStatus.Cancelada => "<div class='cancelada'>NF-e CANCELADA</div>",
            _ => $"<div class='homolog'>Status: {nfe.Status}</div>"
        };

        // Items table
        var itensHtml = new System.Text.StringBuilder();
        foreach (var item in nfe.Itens.OrderBy(i => i.NumeroItem))
        {
            itensHtml.Append($@"
                <tr>
                    <td>{item.CodigoProduto}</td>
                    <td class='left'>{(nfe.Ambiente == 2 ? "HOMOLOGACAO" : item.DescricaoProduto)}</td>
                    <td>{item.Ncm}</td>
                    <td>{item.CstIcms ?? item.Csosn}</td>
                    <td>{item.Cfop}</td>
                    <td>{item.Unidade}</td>
                    <td class='right'>{item.Quantidade:N4}</td>
                    <td class='right'>{item.ValorUnitario:N4}</td>
                    <td class='right'>{item.ValorTotal:N2}</td>
                    <td class='right'>{item.BaseIcms:N2}</td>
                    <td class='right'>{item.ValorIcms:N2}</td>
                    <td class='right'>{item.AliquotaIcms:N2}</td>
                    <td class='right'>{item.ValorIpi:N2}</td>
                </tr>");
        }

        // Duplicatas
        var dupHtml = new System.Text.StringBuilder();
        foreach (var dup in nfe.Parcelas.OrderBy(p => p.NumeroParcela))
        {
            dupHtml.Append($"<span class='dup'>{dup.NumeroParcela} - {dup.DataVencimento:dd/MM/yyyy} - R$ {dup.Valor:N2}</span>");
        }

        var tipoNfLabel = nfe.TipoNf == 0 ? "ENTRADA" : "SAIDA";
        var totalPaginas = 1;

        var transpNome = transp?.Nome ?? "";
        var transpCpfCnpj = transp != null ? Domain.Helpers.CpfCnpjHelper.SomenteDigitos(transp.CpfCnpj) : "";
        var transpEnd = transp?.Enderecos?.FirstOrDefault(e => e.Principal) ?? transp?.Enderecos?.FirstOrDefault();

        var modFreteLabel = nfe.ModFrete switch
        {
            0 => "0-Emitente",
            1 => "1-Destinatario",
            2 => "2-Terceiros",
            9 => "9-Sem Frete",
            _ => nfe.ModFrete.ToString()
        };

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<title>DANFE NF-e {nfe.Numero}</title>
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
    <div style='font-size:9px'>N.:{nfe.Numero:D9}  Serie:{nfe.Serie:D3}  Folha 1/{totalPaginas}</div>
  </div>
  <div class='header-right'>
    <div class='chave'>{chaveFormatada}</div>
    <div class='proto'>
      Protocolo de autorização: {nfe.Protocolo}<br>
      Data: {nfe.DataAutorizacao:dd/MM/yyyy HH:mm:ss}
    </div>
  </div>
</div>

<!-- NATUREZA / PROTOCOLO -->
<div class='section'>
  <div class='fields fields-2'>
    <div class='cell'><label>NATUREZA DA OPERACAO</label><span>{nfe.NatOp}</span></div>
    <div class='cell'><label>CNPJ</label><span>{f.Cnpj}</span></div>
  </div>
  <div class='fields fields-3'>
    <div class='cell'><label>INSCRICAO ESTADUAL</label><span>{f.InscricaoEstadual}</span></div>
    <div class='cell'><label>DATA DE EMISSAO</label><span>{nfe.DataEmissao:dd/MM/yyyy HH:mm}</span></div>
    <div class='cell'><label>DATA SAIDA/ENTRADA</label><span>{nfe.DataSaidaEntrada:dd/MM/yyyy HH:mm}</span></div>
  </div>
</div>

<!-- DESTINATARIO -->
<div class='section'>
  <div class='section-title'>DESTINATARIO / REMETENTE</div>
  <div class='fields fields-dest'>
    <div class='cell'><label>NOME / RAZAO SOCIAL</label><span>{dest?.RazaoSocial ?? dest?.Nome}</span></div>
    <div class='cell'><label>CNPJ/CPF</label><span>{dest?.CpfCnpj}</span></div>
    <div class='cell'><label>DATA EMISSAO</label><span>{nfe.DataEmissao:dd/MM/yyyy}</span></div>
  </div>
  <div class='fields fields-4'>
    <div class='cell'><label>ENDERECO</label><span>{destEnd?.Rua}, {destEnd?.Numero}{(string.IsNullOrEmpty(destEnd?.Complemento) ? "" : $" - {destEnd?.Complemento}")}</span></div>
    <div class='cell'><label>BAIRRO</label><span>{destEnd?.Bairro}</span></div>
    <div class='cell'><label>CEP</label><span>{destEnd?.Cep}</span></div>
    <div class='cell'><label>DATA SAIDA/ENTRADA</label><span>{nfe.DataSaidaEntrada:dd/MM/yyyy}</span></div>
  </div>
  <div class='fields fields-4'>
    <div class='cell'><label>MUNICIPIO</label><span>{destEnd?.Cidade}</span></div>
    <div class='cell'><label>UF</label><span>{destEnd?.Uf}</span></div>
    <div class='cell'><label>INSCRICAO ESTADUAL</label><span>{dest?.InscricaoEstadual}</span></div>
    <div class='cell'><label>HORA SAIDA/ENTRADA</label><span>{nfe.DataSaidaEntrada:HH:mm:ss}</span></div>
  </div>
</div>

<!-- CALCULO DO IMPOSTO -->
<div class='section'>
  <div class='section-title'>CALCULO DO IMPOSTO</div>
  <div class='fields fields-tot'>
    <div class='cell'><label>BASE CALC. ICMS</label><span>{nfe.ValorIcms:N2}</span></div>
    <div class='cell'><label>VALOR ICMS</label><span>{nfe.ValorIcms:N2}</span></div>
    <div class='cell'><label>BASE CALC. ICMS ST</label><span>{nfe.ValorIcmsSt:N2}</span></div>
    <div class='cell'><label>VALOR ICMS ST</label><span>{nfe.ValorIcmsSt:N2}</span></div>
    <div class='cell'><label>VALOR PRODUTOS</label><span>{nfe.ValorProdutos:N2}</span></div>
    <div class='cell'><label>VALOR TOTAL NF-e</label><span>{nfe.ValorNota:N2}</span></div>
  </div>
  <div class='fields fields-tot2'>
    <div class='cell'><label>VALOR FRETE</label><span>{nfe.ValorFrete:N2}</span></div>
    <div class='cell'><label>VALOR SEGURO</label><span>{nfe.ValorSeguro:N2}</span></div>
    <div class='cell'><label>DESCONTO</label><span>{nfe.ValorDesconto:N2}</span></div>
    <div class='cell'><label>OUTRAS DESP.</label><span>{nfe.ValorOutros:N2}</span></div>
    <div class='cell'><label>VALOR IPI</label><span>{nfe.ValorIpi:N2}</span></div>
    <div class='cell'><label>V. APROX. TRIBUTOS</label><span>{nfe.ValorTotalTributos:N2}</span></div>
  </div>
</div>

<!-- TRANSPORTADORA -->
<div class='section'>
  <div class='section-title'>TRANSPORTADOR / VOLUMES TRANSPORTADOS</div>
  <div class='fields fields-transp'>
    <div class='cell'><label>RAZAO SOCIAL</label><span>{transpNome}</span></div>
    <div class='cell'><label>FRETE POR CONTA</label><span>{modFreteLabel}</span></div>
    <div class='cell'><label>CNPJ/CPF</label><span>{transpCpfCnpj}</span></div>
    <div class='cell'><label>PLACA</label><span>{nfe.PlacaVeiculo} {nfe.UfVeiculo}</span></div>
  </div>
  <div class='fields fields-5'>
    <div class='cell'><label>ENDERECO</label><span>{transpEnd?.Rua}</span></div>
    <div class='cell'><label>MUNICIPIO</label><span>{transpEnd?.Cidade}</span></div>
    <div class='cell'><label>UF</label><span>{transpEnd?.Uf}</span></div>
    <div class='cell'><label>QUANTIDADE</label><span>{nfe.VolumeQuantidade}</span></div>
    <div class='cell'><label>ESPECIE</label><span>{nfe.VolumeEspecie}</span></div>
  </div>
  <div class='fields fields-4'>
    <div class='cell'><label>PESO LIQUIDO</label><span>{nfe.VolumePesoLiquido:N3}</span></div>
    <div class='cell'><label>PESO BRUTO</label><span>{nfe.VolumePesoBruto:N3}</span></div>
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
{(nfe.Parcelas.Any() ? $@"
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
    {nfe.Observacao ?? ""}
    {(nfe.ChaveNfeReferenciada != null ? $"<br>NF-e Referenciada: {nfe.ChaveNfeReferenciada}" : "")}
  </div>
</div>

<div class='footer'>Documento gerado por ZulexPharma ERP</div>

</body>
</html>";
    }

    // ═══ Request DTOs ═══

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
