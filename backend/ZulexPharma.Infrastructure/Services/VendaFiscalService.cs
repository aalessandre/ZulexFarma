using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Emissão fiscal unificada: NFe modelo 55 (fluxo com rascunho) e NFC-e modelo 65
/// (fluxo direto a partir de venda finalizada). Substitui NfeService + NfceService.
/// Opera sobre Venda + VendaFiscal (1:1) + VendaItemFiscal (1:1 de VendaItem).
/// PRESERVA integralmente a lógica SEFAZ/XML/assinatura dos serviços originais.
/// </summary>
public class VendaFiscalService : IVendaFiscalService
{
    private readonly AppDbContext _db;

    public VendaFiscalService(AppDbContext db) => _db = db;

    // ═══════════════════════════════════════════════════════════════════
    //  LISTAGEM / LEITURA
    // ═══════════════════════════════════════════════════════════════════

    public async Task<List<VendaFiscalListDto>> ListarAsync(long? filialId = null)
    {
        var query = _db.VendaFiscais
            .Include(vf => vf.Venda).ThenInclude(v => v.DestinatarioPessoa)
            .AsQueryable();

        if (filialId.HasValue)
            query = query.Where(vf => vf.Venda.FilialId == filialId.Value);

        return await query
            .OrderByDescending(vf => vf.CriadoEm)
            .Select(vf => new VendaFiscalListDto
            {
                Id = vf.VendaId,
                VendaId = vf.VendaId,
                Modelo = vf.Modelo,
                Numero = vf.Numero,
                Serie = vf.Serie,
                NatOp = vf.NatOp,
                DestinatarioNome = vf.Venda.DestinatarioPessoa != null
                    ? (vf.Venda.DestinatarioPessoa.RazaoSocial ?? vf.Venda.DestinatarioPessoa.Nome)
                    : null,
                DestinatarioCpfCnpj = vf.Venda.DestinatarioPessoa != null
                    ? vf.Venda.DestinatarioPessoa.CpfCnpj
                    : null,
                DataEmissao = vf.DataEmissao,
                ValorNota = vf.ValorNota,
                Status = vf.Venda.StatusFiscal,
                ChaveAcesso = vf.ChaveAcesso,
                TipoNf = vf.TipoNf,
                Finalidade = vf.Finalidade,
                TipoOperacao = vf.Venda.TipoOperacao,
                CriadoEm = vf.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<VendaFiscalDetalheDto> ObterAsync(long vendaId)
    {
        var vf = await _db.VendaFiscais
            .Include(x => x.NaturezaOperacao).ThenInclude(no => no!.Regras)
            .Include(x => x.TransportadoraPessoa)
            .Include(x => x.Venda).ThenInclude(v => v.DestinatarioPessoa).ThenInclude(p => p!.Enderecos)
            .Include(x => x.Venda).ThenInclude(v => v.Itens).ThenInclude(i => i.Fiscal)
            .FirstOrDefaultAsync(x => x.VendaId == vendaId)
            ?? throw new KeyNotFoundException("Documento fiscal não encontrado.");

        return MapToDetalhe(vf);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NFe 55 — CRIAR / ATUALIZAR / EXCLUIR RASCUNHO
    // ═══════════════════════════════════════════════════════════════════

    public async Task<VendaFiscalListDto> CriarRascunhoNfeAsync(VendaFiscalFormDto dto)
    {
        var natOp = await _db.Set<NaturezaOperacao>().FindAsync(dto.NaturezaOperacaoId)
            ?? throw new KeyNotFoundException("Natureza de operação não encontrada.");

        var agora = DataHoraHelper.Agora();

        // Criar Venda (cabeçalho lean)
        var venda = new Venda
        {
            FilialId = dto.FilialId,
            TipoOperacao = dto.TipoOperacao,
            ModeloDocumento = ModeloDocumento.Nfe,
            StatusFiscal = StatusFiscal.Rascunho,
            NaturezaOperacaoId = dto.NaturezaOperacaoId,
            DestinatarioPessoaId = dto.DestinatarioPessoaId,
            FilialDestinoId = dto.FilialDestinoId,
            Origem = VendaOrigem.PreVenda,
            Status = VendaStatus.Aberta,
            DataPreVenda = agora,
            Observacao = dto.Observacao
        };

        // Criar VendaItens + VendaItemFiscal
        int nItem = 1;
        decimal totalBruto = 0, totalDesconto = 0;
        foreach (var itemDto in dto.Itens)
        {
            var valorTotal = Math.Round(itemDto.Quantidade * itemDto.ValorUnitario, 2);
            totalBruto += valorTotal;
            totalDesconto += itemDto.ValorDesconto;

            var vi = new VendaItem
            {
                ProdutoId = itemDto.ProdutoId,
                ProdutoCodigo = itemDto.CodigoProduto,
                ProdutoNome = itemDto.DescricaoProduto,
                PrecoVenda = itemDto.ValorUnitario,
                Quantidade = (int)itemDto.Quantidade,
                PrecoUnitario = itemDto.ValorUnitario,
                ValorDesconto = itemDto.ValorDesconto,
                Total = valorTotal - itemDto.ValorDesconto,
                Ordem = nItem,
                Fiscal = BuildVendaItemFiscal(itemDto, nItem)
            };
            venda.Itens.Add(vi);
            nItem++;
        }

        venda.TotalBruto = totalBruto;
        venda.TotalDesconto = totalDesconto;
        venda.TotalLiquido = totalBruto - totalDesconto;
        venda.TotalItens = venda.Itens.Count;

        // Criar VendaFiscal
        var vf = new VendaFiscal
        {
            Venda = venda,
            Modelo = ModeloDocumento.Nfe,
            Finalidade = (FinalidadeDocumento)natOp.FinalidadeNfe,
            NatOp = natOp.Descricao,
            TipoNf = natOp.TipoNf,
            IdentificadorDestino = natOp.IdentificadorDestino,
            NaturezaOperacaoId = dto.NaturezaOperacaoId,
            DataEmissao = agora,
            DataSaidaEntrada = dto.DataSaidaEntrada,
            ChaveNfeReferenciada = dto.ChaveNfeReferenciada,
            Observacao = dto.Observacao,
            // Transporte
            ModFrete = dto.ModFrete,
            TransportadoraPessoaId = dto.TransportadoraPessoaId,
            PlacaVeiculo = dto.PlacaVeiculo,
            UfVeiculo = dto.UfVeiculo,
            VolumeQuantidade = dto.VolumeQuantidade,
            VolumeEspecie = dto.VolumeEspecie,
            VolumePesoLiquido = dto.VolumePesoLiquido,
            VolumePesoBruto = dto.VolumePesoBruto,
        };

        RecalcularTotaisFiscais(vf, venda);
        venda.Fiscal = vf;

        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync();

        return new VendaFiscalListDto
        {
            Id = venda.Id,
            VendaId = venda.Id,
            Modelo = vf.Modelo,
            Numero = vf.Numero,
            Serie = vf.Serie,
            NatOp = vf.NatOp,
            DataEmissao = vf.DataEmissao,
            ValorNota = vf.ValorNota,
            Status = venda.StatusFiscal,
            ChaveAcesso = vf.ChaveAcesso,
            TipoNf = vf.TipoNf,
            Finalidade = vf.Finalidade,
            TipoOperacao = venda.TipoOperacao,
            CriadoEm = vf.CriadoEm
        };
    }

    public async Task AtualizarRascunhoNfeAsync(long vendaId, VendaFiscalFormDto dto)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Fiscal)
            .Include(v => v.Fiscal)
            .FirstOrDefaultAsync(v => v.Id == vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");

        if (venda.StatusFiscal != StatusFiscal.Rascunho && venda.StatusFiscal != StatusFiscal.Rejeitado)
            throw new InvalidOperationException("Somente rascunho ou rejeitado pode ser alterado.");

        var vf = venda.Fiscal
            ?? throw new InvalidOperationException("VendaFiscal não encontrada para esta venda.");

        var natOp = await _db.Set<NaturezaOperacao>().FindAsync(dto.NaturezaOperacaoId)
            ?? throw new KeyNotFoundException("Natureza de operação não encontrada.");

        // Rejeitado volta pra Rascunho ao salvar (permite re-emitir)
        venda.StatusFiscal = StatusFiscal.Rascunho;

        // Atualizar Venda
        venda.FilialId = dto.FilialId;
        venda.TipoOperacao = dto.TipoOperacao;
        venda.NaturezaOperacaoId = dto.NaturezaOperacaoId;
        venda.DestinatarioPessoaId = dto.DestinatarioPessoaId;
        venda.FilialDestinoId = dto.FilialDestinoId;
        venda.Observacao = dto.Observacao;
        venda.AtualizadoEm = DataHoraHelper.Agora();

        // Atualizar VendaFiscal
        vf.NaturezaOperacaoId = dto.NaturezaOperacaoId;
        vf.NatOp = natOp.Descricao;
        vf.TipoNf = natOp.TipoNf;
        vf.Finalidade = (FinalidadeDocumento)natOp.FinalidadeNfe;
        vf.IdentificadorDestino = natOp.IdentificadorDestino;
        vf.DataSaidaEntrada = dto.DataSaidaEntrada;
        vf.ChaveNfeReferenciada = dto.ChaveNfeReferenciada;
        vf.Observacao = dto.Observacao;
        vf.ModFrete = dto.ModFrete;
        vf.TransportadoraPessoaId = dto.TransportadoraPessoaId;
        vf.PlacaVeiculo = dto.PlacaVeiculo;
        vf.UfVeiculo = dto.UfVeiculo;
        vf.VolumeQuantidade = dto.VolumeQuantidade;
        vf.VolumeEspecie = dto.VolumeEspecie;
        vf.VolumePesoLiquido = dto.VolumePesoLiquido;
        vf.VolumePesoBruto = dto.VolumePesoBruto;
        vf.AtualizadoEm = DataHoraHelper.Agora();

        // Remover itens antigos (fiscal cascateia via FK)
        foreach (var i in venda.Itens)
        {
            if (i.Fiscal != null) _db.VendaItemFiscais.Remove(i.Fiscal);
        }
        _db.VendaItens.RemoveRange(venda.Itens);
        venda.Itens.Clear();

        int nItem = 1;
        decimal totalBruto = 0, totalDesconto = 0;
        foreach (var itemDto in dto.Itens)
        {
            var valorTotal = Math.Round(itemDto.Quantidade * itemDto.ValorUnitario, 2);
            totalBruto += valorTotal;
            totalDesconto += itemDto.ValorDesconto;

            var vi = new VendaItem
            {
                ProdutoId = itemDto.ProdutoId,
                ProdutoCodigo = itemDto.CodigoProduto,
                ProdutoNome = itemDto.DescricaoProduto,
                PrecoVenda = itemDto.ValorUnitario,
                Quantidade = (int)itemDto.Quantidade,
                PrecoUnitario = itemDto.ValorUnitario,
                ValorDesconto = itemDto.ValorDesconto,
                Total = valorTotal - itemDto.ValorDesconto,
                Ordem = nItem,
                Fiscal = BuildVendaItemFiscal(itemDto, nItem)
            };
            venda.Itens.Add(vi);
            nItem++;
        }

        venda.TotalBruto = totalBruto;
        venda.TotalDesconto = totalDesconto;
        venda.TotalLiquido = totalBruto - totalDesconto;
        venda.TotalItens = venda.Itens.Count;

        RecalcularTotaisFiscais(vf, venda);
        await _db.SaveChangesAsync();
    }

    public async Task<string> ExcluirRascunhoNfeAsync(long vendaId)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Fiscal)
            .Include(v => v.Fiscal)
            .FirstOrDefaultAsync(v => v.Id == vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");

        if (venda.StatusFiscal != StatusFiscal.Rascunho && venda.StatusFiscal != StatusFiscal.Rejeitado)
            throw new InvalidOperationException("Somente rascunho ou rejeitado pode ser excluído.");

        foreach (var i in venda.Itens)
        {
            if (i.Fiscal != null) _db.VendaItemFiscais.Remove(i.Fiscal);
        }
        _db.VendaItens.RemoveRange(venda.Itens);
        if (venda.Fiscal != null) _db.VendaFiscais.Remove(venda.Fiscal);
        _db.Vendas.Remove(venda);
        await _db.SaveChangesAsync();
        return "Documento fiscal excluído com sucesso.";
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EMISSÃO NFe 55
    // ═══════════════════════════════════════════════════════════════════

    public async Task<VendaFiscalEmissaoResult> EmitirNfeAsync(long vendaId)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Fiscal)
            .Include(v => v.Fiscal).ThenInclude(f => f!.NaturezaOperacao).ThenInclude(no => no!.Regras)
            .Include(v => v.Fiscal).ThenInclude(f => f!.TransportadoraPessoa).ThenInclude(p => p!.Enderecos)
            .Include(v => v.DestinatarioPessoa).ThenInclude(p => p!.Enderecos)
            .Include(v => v.NaturezaOperacao)
            .Include(v => v.Filial)
            .FirstOrDefaultAsync(v => v.Id == vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");

        if (venda.StatusFiscal != StatusFiscal.Rascunho && venda.StatusFiscal != StatusFiscal.Rejeitado)
            throw new InvalidOperationException("Somente documento em rascunho ou rejeitado pode ser emitido.");

        if (!venda.Itens.Any())
            throw new ArgumentException("Documento não possui itens.");

        var vf = venda.Fiscal
            ?? throw new InvalidOperationException("VendaFiscal não encontrada para esta venda.");

        var filial = venda.Filial ?? await _db.Filiais.FindAsync(venda.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        if (certDb.Validade <= DataHoraHelper.Agora())
            throw new ArgumentException("Certificado digital expirado.");

        // IBPTax para vTotTrib
        var ncms = venda.Itens
            .Where(i => i.Fiscal != null)
            .Select(i => i.Fiscal!.Ncm.Replace(".", "").PadRight(8, '0')[..8])
            .Distinct()
            .ToList();
        var ibptDict = await _db.IbptTaxes
            .Where(x => ncms.Contains(x.Ncm) && x.Uf == filial.Uf && x.Tipo == 0)
            .GroupBy(x => x.Ncm).Select(g => g.First())
            .ToDictionaryAsync(x => x.Ncm);

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));
        var serie = int.Parse(configs.GetValueOrDefault("fiscal.nfe.serie", "1"));
        var regimeTributario = int.Parse(configs.GetValueOrDefault("fiscal.regime.tributario", "1"));

        var ultimoNumero = await _db.VendaFiscais
            .Where(x => x.Venda.FilialId == filial.Id
                     && x.Serie == serie
                     && x.Modelo == ModeloDocumento.Nfe
                     && x.Venda.StatusFiscal != StatusFiscal.Rascunho)
            .MaxAsync(x => (int?)x.Numero) ?? 0;
        var numero = ultimoNumero + 1;

        var ufCodigo = ObterCodigoUf(filial.Uf);
        var agora = DataHoraHelper.Agora();
        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var codigoNumerico = new Random().Next(10000000, 99999999);
        var chaveAcesso = GerarChaveAcesso(ufCodigo, agora, cnpj, 55, serie, numero, 1, codigoNumerico);

        var xml = MontarXmlNfe(venda, vf, filial, chaveAcesso, numero, serie, ambiente, regimeTributario,
            ufCodigo, cnpj, agora, codigoNumerico, ibptDict);

        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
        string xmlAssinado;
        try { xmlAssinado = AssinarXml(xml, cert); }
        finally { cert.Dispose(); }

        var urlAutorizacao = ObterUrlAutorizacaoNfe(filial.Uf, ambiente);
        string xmlRetorno;
        try
        {
            cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
            var soapEnvelope = MontarSoapEnvelope(xmlAssinado);
            xmlRetorno = await EnviarSoap(urlAutorizacao, soapEnvelope, cert);
        }
        finally { cert.Dispose(); }

        var resultado = ProcessarRetorno(xmlRetorno);

        Log.Information("NF-e Retorno | VendaId={VendaId} | cStat={CodStatus} | xMotivo={Motivo} | Autorizada={Auth}",
            vendaId, resultado.CodigoStatus, resultado.MotivoStatus, resultado.Autorizada);

        // Atualizar entidade
        vf.Numero = numero;
        vf.Serie = serie;
        vf.ChaveAcesso = chaveAcesso;
        vf.Protocolo = resultado.Protocolo;
        vf.DataEmissao = agora;
        vf.Ambiente = ambiente;
        vf.CodigoStatus = resultado.CodigoStatus;
        vf.MotivoStatus = resultado.MotivoStatus;
        vf.XmlEnvio = xmlAssinado;
        vf.XmlRetorno = xmlRetorno;
        vf.AtualizadoEm = agora;
        if (resultado.Autorizada)
            vf.DataAutorizacao = agora;

        venda.StatusFiscal = resultado.Autorizada ? StatusFiscal.Autorizado : StatusFiscal.Rejeitado;
        venda.AtualizadoEm = agora;

        await _db.SaveChangesAsync();

        return new VendaFiscalEmissaoResult
        {
            VendaFiscalId = vf.Id,
            VendaId = venda.Id,
            Modelo = ModeloDocumento.Nfe,
            Numero = numero,
            Serie = serie,
            ChaveAcesso = chaveAcesso,
            Protocolo = resultado.Protocolo,
            CodigoStatus = resultado.CodigoStatus,
            MotivoStatus = resultado.MotivoStatus,
            Autorizada = resultado.Autorizada
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EMISSÃO NFC-e 65 (a partir de venda finalizada)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<VendaFiscalEmissaoResult> EmitirNfceAsync(long vendaId)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Pagamentos).ThenInclude(p => p.TipoPagamento)
            .Include(v => v.Cliente).ThenInclude(c => c!.Pessoa)
            .Include(v => v.Fiscal)
            .FirstOrDefaultAsync(v => v.Id == vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");

        var filial = await _db.Filiais.FindAsync(venda.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        if (certDb.Validade <= DataHoraHelper.Agora())
            throw new ArgumentException("Certificado digital expirado.");

        var prodIds = venda.Itens.Select(i => i.ProdutoId).ToList();
        var fiscais = await _db.ProdutosFiscal
            .Where(f => prodIds.Contains(f.ProdutoId) && f.FilialId == filial.Id)
            .ToDictionaryAsync(f => f.ProdutoId);
        var produtos = await _db.Produtos
            .Where(p => prodIds.Contains(p.Id)).Include(p => p.Ncm)
            .ToDictionaryAsync(p => p.Id);

        // IBPTax para vTotTrib
        var ncms = produtos.Values.Select(p => (p.Ncm?.CodigoNcm ?? "00000000").Replace(".", "").PadRight(8, '0')[..8]).Distinct().ToList();
        var ibptDict = await _db.IbptTaxes
            .Where(x => ncms.Contains(x.Ncm) && x.Uf == filial.Uf && x.Tipo == 0)
            .GroupBy(x => x.Ncm).Select(g => g.First())
            .ToDictionaryAsync(x => x.Ncm);

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));
        var csc = configs.GetValueOrDefault("fiscal.nfce.csc", "");
        var cscId = configs.GetValueOrDefault("fiscal.nfce.csc.id", "1");
        var serie = int.Parse(configs.GetValueOrDefault("fiscal.nfce.serie", "1"));
        var regimeTributario = int.Parse(configs.GetValueOrDefault("fiscal.regime.tributario", "1"));

        var ultimoNumero = await _db.VendaFiscais
            .Where(x => x.Venda.FilialId == filial.Id && x.Serie == serie && x.Modelo == ModeloDocumento.Nfce)
            .MaxAsync(x => (int?)x.Numero) ?? 0;
        var numero = ultimoNumero + 1;

        var ufCodigo = ObterCodigoUf(filial.Uf);
        var agora = DataHoraHelper.Agora();
        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var codigoNumerico = new Random().Next(10000000, 99999999);
        var chaveAcesso = GerarChaveAcesso(ufCodigo, agora, cnpj, 65, serie, numero, 1, codigoNumerico);

        var xml = MontarXmlNfce(venda, filial, chaveAcesso, numero, serie, ambiente, regimeTributario,
            ufCodigo, cnpj, agora, codigoNumerico, fiscais, produtos, ibptDict);

        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
        string xmlAssinado;
        try { xmlAssinado = AssinarXml(xml, cert); }
        finally { cert.Dispose(); }

        // Inserir infNFeSupl ANTES da Signature
        var qrCodeUrl = GerarUrlQrCode(chaveAcesso, ambiente, csc, cscId, filial.Uf);
        var urlConsulta = ObterUrlConsultaNfce(filial.Uf, ambiente);
        var qrCodeEscaped = qrCodeUrl.Replace("&", "&amp;");
        var infNFeSupl = $"<infNFeSupl><qrCode><![CDATA[{qrCodeEscaped}]]></qrCode><urlChave>{urlConsulta}</urlChave></infNFeSupl>";
        var sigTag = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">";
        if (xmlAssinado.Contains(sigTag))
            xmlAssinado = xmlAssinado.Replace(sigTag, infNFeSupl + sigTag);
        else
            xmlAssinado = xmlAssinado.Replace("</NFe>", infNFeSupl + "</NFe>");

        var urlAutorizacao = ObterUrlAutorizacaoNfce(filial.Uf, ambiente);
        string xmlRetorno;
        try
        {
            cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
            var soapEnvelope = MontarSoapEnvelope(xmlAssinado);
            xmlRetorno = await EnviarSoap(urlAutorizacao, soapEnvelope, cert);
        }
        finally { cert.Dispose(); }

        var resultado = ProcessarRetorno(xmlRetorno);

        Log.Information("NFC-e Retorno | VendaId={VendaId} | cStat={CodStatus} | xMotivo={Motivo} | Autorizada={Auth}",
            vendaId, resultado.CodigoStatus, resultado.MotivoStatus, resultado.Autorizada);

        // Log do fragmento <pag> do XML enviado para debug
        var pagStart = xmlAssinado.IndexOf("<pag>");
        var pagEnd = xmlAssinado.IndexOf("</pag>");
        if (pagStart >= 0 && pagEnd > pagStart)
            Log.Information("NFC-e <pag> enviado: {Pag}", xmlAssinado.Substring(pagStart, pagEnd - pagStart + 6));

        // Criar ou atualizar VendaFiscal (1:1 Venda)
        var vf = venda.Fiscal;
        if (vf == null)
        {
            vf = new VendaFiscal
            {
                VendaId = venda.Id,
                Modelo = ModeloDocumento.Nfce,
                Finalidade = FinalidadeDocumento.Normal,
                NatOp = "VENDA",
                TipoNf = 1,
                IdentificadorDestino = 1
            };
            _db.VendaFiscais.Add(vf);
        }
        else
        {
            vf.Modelo = ModeloDocumento.Nfce;
            vf.Finalidade = FinalidadeDocumento.Normal;
            vf.NatOp = "VENDA";
            vf.TipoNf = 1;
            vf.IdentificadorDestino = 1;
            vf.AtualizadoEm = agora;
        }

        vf.Numero = numero;
        vf.Serie = serie;
        vf.ChaveAcesso = chaveAcesso;
        vf.Protocolo = resultado.Protocolo;
        vf.DataEmissao = agora;
        vf.DataAutorizacao = resultado.Autorizada ? agora : null;
        vf.Ambiente = ambiente;
        vf.CodigoStatus = resultado.CodigoStatus;
        vf.MotivoStatus = resultado.MotivoStatus;
        vf.XmlEnvio = xmlAssinado;
        vf.XmlRetorno = xmlRetorno;
        vf.ValorProdutos = venda.TotalBruto;
        vf.ValorDesconto = venda.TotalDesconto;
        vf.ValorNota = venda.TotalLiquido;

        // Criar snapshots VendaItemFiscal para cada item (se ainda não existirem)
        int nItem = 1;
        foreach (var item in venda.Itens)
        {
            if (item.Fiscal != null) { nItem++; continue; }

            var prod = produtos.GetValueOrDefault(item.ProdutoId);
            var fiscal = fiscais.GetValueOrDefault(item.ProdutoId);
            var ncmRaw = (prod?.Ncm?.CodigoNcm ?? "00000000").Replace(".", "").PadRight(8, '0');
            if (ncmRaw.Length > 8) ncmRaw = ncmRaw[..8];
            var valorBruto = Math.Round(item.PrecoVenda * item.Quantidade, 2);
            var valorDesc = Math.Round(item.ValorDesconto, 2);
            var valorLiquido = valorBruto - valorDesc;
            decimal vTotTribItem = 0;
            if (ibptDict.TryGetValue(ncmRaw, out var ibpt))
                vTotTribItem = Math.Round(valorLiquido * (ibpt.AliqNacional + ibpt.AliqEstadual + ibpt.AliqMunicipal) / 100, 2);

            var vif = new VendaItemFiscal
            {
                VendaItemId = item.Id,
                NumeroItem = nItem,
                CodigoProduto = item.ProdutoCodigo,
                CodigoBarras = "SEM GTIN",
                DescricaoProduto = item.ProdutoNome,
                Ncm = ncmRaw,
                Cest = fiscal?.Cest,
                Cfop = fiscal?.Cfop ?? "5102",
                Unidade = "UN",
                OrigemMercadoria = fiscal?.OrigemMercadoria ?? "0",
                CstIcms = fiscal?.CstIcms,
                Csosn = fiscal?.Csosn,
                CstPis = fiscal?.CstPis ?? "49",
                AliquotaPis = fiscal?.AliquotaPis ?? 0,
                CstCofins = fiscal?.CstCofins ?? "49",
                AliquotaCofins = fiscal?.AliquotaCofins ?? 0,
                AliquotaIcms = fiscal?.AliquotaIcms ?? 0,
                BaseIcms = regimeTributario > 2 ? valorLiquido : 0,
                ValorIcms = regimeTributario > 2 ? Math.Round(valorLiquido * (fiscal?.AliquotaIcms ?? 0) / 100, 2) : 0,
                ValorTotalTributos = vTotTribItem,
                IndicadorTotal = 1
            };
            _db.VendaItemFiscais.Add(vif);
            nItem++;
        }

        venda.ModeloDocumento = ModeloDocumento.Nfce;
        venda.StatusFiscal = resultado.Autorizada ? StatusFiscal.Autorizado : StatusFiscal.Rejeitado;
        if (resultado.Autorizada)
            venda.DataEmissaoCupom = agora;
        venda.AtualizadoEm = agora;

        var cfgNumero = await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "fiscal.nfce.numero.atual");
        if (cfgNumero != null) cfgNumero.Valor = numero.ToString();

        await _db.SaveChangesAsync();

        return new VendaFiscalEmissaoResult
        {
            VendaFiscalId = vf.Id,
            VendaId = venda.Id,
            Modelo = ModeloDocumento.Nfce,
            Numero = numero,
            Serie = serie,
            ChaveAcesso = chaveAcesso,
            Protocolo = resultado.Protocolo,
            CodigoStatus = resultado.CodigoStatus,
            MotivoStatus = resultado.MotivoStatus,
            Autorizada = resultado.Autorizada
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EVENTOS (Cancelamento, CC-e, Inutilização)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<VendaFiscalEventoResult> CancelarAsync(long vendaId, string justificativa)
    {
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
            throw new ArgumentException("Justificativa deve ter no mínimo 15 caracteres.");

        var vf = await _db.VendaFiscais
            .Include(x => x.Venda)
            .FirstOrDefaultAsync(x => x.VendaId == vendaId)
            ?? throw new KeyNotFoundException("Documento fiscal não encontrado.");

        if (vf.Venda.StatusFiscal != StatusFiscal.Autorizado)
            throw new InvalidOperationException("Somente documento autorizado pode ser cancelado.");

        var filial = await _db.Filiais.FindAsync(vf.Venda.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));

        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var agora = DataHoraHelper.Agora();
        var nSeqEvento = 1;

        var xmlEvento = MontarXmlEventoCancelamento(vf.ChaveAcesso, cnpj, ambiente, agora, nSeqEvento, vf.Protocolo!, justificativa);

        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
        string xmlAssinado;
        try { xmlAssinado = AssinarXmlEvento(xmlEvento, cert); }
        finally { cert.Dispose(); }

        var url = ObterUrlEventoNfe(filial.Uf, ambiente);
        string xmlRetorno;
        try
        {
            cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
            var soapEnvelope = MontarSoapEnvelopeEvento(xmlAssinado);
            xmlRetorno = await EnviarSoap(url, soapEnvelope, cert);
        }
        finally { cert.Dispose(); }

        var resultado = ProcessarRetornoEvento(xmlRetorno);

        Log.Information("Documento Fiscal Cancelamento | VendaId={Id} | cStat={CodStatus} | xMotivo={Motivo}",
            vendaId, resultado.CodigoStatus, resultado.MotivoStatus);

        if (resultado.Sucesso)
        {
            vf.Venda.StatusFiscal = StatusFiscal.Cancelado;
            vf.XmlCancelamento = xmlAssinado;
            vf.AtualizadoEm = agora;
            vf.Venda.AtualizadoEm = agora;
            await _db.SaveChangesAsync();
        }

        resultado.XmlEvento = xmlAssinado;
        return resultado;
    }

    public async Task<VendaFiscalEventoResult> CartaCorrecaoAsync(long vendaId, string textoCorrecao)
    {
        if (string.IsNullOrWhiteSpace(textoCorrecao) || textoCorrecao.Length < 15)
            throw new ArgumentException("Texto da correção deve ter no mínimo 15 caracteres.");

        var vf = await _db.VendaFiscais
            .Include(x => x.Venda)
            .FirstOrDefaultAsync(x => x.VendaId == vendaId)
            ?? throw new KeyNotFoundException("Documento fiscal não encontrado.");

        if (vf.Venda.StatusFiscal != StatusFiscal.Autorizado)
            throw new InvalidOperationException("Somente documento autorizado pode receber carta de correção.");

        var filial = await _db.Filiais.FindAsync(vf.Venda.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));

        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var agora = DataHoraHelper.Agora();

        // Conta quantas CC-e já existem para incrementar nSeqEvento
        var nSeqEvento = string.IsNullOrEmpty(vf.XmlCartaCorrecao) ? 1 : 2; // Simplificado

        var xmlEvento = MontarXmlEventoCartaCorrecao(vf.ChaveAcesso, cnpj, ambiente, agora, nSeqEvento, textoCorrecao);

        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
        string xmlAssinado;
        try { xmlAssinado = AssinarXmlEvento(xmlEvento, cert); }
        finally { cert.Dispose(); }

        var url = ObterUrlEventoNfe(filial.Uf, ambiente);
        string xmlRetorno;
        try
        {
            cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
            var soapEnvelope = MontarSoapEnvelopeEvento(xmlAssinado);
            xmlRetorno = await EnviarSoap(url, soapEnvelope, cert);
        }
        finally { cert.Dispose(); }

        var resultado = ProcessarRetornoEvento(xmlRetorno);

        Log.Information("Documento Fiscal CC-e | VendaId={Id} | cStat={CodStatus} | xMotivo={Motivo}",
            vendaId, resultado.CodigoStatus, resultado.MotivoStatus);

        if (resultado.Sucesso)
        {
            vf.XmlCartaCorrecao = xmlAssinado;
            vf.AtualizadoEm = agora;
            await _db.SaveChangesAsync();
        }

        resultado.XmlEvento = xmlAssinado;
        return resultado;
    }

