using ZulexPharma.Application.DTOs.FarmaciaPopular;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Cliente SOAP 1.1 RPC/encoded do WebService DATASUS Farmácia Popular.
/// Não faz nada de estado — só monta envelope, dispara HttpClient, parseia retorno.
/// A orquestração (persistência, status, retentativa) fica no FarmaciaPopularService.
/// </summary>
public interface IFarmaciaPopularSoapClient
{
    /// <summary>Fase 1 — executarSolicitacao. Obtém NuAutorizacao + subsídios por item.</summary>
    Task<SolicitacaoRetornoDto> ExecutarSolicitacaoAsync(SolicitacaoRequest req, CredenciaisFp cred, CancellationToken ct = default);

    /// <summary>Fase 2 — confirmarSolicitacao. Envia NuCupomFiscal da NFC-e emitida.</summary>
    Task<ConfirmacaoRetornoDto> ConfirmarSolicitacaoAsync(string coSolicitacaoFarmacia, string nuAutorizacao, string nuCupomFiscal, CredenciaisFp cred, CancellationToken ct = default);

    /// <summary>Fase 3 — receberMedicamento. Confirma entrega ao paciente.</summary>
    Task<ConfirmacaoRetornoDto> ReceberMedicamentoAsync(string coSolicitacaoFarmacia, string nuAutorizacao, CredenciaisFp cred, CancellationToken ct = default);

    /// <summary>Fase E — estornar. Aciona em caso de cancelamento pós-efetivação.</summary>
    Task<ConfirmacaoRetornoDto> EstornarAsync(string coSolicitacaoFarmacia, string nuAutorizacao, string motivo, CredenciaisFp cred, CancellationToken ct = default);
}

public class SolicitacaoRequest
{
    public string CoSolicitacaoFarmacia { get; set; } = string.Empty;
    public string NuCnpj { get; set; } = string.Empty;
    public string NuCpf { get; set; } = string.Empty;
    public string NuCrm { get; set; } = string.Empty;
    public string SgUfCrm { get; set; } = string.Empty;
    public DateOnly DtEmissaoReceita { get; set; }
    public string DnaEstacao { get; set; } = string.Empty;
    public List<MedicamentoDto> Medicamentos { get; set; } = new();
}

public class MedicamentoDto
{
    public string CoCodigoBarra { get; set; } = string.Empty;
    public decimal QtSolicitada { get; set; }
    public decimal VlPrecoVenda { get; set; }
    public decimal QtPrescrita { get; set; }
}

public class CredenciaisFp
{
    public string UsuarioFarmacia { get; set; } = string.Empty;
    public string SenhaFarmacia { get; set; } = string.Empty;
    public string UsuarioVendedor { get; set; } = string.Empty;
    public string SenhaVendedor { get; set; } = string.Empty;
}
