using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Ncm;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class NcmService : INcmService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;

    private const string TELA = "NCM";
    private const string ENTIDADE = "Ncm";

    public NcmService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<NcmListDto>> ListarAsync(string? busca = null)
    {
        IQueryable<Ncm> query = _db.Ncms;

        if (!string.IsNullOrWhiteSpace(busca) && busca.Trim().Length >= 4)
        {
            var termo = busca.Trim().ToUpper();
            query = query.Where(n => n.CodigoNcm.Contains(termo) || n.Descricao.Contains(termo));
        }
        else
        {
            return new List<NcmListDto>();
        }

        return await query
            .OrderBy(n => n.CodigoNcm)
            .Take(200)
            .Select(n => new NcmListDto
            {
                Id = n.Id,
                CodigoNcm = n.CodigoNcm,
                Descricao = n.Descricao,
                ExTipi = n.ExTipi,
                UnidadeTributavel = n.UnidadeTributavel,
                Ativo = n.Ativo,
                CriadoEm = n.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<NcmDetalheDto> ObterAsync(long id)
    {
        var n = await _db.Ncms
            .Include(x => x.Federais)
            .Include(x => x.IcmsUfs)
            .Include(x => x.StUfs)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"NCM {id} não encontrado.");

        return new NcmDetalheDto
        {
            Id = n.Id,
            CodigoNcm = n.CodigoNcm,
            Descricao = n.Descricao,
            ExTipi = n.ExTipi,
            UnidadeTributavel = n.UnidadeTributavel,
            Ativo = n.Ativo,
            CriadoEm = n.CriadoEm,
            Federais = n.Federais.Select(f => new NcmFederalDto
            {
                Id = f.Id, AliquotaIi = f.AliquotaIi, AliquotaIpi = f.AliquotaIpi,
                CstIpi = f.CstIpi, AliquotaPis = f.AliquotaPis, CstPis = f.CstPis,
                AliquotaCofins = f.AliquotaCofins, CstCofins = f.CstCofins,
                VigenciaInicio = f.VigenciaInicio, VigenciaFim = f.VigenciaFim
            }).ToList(),
            IcmsUfs = n.IcmsUfs.OrderBy(i => i.Uf).Select(i => new NcmIcmsUfDto
            {
                Id = i.Id, Uf = i.Uf, CstIcms = i.CstIcms, Csosn = i.Csosn,
                AliquotaIcms = i.AliquotaIcms, ReducaoBaseCalculo = i.ReducaoBaseCalculo,
                AliquotaFcp = i.AliquotaFcp, Cbenef = i.Cbenef,
                VigenciaInicio = i.VigenciaInicio, VigenciaFim = i.VigenciaFim
            }).ToList(),
            StUfs = n.StUfs.OrderBy(s => s.UfOrigem).ThenBy(s => s.UfDestino).Select(s => new NcmStUfDto
            {
                Id = s.Id, UfOrigem = s.UfOrigem, UfDestino = s.UfDestino,
                Mva = s.Mva, MvaAjustado = s.MvaAjustado,
                AliquotaIcmsSt = s.AliquotaIcmsSt, ReducaoBaseCalculoSt = s.ReducaoBaseCalculoSt,
                Cest = s.Cest, VigenciaInicio = s.VigenciaInicio, VigenciaFim = s.VigenciaFim
            }).ToList()
        };
    }

    public async Task<NcmListDto> CriarAsync(NcmFormDto dto)
    {
        ValidarCodigo(dto.CodigoNcm);

        if (await _db.Ncms.AnyAsync(n => n.CodigoNcm == dto.CodigoNcm.Trim()))
            throw new ArgumentException("Código NCM já cadastrado.");

        var ncm = new Ncm
        {
            CodigoNcm = dto.CodigoNcm.Trim(),
            Descricao = dto.Descricao.Trim().ToUpper(),
            ExTipi = dto.ExTipi?.Trim(),
            UnidadeTributavel = dto.UnidadeTributavel?.Trim().ToUpper(),
            Ativo = dto.Ativo
        };

        _db.Ncms.Add(ncm);
        await _db.SaveChangesAsync();

        SincronizarSubTabelas(ncm, dto);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, ncm.Id,
            novo: NcmParaDict(ncm));

        return new NcmListDto
        {
            Id = ncm.Id, CodigoNcm = ncm.CodigoNcm, Descricao = ncm.Descricao,
            ExTipi = ncm.ExTipi, UnidadeTributavel = ncm.UnidadeTributavel,
            Ativo = ncm.Ativo, CriadoEm = ncm.CriadoEm
        };
    }

    public async Task AtualizarAsync(long id, NcmFormDto dto)
    {
        var ncm = await _db.Ncms
            .Include(n => n.Federais)
            .Include(n => n.IcmsUfs)
            .Include(n => n.StUfs)
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException($"NCM {id} não encontrado.");

        ValidarCodigo(dto.CodigoNcm);

        if (await _db.Ncms.AnyAsync(n => n.CodigoNcm == dto.CodigoNcm.Trim() && n.Id != id))
            throw new ArgumentException("Código NCM já cadastrado.");

        var anterior = NcmParaDict(ncm);

        ncm.CodigoNcm = dto.CodigoNcm.Trim();
        ncm.Descricao = dto.Descricao.Trim().ToUpper();
        ncm.ExTipi = dto.ExTipi?.Trim();
        ncm.UnidadeTributavel = dto.UnidadeTributavel?.Trim().ToUpper();
        ncm.Ativo = dto.Ativo;

        SincronizarSubTabelas(ncm, dto);
        await _db.SaveChangesAsync();

        var novo = NcmParaDict(ncm);
        if (!DictsIguais(anterior, novo))
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var ncm = await _db.Ncms.FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException($"NCM {id} não encontrado.");

        var dados = NcmParaDict(ncm);

        _db.Ncms.Remove(ncm);
        try
        {
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
            return "excluido";
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var recarregado = await _db.Ncms.FirstAsync(n => n.Id == id);
            recarregado.Ativo = false;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
            return "desativado";
        }
    }

    private void SincronizarSubTabelas(Ncm ncm, NcmFormDto dto)
    {
        // Federal
        var fedIds = dto.Federais.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();
        foreach (var f in ncm.Federais.Where(f => !fedIds.Contains(f.Id)).ToList())
            _db.NcmFederais.Remove(f);
        foreach (var d in dto.Federais)
        {
            if (d.Id.HasValue)
            {
                var e = ncm.Federais.FirstOrDefault(f => f.Id == d.Id.Value);
                if (e != null)
                {
                    e.AliquotaIi = d.AliquotaIi; e.AliquotaIpi = d.AliquotaIpi; e.CstIpi = d.CstIpi;
                    e.AliquotaPis = d.AliquotaPis; e.CstPis = d.CstPis;
                    e.AliquotaCofins = d.AliquotaCofins; e.CstCofins = d.CstCofins;
                    e.VigenciaInicio = d.VigenciaInicio; e.VigenciaFim = d.VigenciaFim;
                }
            }
            else
            {
                _db.NcmFederais.Add(new NcmFederal
                {
                    NcmId = ncm.Id, AliquotaIi = d.AliquotaIi, AliquotaIpi = d.AliquotaIpi,
                    CstIpi = d.CstIpi, AliquotaPis = d.AliquotaPis, CstPis = d.CstPis,
                    AliquotaCofins = d.AliquotaCofins, CstCofins = d.CstCofins,
                    VigenciaInicio = d.VigenciaInicio, VigenciaFim = d.VigenciaFim
                });
            }
        }

        // ICMS UF
        var icmsIds = dto.IcmsUfs.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        foreach (var i in ncm.IcmsUfs.Where(i => !icmsIds.Contains(i.Id)).ToList())
            _db.NcmIcmsUfs.Remove(i);
        foreach (var d in dto.IcmsUfs)
        {
            if (d.Id.HasValue)
            {
                var e = ncm.IcmsUfs.FirstOrDefault(i => i.Id == d.Id.Value);
                if (e != null)
                {
                    e.Uf = d.Uf.Trim().ToUpper(); e.CstIcms = d.CstIcms; e.Csosn = d.Csosn;
                    e.AliquotaIcms = d.AliquotaIcms; e.ReducaoBaseCalculo = d.ReducaoBaseCalculo;
                    e.AliquotaFcp = d.AliquotaFcp; e.Cbenef = d.Cbenef;
                    e.VigenciaInicio = d.VigenciaInicio; e.VigenciaFim = d.VigenciaFim;
                }
            }
            else
            {
                _db.NcmIcmsUfs.Add(new NcmIcmsUf
                {
                    NcmId = ncm.Id, Uf = d.Uf.Trim().ToUpper(), CstIcms = d.CstIcms, Csosn = d.Csosn,
                    AliquotaIcms = d.AliquotaIcms, ReducaoBaseCalculo = d.ReducaoBaseCalculo,
                    AliquotaFcp = d.AliquotaFcp, Cbenef = d.Cbenef,
                    VigenciaInicio = d.VigenciaInicio, VigenciaFim = d.VigenciaFim
                });
            }
        }

        // ST UF
        var stIds = dto.StUfs.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
        foreach (var s in ncm.StUfs.Where(s => !stIds.Contains(s.Id)).ToList())
            _db.NcmStUfs.Remove(s);
        foreach (var d in dto.StUfs)
        {
            if (d.Id.HasValue)
            {
                var e = ncm.StUfs.FirstOrDefault(s => s.Id == d.Id.Value);
                if (e != null)
                {
                    e.UfOrigem = d.UfOrigem.Trim().ToUpper(); e.UfDestino = d.UfDestino.Trim().ToUpper();
                    e.Mva = d.Mva; e.MvaAjustado = d.MvaAjustado;
                    e.AliquotaIcmsSt = d.AliquotaIcmsSt; e.ReducaoBaseCalculoSt = d.ReducaoBaseCalculoSt;
                    e.Cest = d.Cest; e.VigenciaInicio = d.VigenciaInicio; e.VigenciaFim = d.VigenciaFim;
                }
            }
            else
            {
                _db.NcmStUfs.Add(new NcmStUf
                {
                    NcmId = ncm.Id, UfOrigem = d.UfOrigem.Trim().ToUpper(),
                    UfDestino = d.UfDestino.Trim().ToUpper(),
                    Mva = d.Mva, MvaAjustado = d.MvaAjustado,
                    AliquotaIcmsSt = d.AliquotaIcmsSt, ReducaoBaseCalculoSt = d.ReducaoBaseCalculoSt,
                    Cest = d.Cest, VigenciaInicio = d.VigenciaInicio, VigenciaFim = d.VigenciaFim
                });
            }
        }
    }

    public async Task<object> ImportarCsvAsync(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo))
            throw new FileNotFoundException("Arquivo CSV não encontrado.", caminhoArquivo);

        var linhas = await File.ReadAllLinesAsync(caminhoArquivo);
        if (linhas.Length < 2)
            throw new ArgumentException("Arquivo CSV vazio ou sem dados.");

        // Pegar primeira filial existente
        var filialId = await _db.Filiais.Select(f => f.Id).FirstOrDefaultAsync();

        // Carregar códigos existentes para evitar duplicatas
        var codigosExistentes = (await _db.Ncms
            .Select(n => n.CodigoNcm)
            .ToListAsync())
            .ToHashSet();

        var inseridos = 0;
        var ignorados = 0;
        var erros = new List<string>();

        // Processar em lotes de 500
        var lote = new List<Ncm>();
        for (int i = 1; i < linhas.Length; i++)
        {
            var linha = linhas[i].Trim();
            if (string.IsNullOrEmpty(linha)) continue;

            var partes = linha.Split(';');
            if (partes.Length < 2)
            {
                erros.Add($"Linha {i + 1}: formato inválido");
                continue;
            }

            var codigo = partes[0].Trim();
            var descricao = partes[1].Trim().ToUpper();

            if (string.IsNullOrEmpty(codigo) || codigo.Length < 2 || codigo.Length > 10)
            {
                erros.Add($"Linha {i + 1}: código '{codigo}' inválido");
                continue;
            }

            if (codigosExistentes.Contains(codigo))
            {
                ignorados++;
                continue;
            }

            codigosExistentes.Add(codigo);
            lote.Add(new Ncm
            {
                CodigoNcm = codigo,
                Descricao = descricao,
                Ativo = true,
                FilialOrigemId = filialId > 0 ? filialId : null
            });
            inseridos++;

            if (lote.Count >= 500)
            {
                _db.Ncms.AddRange(lote);
                await _db.SaveChangesAsync();
                lote.Clear();
            }
        }

        if (lote.Count > 0)
        {
            _db.Ncms.AddRange(lote);
            await _db.SaveChangesAsync();
        }

        await _log.RegistrarAsync(TELA, "IMPORTAÇÃO", ENTIDADE, 0,
            novo: new Dictionary<string, string?>
            {
                ["Arquivo"] = Path.GetFileName(caminhoArquivo),
                ["Inseridos"] = inseridos.ToString(),
                ["Ignorados (duplicados)"] = ignorados.ToString(),
                ["Erros"] = erros.Count.ToString()
            });

        return new { inseridos, ignorados, erros = erros.Take(20).ToList(), totalErros = erros.Count };
    }

    private static void ValidarCodigo(string codigo)
    {
        var d = new string(codigo.Where(char.IsDigit).ToArray());
        if (d.Length < 2 || d.Length > 10)
            throw new ArgumentException("Código NCM deve ter entre 2 e 10 dígitos.");
    }

    private static Dictionary<string, string?> NcmParaDict(Ncm n) => new()
    {
        ["Código"] = n.CodigoNcm,
        ["Descrição"] = n.Descricao,
        ["Ex-TIPI"] = n.ExTipi,
        ["Unid. Tributável"] = n.UnidadeTributavel,
        ["Ativo"] = n.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
