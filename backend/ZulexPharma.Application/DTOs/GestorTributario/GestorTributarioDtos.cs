namespace ZulexPharma.Application.DTOs.GestorTributario;

// ══ Dados fiscais normalizados (agnóstico de provider) ═════════════
public class ProdutoFiscalExternoDto
{
    public string Ean { get; set; } = "";
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string? Cfop { get; set; }
    public string? ExTipi { get; set; }
    public string? Origem { get; set; }

    // ICMS
    public string? Csosn { get; set; }
    public string? CstIcms { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal AliquotaFcp { get; set; }
    public string? ModBc { get; set; }
    public decimal PercentualReducaoBc { get; set; }
    public string? CodigoBeneficio { get; set; }
    public string? DispositivoLegalIcms { get; set; }

    // ST
    public bool TemSubstituicaoTributaria { get; set; }
    public decimal MvaOriginal { get; set; }
    public decimal MvaAjustado4 { get; set; }
    public decimal MvaAjustado7 { get; set; }
    public decimal MvaAjustado12 { get; set; }
    public decimal AliquotaIcmsSt { get; set; }
    public decimal AliquotaFcpSt { get; set; }
    public decimal AliquotaIcmsInternoEntrada { get; set; }
    /// <summary>Dicionário UF → alíquota retornado pela Avant (icms_entrada.por_uf). Usado para extrair o valor da filial.</summary>
    public Dictionary<string, decimal>? IcmsEntradaPorUf { get; set; }

    // PIS/COFINS
    public string? CstPis { get; set; }
    public decimal AliquotaPis { get; set; }
    public string? CstPisEntrada { get; set; }
    public string? CstCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    public string? CstCofinsEntrada { get; set; }
    public string? NaturezaReceita { get; set; }

    // IPI
    public string? CstIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
    public string? EnquadramentoIpi { get; set; }
    public string? CstIpiEntrada { get; set; }
    public decimal AliquotaIpiEntrada { get; set; }
    public decimal AliquotaIpiIndustria { get; set; }

    // Reforma Tributária 2026+
    public string? CstIs { get; set; }
    public string? ClassTribIs { get; set; }
    public decimal AliquotaIs { get; set; }
    public string? CstIbsCbs { get; set; }
    public string? ClassTribIbsCbs { get; set; }
    public decimal AliquotaIbsUf { get; set; }
    public decimal AliquotaIbsMun { get; set; }
    public decimal AliquotaCbs { get; set; }

    // Dados de descrição padronizada (informativo)
    public string? DescricaoPadronizada { get; set; }
    public bool Encontrado { get; set; } = true;
}

public class ProdutoRevisaoDto
{
    public long ProdutoId { get; set; }      // nosso id
    public string CodInterno { get; set; } = "";
    public string Ean { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? NcmAtual { get; set; }
    public string? CestAtual { get; set; }

    // Dados fiscais atuais do produto (opcionais — se não forem passados, defaults são usados)
    public string? ExTipiAtual { get; set; }
    public string? CfopAtual { get; set; }
    public string? CsosnAtual { get; set; }
    public string? CstAtual { get; set; }
    public decimal? PIcmsAtual { get; set; }
    public decimal? PFcpAtual { get; set; }
    public string? CstPisAtual { get; set; }
    public string? CstCofinsAtual { get; set; }
}

public class ResultadoRevisaoDto
{
    public int TotalEnviados { get; set; }
    public int TotalEncontrados { get; set; }
    public List<ProdutoFiscalExternoDto> Itens { get; set; } = new();
}

// ══ Config + Status ════════════════════════════════════════════════
public class GestorTributarioStatusDto
{
    public bool Configurado { get; set; }
    public bool Ativo { get; set; }
    public string? Provider { get; set; }
    public string? CnpjCliente { get; set; }
    public int? IdParceiro { get; set; }
    public bool TokenDefinido { get; set; }

    public int Ano { get; set; }
    public int Mes { get; set; }
    public int RequisicoesUsadas { get; set; }
    public int LimiteMensal { get; set; }
    public int RequisicoesDisponiveis => Math.Max(0, LimiteMensal - RequisicoesUsadas);
    public decimal PercentualUsado => LimiteMensal > 0
        ? Math.Round((decimal)RequisicoesUsadas / LimiteMensal * 100, 1)
        : 0;
    public string NivelAlerta =>
        PercentualUsado >= 95 ? "critico"
      : PercentualUsado >= 80 ? "atencao"
      : "ok";

    public DateTime? UltimaChamadaEm { get; set; }
}

// ══ Jobs ════════════════════════════════════════════════════════════
public class GestorTributarioJobDto
{
    public long Id { get; set; }
    public int Tipo { get; set; }
    public string TipoNome { get; set; } = "";
    public int Status { get; set; }
    public string StatusNome { get; set; } = "";
    public string Provider { get; set; } = "";
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int TotalItens { get; set; }
    public int ItensProcessados { get; set; }
    public int ItensAtualizados { get; set; }
    public int ItensNaoEncontrados { get; set; }
    public int ItensComErro { get; set; }
    public int RequisicoesUsadas { get; set; }
    public decimal Progresso => TotalItens > 0
        ? Math.Round((decimal)ItensProcessados / TotalItens * 100, 1)
        : 0;
    public string? MensagemErro { get; set; }
    public string? UsuarioNome { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class RevisarBaseRequest
{
    public long? FilialId { get; set; }
    public long? GrupoProdutoId { get; set; }
    public long? FabricanteId { get; set; }
    /// <summary>Se true, só revisa produtos sem NCM preenchido.</summary>
    public bool SomenteSemFiscal { get; set; }
    /// <summary>Se true, ignora a flag NaoAtualizarGestorTributario (força atualização).</summary>
    public bool ForcarAtualizacao { get; set; }
}

public class IniciarJobResponse
{
    public long JobId { get; set; }
    public string Mensagem { get; set; } = "";
}
