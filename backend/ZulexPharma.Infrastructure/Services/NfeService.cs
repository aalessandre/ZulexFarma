using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Nfe;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class NfeService : INfeService
{
    private readonly AppDbContext _db;

    public NfeService(AppDbContext db) => _db = db;

    // ═══════════════════════════════════════════════════════════════════
    //  CRUD
    // ═══════════════════════════════════════════════════════════════════

    public async Task<List<NfeListDto>> ListarAsync(long? filialId = null)
    {
        var query = _db.Nfes
            .Include(n => n.DestinatarioPessoa)
            .AsQueryable();

        if (filialId.HasValue)
            query = query.Where(n => n.FilialId == filialId.Value);

        return await query
            .OrderByDescending(n => n.CriadoEm)
            .Select(n => new NfeListDto
            {
                Id = n.Id,
                Codigo = n.Codigo,
                Numero = n.Numero,
                Serie = n.Serie,
                NatOp = n.NatOp,
                DestinatarioNome = n.DestinatarioPessoa != null ? n.DestinatarioPessoa.Nome : null,
                DestinatarioCpfCnpj = n.DestinatarioPessoa != null ? n.DestinatarioPessoa.CpfCnpj : null,
                DataEmissao = n.DataEmissao,
                ValorNota = n.ValorNota,
                Status = n.Status,
                ChaveAcesso = n.ChaveAcesso,
                TipoNf = n.TipoNf,
                FinalidadeNfe = n.FinalidadeNfe,
                CriadoEm = n.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<NfeDetalheDto> ObterAsync(long id)
    {
        var nfe = await _db.Nfes
            .Include(n => n.NaturezaOperacao).ThenInclude(no => no.Regras)
            .Include(n => n.DestinatarioPessoa).ThenInclude(p => p!.Enderecos)
            .Include(n => n.TransportadoraPessoa)
            .Include(n => n.Itens)
            .Include(n => n.Parcelas)
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException("NF-e não encontrada.");

        return MapToDetalhe(nfe);
    }

    public async Task<NfeListDto> CriarRascunhoAsync(NfeFormDto dto)
    {
        var natOp = await _db.Set<NaturezaOperacao>().FindAsync(dto.NaturezaOperacaoId)
            ?? throw new KeyNotFoundException("Natureza de operação não encontrada.");

        var nfe = new Nfe
        {
            FilialId = dto.FilialId,
            NaturezaOperacaoId = dto.NaturezaOperacaoId,
            DestinatarioPessoaId = dto.DestinatarioPessoaId,
            NatOp = natOp.Descricao,
            TipoNf = natOp.TipoNf,
            FinalidadeNfe = (FinalidadeNfe)natOp.FinalidadeNfe,
            IdentificadorDestino = natOp.IdentificadorDestino,
            DataEmissao = DataHoraHelper.Agora(),
            DataSaidaEntrada = dto.DataSaidaEntrada,
            Status = NfeStatus.Rascunho,
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

            // Cobranca
            NumeroFatura = dto.NumeroFatura,
            ValorOriginalFatura = dto.ValorOriginalFatura,
            ValorLiquidoFatura = dto.ValorLiquidoFatura,
        };

        int nItem = 1;
        foreach (var itemDto in dto.Itens)
        {
            var valorTotal = Math.Round(itemDto.Quantidade * itemDto.ValorUnitario, 2);
            nfe.Itens.Add(new NfeItem
            {
                NumeroItem = nItem++,
                ProdutoId = itemDto.ProdutoId,
                ProdutoLoteId = itemDto.ProdutoLoteId,
                CodigoProduto = itemDto.CodigoProduto,
                CodigoBarras = itemDto.CodigoBarras,
                DescricaoProduto = itemDto.DescricaoProduto,
                Ncm = itemDto.Ncm,
                Cest = itemDto.Cest,
                Cfop = itemDto.Cfop,
                Unidade = itemDto.Unidade,
                Quantidade = itemDto.Quantidade,
                ValorUnitario = itemDto.ValorUnitario,
                ValorTotal = valorTotal,
                ValorDesconto = itemDto.ValorDesconto,
                ValorFrete = itemDto.ValorFrete,
                ValorSeguro = itemDto.ValorSeguro,
                ValorOutros = itemDto.ValorOutros,

                CodigoAnvisa = itemDto.CodigoAnvisa,
                RastroLote = itemDto.RastroLote,
                RastroFabricacao = itemDto.RastroFabricacao,
                RastroValidade = itemDto.RastroValidade,
                RastroQuantidade = itemDto.RastroQuantidade,

                OrigemMercadoria = itemDto.OrigemMercadoria,
                CstIcms = itemDto.CstIcms,
                Csosn = itemDto.Csosn,
                ModBcIcms = itemDto.ModBcIcms,
                BaseIcms = itemDto.BaseIcms,
                AliquotaIcms = itemDto.AliquotaIcms,
                ValorIcms = itemDto.ValorIcms,
                PercentualReducaoBc = itemDto.PercentualReducaoBc,
                ValorIcmsDesonerado = itemDto.ValorIcmsDesonerado,
                MotivoDesoneracaoIcms = itemDto.MotivoDesoneracaoIcms,
                CodigoBeneficioFiscal = itemDto.CodigoBeneficioFiscal,

                ModBcIcmsSt = itemDto.ModBcIcmsSt,
                MvaSt = itemDto.MvaSt,
                BaseIcmsSt = itemDto.BaseIcmsSt,
                AliquotaIcmsSt = itemDto.AliquotaIcmsSt,
                ValorIcmsSt = itemDto.ValorIcmsSt,

                BaseFcp = itemDto.BaseFcp,
                AliquotaFcp = itemDto.AliquotaFcp,
                ValorFcp = itemDto.ValorFcp,
                BaseFcpSt = itemDto.BaseFcpSt,
                AliquotaFcpSt = itemDto.AliquotaFcpSt,
                ValorFcpSt = itemDto.ValorFcpSt,

                CstPis = itemDto.CstPis,
                BasePis = itemDto.BasePis,
                AliquotaPis = itemDto.AliquotaPis,
                ValorPis = itemDto.ValorPis,

                CstCofins = itemDto.CstCofins,
                BaseCofins = itemDto.BaseCofins,
                AliquotaCofins = itemDto.AliquotaCofins,
                ValorCofins = itemDto.ValorCofins,

                CstIpi = itemDto.CstIpi,
                EnquadramentoIpi = itemDto.EnquadramentoIpi,
                BaseIpi = itemDto.BaseIpi,
                AliquotaIpi = itemDto.AliquotaIpi,
                ValorIpi = itemDto.ValorIpi,

                ValorTotalTributos = itemDto.ValorTotalTributos,
            });
        }

        foreach (var parcDto in dto.Parcelas)
        {
            nfe.Parcelas.Add(new NfeParcela
            {
                NumeroParcela = parcDto.NumeroParcela,
                DataVencimento = parcDto.DataVencimento,
                Valor = parcDto.Valor,
            });
        }

        RecalcularTotais(nfe);
        _db.Nfes.Add(nfe);
        await _db.SaveChangesAsync();

        return new NfeListDto
        {
            Id = nfe.Id, Codigo = nfe.Codigo, Numero = nfe.Numero, Serie = nfe.Serie,
            NatOp = nfe.NatOp, DataEmissao = nfe.DataEmissao, ValorNota = nfe.ValorNota,
            Status = nfe.Status, ChaveAcesso = nfe.ChaveAcesso, TipoNf = nfe.TipoNf,
            FinalidadeNfe = nfe.FinalidadeNfe, CriadoEm = nfe.CriadoEm
        };
    }

    public async Task AtualizarRascunhoAsync(long id, NfeFormDto dto)
    {
        var nfe = await _db.Nfes
            .Include(n => n.Itens)
            .Include(n => n.Parcelas)
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException("NF-e não encontrada.");

        if (nfe.Status != NfeStatus.Rascunho)
            throw new InvalidOperationException("Somente NF-e em rascunho pode ser alterada.");

        var natOp = await _db.Set<NaturezaOperacao>().FindAsync(dto.NaturezaOperacaoId)
            ?? throw new KeyNotFoundException("Natureza de operação não encontrada.");

        nfe.FilialId = dto.FilialId;
        nfe.NaturezaOperacaoId = dto.NaturezaOperacaoId;
        nfe.DestinatarioPessoaId = dto.DestinatarioPessoaId;
        nfe.NatOp = natOp.Descricao;
        nfe.TipoNf = natOp.TipoNf;
        nfe.FinalidadeNfe = (FinalidadeNfe)natOp.FinalidadeNfe;
        nfe.IdentificadorDestino = natOp.IdentificadorDestino;
        nfe.DataSaidaEntrada = dto.DataSaidaEntrada;
        nfe.ChaveNfeReferenciada = dto.ChaveNfeReferenciada;
        nfe.Observacao = dto.Observacao;
        nfe.ModFrete = dto.ModFrete;
        nfe.TransportadoraPessoaId = dto.TransportadoraPessoaId;
        nfe.PlacaVeiculo = dto.PlacaVeiculo;
        nfe.UfVeiculo = dto.UfVeiculo;
        nfe.VolumeQuantidade = dto.VolumeQuantidade;
        nfe.VolumeEspecie = dto.VolumeEspecie;
        nfe.VolumePesoLiquido = dto.VolumePesoLiquido;
        nfe.VolumePesoBruto = dto.VolumePesoBruto;
        nfe.NumeroFatura = dto.NumeroFatura;
        nfe.ValorOriginalFatura = dto.ValorOriginalFatura;
        nfe.ValorLiquidoFatura = dto.ValorLiquidoFatura;
        nfe.AtualizadoEm = DataHoraHelper.Agora();

        // Remove itens antigos
        _db.NfeItens.RemoveRange(nfe.Itens);
        nfe.Itens.Clear();

        int nItem = 1;
        foreach (var itemDto in dto.Itens)
        {
            var valorTotal = Math.Round(itemDto.Quantidade * itemDto.ValorUnitario, 2);
            nfe.Itens.Add(new NfeItem
            {
                NumeroItem = nItem++,
                ProdutoId = itemDto.ProdutoId,
                ProdutoLoteId = itemDto.ProdutoLoteId,
                CodigoProduto = itemDto.CodigoProduto,
                CodigoBarras = itemDto.CodigoBarras,
                DescricaoProduto = itemDto.DescricaoProduto,
                Ncm = itemDto.Ncm, Cest = itemDto.Cest, Cfop = itemDto.Cfop,
                Unidade = itemDto.Unidade, Quantidade = itemDto.Quantidade,
                ValorUnitario = itemDto.ValorUnitario, ValorTotal = valorTotal,
                ValorDesconto = itemDto.ValorDesconto, ValorFrete = itemDto.ValorFrete,
                ValorSeguro = itemDto.ValorSeguro, ValorOutros = itemDto.ValorOutros,
                CodigoAnvisa = itemDto.CodigoAnvisa,
                RastroLote = itemDto.RastroLote, RastroFabricacao = itemDto.RastroFabricacao,
                RastroValidade = itemDto.RastroValidade, RastroQuantidade = itemDto.RastroQuantidade,
                OrigemMercadoria = itemDto.OrigemMercadoria,
                CstIcms = itemDto.CstIcms, Csosn = itemDto.Csosn,
                ModBcIcms = itemDto.ModBcIcms, BaseIcms = itemDto.BaseIcms,
                AliquotaIcms = itemDto.AliquotaIcms, ValorIcms = itemDto.ValorIcms,
                PercentualReducaoBc = itemDto.PercentualReducaoBc,
                ValorIcmsDesonerado = itemDto.ValorIcmsDesonerado,
                MotivoDesoneracaoIcms = itemDto.MotivoDesoneracaoIcms,
                CodigoBeneficioFiscal = itemDto.CodigoBeneficioFiscal,
                ModBcIcmsSt = itemDto.ModBcIcmsSt, MvaSt = itemDto.MvaSt,
                BaseIcmsSt = itemDto.BaseIcmsSt, AliquotaIcmsSt = itemDto.AliquotaIcmsSt,
                ValorIcmsSt = itemDto.ValorIcmsSt,
                BaseFcp = itemDto.BaseFcp, AliquotaFcp = itemDto.AliquotaFcp, ValorFcp = itemDto.ValorFcp,
                BaseFcpSt = itemDto.BaseFcpSt, AliquotaFcpSt = itemDto.AliquotaFcpSt, ValorFcpSt = itemDto.ValorFcpSt,
                CstPis = itemDto.CstPis, BasePis = itemDto.BasePis,
                AliquotaPis = itemDto.AliquotaPis, ValorPis = itemDto.ValorPis,
                CstCofins = itemDto.CstCofins, BaseCofins = itemDto.BaseCofins,
                AliquotaCofins = itemDto.AliquotaCofins, ValorCofins = itemDto.ValorCofins,
                CstIpi = itemDto.CstIpi, EnquadramentoIpi = itemDto.EnquadramentoIpi,
                BaseIpi = itemDto.BaseIpi, AliquotaIpi = itemDto.AliquotaIpi, ValorIpi = itemDto.ValorIpi,
                ValorTotalTributos = itemDto.ValorTotalTributos,
            });
        }

        // Remove parcelas antigas
        _db.NfeParcelas.RemoveRange(nfe.Parcelas);
        nfe.Parcelas.Clear();

        foreach (var parcDto in dto.Parcelas)
        {
            nfe.Parcelas.Add(new NfeParcela
            {
                NumeroParcela = parcDto.NumeroParcela,
                DataVencimento = parcDto.DataVencimento,
                Valor = parcDto.Valor,
            });
        }

        RecalcularTotais(nfe);
        await _db.SaveChangesAsync();
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var nfe = await _db.Nfes
            .Include(n => n.Itens)
            .Include(n => n.Parcelas)
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException("NF-e não encontrada.");

        if (nfe.Status != NfeStatus.Rascunho)
            throw new InvalidOperationException("Somente NF-e em rascunho pode ser excluída.");

        _db.NfeItens.RemoveRange(nfe.Itens);
        _db.NfeParcelas.RemoveRange(nfe.Parcelas);
        _db.Nfes.Remove(nfe);
        await _db.SaveChangesAsync();
        return "NF-e excluída com sucesso.";
    }

    private static void RecalcularTotais(Nfe nfe)
    {
        nfe.ValorProdutos = nfe.Itens.Sum(i => i.ValorTotal);
        nfe.ValorDesconto = nfe.Itens.Sum(i => i.ValorDesconto);
        nfe.ValorFrete = nfe.Itens.Sum(i => i.ValorFrete);
        nfe.ValorSeguro = nfe.Itens.Sum(i => i.ValorSeguro);
        nfe.ValorOutros = nfe.Itens.Sum(i => i.ValorOutros);
        nfe.ValorIcms = nfe.Itens.Sum(i => i.ValorIcms);
        nfe.ValorIcmsSt = nfe.Itens.Sum(i => i.ValorIcmsSt);
        nfe.ValorIpi = nfe.Itens.Sum(i => i.ValorIpi);
        nfe.ValorPis = nfe.Itens.Sum(i => i.ValorPis);
        nfe.ValorCofins = nfe.Itens.Sum(i => i.ValorCofins);
        nfe.ValorTotalTributos = nfe.Itens.Sum(i => i.ValorTotalTributos);
        nfe.ValorNota = nfe.ValorProdutos - nfe.ValorDesconto + nfe.ValorFrete
                      + nfe.ValorSeguro + nfe.ValorOutros + nfe.ValorIcmsSt + nfe.ValorIpi;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EMISSÃO
    // ═══════════════════════════════════════════════════════════════════

    public async Task<NfeEmissaoResult> EmitirAsync(long nfeId)
    {
        var nfe = await _db.Nfes
            .Include(n => n.Itens)
            .Include(n => n.Parcelas)
            .Include(n => n.NaturezaOperacao).ThenInclude(no => no.Regras)
            .Include(n => n.DestinatarioPessoa).ThenInclude(p => p!.Enderecos)
            .FirstOrDefaultAsync(n => n.Id == nfeId)
            ?? throw new KeyNotFoundException("NF-e não encontrada.");

        if (nfe.Status != NfeStatus.Rascunho && nfe.Status != NfeStatus.Rejeitada)
            throw new InvalidOperationException("Somente NF-e em rascunho ou rejeitada pode ser emitida.");

        if (!nfe.Itens.Any())
            throw new ArgumentException("NF-e não possui itens.");

        var filial = await _db.Filiais.FindAsync(nfe.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        if (certDb.Validade <= DataHoraHelper.Agora())
            throw new ArgumentException("Certificado digital expirado.");

        // IBPTax para vTotTrib
        var prodIds = nfe.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _db.Produtos
            .Where(p => prodIds.Contains(p.Id)).Include(p => p.Ncm)
            .ToDictionaryAsync(p => p.Id);
        var ncms = nfe.Itens.Select(i => i.Ncm.Replace(".", "").PadRight(8, '0')[..8]).Distinct().ToList();
        var ibptDict = await _db.IbptTaxes
            .Where(x => ncms.Contains(x.Ncm) && x.Uf == filial.Uf && x.Tipo == 0)
            .GroupBy(x => x.Ncm).Select(g => g.First())
            .ToDictionaryAsync(x => x.Ncm);

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));
        var serie = int.Parse(configs.GetValueOrDefault("fiscal.nfe.serie", "1"));
        var regimeTributario = int.Parse(configs.GetValueOrDefault("fiscal.regime.tributario", "1"));

        var ultimoNumero = await _db.Nfes
            .Where(n => n.FilialId == filial.Id && n.Serie == serie && n.Status != NfeStatus.Rascunho)
            .MaxAsync(n => (int?)n.Numero) ?? 0;
        var numero = ultimoNumero + 1;

        var ufCodigo = ObterCodigoUf(filial.Uf);
        var agora = DataHoraHelper.Agora();
        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var codigoNumerico = new Random().Next(10000000, 99999999);
        var chaveAcesso = GerarChaveAcesso(ufCodigo, agora, cnpj, 55, serie, numero, 1, codigoNumerico);

        var xml = MontarXmlNfe(nfe, filial, chaveAcesso, numero, serie, ambiente, regimeTributario,
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

        Log.Information("NF-e Retorno | NfeId={NfeId} | cStat={CodStatus} | xMotivo={Motivo} | Autorizada={Auth}",
            nfeId, resultado.CodigoStatus, resultado.MotivoStatus, resultado.Autorizada);

        // Atualizar entidade
        nfe.Numero = numero;
        nfe.Serie = serie;
        nfe.ChaveAcesso = chaveAcesso;
        nfe.Protocolo = resultado.Protocolo;
        nfe.DataEmissao = agora;
        nfe.Ambiente = ambiente;
        nfe.CodigoStatus = resultado.CodigoStatus;
        nfe.MotivoStatus = resultado.MotivoStatus;
        nfe.XmlEnvio = xmlAssinado;
        nfe.XmlRetorno = xmlRetorno;
        nfe.Status = resultado.Autorizada ? NfeStatus.Autorizada : NfeStatus.Rejeitada;
        if (resultado.Autorizada)
            nfe.DataAutorizacao = agora;

        nfe.AtualizadoEm = agora;
        await _db.SaveChangesAsync();

        return new NfeEmissaoResult
        {
            NfeId = nfe.Id, Numero = numero, Serie = serie, ChaveAcesso = chaveAcesso,
            Protocolo = resultado.Protocolo, CodigoStatus = resultado.CodigoStatus,
            MotivoStatus = resultado.MotivoStatus, Autorizada = resultado.Autorizada
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  XML NF-e modelo 55 layout 4.00
    // ═══════════════════════════════════════════════════════════════════

    private string MontarXmlNfe(Nfe nfe, Filial filial, string chaveAcesso, int numero, int serie,
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
        sb.Append($"<natOp>{Esc(nfe.NatOp)}</natOp>");
        sb.Append("<mod>55</mod>");
        sb.Append($"<serie>{serie}</serie>");
        sb.Append($"<nNF>{numero}</nNF>");
        sb.Append($"<dhEmi>{agora:yyyy-MM-ddTHH:mm:sszzz}</dhEmi>");
        if (nfe.DataSaidaEntrada.HasValue)
            sb.Append($"<dhSaiEnt>{nfe.DataSaidaEntrada.Value:yyyy-MM-ddTHH:mm:sszzz}</dhSaiEnt>");
        sb.Append($"<tpNF>{nfe.TipoNf}</tpNF>");
        sb.Append($"<idDest>{nfe.IdentificadorDestino}</idDest>");
        sb.Append($"<cMunFG>{filial.CodigoIbgeMunicipio ?? "0000000"}</cMunFG>");
        sb.Append("<tpImp>1</tpImp>"); // DANFE Retrato
        sb.Append("<tpEmis>1</tpEmis>");
        sb.Append($"<cDV>{chaveAcesso[43]}</cDV>");
        sb.Append($"<tpAmb>{ambiente}</tpAmb>");
        sb.Append($"<finNFe>{(int)nfe.FinalidadeNfe}</finNFe>");
        sb.Append($"<indFinal>{nfe.NaturezaOperacao?.IndicadorFinalidade ?? 0}</indFinal>");
        sb.Append($"<indPres>{nfe.NaturezaOperacao?.IndicadorPresenca ?? 0}</indPres>");
        sb.Append("<procEmi>0</procEmi>");
        sb.Append("<verProc>ZulexPharma1.0</verProc>");
        sb.Append("</ide>");

        // ── NFref (se houver chave referenciada) ──
        if (!string.IsNullOrEmpty(nfe.ChaveNfeReferenciada))
        {
            sb.Append("<NFref><refNFe>");
            sb.Append(nfe.ChaveNfeReferenciada);
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
        AppendDestinatario(sb, nfe, ambiente);

        // ── det (itens) ──
        decimal sumVBc = 0, sumVIcms = 0, sumVBcSt = 0, sumVSt = 0, sumVProd = 0;
        decimal sumVFrete = 0, sumVSeg = 0, sumVDesc = 0, sumVOutro = 0, sumVIpi = 0;
        decimal sumVPis = 0, sumVCofins = 0, sumVFcp = 0, sumVFcpSt = 0;
        decimal sumVIcmsDeson = 0, sumVTotTrib = 0;

        int nItem = 1;
        foreach (var item in nfe.Itens.OrderBy(i => i.NumeroItem))
        {
            var ncmRaw = item.Ncm.Replace(".", "").PadRight(8, '0');
            if (ncmRaw.Length > 8) ncmRaw = ncmRaw[..8];

            decimal vTotTribItem = 0;
            var valorLiquido = item.ValorTotal - item.ValorDesconto;
            if (ibptDict.TryGetValue(ncmRaw, out var ibpt))
                vTotTribItem = Math.Round(valorLiquido * (ibpt.AliqNacional + ibpt.AliqEstadual + ibpt.AliqMunicipal) / 100, 2);

            // Atualizar o item com vTotTrib calculado se ainda zerado
            if (item.ValorTotalTributos == 0) item.ValorTotalTributos = vTotTribItem;

            var xProd = ambiente == 2 ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Esc(item.DescricaoProduto);

            sb.Append($"<det nItem=\"{nItem}\">");
            sb.Append("<prod>");
            sb.Append($"<cProd>{item.CodigoProduto}</cProd>");
            sb.Append($"<cEAN>{Esc(item.CodigoBarras)}</cEAN>");
            sb.Append($"<xProd>{xProd}</xProd>");
            sb.Append($"<NCM>{ncmRaw}</NCM>");
            if (!string.IsNullOrEmpty(item.Cest)) sb.Append($"<CEST>{item.Cest}</CEST>");
            sb.Append($"<CFOP>{item.Cfop}</CFOP>");
            sb.Append($"<uCom>{Esc(item.Unidade)}</uCom>");
            sb.Append($"<qCom>{D4(item.Quantidade)}</qCom>");
            sb.Append($"<vUnCom>{D4(item.ValorUnitario)}</vUnCom>");
            sb.Append($"<vProd>{D2(item.ValorTotal)}</vProd>");
            sb.Append($"<cEANTrib>{Esc(item.CodigoBarras)}</cEANTrib>");
            sb.Append($"<uTrib>{Esc(item.Unidade)}</uTrib>");
            sb.Append($"<qTrib>{D4(item.Quantidade)}</qTrib>");
            sb.Append($"<vUnTrib>{D4(item.ValorUnitario)}</vUnTrib>");
            if (item.ValorFrete > 0) sb.Append($"<vFrete>{D2(item.ValorFrete)}</vFrete>");
            if (item.ValorSeguro > 0) sb.Append($"<vSeg>{D2(item.ValorSeguro)}</vSeg>");
            if (item.ValorDesconto > 0) sb.Append($"<vDesc>{D2(item.ValorDesconto)}</vDesc>");
            if (item.ValorOutros > 0) sb.Append($"<vOutro>{D2(item.ValorOutros)}</vOutro>");
            sb.Append($"<indTot>{item.IndicadorTotal}</indTot>");

            // med (medicamento com código ANVISA)
            if (!string.IsNullOrWhiteSpace(item.CodigoAnvisa))
            {
                sb.Append("<med>");
                sb.Append($"<cProdANVISA>{Esc(item.CodigoAnvisa)}</cProdANVISA>");
                sb.Append("</med>");
            }

            // rastro (rastreabilidade de lote)
            if (!string.IsNullOrWhiteSpace(item.RastroLote))
            {
                sb.Append("<rastro>");
                sb.Append($"<nLote>{Esc(item.RastroLote)}</nLote>");
                sb.Append($"<qLote>{D4(item.RastroQuantidade ?? item.Quantidade)}</qLote>");
                if (item.RastroFabricacao.HasValue)
                    sb.Append($"<dFab>{item.RastroFabricacao.Value:yyyy-MM-dd}</dFab>");
                if (item.RastroValidade.HasValue)
                    sb.Append($"<dVal>{item.RastroValidade.Value:yyyy-MM-dd}</dVal>");
                sb.Append("</rastro>");
            }

            sb.Append("</prod>");

            // ── imposto ──
            sb.Append("<imposto>");
            sb.Append($"<vTotTrib>{D2(item.ValorTotalTributos)}</vTotTrib>");

            // ICMS
            sb.Append("<ICMS>");
            AppendIcms(sb, item, crt);
            sb.Append("</ICMS>");

            // IPI
            AppendIpi(sb, item);

            // PIS
            AppendPis(sb, item);

            // COFINS
            AppendCofins(sb, item);

            sb.Append("</imposto>");
            sb.Append("</det>");

            // Acumular totais
            if (item.IndicadorTotal == 1)
            {
                sumVProd += item.ValorTotal;
                sumVDesc += item.ValorDesconto;
                sumVFrete += item.ValorFrete;
                sumVSeg += item.ValorSeguro;
                sumVOutro += item.ValorOutros;
            }
            sumVBc += item.BaseIcms;
            sumVIcms += item.ValorIcms;
            sumVBcSt += item.BaseIcmsSt;
            sumVSt += item.ValorIcmsSt;
            sumVIpi += item.ValorIpi;
            sumVPis += item.ValorPis;
            sumVCofins += item.ValorCofins;
            sumVFcp += item.ValorFcp;
            sumVFcpSt += item.ValorFcpSt;
            sumVIcmsDeson += item.ValorIcmsDesonerado;
            sumVTotTrib += item.ValorTotalTributos;

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
        sb.Append($"<modFrete>{nfe.ModFrete}</modFrete>");
        if (nfe.TransportadoraPessoa != null)
        {
            var transp = nfe.TransportadoraPessoa;
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
        if (!string.IsNullOrEmpty(nfe.PlacaVeiculo))
        {
            sb.Append("<veicTransp>");
            sb.Append($"<placa>{nfe.PlacaVeiculo}</placa>");
            sb.Append($"<UF>{nfe.UfVeiculo ?? filial.Uf}</UF>");
            sb.Append("</veicTransp>");
        }
        if (nfe.VolumeQuantidade.HasValue && nfe.VolumeQuantidade.Value > 0)
        {
            sb.Append("<vol>");
            sb.Append($"<qVol>{nfe.VolumeQuantidade.Value}</qVol>");
            if (!string.IsNullOrEmpty(nfe.VolumeEspecie)) sb.Append($"<esp>{Esc(nfe.VolumeEspecie)}</esp>");
            if (nfe.VolumePesoLiquido.HasValue) sb.Append($"<pesoL>{D3(nfe.VolumePesoLiquido.Value)}</pesoL>");
            if (nfe.VolumePesoBruto.HasValue) sb.Append($"<pesoB>{D3(nfe.VolumePesoBruto.Value)}</pesoB>");
            sb.Append("</vol>");
        }
        sb.Append("</transp>");

        // ── cobr (cobrança / duplicatas) ──
        if (nfe.Parcelas.Any())
        {
            sb.Append("<cobr>");
            sb.Append("<fat>");
            sb.Append($"<nFat>{Esc(nfe.NumeroFatura ?? numero.ToString())}</nFat>");
            sb.Append($"<vOrig>{D2(nfe.ValorOriginalFatura ?? vNF)}</vOrig>");
            sb.Append($"<vDesc>{D2(nfe.ValorDesconto)}</vDesc>");
            sb.Append($"<vLiq>{D2(nfe.ValorLiquidoFatura ?? vNF)}</vLiq>");
            sb.Append("</fat>");
            foreach (var dup in nfe.Parcelas.OrderBy(p => p.NumeroParcela))
            {
                sb.Append("<dup>");
                sb.Append($"<nDup>{Esc(dup.NumeroParcela)}</nDup>");
                sb.Append($"<dVenc>{dup.DataVencimento:yyyy-MM-dd}</dVenc>");
                sb.Append($"<vDup>{D2(dup.Valor)}</vDup>");
                sb.Append("</dup>");
            }
            sb.Append("</cobr>");
        }

        // ── pag ──
        sb.Append("<pag>");
        if (nfe.Parcelas.Any())
        {
            // NF-e com duplicatas: tPag=90 (Sem Pagamento)
            sb.Append("<detPag><tPag>90</tPag><vPag>0.00</vPag></detPag>");
        }
        else
        {
            // NF-e sem duplicatas (ex: devolução, transferência): tPag=90
            sb.Append("<detPag><tPag>90</tPag><vPag>0.00</vPag></detPag>");
        }
        sb.Append("</pag>");

        // ── infAdic ──
        var infCpl = nfe.Observacao ?? nfe.NaturezaOperacao?.Observacao ?? "Documento emitido por ZulexPharma ERP";
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

    // ── Destinatário ──
    private static void AppendDestinatario(StringBuilder sb, Nfe nfe, int ambiente)
    {
        var dest = nfe.DestinatarioPessoa;
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
    private static void AppendIcms(StringBuilder sb, NfeItem item, int crt)
    {
        var orig = item.OrigemMercadoria;

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
    private static void AppendIpi(StringBuilder sb, NfeItem item)
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
    private static void AppendPis(StringBuilder sb, NfeItem item)
    {
        var cstPis = item.CstPis;
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
    private static void AppendCofins(StringBuilder sb, NfeItem item)
    {
        var cstCofins = item.CstCofins;
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
    //  EVENTOS
    // ═══════════════════════════════════════════════════════════════════

    public async Task<NfeEventoResult> CancelarAsync(long nfeId, string justificativa)
    {
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
            throw new ArgumentException("Justificativa deve ter no mínimo 15 caracteres.");

        var nfe = await _db.Nfes.FindAsync(nfeId)
            ?? throw new KeyNotFoundException("NF-e não encontrada.");

        if (nfe.Status != NfeStatus.Autorizada)
            throw new InvalidOperationException("Somente NF-e autorizada pode ser cancelada.");

        var filial = await _db.Filiais.FindAsync(nfe.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));

        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var agora = DataHoraHelper.Agora();
        var nSeqEvento = 1;

        var xmlEvento = MontarXmlEventoCancelamento(nfe.ChaveAcesso, cnpj, ambiente, agora, nSeqEvento, nfe.Protocolo!, justificativa);

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

        Log.Information("NF-e Cancelamento | NfeId={NfeId} | cStat={CodStatus} | xMotivo={Motivo}",
            nfeId, resultado.CodigoStatus, resultado.MotivoStatus);

        if (resultado.Sucesso)
        {
            nfe.Status = NfeStatus.Cancelada;
            nfe.XmlCancelamento = xmlAssinado;
            nfe.AtualizadoEm = agora;
            await _db.SaveChangesAsync();
        }

        resultado.XmlEvento = xmlAssinado;
        return resultado;
    }

    public async Task<NfeEventoResult> CartaCorrecaoAsync(long nfeId, string textoCorrecao)
    {
        if (string.IsNullOrWhiteSpace(textoCorrecao) || textoCorrecao.Length < 15)
            throw new ArgumentException("Texto da correção deve ter no mínimo 15 caracteres.");

        var nfe = await _db.Nfes.FindAsync(nfeId)
            ?? throw new KeyNotFoundException("NF-e não encontrada.");

        if (nfe.Status != NfeStatus.Autorizada)
            throw new InvalidOperationException("Somente NF-e autorizada pode receber carta de correção.");

        var filial = await _db.Filiais.FindAsync(nfe.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));

        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var agora = DataHoraHelper.Agora();

        // Conta quantas CC-e já existem para incrementar nSeqEvento
        var nSeqEvento = string.IsNullOrEmpty(nfe.XmlCartaCorrecao) ? 1 : 2; // Simplificado

        var xmlEvento = MontarXmlEventoCartaCorrecao(nfe.ChaveAcesso, cnpj, ambiente, agora, nSeqEvento, textoCorrecao);

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

        Log.Information("NF-e CC-e | NfeId={NfeId} | cStat={CodStatus} | xMotivo={Motivo}",
            nfeId, resultado.CodigoStatus, resultado.MotivoStatus);

        if (resultado.Sucesso)
        {
            nfe.XmlCartaCorrecao = xmlAssinado;
            nfe.AtualizadoEm = agora;
            await _db.SaveChangesAsync();
        }

        resultado.XmlEvento = xmlAssinado;
        return resultado;
    }

    public async Task<NfeEventoResult> InutilizarAsync(long filialId, int serie, int numInicial, int numFinal, string justificativa)
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
    //  Retorno
    // ═══════════════════════════════════════════════════════════════════

    private static RetornoSefaz ProcessarRetorno(string xmlRetorno)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlRetorno);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

        var cStat = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:cStat", ns)?.InnerText;
        var xMotivo = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:xMotivo", ns)?.InnerText;
        var nProt = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:nProt", ns)?.InnerText;

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

    private static NfeEventoResult ProcessarRetornoEvento(string xmlRetorno)
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

        return new NfeEventoResult
        {
            Sucesso = sucesso,
            CodigoStatus = statusCode,
            MotivoStatus = xMotivo,
            Protocolo = nProt
        };
    }

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

    private static int ObterCodigoUf(string uf) => uf.ToUpper() switch
    {
        "AC" => 12, "AL" => 27, "AP" => 16, "AM" => 13, "BA" => 29, "CE" => 23,
        "DF" => 53, "ES" => 32, "GO" => 52, "MA" => 21, "MT" => 51, "MS" => 50,
        "MG" => 31, "PA" => 15, "PB" => 25, "PR" => 41, "PE" => 26, "PI" => 22,
        "RJ" => 33, "RN" => 24, "RS" => 43, "RO" => 11, "RR" => 14, "SC" => 42,
        "SP" => 35, "SE" => 28, "TO" => 17, _ => 42
    };

    // ═══ SEFAZ URLs NF-e 55 ═══

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

    // ═══ Mapping ═══

    private static NfeDetalheDto MapToDetalhe(Nfe nfe)
    {
        return new NfeDetalheDto
        {
            Id = nfe.Id, Codigo = nfe.Codigo, Numero = nfe.Numero, Serie = nfe.Serie,
            NatOp = nfe.NatOp,
            DestinatarioNome = nfe.DestinatarioPessoa?.RazaoSocial ?? nfe.DestinatarioPessoa?.Nome,
            DestinatarioCpfCnpj = nfe.DestinatarioPessoa?.CpfCnpj,
            DataEmissao = nfe.DataEmissao, ValorNota = nfe.ValorNota,
            Status = nfe.Status, ChaveAcesso = nfe.ChaveAcesso,
            TipoNf = nfe.TipoNf, FinalidadeNfe = nfe.FinalidadeNfe,
            CriadoEm = nfe.CriadoEm,

            FilialId = nfe.FilialId,
            NaturezaOperacaoId = nfe.NaturezaOperacaoId,
            NaturezaOperacaoDescricao = nfe.NaturezaOperacao?.Descricao ?? string.Empty,
            DestinatarioPessoaId = nfe.DestinatarioPessoaId,
            Protocolo = nfe.Protocolo,
            DataAutorizacao = nfe.DataAutorizacao,
            DataSaidaEntrada = nfe.DataSaidaEntrada,
            Ambiente = nfe.Ambiente,
            IdentificadorDestino = nfe.IdentificadorDestino,
            CodigoStatus = nfe.CodigoStatus,
            MotivoStatus = nfe.MotivoStatus,

            ModFrete = nfe.ModFrete,
            TransportadoraPessoaId = nfe.TransportadoraPessoaId,
            TransportadoraNome = nfe.TransportadoraPessoa?.Nome,
            PlacaVeiculo = nfe.PlacaVeiculo, UfVeiculo = nfe.UfVeiculo,
            VolumeQuantidade = nfe.VolumeQuantidade, VolumeEspecie = nfe.VolumeEspecie,
            VolumePesoLiquido = nfe.VolumePesoLiquido, VolumePesoBruto = nfe.VolumePesoBruto,

            ValorProdutos = nfe.ValorProdutos, ValorDesconto = nfe.ValorDesconto,
            ValorFrete = nfe.ValorFrete, ValorSeguro = nfe.ValorSeguro,
            ValorOutros = nfe.ValorOutros, ValorIcms = nfe.ValorIcms,
            ValorIcmsSt = nfe.ValorIcmsSt, ValorIpi = nfe.ValorIpi,
            ValorPis = nfe.ValorPis, ValorCofins = nfe.ValorCofins,
            ValorTotalTributos = nfe.ValorTotalTributos,

            NumeroFatura = nfe.NumeroFatura,
            ValorOriginalFatura = nfe.ValorOriginalFatura,
            ValorLiquidoFatura = nfe.ValorLiquidoFatura,

            ChaveNfeReferenciada = nfe.ChaveNfeReferenciada,
            Observacao = nfe.Observacao,

            XmlEnvio = nfe.XmlEnvio, XmlRetorno = nfe.XmlRetorno,
            XmlCancelamento = nfe.XmlCancelamento, XmlCartaCorrecao = nfe.XmlCartaCorrecao,

            Itens = nfe.Itens.OrderBy(i => i.NumeroItem).Select(i => new NfeItemDto
            {
                Id = i.Id, NumeroItem = i.NumeroItem, ProdutoId = i.ProdutoId,
                ProdutoLoteId = i.ProdutoLoteId,
                CodigoProduto = i.CodigoProduto, CodigoBarras = i.CodigoBarras,
                DescricaoProduto = i.DescricaoProduto, Ncm = i.Ncm, Cest = i.Cest,
                Cfop = i.Cfop, Unidade = i.Unidade, Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario, ValorTotal = i.ValorTotal,
                ValorDesconto = i.ValorDesconto, ValorFrete = i.ValorFrete,
                ValorSeguro = i.ValorSeguro, ValorOutros = i.ValorOutros,
                CodigoAnvisa = i.CodigoAnvisa, RastroLote = i.RastroLote,
                RastroFabricacao = i.RastroFabricacao, RastroValidade = i.RastroValidade,
                RastroQuantidade = i.RastroQuantidade,
                OrigemMercadoria = i.OrigemMercadoria, CstIcms = i.CstIcms, Csosn = i.Csosn,
                BaseIcms = i.BaseIcms, AliquotaIcms = i.AliquotaIcms, ValorIcms = i.ValorIcms,
                PercentualReducaoBc = i.PercentualReducaoBc,
                CodigoBeneficioFiscal = i.CodigoBeneficioFiscal,
                MvaSt = i.MvaSt, BaseIcmsSt = i.BaseIcmsSt,
                AliquotaIcmsSt = i.AliquotaIcmsSt, ValorIcmsSt = i.ValorIcmsSt,
                BaseFcp = i.BaseFcp, AliquotaFcp = i.AliquotaFcp, ValorFcp = i.ValorFcp,
                CstPis = i.CstPis, BasePis = i.BasePis,
                AliquotaPis = i.AliquotaPis, ValorPis = i.ValorPis,
                CstCofins = i.CstCofins, BaseCofins = i.BaseCofins,
                AliquotaCofins = i.AliquotaCofins, ValorCofins = i.ValorCofins,
                CstIpi = i.CstIpi, EnquadramentoIpi = i.EnquadramentoIpi,
                BaseIpi = i.BaseIpi, AliquotaIpi = i.AliquotaIpi, ValorIpi = i.ValorIpi,
                ValorTotalTributos = i.ValorTotalTributos,
            }).ToList(),

            Parcelas = nfe.Parcelas.OrderBy(p => p.NumeroParcela).Select(p => new NfeParcelaDto
            {
                Id = p.Id, NumeroParcela = p.NumeroParcela,
                DataVencimento = p.DataVencimento, Valor = p.Valor
            }).ToList(),
        };
    }

    // ═══ DTO interno ═══
    private class RetornoSefaz
    {
        public int CodigoStatus { get; set; }
        public string? MotivoStatus { get; set; }
        public string? Protocolo { get; set; }
        public bool Autorizada { get; set; }
    }
}