    public async Task<VendaFiscalEventoResult> InutilizarAsync(long filialId, int serie, int numInicial, int numFinal, string justificativa)
    {
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
            throw new ArgumentException("Justificativa deve ter no mínimo 15 caracteres.");

        var filial = await _db.Filiais.FindAsync(filialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));

        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var ufCodigo = ObterCodigoUf(filial.Uf);
        var ano = DataHoraHelper.Agora().Year % 100;

        var id = $"ID{ufCodigo:D2}{ano:D2}{cnpj}55{serie:D3}{numInicial:D9}{numFinal:D9}";

        var xmlInut = $"<inutNFe xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\">" +
            $"<infInut Id=\"{id}\">" +
            $"<tpAmb>{ambiente}</tpAmb>" +
            "<xServ>INUTILIZAR</xServ>" +
            $"<cUF>{ufCodigo}</cUF>" +
            $"<ano>{ano:D2}</ano>" +
            $"<CNPJ>{cnpj}</CNPJ>" +
            "<mod>55</mod>" +
            $"<serie>{serie}</serie>" +
            $"<nNFIni>{numInicial}</nNFIni>" +
            $"<nNFFin>{numFinal}</nNFFin>" +
            $"<xJust>{Esc(justificativa)}</xJust>" +
            "</infInut></inutNFe>";

        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
        string xmlAssinado;
        try { xmlAssinado = AssinarXmlInutilizacao(xmlInut, cert); }
        finally { cert.Dispose(); }

