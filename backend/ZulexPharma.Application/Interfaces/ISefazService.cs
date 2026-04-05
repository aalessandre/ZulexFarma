using ZulexPharma.Application.DTOs.Fiscal;

namespace ZulexPharma.Application.Interfaces;

public interface ISefazService
{
    Task<CertificadoInfoDto> UploadCertificadoAsync(CertificadoUploadRequest request);
    Task<CertificadoInfoDto?> ObterCertificadoAsync(long filialId);
    Task<ConsultaSefazResult> ConsultarNfePendentesAsync(long filialId);
    Task<ConsultaSefazResult> ConsultarPorChaveAsync(long filialId, string chaveNfe);
}
