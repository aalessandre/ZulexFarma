using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.SelfCheckout;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services.SelfCheckout;

public class ErpConnectorFactory : IErpConnectorFactory
{
    private readonly AppDbContext _db;

    public ErpConnectorFactory(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IErpConnector?> CriarParaFilialAsync(long filialId, CancellationToken ct = default)
    {
        var cfg = await _db.SelfCheckoutConfiguracoes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.FilialId == filialId && c.Ativo, ct);

        if (cfg == null) return null;

        var senha = CriptografiaHelper.Decrypt(cfg.SenhaBancoCriptografada) ?? string.Empty;
        return Construir(cfg.ErpOrigem, cfg.HostBanco, cfg.NomeBanco, cfg.UsuarioBanco, senha,
            cfg.FilialErpOrigem, cfg.CodigoNaturezaOperacaoNfce);
    }

    public IErpConnector CriarTransiente(ConfiguracaoConexaoErpDto config)
    {
        // Teste de conexão / busca de produto não dependem da natureza configurada.
        return Construir(config.ErpOrigem, config.HostBanco, config.NomeBanco,
            config.UsuarioBanco, config.SenhaBanco, config.FilialErpOrigem, codigoNaturezaOperacaoNfce: null);
    }

    private static IErpConnector Construir(
        ErpOrigem erp, string host, string banco, string usuario, string senha,
        string filialExterna, int? codigoNaturezaOperacaoNfce)
    {
        return erp switch
        {
            ErpOrigem.Inovafarma => CriarInovafarma(host, banco, usuario, senha, filialExterna, codigoNaturezaOperacaoNfce),
            _ => throw new NotSupportedException($"ERP origem não suportado: {erp}")
        };
    }

    private static InovafarmaConnector CriarInovafarma(
        string host, string banco, string usuario, string senha, string filialExterna, int? codigoNaturezaOperacaoNfce)
    {
        var cs = new SqlConnectionStringBuilder
        {
            DataSource = host,
            InitialCatalog = banco,
            UserID = usuario,
            Password = senha,
            TrustServerCertificate = true,
            Encrypt = false,
            ConnectTimeout = 10
        }.ToString();

        if (!short.TryParse(filialExterna, out var codigoEmpresa))
            throw new InvalidOperationException(
                $"Filial do Inovafarma inválida: '{filialExterna}'. Use o CodigoEmpresa numérico (smallint).");

        return new InovafarmaConnector(cs, codigoEmpresa, codigoNaturezaOperacaoNfce);
    }
}