        var url = ObterUrlInutilizacaoNfe(filial.Uf, ambiente);
        string xmlRetorno;
        try
        {
            cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
            var soapEnvelope = MontarSoapEnvelopeInutilizacao(xmlAssinado);
            xmlRetorno = await EnviarSoap(url, soapEnvelope, cert);
        }
        finally { cert.Dispose(); }

        var resultado = ProcessarRetornoEvento(xmlRetorno);

        Log.Information("NF-e Inutilização | Filial={FilialId} | Serie={Serie} | Ini={Ini} | Fin={Fin} | cStat={CodStatus}",
            filialId, serie, numInicial, numFinal, resultado.CodigoStatus);

        resultado.XmlEvento = xmlAssinado;
        return resultado;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  XML NF-e modelo 55 layout 4.00
    // ═══════════════════════════════════════════════════════════════════

    private string MontarXmlNfe(Venda venda, VendaFiscal vf, Filial filial, string chaveAcesso, int numero, int serie,
        int ambiente, int crt, int ufCodigo, string cnpj, DateTime agora, int codigoNumerico,
        Dictionary<string, IbptTax> ibptDict)
    {
        var sb = new StringBuilder();
        sb.Append("<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\">");
        sb.Append($"<infNFe versao=\"4.00\" Id=\"NFe{chaveAcesso}\">");

        // ── ide ──
        sb.Append("<ide>");
        sb.Append($"<cUF>{ufCodigo}</cUF>");
        sb.Append($"<cNF>{codigoNumerico:D8}</cNF>");
        sb.Append($"<natOp>{Esc(vf.NatOp)}</natOp>");
        sb.Append("<mod>55</mod>");
        sb.Append($"<serie>{serie}</serie>");
        sb.Append($"<nNF>{numero}</nNF>");
        sb.Append($"<dhEmi>{agora:yyyy-MM-ddTHH:mm:sszzz}</dhEmi>");
        if (vf.DataSaidaEntrada.HasValue)
            sb.Append($"<dhSaiEnt>{vf.DataSaidaEntrada.Value:yyyy-MM-ddTHH:mm:sszzz}</dhSaiEnt>");
        sb.Append($"<tpNF>{vf.TipoNf}</tpNF>");
        sb.Append($"<idDest>{vf.IdentificadorDestino}</idDest>");
        sb.Append($"<cMunFG>{filial.CodigoIbgeMunicipio ?? "0000000"}</cMunFG>");
        sb.Append("<tpImp>1</tpImp>"); // DANFE Retrato
        sb.Append("<tpEmis>1</tpEmis>");
        sb.Append($"<cDV>{chaveAcesso[43]}</cDV>");
        sb.Append($"<tpAmb>{ambiente}</tpAmb>");
        sb.Append($"<finNFe>{(int)vf.Finalidade}</finNFe>");
        sb.Append($"<indFinal>{vf.NaturezaOperacao?.IndicadorFinalidade ?? 0}</indFinal>");
        sb.Append($"<indPres>{vf.NaturezaOperacao?.IndicadorPresenca ?? 0}</indPres>");
        sb.Append("<procEmi>0</procEmi>");
        sb.Append("<verProc>ZulexPharma1.0</verProc>");
        sb.Append("</ide>");

        // ── NFref (se houver chave referenciada) ──
        if (!string.IsNullOrEmpty(vf.ChaveNfeReferenciada))
        {
            sb.Append("<NFref><refNFe>");
            sb.Append(vf.ChaveNfeReferenciada);
            sb.Append("</refNFe></NFref>");
        }

        // ── emit ──
        sb.Append("<emit>");
        sb.Append($"<CNPJ>{cnpj}</CNPJ>");
        sb.Append($"<xNome>{Esc(filial.RazaoSocial)}</xNome>");
        sb.Append($"<xFant>{Esc(filial.NomeFantasia)}</xFant>");
        sb.Append("<enderEmit>");
        sb.Append($"<xLgr>{Esc(filial.Rua)}</xLgr>");
        sb.Append($"<nro>{Esc(filial.Numero)}</nro>");
        sb.Append($"<xBairro>{Esc(filial.Bairro)}</xBairro>");
        sb.Append($"<cMun>{filial.CodigoIbgeMunicipio ?? "0000000"}</cMun>");
        sb.Append($"<xMun>{Esc(filial.Cidade)}</xMun>");
        sb.Append($"<UF>{filial.Uf}</UF>");
        sb.Append($"<CEP>{filial.Cep.Replace("-", "")}</CEP>");
        sb.Append("<cPais>1058</cPais>");
        sb.Append("<xPais>Brasil</xPais>");
        var fone = filial.Telefone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        if (!string.IsNullOrEmpty(fone)) sb.Append($"<fone>{fone}</fone>");
        sb.Append("</enderEmit>");
        var ie = filial.InscricaoEstadual?.Replace(".", "").Replace("-", "").Replace("/", "");
        if (!string.IsNullOrEmpty(ie)) sb.Append($"<IE>{ie}</IE>");
        sb.Append($"<CRT>{crt}</CRT>");
        sb.Append("</emit>");

        // ── dest (obrigatório para NF-e 55) ──
        AppendDestinatario(sb, venda, ambiente);

        // ── det (itens) ──
        decimal sumVBc = 0, sumVIcms = 0, sumVBcSt = 0, sumVSt = 0, sumVProd = 0;
        decimal sumVFrete = 0, sumVSeg = 0, sumVDesc = 0, sumVOutro = 0, sumVIpi = 0;
        decimal sumVPis = 0, sumVCofins = 0, sumVFcp = 0, sumVFcpSt = 0;
        decimal sumVIcmsDeson = 0, sumVTotTrib = 0;

        int nItem = 1;
        foreach (var itemTuple in venda.Itens
                 .Where(i => i.Fiscal != null)
                 .OrderBy(i => i.Fiscal!.NumeroItem)
                 .Select(i => (Item: i, Fiscal: i.Fiscal!)))
        {
            var item = itemTuple.Item;
            var fiscal = itemTuple.Fiscal;
            var valorUnitario = item.PrecoUnitario;
            var quantidade = (decimal)item.Quantidade;
            var valorTotal = Math.Round(valorUnitario * quantidade, 2);
            var valorDesc = item.ValorDesconto;

            var ncmRaw = fiscal.Ncm.Replace(".", "").PadRight(8, '0');
            if (ncmRaw.Length > 8) ncmRaw = ncmRaw[..8];

            decimal vTotTribItem = 0;
            var valorLiquido = valorTotal - valorDesc;
            if (ibptDict.TryGetValue(ncmRaw, out var ibpt))
                vTotTribItem = Math.Round(valorLiquido * (ibpt.AliqNacional + ibpt.AliqEstadual + ibpt.AliqMunicipal) / 100, 2);

            // Atualizar o item com vTotTrib calculado se ainda zerado
            if (fiscal.ValorTotalTributos == 0) fiscal.ValorTotalTributos = vTotTribItem;

            var xProd = ambiente == 2 ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Esc(fiscal.DescricaoProduto);

            sb.Append($"<det nItem=\"{nItem}\">");
            sb.Append("<prod>");
            var cProdEmit = string.IsNullOrWhiteSpace(fiscal.CodigoProduto) ? item.ProdutoId.ToString() : fiscal.CodigoProduto;
            var cfopEmit = string.IsNullOrWhiteSpace(fiscal.Cfop) ? "5102" : fiscal.Cfop;
            sb.Append($"<cProd>{cProdEmit}</cProd>");
            sb.Append($"<cEAN>{Esc(fiscal.CodigoBarras)}</cEAN>");
            sb.Append($"<xProd>{xProd}</xProd>");
            sb.Append($"<NCM>{ncmRaw}</NCM>");
            if (!string.IsNullOrEmpty(fiscal.Cest)) sb.Append($"<CEST>{fiscal.Cest}</CEST>");
            sb.Append($"<CFOP>{cfopEmit}</CFOP>");
            sb.Append($"<uCom>{Esc(fiscal.Unidade)}</uCom>");
            sb.Append($"<qCom>{D4(quantidade)}</qCom>");
            sb.Append($"<vUnCom>{D4(valorUnitario)}</vUnCom>");
            sb.Append($"<vProd>{D2(valorTotal)}</vProd>");
            sb.Append($"<cEANTrib>{Esc(fiscal.CodigoBarras)}</cEANTrib>");
            sb.Append($"<uTrib>{Esc(fiscal.Unidade)}</uTrib>");
            sb.Append($"<qTrib>{D4(quantidade)}</qTrib>");
            sb.Append($"<vUnTrib>{D4(valorUnitario)}</vUnTrib>");
            if (fiscal.ValorFrete > 0) sb.Append($"<vFrete>{D2(fiscal.ValorFrete)}</vFrete>");
            if (fiscal.ValorSeguro > 0) sb.Append($"<vSeg>{D2(fiscal.ValorSeguro)}</vSeg>");
            if (valorDesc > 0) sb.Append($"<vDesc>{D2(valorDesc)}</vDesc>");
            if (fiscal.ValorOutros > 0) sb.Append($"<vOutro>{D2(fiscal.ValorOutros)}</vOutro>");
            sb.Append($"<indTot>{fiscal.IndicadorTotal}</indTot>");

            // med (medicamento com código ANVISA)
            if (!string.IsNullOrWhiteSpace(fiscal.CodigoAnvisa))
            {
                sb.Append("<med>");
                sb.Append($"<cProdANVISA>{Esc(fiscal.CodigoAnvisa)}</cProdANVISA>");
                sb.Append("</med>");
            }

            // rastro (rastreabilidade de lote)
            if (!string.IsNullOrWhiteSpace(fiscal.RastroLote))
            {
                sb.Append("<rastro>");
                sb.Append($"<nLote>{Esc(fiscal.RastroLote)}</nLote>");
                sb.Append($"<qLote>{D4(fiscal.RastroQuantidade ?? quantidade)}</qLote>");
                if (fiscal.RastroFabricacao.HasValue)
                    sb.Append($"<dFab>{fiscal.RastroFabricacao.Value:yyyy-MM-dd}</dFab>");
                if (fiscal.RastroValidade.HasValue)
                    sb.Append($"<dVal>{fiscal.RastroValidade.Value:yyyy-MM-dd}</dVal>");
                sb.Append("</rastro>");
            }

            sb.Append("</prod>");

            // ── imposto ──
            sb.Append("<imposto>");
            sb.Append($"<vTotTrib>{D2(fiscal.ValorTotalTributos)}</vTotTrib>");

            // ICMS
            sb.Append("<ICMS>");
            AppendIcms(sb, fiscal, crt);
            sb.Append("</ICMS>");

            // IPI
            AppendIpi(sb, fiscal);

            // PIS
            AppendPis(sb, fiscal);

            // COFINS
            AppendCofins(sb, fiscal);

            sb.Append("</imposto>");
            sb.Append("</det>");

            // Acumular totais
            if (fiscal.IndicadorTotal == 1)
            {
                sumVProd += valorTotal;
                sumVDesc += valorDesc;
                sumVFrete += fiscal.ValorFrete;
                sumVSeg += fiscal.ValorSeguro;
                sumVOutro += fiscal.ValorOutros;
            }
            sumVBc += fiscal.BaseIcms;
            sumVIcms += fiscal.ValorIcms;
            sumVBcSt += fiscal.BaseIcmsSt;
            sumVSt += fiscal.ValorIcmsSt;
            sumVIpi += fiscal.ValorIpi;
            sumVPis += fiscal.ValorPis;
            sumVCofins += fiscal.ValorCofins;
            sumVFcp += fiscal.ValorFcp;
            sumVFcpSt += fiscal.ValorFcpSt;
            sumVIcmsDeson += fiscal.ValorIcmsDesonerado;
            sumVTotTrib += fiscal.ValorTotalTributos;

            nItem++;
        }

        // ── total ──
        var vNF = sumVProd - sumVDesc + sumVFrete + sumVSeg + sumVOutro + sumVSt + sumVIpi;
        sb.Append("<total><ICMSTot>");
        sb.Append($"<vBC>{D2(sumVBc)}</vBC>");
        sb.Append($"<vICMS>{D2(sumVIcms)}</vICMS>");
        sb.Append($"<vICMSDeson>{D2(sumVIcmsDeson)}</vICMSDeson>");
        sb.Append("<vFCPUFDest>0.00</vFCPUFDest><vICMSUFDest>0.00</vICMSUFDest><vICMSUFRemet>0.00</vICMSUFRemet>");
        sb.Append($"<vFCP>{D2(sumVFcp)}</vFCP>");
        sb.Append($"<vBCST>{D2(sumVBcSt)}</vBCST>");
        sb.Append($"<vST>{D2(sumVSt)}</vST>");
        sb.Append($"<vFCPST>{D2(sumVFcpSt)}</vFCPST>");
        sb.Append("<vFCPSTRet>0.00</vFCPSTRet>");
        sb.Append($"<vProd>{D2(sumVProd)}</vProd>");
        sb.Append($"<vFrete>{D2(sumVFrete)}</vFrete>");
        sb.Append($"<vSeg>{D2(sumVSeg)}</vSeg>");
        sb.Append($"<vDesc>{D2(sumVDesc)}</vDesc>");
        sb.Append("<vII>0.00</vII>");
        sb.Append($"<vIPI>{D2(sumVIpi)}</vIPI>");
        sb.Append("<vIPIDevol>0.00</vIPIDevol>");
        sb.Append($"<vPIS>{D2(sumVPis)}</vPIS>");
        sb.Append($"<vCOFINS>{D2(sumVCofins)}</vCOFINS>");
        sb.Append($"<vOutro>{D2(sumVOutro)}</vOutro>");
        sb.Append($"<vNF>{D2(vNF)}</vNF>");
        sb.Append($"<vTotTrib>{D2(sumVTotTrib)}</vTotTrib>");
        sb.Append("</ICMSTot></total>");

        // ── transp ──
        sb.Append("<transp>");
        sb.Append($"<modFrete>{vf.ModFrete}</modFrete>");
        if (vf.TransportadoraPessoa != null)
        {
            var transp = vf.TransportadoraPessoa;
            var transpCpfCnpj = CpfCnpjHelper.SomenteDigitos(transp.CpfCnpj);
            sb.Append("<transporta>");
            if (transpCpfCnpj.Length == 14) sb.Append($"<CNPJ>{transpCpfCnpj}</CNPJ>");
            else if (transpCpfCnpj.Length == 11) sb.Append($"<CPF>{transpCpfCnpj}</CPF>");
            sb.Append($"<xNome>{Esc(transp.Nome)}</xNome>");
            var transpEnd = transp.Enderecos?.FirstOrDefault(e => e.Principal) ?? transp.Enderecos?.FirstOrDefault();
            if (transpEnd != null)
            {
                sb.Append($"<xEnder>{Esc(transpEnd.Rua)}, {Esc(transpEnd.Numero)}</xEnder>");
                sb.Append($"<xMun>{Esc(transpEnd.Cidade)}</xMun>");
                sb.Append($"<UF>{transpEnd.Uf}</UF>");
            }
            if (!string.IsNullOrEmpty(transp.InscricaoEstadual))
                sb.Append($"<IE>{transp.InscricaoEstadual.Replace(".", "").Replace("-", "").Replace("/", "")}</IE>");
            sb.Append("</transporta>");
        }
        if (!string.IsNullOrEmpty(vf.PlacaVeiculo))
        {
            sb.Append("<veicTransp>");
            sb.Append($"<placa>{vf.PlacaVeiculo}</placa>");
            sb.Append($"<UF>{vf.UfVeiculo ?? filial.Uf}</UF>");
            sb.Append("</veicTransp>");
        }
        if (vf.VolumeQuantidade.HasValue && vf.VolumeQuantidade.Value > 0)
        {
            sb.Append("<vol>");
            sb.Append($"<qVol>{vf.VolumeQuantidade.Value}</qVol>");
            if (!string.IsNullOrEmpty(vf.VolumeEspecie)) sb.Append($"<esp>{Esc(vf.VolumeEspecie)}</esp>");
            if (vf.VolumePesoLiquido.HasValue) sb.Append($"<pesoL>{D3(vf.VolumePesoLiquido.Value)}</pesoL>");
            if (vf.VolumePesoBruto.HasValue) sb.Append($"<pesoB>{D3(vf.VolumePesoBruto.Value)}</pesoB>");
            sb.Append("</vol>");
        }
        sb.Append("</transp>");

        // ── pag (NF-e sem duplicatas: tPag=90) ──
        // As parcelas a prazo viram ContaReceber (fluxo separado); no XML NF-e usa-se tPag=90.
        sb.Append("<pag>");
        sb.Append("<detPag><tPag>90</tPag><vPag>0.00</vPag></detPag>");
        sb.Append("</pag>");

        // ── infAdic ──
        var infCpl = vf.Observacao ?? vf.NaturezaOperacao?.Observacao ?? "Documento emitido por ZulexPharma ERP";
        sb.Append($"<infAdic><infCpl>{Esc(infCpl)}</infCpl></infAdic>");

        // ── infRespTec ──
        sb.Append("<infRespTec>");
        sb.Append($"<CNPJ>{cnpj}</CNPJ>");
        sb.Append($"<xContato>{Esc(filial.RazaoSocial)}</xContato>");
        sb.Append($"<email>{filial.Email}</email>");
        var foneTec = filial.Telefone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        sb.Append($"<fone>{foneTec}</fone>");
        sb.Append("</infRespTec>");

        sb.Append("</infNFe>");
        sb.Append("</NFe>");
        return sb.ToString();
    }

    // ── Destinatário NF-e 55 ──
    private static void AppendDestinatario(StringBuilder sb, Venda venda, int ambiente)
    {
        var dest = venda.DestinatarioPessoa;
        if (dest == null)
            throw new ArgumentException("Destinatário é obrigatório para NF-e modelo 55.");

        var cpfCnpj = CpfCnpjHelper.SomenteDigitos(dest.CpfCnpj);

        sb.Append("<dest>");
        if (cpfCnpj.Length == 14) sb.Append($"<CNPJ>{cpfCnpj}</CNPJ>");
        else if (cpfCnpj.Length == 11) sb.Append($"<CPF>{cpfCnpj}</CPF>");

        var xNome = ambiente == 2 ? "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Esc(dest.RazaoSocial ?? dest.Nome);
        sb.Append($"<xNome>{xNome}</xNome>");

        var endereco = dest.Enderecos?.FirstOrDefault(e => e.Principal) ?? dest.Enderecos?.FirstOrDefault();
        if (endereco != null)
        {
            sb.Append("<enderDest>");
            sb.Append($"<xLgr>{Esc(endereco.Rua)}</xLgr>");
            sb.Append($"<nro>{Esc(endereco.Numero)}</nro>");
            if (!string.IsNullOrEmpty(endereco.Complemento)) sb.Append($"<xCpl>{Esc(endereco.Complemento)}</xCpl>");
            sb.Append($"<xBairro>{Esc(endereco.Bairro)}</xBairro>");
            sb.Append($"<cMun>{endereco.CodigoIbgeMunicipio ?? "0000000"}</cMun>");
            sb.Append($"<xMun>{Esc(endereco.Cidade)}</xMun>");
            sb.Append($"<UF>{endereco.Uf}</UF>");
            sb.Append($"<CEP>{endereco.Cep.Replace("-", "")}</CEP>");
            sb.Append("<cPais>1058</cPais>");
            sb.Append("<xPais>Brasil</xPais>");
            sb.Append("</enderDest>");
        }

        // indIEDest: 1=Contribuinte, 2=Isento, 9=Nao contribuinte
        var ieDest = dest.InscricaoEstadual?.Replace(".", "").Replace("-", "").Replace("/", "");
        if (!string.IsNullOrEmpty(ieDest) && ieDest.ToUpper() != "ISENTO")
        {
            sb.Append("<indIEDest>1</indIEDest>");
            sb.Append($"<IE>{ieDest}</IE>");
        }
        else if (ieDest?.ToUpper() == "ISENTO")
        {
            sb.Append("<indIEDest>2</indIEDest>");
        }
        else
        {
            sb.Append("<indIEDest>9</indIEDest>");
        }

        sb.Append("</dest>");
    }

    // ── ICMS por CST/CSOSN ──
    private static void AppendIcms(StringBuilder sb, VendaItemFiscal item, int crt)
    {
        var orig = string.IsNullOrWhiteSpace(item.OrigemMercadoria) ? "0" : item.OrigemMercadoria;

        if (crt <= 2)
        {
            // Simples Nacional - CSOSN
            var csosn = (item.Csosn ?? "102").TrimStart('0');
            if (string.IsNullOrEmpty(csosn)) csosn = "102";

            switch (csosn)
            {
                case "101":
                    sb.Append("<ICMSSN101>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CSOSN>{csosn}</CSOSN>");
                    sb.Append($"<pCredSN>{D2(item.AliquotaIcms)}</pCredSN>");
                    sb.Append($"<vCredICMSSN>{D2(item.ValorIcms)}</vCredICMSSN>");
                    sb.Append("</ICMSSN101>");
                    break;
                case "201":
                    sb.Append("<ICMSSN201>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CSOSN>{csosn}</CSOSN>");
                    sb.Append($"<modBCST>{item.ModBcIcmsSt ?? "4"}</modBCST>");
                    if (item.MvaSt > 0) sb.Append($"<pMVAST>{D2(item.MvaSt)}</pMVAST>");
                    sb.Append($"<vBCST>{D2(item.BaseIcmsSt)}</vBCST>");
                    sb.Append($"<pICMSST>{D2(item.AliquotaIcmsSt)}</pICMSST>");
                    sb.Append($"<vICMSST>{D2(item.ValorIcmsSt)}</vICMSST>");
                    sb.Append($"<pCredSN>{D2(item.AliquotaIcms)}</pCredSN>");
                    sb.Append($"<vCredICMSSN>{D2(item.ValorIcms)}</vCredICMSSN>");
                    sb.Append("</ICMSSN201>");
                    break;
                case "202": case "203":
                    sb.Append($"<ICMSSN202>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CSOSN>{csosn}</CSOSN>");
                    sb.Append($"<modBCST>{item.ModBcIcmsSt ?? "4"}</modBCST>");
                    if (item.MvaSt > 0) sb.Append($"<pMVAST>{D2(item.MvaSt)}</pMVAST>");
                    sb.Append($"<vBCST>{D2(item.BaseIcmsSt)}</vBCST>");
                    sb.Append($"<pICMSST>{D2(item.AliquotaIcmsSt)}</pICMSST>");
                    sb.Append($"<vICMSST>{D2(item.ValorIcmsSt)}</vICMSST>");
                    sb.Append("</ICMSSN202>");
                    break;
                case "500":
                    sb.Append("<ICMSSN500>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CSOSN>{csosn}</CSOSN>");
                    if (item.BaseIcmsSt > 0)
                    {
                        sb.Append($"<vBCSTRet>{D2(item.BaseIcmsSt)}</vBCSTRet>");
                        sb.Append($"<pST>{D2(item.AliquotaIcmsSt)}</pST>");
                        sb.Append($"<vICMSSubstituto>{D2(item.ValorIcmsSt)}</vICMSSubstituto>");
                        sb.Append($"<vICMSSTRet>{D2(item.ValorIcmsSt)}</vICMSSTRet>");
                    }
                    sb.Append("</ICMSSN500>");
                    break;
                case "900":
                    sb.Append("<ICMSSN900>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CSOSN>{csosn}</CSOSN>");
                    sb.Append($"<modBC>{item.ModBcIcms ?? "3"}</modBC>");
                    sb.Append($"<vBC>{D2(item.BaseIcms)}</vBC>");
                    sb.Append($"<pICMS>{D2(item.AliquotaIcms)}</pICMS>");
                    sb.Append($"<vICMS>{D2(item.ValorIcms)}</vICMS>");
                    if (item.BaseIcmsSt > 0)
                    {
                        sb.Append($"<modBCST>{item.ModBcIcmsSt ?? "4"}</modBCST>");
                        if (item.MvaSt > 0) sb.Append($"<pMVAST>{D2(item.MvaSt)}</pMVAST>");
                        sb.Append($"<vBCST>{D2(item.BaseIcmsSt)}</vBCST>");
                        sb.Append($"<pICMSST>{D2(item.AliquotaIcmsSt)}</pICMSST>");
                        sb.Append($"<vICMSST>{D2(item.ValorIcmsSt)}</vICMSST>");
                    }
                    sb.Append($"<pCredSN>{D2(item.AliquotaIcms)}</pCredSN>");
                    sb.Append($"<vCredICMSSN>{D2(item.ValorIcms)}</vCredICMSSN>");
                    sb.Append("</ICMSSN900>");
                    break;
                default: // 102, 103, 300, 400
                    sb.Append("<ICMSSN102>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CSOSN>{csosn}</CSOSN>");
                    sb.Append("</ICMSSN102>");
                    break;
            }
        }
        else
        {
            // Regime Normal - CST
            var cst = item.CstIcms ?? "00";

            switch (cst)
            {
                case "00":
                    sb.Append("<ICMS00>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append($"<modBC>{item.ModBcIcms ?? "3"}</modBC>");
                    sb.Append($"<vBC>{D2(item.BaseIcms)}</vBC>");
                    sb.Append($"<pICMS>{D2(item.AliquotaIcms)}</pICMS>");
                    sb.Append($"<vICMS>{D2(item.ValorIcms)}</vICMS>");
                    if (item.AliquotaFcp > 0)
                    {
                        sb.Append($"<pFCP>{D2(item.AliquotaFcp)}</pFCP>");
                        sb.Append($"<vFCP>{D2(item.ValorFcp)}</vFCP>");
                    }
                    sb.Append("</ICMS00>");
                    break;
                case "10":
                    sb.Append("<ICMS10>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append($"<modBC>{item.ModBcIcms ?? "3"}</modBC>");
                    sb.Append($"<vBC>{D2(item.BaseIcms)}</vBC>");
                    sb.Append($"<pICMS>{D2(item.AliquotaIcms)}</pICMS>");
                    sb.Append($"<vICMS>{D2(item.ValorIcms)}</vICMS>");
                    if (item.AliquotaFcp > 0)
                    {
                        sb.Append($"<vBCFCP>{D2(item.BaseFcp)}</vBCFCP>");
                        sb.Append($"<pFCP>{D2(item.AliquotaFcp)}</pFCP>");
                        sb.Append($"<vFCP>{D2(item.ValorFcp)}</vFCP>");
                    }
                    sb.Append($"<modBCST>{item.ModBcIcmsSt ?? "4"}</modBCST>");
                    if (item.MvaSt > 0) sb.Append($"<pMVAST>{D2(item.MvaSt)}</pMVAST>");
                    sb.Append($"<vBCST>{D2(item.BaseIcmsSt)}</vBCST>");
                    sb.Append($"<pICMSST>{D2(item.AliquotaIcmsSt)}</pICMSST>");
                    sb.Append($"<vICMSST>{D2(item.ValorIcmsSt)}</vICMSST>");
                    if (item.AliquotaFcpSt > 0)
                    {
                        sb.Append($"<vBCFCPST>{D2(item.BaseFcpSt)}</vBCFCPST>");
                        sb.Append($"<pFCPST>{D2(item.AliquotaFcpSt)}</pFCPST>");
                        sb.Append($"<vFCPST>{D2(item.ValorFcpSt)}</vFCPST>");
                    }
                    sb.Append("</ICMS10>");
                    break;
                case "20":
                    sb.Append("<ICMS20>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append($"<modBC>{item.ModBcIcms ?? "3"}</modBC>");
                    sb.Append($"<pRedBC>{D2(item.PercentualReducaoBc)}</pRedBC>");
                    sb.Append($"<vBC>{D2(item.BaseIcms)}</vBC>");
                    sb.Append($"<pICMS>{D2(item.AliquotaIcms)}</pICMS>");
                    sb.Append($"<vICMS>{D2(item.ValorIcms)}</vICMS>");
                    if (item.AliquotaFcp > 0)
                    {
                        sb.Append($"<vBCFCP>{D2(item.BaseFcp)}</vBCFCP>");
                        sb.Append($"<pFCP>{D2(item.AliquotaFcp)}</pFCP>");
                        sb.Append($"<vFCP>{D2(item.ValorFcp)}</vFCP>");
                    }
                    if (item.ValorIcmsDesonerado > 0)
                    {
                        sb.Append($"<vICMSDeson>{D2(item.ValorIcmsDesonerado)}</vICMSDeson>");
                        sb.Append($"<motDesICMS>{item.MotivoDesoneracaoIcms ?? "9"}</motDesICMS>");
                    }
                    sb.Append("</ICMS20>");
                    break;
                case "30":
                    sb.Append("<ICMS30>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append($"<modBCST>{item.ModBcIcmsSt ?? "4"}</modBCST>");
                    if (item.MvaSt > 0) sb.Append($"<pMVAST>{D2(item.MvaSt)}</pMVAST>");
                    sb.Append($"<vBCST>{D2(item.BaseIcmsSt)}</vBCST>");
                    sb.Append($"<pICMSST>{D2(item.AliquotaIcmsSt)}</pICMSST>");
                    sb.Append($"<vICMSST>{D2(item.ValorIcmsSt)}</vICMSST>");
                    if (item.AliquotaFcpSt > 0)
                    {
                        sb.Append($"<vBCFCPST>{D2(item.BaseFcpSt)}</vBCFCPST>");
                        sb.Append($"<pFCPST>{D2(item.AliquotaFcpSt)}</pFCPST>");
                        sb.Append($"<vFCPST>{D2(item.ValorFcpSt)}</vFCPST>");
                    }
                    if (item.ValorIcmsDesonerado > 0)
                    {
                        sb.Append($"<vICMSDeson>{D2(item.ValorIcmsDesonerado)}</vICMSDeson>");
                        sb.Append($"<motDesICMS>{item.MotivoDesoneracaoIcms ?? "9"}</motDesICMS>");
                    }
                    sb.Append("</ICMS30>");
                    break;
                case "40": case "41": case "50":
                    sb.Append("<ICMS40>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    if (item.ValorIcmsDesonerado > 0)
                    {
                        sb.Append($"<vICMSDeson>{D2(item.ValorIcmsDesonerado)}</vICMSDeson>");
                        sb.Append($"<motDesICMS>{item.MotivoDesoneracaoIcms ?? "9"}</motDesICMS>");
                    }
                    sb.Append("</ICMS40>");
                    break;
                case "51":
                    sb.Append("<ICMS51>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append($"<modBC>{item.ModBcIcms ?? "3"}</modBC>");
                    if (item.PercentualReducaoBc > 0) sb.Append($"<pRedBC>{D2(item.PercentualReducaoBc)}</pRedBC>");
                    sb.Append($"<vBC>{D2(item.BaseIcms)}</vBC>");
                    sb.Append($"<pICMS>{D2(item.AliquotaIcms)}</pICMS>");
                    sb.Append($"<vICMSOp>{D2(item.ValorIcms)}</vICMSOp>");
                    sb.Append("<pDif>0.00</pDif>");
                    sb.Append("<vICMSDif>0.00</vICMSDif>");
                    sb.Append($"<vICMS>{D2(item.ValorIcms)}</vICMS>");
                    sb.Append("</ICMS51>");
                    break;
                case "60":
                    sb.Append("<ICMS60>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    if (item.BaseIcmsSt > 0)
                    {
                        sb.Append($"<vBCSTRet>{D2(item.BaseIcmsSt)}</vBCSTRet>");
                        sb.Append($"<pST>{D2(item.AliquotaIcmsSt)}</pST>");
                        sb.Append($"<vICMSSubstituto>{D2(item.ValorIcmsSt)}</vICMSSubstituto>");
                        sb.Append($"<vICMSSTRet>{D2(item.ValorIcmsSt)}</vICMSSTRet>");
                    }
                    if (item.BaseFcpSt > 0)
                    {
                        sb.Append($"<vBCFCPSTRet>{D2(item.BaseFcpSt)}</vBCFCPSTRet>");
                        sb.Append($"<pFCPSTRet>{D2(item.AliquotaFcpSt)}</pFCPSTRet>");
                        sb.Append($"<vFCPSTRet>{D2(item.ValorFcpSt)}</vFCPSTRet>");
                    }
                    sb.Append("</ICMS60>");
                    break;
                case "70":
                    sb.Append("<ICMS70>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append($"<modBC>{item.ModBcIcms ?? "3"}</modBC>");
                    sb.Append($"<pRedBC>{D2(item.PercentualReducaoBc)}</pRedBC>");
                    sb.Append($"<vBC>{D2(item.BaseIcms)}</vBC>");
                    sb.Append($"<pICMS>{D2(item.AliquotaIcms)}</pICMS>");
                    sb.Append($"<vICMS>{D2(item.ValorIcms)}</vICMS>");
                    if (item.AliquotaFcp > 0)
                    {
                        sb.Append($"<vBCFCP>{D2(item.BaseFcp)}</vBCFCP>");
                        sb.Append($"<pFCP>{D2(item.AliquotaFcp)}</pFCP>");
                        sb.Append($"<vFCP>{D2(item.ValorFcp)}</vFCP>");
                    }
                    sb.Append($"<modBCST>{item.ModBcIcmsSt ?? "4"}</modBCST>");
                    if (item.MvaSt > 0) sb.Append($"<pMVAST>{D2(item.MvaSt)}</pMVAST>");
                    sb.Append($"<vBCST>{D2(item.BaseIcmsSt)}</vBCST>");
                    sb.Append($"<pICMSST>{D2(item.AliquotaIcmsSt)}</pICMSST>");
                    sb.Append($"<vICMSST>{D2(item.ValorIcmsSt)}</vICMSST>");
                    if (item.AliquotaFcpSt > 0)
                    {
                        sb.Append($"<vBCFCPST>{D2(item.BaseFcpSt)}</vBCFCPST>");
                        sb.Append($"<pFCPST>{D2(item.AliquotaFcpSt)}</pFCPST>");
                        sb.Append($"<vFCPST>{D2(item.ValorFcpSt)}</vFCPST>");
                    }
                    if (item.ValorIcmsDesonerado > 0)
                    {
                        sb.Append($"<vICMSDeson>{D2(item.ValorIcmsDesonerado)}</vICMSDeson>");
                        sb.Append($"<motDesICMS>{item.MotivoDesoneracaoIcms ?? "9"}</motDesICMS>");
                    }
                    sb.Append("</ICMS70>");
                    break;
                case "90":
                    sb.Append("<ICMS90>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append($"<modBC>{item.ModBcIcms ?? "3"}</modBC>");
                    sb.Append($"<vBC>{D2(item.BaseIcms)}</vBC>");
                    if (item.PercentualReducaoBc > 0) sb.Append($"<pRedBC>{D2(item.PercentualReducaoBc)}</pRedBC>");
                    sb.Append($"<pICMS>{D2(item.AliquotaIcms)}</pICMS>");
                    sb.Append($"<vICMS>{D2(item.ValorIcms)}</vICMS>");
                    if (item.AliquotaFcp > 0)
                    {
                        sb.Append($"<vBCFCP>{D2(item.BaseFcp)}</vBCFCP>");
                        sb.Append($"<pFCP>{D2(item.AliquotaFcp)}</pFCP>");
                        sb.Append($"<vFCP>{D2(item.ValorFcp)}</vFCP>");
                    }
                    if (item.BaseIcmsSt > 0)
                    {
                        sb.Append($"<modBCST>{item.ModBcIcmsSt ?? "4"}</modBCST>");
                        if (item.MvaSt > 0) sb.Append($"<pMVAST>{D2(item.MvaSt)}</pMVAST>");
                        sb.Append($"<vBCST>{D2(item.BaseIcmsSt)}</vBCST>");
                        sb.Append($"<pICMSST>{D2(item.AliquotaIcmsSt)}</pICMSST>");
                        sb.Append($"<vICMSST>{D2(item.ValorIcmsSt)}</vICMSST>");
                    }
                    if (item.ValorIcmsDesonerado > 0)
                    {
                        sb.Append($"<vICMSDeson>{D2(item.ValorIcmsDesonerado)}</vICMSDeson>");
                        sb.Append($"<motDesICMS>{item.MotivoDesoneracaoIcms ?? "9"}</motDesICMS>");
                    }
                    sb.Append("</ICMS90>");
                    break;
                default:
                    sb.Append("<ICMS00>");
                    sb.Append($"<orig>{orig}</orig>");
                    sb.Append($"<CST>{cst}</CST>");
                    sb.Append("<modBC>3</modBC><vBC>0.00</vBC><pICMS>0.00</pICMS><vICMS>0.00</vICMS>");
                    sb.Append("</ICMS00>");
                    break;
            }
        }
    }

    // ── IPI ──
    private static void AppendIpi(StringBuilder sb, VendaItemFiscal item)
    {
        var cstIpi = item.CstIpi;
        if (string.IsNullOrEmpty(cstIpi)) return; // Sem IPI

        sb.Append("<IPI>");
        if (!string.IsNullOrEmpty(item.EnquadramentoIpi))
            sb.Append($"<cEnq>{item.EnquadramentoIpi}</cEnq>");
        else
            sb.Append("<cEnq>999</cEnq>");

        // CST 00, 49, 50, 99 = IPITrib; outros = IPINT
        if (cstIpi == "00" || cstIpi == "49" || cstIpi == "50" || cstIpi == "99")
        {
            sb.Append("<IPITrib>");
            sb.Append($"<CST>{cstIpi}</CST>");
            sb.Append($"<vBC>{D2(item.BaseIpi)}</vBC>");
            sb.Append($"<pIPI>{D2(item.AliquotaIpi)}</pIPI>");
            sb.Append($"<vIPI>{D2(item.ValorIpi)}</vIPI>");
            sb.Append("</IPITrib>");
        }
        else
        {
            sb.Append("<IPINT>");
            sb.Append($"<CST>{cstIpi}</CST>");
            sb.Append("</IPINT>");
        }
        sb.Append("</IPI>");
    }

    // ── PIS ──
    private static void AppendPis(StringBuilder sb, VendaItemFiscal item)
    {
        var cstPis = string.IsNullOrWhiteSpace(item.CstPis) ? "49" : item.CstPis;
        sb.Append("<PIS>");
        if (cstPis == "01" || cstPis == "02")
        {
            sb.Append("<PISAliq>");
            sb.Append($"<CST>{cstPis}</CST>");
            sb.Append($"<vBC>{D2(item.BasePis)}</vBC>");
            sb.Append($"<pPIS>{D2(item.AliquotaPis)}</pPIS>");
            sb.Append($"<vPIS>{D2(item.ValorPis)}</vPIS>");
            sb.Append("</PISAliq>");
        }
        else if (cstPis == "04" || cstPis == "05" || cstPis == "06" || cstPis == "07" || cstPis == "08" || cstPis == "09")
        {
            sb.Append("<PISNT>");
            sb.Append($"<CST>{cstPis}</CST>");
            sb.Append("</PISNT>");
        }
        else
        {
            sb.Append("<PISOutr>");
            sb.Append($"<CST>{cstPis}</CST>");
            sb.Append($"<vBC>{D2(item.BasePis)}</vBC>");
            sb.Append($"<pPIS>{D2(item.AliquotaPis)}</pPIS>");
            sb.Append($"<vPIS>{D2(item.ValorPis)}</vPIS>");
            sb.Append("</PISOutr>");
        }
        sb.Append("</PIS>");
    }

    // ── COFINS ──
    private static void AppendCofins(StringBuilder sb, VendaItemFiscal item)
    {
        var cstCofins = string.IsNullOrWhiteSpace(item.CstCofins) ? "49" : item.CstCofins;
        sb.Append("<COFINS>");
        if (cstCofins == "01" || cstCofins == "02")
        {
            sb.Append("<COFINSAliq>");
            sb.Append($"<CST>{cstCofins}</CST>");
            sb.Append($"<vBC>{D2(item.BaseCofins)}</vBC>");
            sb.Append($"<pCOFINS>{D2(item.AliquotaCofins)}</pCOFINS>");
            sb.Append($"<vCOFINS>{D2(item.ValorCofins)}</vCOFINS>");
            sb.Append("</COFINSAliq>");
        }
        else if (cstCofins == "04" || cstCofins == "05" || cstCofins == "06" || cstCofins == "07" || cstCofins == "08" || cstCofins == "09")
        {
            sb.Append("<COFINSNT>");
            sb.Append($"<CST>{cstCofins}</CST>");
            sb.Append("</COFINSNT>");
        }
        else
        {
            sb.Append("<COFINSOutr>");
            sb.Append($"<CST>{cstCofins}</CST>");
            sb.Append($"<vBC>{D2(item.BaseCofins)}</vBC>");
            sb.Append($"<pCOFINS>{D2(item.AliquotaCofins)}</pCOFINS>");
            sb.Append($"<vCOFINS>{D2(item.ValorCofins)}</vCOFINS>");
            sb.Append("</COFINSOutr>");
        }
        sb.Append("</COFINS>");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  XML NFC-e layout 4.00 (emissão direta a partir de Venda)
    // ═══════════════════════════════════════════════════════════════════

    private string MontarXmlNfce(Venda venda, Filial filial, string chaveAcesso, int numero, int serie,
        int ambiente, int crt, int ufCodigo, string cnpj, DateTime agora, int codigoNumerico,
        Dictionary<long, ProdutoFiscal> fiscais, Dictionary<long, Produto> produtos,
        Dictionary<string, IbptTax> ibptDict)
    {
        var sb = new StringBuilder();
        sb.Append("<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\">");
        sb.Append($"<infNFe versao=\"4.00\" Id=\"NFe{chaveAcesso}\">");

        // ide
        sb.Append("<ide>");
        sb.Append($"<cUF>{ufCodigo}</cUF>");
        sb.Append($"<cNF>{codigoNumerico:D8}</cNF>");
        sb.Append("<natOp>VENDA</natOp>");
        sb.Append("<mod>65</mod>");
        sb.Append($"<serie>{serie}</serie>");
        sb.Append($"<nNF>{numero}</nNF>");
        sb.Append($"<dhEmi>{agora:yyyy-MM-ddTHH:mm:sszzz}</dhEmi>");
        sb.Append("<tpNF>1</tpNF>");
        sb.Append("<idDest>1</idDest>");
        sb.Append($"<cMunFG>{filial.CodigoIbgeMunicipio ?? "0000000"}</cMunFG>");
        sb.Append("<tpImp>4</tpImp>");
        sb.Append("<tpEmis>1</tpEmis>");
        sb.Append($"<cDV>{chaveAcesso[43]}</cDV>");
        sb.Append($"<tpAmb>{ambiente}</tpAmb>");
        sb.Append("<finNFe>1</finNFe>");
        sb.Append("<indFinal>1</indFinal>");
        sb.Append("<indPres>1</indPres>");
        sb.Append("<procEmi>0</procEmi>");
        sb.Append("<verProc>ZulexPharma1.0</verProc>");
        sb.Append("</ide>");

        // emit
        sb.Append("<emit>");
        sb.Append($"<CNPJ>{cnpj}</CNPJ>");
        sb.Append($"<xNome>{Esc(filial.RazaoSocial)}</xNome>");
        sb.Append($"<xFant>{Esc(filial.NomeFantasia)}</xFant>");
        sb.Append("<enderEmit>");
        sb.Append($"<xLgr>{Esc(filial.Rua)}</xLgr>");
        sb.Append($"<nro>{Esc(filial.Numero)}</nro>");
        sb.Append($"<xBairro>{Esc(filial.Bairro)}</xBairro>");
        sb.Append($"<cMun>{filial.CodigoIbgeMunicipio ?? "0000000"}</cMun>");
        sb.Append($"<xMun>{Esc(filial.Cidade)}</xMun>");
        sb.Append($"<UF>{filial.Uf}</UF>");
        sb.Append($"<CEP>{filial.Cep.Replace("-", "")}</CEP>");
        sb.Append("<cPais>1058</cPais>");
        sb.Append("<xPais>Brasil</xPais>");
        var fone = filial.Telefone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        if (!string.IsNullOrEmpty(fone)) sb.Append($"<fone>{fone}</fone>");
        sb.Append("</enderEmit>");
        var ie = filial.InscricaoEstadual?.Replace(".", "").Replace("-", "").Replace("/", "");
        if (!string.IsNullOrEmpty(ie)) sb.Append($"<IE>{ie}</IE>");
        sb.Append($"<CRT>{crt}</CRT>");
        sb.Append("</emit>");

        // dest (opcional NFC-e)
        if (venda.Cliente?.Pessoa != null)
        {
            var cpfCnpjDest = CpfCnpjHelper.SomenteDigitos(venda.Cliente.Pessoa.CpfCnpj);
            if (cpfCnpjDest.Length == 11 || cpfCnpjDest.Length == 14)
            {
                sb.Append("<dest>");
                if (cpfCnpjDest.Length == 11) sb.Append($"<CPF>{cpfCnpjDest}</CPF>");
                else sb.Append($"<CNPJ>{cpfCnpjDest}</CNPJ>");
                var xNome = ambiente == 2 ? "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Esc(venda.Cliente.Pessoa.Nome);
                sb.Append($"<xNome>{xNome}</xNome>");
                sb.Append("<indIEDest>9</indIEDest>");
                sb.Append("</dest>");
            }
        }

        // det (produtos)
        int nItem = 1;
        decimal totalProdutos = 0, totalDesconto = 0, totalTributos = 0;
        foreach (var item in venda.Itens)
        {
            var prod = produtos.GetValueOrDefault(item.ProdutoId);
            var fiscal = fiscais.GetValueOrDefault(item.ProdutoId);
            var ncmRaw = (prod?.Ncm?.CodigoNcm ?? "00000000").Replace(".", "").PadRight(8, '0');
            if (ncmRaw.Length > 8) ncmRaw = ncmRaw[..8];
            var cfop = fiscal?.Cfop ?? "5102";
            var origem = fiscal?.OrigemMercadoria ?? "0";
            var valorBruto = Math.Round(item.PrecoVenda * item.Quantidade, 2);
            var valorDesc = Math.Round(item.ValorDesconto, 2);
            totalProdutos += valorBruto;
            totalDesconto += valorDesc;

            var xProd = ambiente == 2 ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Esc(item.ProdutoNome);

            sb.Append($"<det nItem=\"{nItem}\">");
            sb.Append("<prod>");
            sb.Append($"<cProd>{item.ProdutoCodigo}</cProd>");
            sb.Append("<cEAN>SEM GTIN</cEAN>");
            sb.Append($"<xProd>{xProd}</xProd>");
            sb.Append($"<NCM>{ncmRaw}</NCM>");
            if (!string.IsNullOrEmpty(fiscal?.Cest)) sb.Append($"<CEST>{fiscal.Cest}</CEST>");
            sb.Append($"<CFOP>{cfop}</CFOP>");
            sb.Append("<uCom>UN</uCom>");
            sb.Append($"<qCom>{item.Quantidade}.0000</qCom>");
            sb.Append($"<vUnCom>{D4(item.PrecoVenda)}</vUnCom>");
            sb.Append($"<vProd>{D2(valorBruto)}</vProd>");
            sb.Append("<cEANTrib>SEM GTIN</cEANTrib>");
            sb.Append("<uTrib>UN</uTrib>");
            sb.Append($"<qTrib>{item.Quantidade}.0000</qTrib>");
            sb.Append($"<vUnTrib>{D4(item.PrecoVenda)}</vUnTrib>");
            if (valorDesc > 0) sb.Append($"<vDesc>{D2(valorDesc)}</vDesc>");
            sb.Append("<indTot>1</indTot>");
            sb.Append("</prod>");

            // impostos
            var valorLiquido = valorBruto - valorDesc;
            decimal vTotTribItem = 0;
            if (ibptDict.TryGetValue(ncmRaw, out var ibpt))
            {
                vTotTribItem = Math.Round(valorLiquido * (ibpt.AliqNacional + ibpt.AliqEstadual + ibpt.AliqMunicipal) / 100, 2);
            }
            totalTributos += vTotTribItem;

            sb.Append("<imposto>");
            sb.Append($"<vTotTrib>{D2(vTotTribItem)}</vTotTrib>");
            sb.Append("<ICMS>");
            if (crt <= 2)
            {
                var csosn = (fiscal?.Csosn ?? "102").TrimStart('0');
                if (string.IsNullOrEmpty(csosn)) csosn = "102";
                sb.Append("<ICMSSN102>");
                sb.Append($"<orig>{origem}</orig>");
                sb.Append($"<CSOSN>{csosn}</CSOSN>");
                sb.Append("</ICMSSN102>");
            }
            else
            {
                var cstIcms = fiscal?.CstIcms ?? "00";
                var aliqIcms = fiscal?.AliquotaIcms ?? 0;
                sb.Append("<ICMS00>");
                sb.Append($"<orig>{origem}</orig>");
                sb.Append($"<CST>{cstIcms}</CST>");
                sb.Append("<modBC>3</modBC>");
                sb.Append($"<vBC>{D2(valorBruto - valorDesc)}</vBC>");
                sb.Append($"<pICMS>{D2(aliqIcms)}</pICMS>");
                sb.Append($"<vICMS>{D2(Math.Round((valorBruto - valorDesc) * aliqIcms / 100, 2))}</vICMS>");
                sb.Append("</ICMS00>");
            }
            sb.Append("</ICMS>");

            // PIS
            var cstPis = fiscal?.CstPis ?? "49";
            sb.Append("<PIS>");
            if (cstPis == "01" || cstPis == "02")
            {
                sb.Append("<PISAliq>");
                sb.Append($"<CST>{cstPis}</CST>");
                sb.Append($"<vBC>{D2(valorBruto - valorDesc)}</vBC>");
                sb.Append($"<pPIS>{D2(fiscal?.AliquotaPis ?? 0)}</pPIS>");
                sb.Append($"<vPIS>{D2(Math.Round((valorBruto - valorDesc) * (fiscal?.AliquotaPis ?? 0) / 100, 2))}</vPIS>");
                sb.Append("</PISAliq>");
            }
            else if (cstPis == "04" || cstPis == "05" || cstPis == "06" || cstPis == "07" || cstPis == "08" || cstPis == "09")
            {
                sb.Append("<PISNT>");
                sb.Append($"<CST>{cstPis}</CST>");
                sb.Append("</PISNT>");
            }
            else
            {
                sb.Append("<PISOutr>");
                sb.Append($"<CST>{cstPis}</CST>");
                sb.Append("<vBC>0.00</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS>");
                sb.Append("</PISOutr>");
            }
            sb.Append("</PIS>");

            // COFINS
            var cstCofins = fiscal?.CstCofins ?? "49";
            sb.Append("<COFINS>");
            if (cstCofins == "01" || cstCofins == "02")
            {
                sb.Append("<COFINSAliq>");
                sb.Append($"<CST>{cstCofins}</CST>");
                sb.Append($"<vBC>{D2(valorBruto - valorDesc)}</vBC>");
                sb.Append($"<pCOFINS>{D2(fiscal?.AliquotaCofins ?? 0)}</pCOFINS>");
                sb.Append($"<vCOFINS>{D2(Math.Round((valorBruto - valorDesc) * (fiscal?.AliquotaCofins ?? 0) / 100, 2))}</vCOFINS>");
                sb.Append("</COFINSAliq>");
            }
            else if (cstCofins == "04" || cstCofins == "05" || cstCofins == "06" || cstCofins == "07" || cstCofins == "08" || cstCofins == "09")
            {
                sb.Append("<COFINSNT>");
                sb.Append($"<CST>{cstCofins}</CST>");
                sb.Append("</COFINSNT>");
            }
            else
            {
                sb.Append("<COFINSOutr>");
                sb.Append($"<CST>{cstCofins}</CST>");
                sb.Append("<vBC>0.00</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS>");
                sb.Append("</COFINSOutr>");
            }
            sb.Append("</COFINS>");

            sb.Append("</imposto>");
            sb.Append("</det>");
            nItem++;
        }

        // total
        sb.Append("<total><ICMSTot>");
        sb.Append("<vBC>0.00</vBC><vICMS>0.00</vICMS><vICMSDeson>0.00</vICMSDeson>");
        sb.Append("<vFCPUFDest>0.00</vFCPUFDest><vICMSUFDest>0.00</vICMSUFDest><vICMSUFRemet>0.00</vICMSUFRemet>");
        sb.Append("<vFCP>0.00</vFCP><vBCST>0.00</vBCST><vST>0.00</vST><vFCPST>0.00</vFCPST><vFCPSTRet>0.00</vFCPSTRet>");
        sb.Append($"<vProd>{D2(totalProdutos)}</vProd>");
        sb.Append("<vFrete>0.00</vFrete><vSeg>0.00</vSeg>");
        sb.Append($"<vDesc>{D2(totalDesconto)}</vDesc>");
        sb.Append("<vII>0.00</vII><vIPI>0.00</vIPI><vIPIDevol>0.00</vIPIDevol><vPIS>0.00</vPIS><vCOFINS>0.00</vCOFINS><vOutro>0.00</vOutro>");
        sb.Append($"<vNF>{D2(venda.TotalLiquido)}</vNF>");
        sb.Append($"<vTotTrib>{D2(totalTributos)}</vTotTrib>");
        sb.Append("</ICMSTot></total>");

        // transp
        sb.Append("<transp><modFrete>9</modFrete></transp>");

        // pag — REGRA SEFAZ: soma(vPag) = vNF + vTroco
        var pagamentos = venda.Pagamentos.Where(p => p.Valor > 0).ToList();
        var trocoReal = pagamentos.Sum(p => p.Troco);
        var totalNF = venda.TotalLiquido;

        // LOG DIAGNÓSTICO: listar todos os pagamentos e suas modalidades
        foreach (var p in pagamentos)
        {
            Log.Information("NFC-e Pagamento | VendaId={VendaId} | TipoPag={Nome} | Modalidade={Mod} | Valor={Valor} | CartaoTipo={CartaoTipo} | CartaoAut={CartaoAut}",
                venda.Id, p.TipoPagamento?.Nome, p.TipoPagamento?.Modalidade, p.Valor, p.CartaoTipo, p.CartaoAutorizacao ?? "(null)");
        }

        sb.Append("<pag>");
        if (pagamentos.Count == 1)
        {
            var pag = pagamentos[0];
            var tPag = ObterCodigoPagamentoCartao(pag);
            sb.Append($"<detPag><tPag>{tPag}</tPag><vPag>{D2(totalNF + trocoReal)}</vPag>");
            AppendCard(sb, pag);
            sb.Append("</detPag>");
        }
        else
        {
            var somaPag = pagamentos.Sum(p => p.Valor);
            decimal acumulado = 0;
            for (int i = 0; i < pagamentos.Count; i++)
            {
                var pag = pagamentos[i];
                var tPag = ObterCodigoPagamentoCartao(pag);
                decimal vPag;
                if (i == pagamentos.Count - 1)
                    vPag = totalNF + trocoReal - acumulado;
                else
                    vPag = Math.Round(pag.Valor / somaPag * (totalNF + trocoReal), 2);
                acumulado += vPag;
                sb.Append($"<detPag><tPag>{tPag}</tPag><vPag>{D2(vPag)}</vPag>");
                AppendCard(sb, pag);
                sb.Append("</detPag>");
            }
        }
        sb.Append($"<vTroco>{D2(trocoReal)}</vTroco>");
        sb.Append("</pag>");

        sb.Append("<infAdic><infCpl>Documento emitido por ZulexPharma ERP</infCpl></infAdic>");

        // infRespTec (obrigatório no PR e outros estados)
        sb.Append("<infRespTec>");
        sb.Append($"<CNPJ>{cnpj}</CNPJ>");
        sb.Append($"<xContato>{Esc(filial.RazaoSocial)}</xContato>");
        sb.Append($"<email>{filial.Email}</email>");
        var foneTec = filial.Telefone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        sb.Append($"<fone>{foneTec}</fone>");
        sb.Append("</infRespTec>");
        sb.Append("</infNFe>");
        sb.Append("</NFe>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers NFC-e: códigos de pagamento / card
    // ═══════════════════════════════════════════════════════════════════

    private static string ObterCodigoPagamento(ModalidadePagamento? mod) => mod switch
    {
        ModalidadePagamento.VendaVista => "01",
        ModalidadePagamento.VendaCartao => "03",
        ModalidadePagamento.VendaPix => "17",
        ModalidadePagamento.VendaPrazo => "05",
        _ => "99"
    };

    /// <summary>Retorna tPag considerando débito (03) vs crédito (04) quando é cartão.
    /// Se for VendaCartao SEM dados de cartão (autorização vazia), cai para "99" (Outros)
    /// para não gerar XML inválido que faz a SEFAZ rejeitar.</summary>
    private static string ObterCodigoPagamentoCartao(VendaPagamento pag)
    {
        if (pag.TipoPagamento?.Modalidade == ModalidadePagamento.VendaCartao)
        {
            // Proteção: se for cartão sem dados preenchidos, usa "99" (Outros)
            if (string.IsNullOrWhiteSpace(pag.CartaoAutorizacao)) return "99";
            return pag.CartaoTipo == 2 ? "04" : "03";
        }
        return ObterCodigoPagamento(pag.TipoPagamento?.Modalidade);
    }

    /// <summary>Gera o grupo card dentro de detPag.
    /// NT 2020.006: para cartão (03/04) E PIX (17), o grupo card é esperado com tpIntegra.
    /// tpIntegra=2 = pagamento não integrado (manual).</summary>
    private static void AppendCard(StringBuilder sb, VendaPagamento pag)
    {
        var mod = pag.TipoPagamento?.Modalidade;
        var isCartao = mod == ModalidadePagamento.VendaCartao;
        var isPix = mod == ModalidadePagamento.VendaPix;

        if (!isCartao && !isPix) return;

        // Cartão sem dados: não gera card (o tPag já foi ajustado para 99 em ObterCodigoPagamentoCartao)
        if (isCartao && string.IsNullOrWhiteSpace(pag.CartaoAutorizacao)) return;

        sb.Append("<card>");
        sb.Append("<tpIntegra>2</tpIntegra>");
        if (isCartao)
        {
            if (!string.IsNullOrWhiteSpace(pag.CartaoCnpjCredenciadora))
                sb.Append($"<CNPJ>{pag.CartaoCnpjCredenciadora.Replace(".", "").Replace("/", "").Replace("-", "")}</CNPJ>");
            if (!string.IsNullOrWhiteSpace(pag.CartaoBandeira))
                sb.Append($"<tBand>{pag.CartaoBandeira}</tBand>");
            if (!string.IsNullOrWhiteSpace(pag.CartaoAutorizacao))
                sb.Append($"<cAut>{Esc(pag.CartaoAutorizacao)}</cAut>");
        }
        // Para PIX, só <tpIntegra>2</tpIntegra> é suficiente
        sb.Append("</card>");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  XML Eventos
    // ═══════════════════════════════════════════════════════════════════

    private static string MontarXmlEventoCancelamento(string chaveAcesso, string cnpj, int ambiente,
        DateTime agora, int nSeqEvento, string protocolo, string justificativa)
    {
        var tpEvento = "110111";
        var id = $"ID{tpEvento}{chaveAcesso}{nSeqEvento:D2}";
        var ufCodigo = int.Parse(chaveAcesso[..2]);

        return $"<envEvento xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"1.00\">" +
            "<idLote>1</idLote>" +
            $"<evento versao=\"1.00\">" +
            $"<infEvento Id=\"{id}\">" +
            $"<cOrgao>{ufCodigo}</cOrgao>" +
            $"<tpAmb>{ambiente}</tpAmb>" +
            $"<CNPJ>{cnpj}</CNPJ>" +
            $"<chNFe>{chaveAcesso}</chNFe>" +
            $"<dhEvento>{agora:yyyy-MM-ddTHH:mm:sszzz}</dhEvento>" +
            $"<tpEvento>{tpEvento}</tpEvento>" +
            $"<nSeqEvento>{nSeqEvento}</nSeqEvento>" +
            "<verEvento>1.00</verEvento>" +
            "<detEvento versao=\"1.00\">" +
            "<descEvento>Cancelamento</descEvento>" +
            $"<nProt>{protocolo}</nProt>" +
            $"<xJust>{Esc(justificativa)}</xJust>" +
            "</detEvento></infEvento></evento></envEvento>";
    }

    private static string MontarXmlEventoCartaCorrecao(string chaveAcesso, string cnpj, int ambiente,
        DateTime agora, int nSeqEvento, string textoCorrecao)
    {
        var tpEvento = "110110";
        var id = $"ID{tpEvento}{chaveAcesso}{nSeqEvento:D2}";
        var ufCodigo = int.Parse(chaveAcesso[..2]);

        var xCondUso = "A Carta de Correcao e disciplinada pelo paragrafo 1o-A do art. 7o do Convenio S/N, " +
            "de 15 de dezembro de 1970 e pode ser utilizada para regularizacao de erro ocorrido na emissao de " +
            "documento fiscal, desde que o erro nao esteja relacionado com: I - as variaveis que determinam o " +
            "valor do imposto tais como: base de calculo, aliquota, diferenca de preco, quantidade, valor da " +
            "operacao ou da prestacao; II - a correcao de dados cadastrais que implique mudanca do remetente " +
            "ou do destinatario; III - a data de emissao ou de saida.";

        return $"<envEvento xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"1.00\">" +
            "<idLote>1</idLote>" +
            $"<evento versao=\"1.00\">" +
            $"<infEvento Id=\"{id}\">" +
            $"<cOrgao>{ufCodigo}</cOrgao>" +
            $"<tpAmb>{ambiente}</tpAmb>" +
            $"<CNPJ>{cnpj}</CNPJ>" +
            $"<chNFe>{chaveAcesso}</chNFe>" +
            $"<dhEvento>{agora:yyyy-MM-ddTHH:mm:sszzz}</dhEvento>" +
            $"<tpEvento>{tpEvento}</tpEvento>" +
            $"<nSeqEvento>{nSeqEvento}</nSeqEvento>" +
            "<verEvento>1.00</verEvento>" +
            "<detEvento versao=\"1.00\">" +
            "<descEvento>Carta de Correcao</descEvento>" +
            $"<xCorrecao>{Esc(textoCorrecao)}</xCorrecao>" +
            $"<xCondUso>{xCondUso}</xCondUso>" +
            "</detEvento></infEvento></evento></envEvento>";
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Assinatura
    // ═══════════════════════════════════════════════════════════════════

    private static string AssinarXml(string xml, X509Certificate2 cert)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        var signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        var reference = new Reference("#" + doc.GetElementsByTagName("infNFe")[0]!.Attributes!["Id"]!.Value);
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        signedXml.AddReference(reference);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        var infNFe = doc.GetElementsByTagName("infNFe")[0]!;
        infNFe.ParentNode!.InsertAfter(doc.ImportNode(signedXml.GetXml(), true), infNFe);
        return doc.OuterXml;
    }

    private static string AssinarXmlEvento(string xml, X509Certificate2 cert)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        var signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        var reference = new Reference("#" + doc.GetElementsByTagName("infEvento")[0]!.Attributes!["Id"]!.Value);
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        signedXml.AddReference(reference);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        var infEvento = doc.GetElementsByTagName("infEvento")[0]!;
        infEvento.ParentNode!.InsertAfter(doc.ImportNode(signedXml.GetXml(), true), infEvento);
        return doc.OuterXml;
    }

    private static string AssinarXmlInutilizacao(string xml, X509Certificate2 cert)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        var signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        var reference = new Reference("#" + doc.GetElementsByTagName("infInut")[0]!.Attributes!["Id"]!.Value);
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        signedXml.AddReference(reference);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        var infInut = doc.GetElementsByTagName("infInut")[0]!;
        infInut.ParentNode!.InsertAfter(doc.ImportNode(signedXml.GetXml(), true), infInut);
        return doc.OuterXml;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SOAP
    // ═══════════════════════════════════════════════════════════════════

    private static string MontarSoapEnvelope(string xmlAssinado)
    {
        return "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<soap12:Body>" +
            "<nfeDadosMsg xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4\">" +
            "<enviNFe xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\">" +
            "<idLote>1</idLote>" +
            "<indSinc>1</indSinc>" +
            xmlAssinado +
            "</enviNFe>" +
            "</nfeDadosMsg>" +
            "</soap12:Body>" +
            "</soap12:Envelope>";
    }

    private static string MontarSoapEnvelopeEvento(string xmlAssinado)
    {
        return "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<soap12:Body>" +
            "<nfeDadosMsg xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4\">" +
            xmlAssinado +
            "</nfeDadosMsg>" +
            "</soap12:Body>" +
            "</soap12:Envelope>";
    }

    private static string MontarSoapEnvelopeInutilizacao(string xmlAssinado)
    {
        return "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<soap12:Body>" +
            "<nfeDadosMsg xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeInutilizacao4\">" +
            xmlAssinado +
            "</nfeDadosMsg>" +
            "</soap12:Body>" +
            "</soap12:Envelope>";
    }

    private static async Task<string> EnviarSoap(string url, string soapXml, X509Certificate2 cert)
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        var content = new StringContent(soapXml, Encoding.UTF8, "application/soap+xml");
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Retorno SEFAZ
    // ═══════════════════════════════════════════════════════════════════

    private static RetornoSefaz ProcessarRetorno(string xmlRetorno)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlRetorno);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

        // Buscar primeiro no protNFe/infProt (status da nota individual)
        var cStat = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:cStat", ns)?.InnerText;
        var xMotivo = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:xMotivo", ns)?.InnerText;
        var nProt = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:nProt", ns)?.InnerText;

        // Se não encontrou no protNFe, pegar do retorno do lote (rejeição antes do processamento)
        if (string.IsNullOrEmpty(cStat))
        {
            cStat = doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText ?? "0";
            xMotivo = doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText ?? "";
        }

        return new RetornoSefaz
        {
            CodigoStatus = int.Parse(cStat ?? "0"),
            MotivoStatus = xMotivo ?? "",
            Protocolo = nProt,
            Autorizada = cStat == "100"
        };
    }

    private static VendaFiscalEventoResult ProcessarRetornoEvento(string xmlRetorno)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlRetorno);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

        var cStat = doc.SelectSingleNode("//nfe:infEvento/nfe:cStat", ns)?.InnerText
                 ?? doc.SelectSingleNode("//nfe:retInutNFe/nfe:infInut/nfe:cStat", ns)?.InnerText
                 ?? doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText ?? "0";
        var xMotivo = doc.SelectSingleNode("//nfe:infEvento/nfe:xMotivo", ns)?.InnerText
                    ?? doc.SelectSingleNode("//nfe:retInutNFe/nfe:infInut/nfe:xMotivo", ns)?.InnerText
                    ?? doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText ?? "";
        var nProt = doc.SelectSingleNode("//nfe:infEvento/nfe:nProt", ns)?.InnerText
                  ?? doc.SelectSingleNode("//nfe:retInutNFe/nfe:infInut/nfe:nProt", ns)?.InnerText;

        var statusCode = int.Parse(cStat);
        // 128=Lote processado, 135=Cancelamento OK, 135=CC-e OK, 102=Inutilização OK
        var sucesso = statusCode == 135 || statusCode == 128 || statusCode == 102;

        return new VendaFiscalEventoResult
        {
            Sucesso = sucesso,
            CodigoStatus = statusCode,
            MotivoStatus = xMotivo,
            Protocolo = nProt
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  URLs SEFAZ
    // ═══════════════════════════════════════════════════════════════════

    private static string ObterUrlAutorizacaoNfe(string uf, int ambiente) => uf.ToUpper() switch
    {
        "PR" => ambiente == 2 ? "https://homologacao.nfe.sefa.pr.gov.br/nfe/NFeAutorizacao4" : "https://nfe.sefa.pr.gov.br/nfe/NFeAutorizacao4",
        "SP" => ambiente == 2 ? "https://homologacao.nfe.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx" : "https://nfe.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx",
        _ => ambiente == 2 ? "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" : "https://nfe.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx"
    };

    private static string ObterUrlEventoNfe(string uf, int ambiente) => uf.ToUpper() switch
    {
        "PR" => ambiente == 2 ? "https://homologacao.nfe.sefa.pr.gov.br/nfe/NFeRecepcaoEvento4" : "https://nfe.sefa.pr.gov.br/nfe/NFeRecepcaoEvento4",
        _ => ambiente == 2 ? "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeRecepcaoEvento/NFeRecepcaoEvento4.asmx" : "https://nfe.svrs.rs.gov.br/ws/NfeRecepcaoEvento/NFeRecepcaoEvento4.asmx"
    };

    private static string ObterUrlInutilizacaoNfe(string uf, int ambiente) => uf.ToUpper() switch
    {
        "PR" => ambiente == 2 ? "https://homologacao.nfe.sefa.pr.gov.br/nfe/NFeInutilizacao4" : "https://nfe.sefa.pr.gov.br/nfe/NFeInutilizacao4",
        _ => ambiente == 2 ? "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeInutilizacao/NFeInutilizacao4.asmx" : "https://nfe.svrs.rs.gov.br/ws/NfeInutilizacao/NFeInutilizacao4.asmx"
    };

    private static string ObterUrlAutorizacaoNfce(string uf, int ambiente) => uf.ToUpper() switch
    {
        "SP" => ambiente == 2 ? "https://homologacao.nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx" : "https://nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx",
        "PR" => ambiente == 2 ? "https://homologacao.nfce.sefa.pr.gov.br/nfce/NFeAutorizacao4" : "https://nfce.sefa.pr.gov.br/nfce/NFeAutorizacao4",
        "RS" => ambiente == 2 ? "https://nfce-homologacao.sefazrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" : "https://nfce.sefazrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx",
        "MG" => ambiente == 2 ? "https://hnfce.fazenda.mg.gov.br/nfce/services/NFeAutorizacao4" : "https://nfce.fazenda.mg.gov.br/nfce/services/NFeAutorizacao4",
        "SC" => ambiente == 2 ? "https://nfce-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" : "https://nfce.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx",
        _ => ambiente == 2 ? "https://nfce-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" : "https://nfce.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx"
    };

    private static string ObterUrlQrCode(string uf, int ambiente) => uf.ToUpper() switch
    {
        "SP" => ambiente == 2 ? "https://homologacao.nfce.fazenda.sp.gov.br/NFCeConsultaPublica/Paginas/ConsultaQRCode.aspx" : "https://www.nfce.fazenda.sp.gov.br/NFCeConsultaPublica/Paginas/ConsultaQRCode.aspx",
        "PR" => "http://www.fazenda.pr.gov.br/nfce/qrcode",
        "SC" => ambiente == 2 ? "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx" : "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx",
        _ => ambiente == 2 ? "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx" : "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx"
    };

    private static string ObterUrlConsultaNfce(string uf, int ambiente) => uf.ToUpper() switch
    {
        "SP" => "https://www.nfce.fazenda.sp.gov.br/consulta",
        "PR" => "http://www.fazenda.pr.gov.br/nfce/consulta",
        _ => "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx"
    };

    // ═══════════════════════════════════════════════════════════════════
    //  Utilitários
    // ═══════════════════════════════════════════════════════════════════

    private static string D2(decimal v) => v.ToString("F2", CultureInfo.InvariantCulture);
    private static string D3(decimal v) => v.ToString("F3", CultureInfo.InvariantCulture);
    private static string D4(decimal v) => v.ToString("F4", CultureInfo.InvariantCulture);
    private static string Esc(string? s) => s == null ? "" : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string GerarChaveAcesso(int uf, DateTime data, string cnpj, int modelo, int serie, int numero, int tipoEmissao, int codigoNumerico)
    {
        var chave = $"{uf:D2}{data:yyMM}{cnpj}{modelo:D2}{serie:D3}{numero:D9}{tipoEmissao}{codigoNumerico:D8}";
        var digito = CalcularDigitoVerificador(chave);
        return chave + digito;
    }

    private static int CalcularDigitoVerificador(string chave)
    {
        int peso = 2, soma = 0;
        for (var i = chave.Length - 1; i >= 0; i--) { soma += (chave[i] - '0') * peso; peso = peso == 9 ? 2 : peso + 1; }
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static string GerarUrlQrCode(string chaveAcesso, int ambiente, string csc, string cscId, string uf)
    {
        var baseUrl = ObterUrlQrCode(uf, ambiente);

        // Verificar se o estado usa QR Code v3 (PR e outros)
        if (uf.ToUpper() == "PR")
        {
            // QR Code v3: URL?p=CHAVE|3|AMBIENTE
            return $"{baseUrl}?p={chaveAcesso}|3|{ambiente}";
        }

        // QR Code v2 (demais estados):
        // URL?p=CHAVE|2|AMBIENTE|IDCSC|HASH
        using var sha1 = SHA1.Create();
        var conteudoHash = $"{cscId}{csc}{chaveAcesso}";
        var hash = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(conteudoHash))).ToLower();
        return $"{baseUrl}?p={chaveAcesso}|2|{ambiente}|{cscId}|{hash}";
    }

    private static int ObterCodigoUf(string uf) => uf.ToUpper() switch
    {
        "AC" => 12, "AL" => 27, "AP" => 16, "AM" => 13, "BA" => 29, "CE" => 23,
        "DF" => 53, "ES" => 32, "GO" => 52, "MA" => 21, "MT" => 51, "MS" => 50,
        "MG" => 31, "PA" => 15, "PB" => 25, "PR" => 41, "PE" => 26, "PI" => 22,
        "RJ" => 33, "RN" => 24, "RS" => 43, "RO" => 11, "RR" => 14, "SC" => 42,
        "SP" => 35, "SE" => 28, "TO" => 17, _ => 42
    };

    // ═══════════════════════════════════════════════════════════════════
    //  Builders / Recalc
    // ═══════════════════════════════════════════════════════════════════

    private static VendaItemFiscal BuildVendaItemFiscal(VendaItemFiscalFormDto dto, int numeroItem)
    {
        // cProd SEFAZ: obrigatório, min 1 char não-espaço. Fallback: ProdutoId se vazio.
        var codigoProduto = string.IsNullOrWhiteSpace(dto.CodigoProduto)
            ? dto.ProdutoId.ToString()
            : dto.CodigoProduto.Trim();

        // CFOP SEFAZ: pattern '[1,2,3,5,6,7]{1}[0-9]{3}'. Fallback 5102 (venda dentro UF)
        // quando o cadastro do produto não trouxe CFOP — operador deve ajustar na natureza de operação.
        var cfop = string.IsNullOrWhiteSpace(dto.Cfop) ? "5102" : dto.Cfop.Trim();

        // CST PIS/COFINS: enumeração [49,50..56,60..67,70,71,90,98,99]. Fallback "49" (outras op.)
        var cstPis = string.IsNullOrWhiteSpace(dto.CstPis) ? "49" : dto.CstPis.Trim();
        var cstCofins = string.IsNullOrWhiteSpace(dto.CstCofins) ? "49" : dto.CstCofins.Trim();

        return new VendaItemFiscal
        {
            NumeroItem = numeroItem,
            CodigoProduto = codigoProduto,
            CodigoBarras = dto.CodigoBarras,
            DescricaoProduto = dto.DescricaoProduto,
            Ncm = dto.Ncm,
            Cest = dto.Cest,
            Cfop = cfop,
            Unidade = dto.Unidade,
            ValorFrete = dto.ValorFrete,
            ValorSeguro = dto.ValorSeguro,
            ValorOutros = dto.ValorOutros,
            IndicadorTotal = 1,

            CodigoAnvisa = dto.CodigoAnvisa,
            RastroLote = dto.RastroLote,
            RastroFabricacao = dto.RastroFabricacao,
            RastroValidade = dto.RastroValidade,
            RastroQuantidade = dto.RastroQuantidade,

            OrigemMercadoria = dto.OrigemMercadoria,
            CstIcms = dto.CstIcms,
            Csosn = dto.Csosn,
            ModBcIcms = dto.ModBcIcms,
            BaseIcms = dto.BaseIcms,
            AliquotaIcms = dto.AliquotaIcms,
            ValorIcms = dto.ValorIcms,
            PercentualReducaoBc = dto.PercentualReducaoBc,
            ValorIcmsDesonerado = dto.ValorIcmsDesonerado,
            MotivoDesoneracaoIcms = dto.MotivoDesoneracaoIcms,
            CodigoBeneficioFiscal = dto.CodigoBeneficioFiscal,

            ModBcIcmsSt = dto.ModBcIcmsSt,
            MvaSt = dto.MvaSt,
            BaseIcmsSt = dto.BaseIcmsSt,
            AliquotaIcmsSt = dto.AliquotaIcmsSt,
            ValorIcmsSt = dto.ValorIcmsSt,

            BaseFcp = dto.BaseFcp,
            AliquotaFcp = dto.AliquotaFcp,
            ValorFcp = dto.ValorFcp,
            BaseFcpSt = dto.BaseFcpSt,
            AliquotaFcpSt = dto.AliquotaFcpSt,
            ValorFcpSt = dto.ValorFcpSt,

            CstPis = cstPis,
            BasePis = dto.BasePis,
            AliquotaPis = dto.AliquotaPis,
            ValorPis = dto.ValorPis,

            CstCofins = cstCofins,
            BaseCofins = dto.BaseCofins,
            AliquotaCofins = dto.AliquotaCofins,
            ValorCofins = dto.ValorCofins,

            CstIpi = dto.CstIpi,
            EnquadramentoIpi = dto.EnquadramentoIpi,
            BaseIpi = dto.BaseIpi,
            AliquotaIpi = dto.AliquotaIpi,
            ValorIpi = dto.ValorIpi,

            ValorTotalTributos = dto.ValorTotalTributos
        };
    }

    private static void RecalcularTotaisFiscais(VendaFiscal vf, Venda venda)
    {
        var fiscais = venda.Itens.Where(i => i.Fiscal != null).Select(i => i.Fiscal!).ToList();
        vf.ValorProdutos = venda.Itens.Sum(i => Math.Round((decimal)i.Quantidade * i.PrecoUnitario, 2));
        vf.ValorDesconto = venda.Itens.Sum(i => i.ValorDesconto);
        vf.ValorFrete = fiscais.Sum(f => f.ValorFrete);
        vf.ValorSeguro = fiscais.Sum(f => f.ValorSeguro);
        vf.ValorOutros = fiscais.Sum(f => f.ValorOutros);
        vf.ValorIcms = fiscais.Sum(f => f.ValorIcms);
        vf.ValorIcmsSt = fiscais.Sum(f => f.ValorIcmsSt);
        vf.ValorIpi = fiscais.Sum(f => f.ValorIpi);
        vf.ValorPis = fiscais.Sum(f => f.ValorPis);
        vf.ValorCofins = fiscais.Sum(f => f.ValorCofins);
        vf.ValorTotalTributos = fiscais.Sum(f => f.ValorTotalTributos);
        vf.ValorNota = vf.ValorProdutos - vf.ValorDesconto + vf.ValorFrete
                     + vf.ValorSeguro + vf.ValorOutros + vf.ValorIcmsSt + vf.ValorIpi;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Mapping
    // ═══════════════════════════════════════════════════════════════════

    private static VendaFiscalDetalheDto MapToDetalhe(VendaFiscal vf)
    {
        var venda = vf.Venda;
        var destPessoa = venda.DestinatarioPessoa;

        return new VendaFiscalDetalheDto
        {
            Id = vf.VendaId,
            VendaId = vf.VendaId,
            Modelo = vf.Modelo,
            Numero = vf.Numero,
            Serie = vf.Serie,
            NatOp = vf.NatOp,
            DestinatarioNome = destPessoa?.RazaoSocial ?? destPessoa?.Nome,
            DestinatarioCpfCnpj = destPessoa?.CpfCnpj,
            DataEmissao = vf.DataEmissao,
            ValorNota = vf.ValorNota,
            Status = venda.StatusFiscal,
            ChaveAcesso = vf.ChaveAcesso,
            TipoNf = vf.TipoNf,
            Finalidade = vf.Finalidade,
            TipoOperacao = venda.TipoOperacao,
            CriadoEm = vf.CriadoEm,

            FilialId = venda.FilialId,
            NaturezaOperacaoId = vf.NaturezaOperacaoId,
            NaturezaOperacaoDescricao = vf.NaturezaOperacao?.Descricao ?? string.Empty,
            DestinatarioPessoaId = venda.DestinatarioPessoaId,
            Protocolo = vf.Protocolo,
            DataAutorizacao = vf.DataAutorizacao,
            DataSaidaEntrada = vf.DataSaidaEntrada,
            Ambiente = vf.Ambiente,
            IdentificadorDestino = vf.IdentificadorDestino,
            CodigoStatus = vf.CodigoStatus,
            MotivoStatus = vf.MotivoStatus,

            ModFrete = vf.ModFrete,
            TransportadoraPessoaId = vf.TransportadoraPessoaId,
            TransportadoraNome = vf.TransportadoraPessoa?.Nome,
            PlacaVeiculo = vf.PlacaVeiculo,
            UfVeiculo = vf.UfVeiculo,
            VolumeQuantidade = vf.VolumeQuantidade,
            VolumeEspecie = vf.VolumeEspecie,
            VolumePesoLiquido = vf.VolumePesoLiquido,
            VolumePesoBruto = vf.VolumePesoBruto,

            ValorProdutos = vf.ValorProdutos,
            ValorDesconto = vf.ValorDesconto,
            ValorFrete = vf.ValorFrete,
            ValorSeguro = vf.ValorSeguro,
            ValorOutros = vf.ValorOutros,
            ValorIcms = vf.ValorIcms,
            ValorIcmsSt = vf.ValorIcmsSt,
            ValorIpi = vf.ValorIpi,
            ValorPis = vf.ValorPis,
            ValorCofins = vf.ValorCofins,
            ValorTotalTributos = vf.ValorTotalTributos,

            ChaveNfeReferenciada = vf.ChaveNfeReferenciada,
            Observacao = vf.Observacao,

            XmlEnvio = vf.XmlEnvio,
            XmlRetorno = vf.XmlRetorno,
            XmlCancelamento = vf.XmlCancelamento,
            XmlCartaCorrecao = vf.XmlCartaCorrecao,

            Itens = venda.Itens
                .Where(i => i.Fiscal != null)
                .OrderBy(i => i.Fiscal!.NumeroItem)
                .Select(i => new VendaItemFiscalDto
                {
                    Id = i.Fiscal!.Id,
                    VendaItemId = i.Id,
                    NumeroItem = i.Fiscal.NumeroItem,
                    ProdutoId = i.ProdutoId,
                    CodigoProduto = i.Fiscal.CodigoProduto,
                    CodigoBarras = i.Fiscal.CodigoBarras,
                    DescricaoProduto = i.Fiscal.DescricaoProduto,
                    Ncm = i.Fiscal.Ncm,
                    Cest = i.Fiscal.Cest,
                    Cfop = i.Fiscal.Cfop,
                    Unidade = i.Fiscal.Unidade,
                    Quantidade = i.Quantidade,
                    ValorUnitario = i.PrecoUnitario,
                    ValorTotal = Math.Round((decimal)i.Quantidade * i.PrecoUnitario, 2),
                    ValorDesconto = i.ValorDesconto,
                    ValorFrete = i.Fiscal.ValorFrete,
                    ValorSeguro = i.Fiscal.ValorSeguro,
                    ValorOutros = i.Fiscal.ValorOutros,

                    CodigoAnvisa = i.Fiscal.CodigoAnvisa,
                    RastroLote = i.Fiscal.RastroLote,
                    RastroFabricacao = i.Fiscal.RastroFabricacao,
                    RastroValidade = i.Fiscal.RastroValidade,
                    RastroQuantidade = i.Fiscal.RastroQuantidade,

                    OrigemMercadoria = i.Fiscal.OrigemMercadoria,
                    CstIcms = i.Fiscal.CstIcms,
                    Csosn = i.Fiscal.Csosn,
                    BaseIcms = i.Fiscal.BaseIcms,
                    AliquotaIcms = i.Fiscal.AliquotaIcms,
                    ValorIcms = i.Fiscal.ValorIcms,
                    PercentualReducaoBc = i.Fiscal.PercentualReducaoBc,
                    CodigoBeneficioFiscal = i.Fiscal.CodigoBeneficioFiscal,

                    MvaSt = i.Fiscal.MvaSt,
                    BaseIcmsSt = i.Fiscal.BaseIcmsSt,
                    AliquotaIcmsSt = i.Fiscal.AliquotaIcmsSt,
                    ValorIcmsSt = i.Fiscal.ValorIcmsSt,

                    BaseFcp = i.Fiscal.BaseFcp,
                    AliquotaFcp = i.Fiscal.AliquotaFcp,
                    ValorFcp = i.Fiscal.ValorFcp,

                    CstPis = i.Fiscal.CstPis,
                    BasePis = i.Fiscal.BasePis,
                    AliquotaPis = i.Fiscal.AliquotaPis,
                    ValorPis = i.Fiscal.ValorPis,

                    CstCofins = i.Fiscal.CstCofins,
                    BaseCofins = i.Fiscal.BaseCofins,
                    AliquotaCofins = i.Fiscal.AliquotaCofins,
                    ValorCofins = i.Fiscal.ValorCofins,

                    CstIpi = i.Fiscal.CstIpi,
                    EnquadramentoIpi = i.Fiscal.EnquadramentoIpi,
                    BaseIpi = i.Fiscal.BaseIpi,
                    AliquotaIpi = i.Fiscal.AliquotaIpi,
                    ValorIpi = i.Fiscal.ValorIpi,

                    ValorTotalTributos = i.Fiscal.ValorTotalTributos,
                    CustoUnitario = i.Fiscal.CustoUnitario
                })
                .ToList()
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DTO interno
    // ═══════════════════════════════════════════════════════════════════

    private class RetornoSefaz
    {
        public int CodigoStatus { get; set; }
        public string? MotivoStatus { get; set; }
        public string? Protocolo { get; set; }
        public bool Autorizada { get; set; }
    }
}
