using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Substancias;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class SubstanciaService : ISubstanciaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Substâncias";
    private const string ENTIDADE = "Substancia";

    public SubstanciaService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<SubstanciaListDto>> ListarAsync()
    {
        try
        {
            return await _db.Substancias.OrderBy(s => s.Nome)
                .Select(s => new SubstanciaListDto
                {
                    Id = s.Id,
                    Nome = s.Nome,
                    Dcb = s.Dcb,
                    Cas = s.Cas,
                    ControleEspecialSngpc = s.ControleEspecialSngpc,
                    ClasseTerapeutica = s.ClasseTerapeutica,
                    ListaPortaria344 = s.ListaPortaria344,
                    TipoReceita = s.TipoReceita,
                    ValidadeReceitaDias = s.ValidadeReceitaDias,
                    Adendo = s.Adendo,
                    CriadoEm = s.CriadoEm,
                    Ativo = s.Ativo
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciaService.ListarAsync");
            throw;
        }
    }

    public async Task<SubstanciaListDto> CriarAsync(SubstanciaFormDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Dcb))  throw new ArgumentException("DCB é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Cas))  throw new ArgumentException("CAS é obrigatório.");

            var s = new Substancia
            {
                Nome = dto.Nome.Trim().ToUpper(),
                Dcb  = dto.Dcb.Trim().ToUpper(),
                Cas  = dto.Cas.Trim().ToUpper(),
                ControleEspecialSngpc = dto.ControleEspecialSngpc,
                ClasseTerapeutica = dto.ClasseTerapeutica?.Trim(),
                ListaPortaria344 = string.IsNullOrWhiteSpace(dto.ListaPortaria344) ? null : dto.ListaPortaria344.Trim().ToUpper(),
                TipoReceita = dto.TipoReceita,
                ValidadeReceitaDias = dto.ValidadeReceitaDias,
                Adendo = dto.Adendo,
                Ativo = dto.Ativo
            };

            _db.Substancias.Add(s);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, s.Id, novo: ParaDict(s));

            return new SubstanciaListDto
            {
                Id = s.Id, Nome = s.Nome, Dcb = s.Dcb, Cas = s.Cas,
                ControleEspecialSngpc = s.ControleEspecialSngpc,
                ClasseTerapeutica = s.ClasseTerapeutica,
                ListaPortaria344 = s.ListaPortaria344,
                TipoReceita = s.TipoReceita,
                ValidadeReceitaDias = s.ValidadeReceitaDias,
                Adendo = s.Adendo,
                CriadoEm = s.CriadoEm, Ativo = s.Ativo
            };
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em SubstanciaService.CriarAsync");
            throw;
        }
    }

    public async Task AtualizarAsync(long id, SubstanciaFormDto dto)
    {
        try
        {
            var s = await _db.Substancias.FindAsync(id)
                ?? throw new KeyNotFoundException($"Substância {id} não encontrada.");

            var anterior = ParaDict(s);
            s.Nome = dto.Nome.Trim().ToUpper();
            s.Dcb  = dto.Dcb.Trim().ToUpper();
            s.Cas  = dto.Cas.Trim().ToUpper();
            s.ControleEspecialSngpc = dto.ControleEspecialSngpc;
            s.ClasseTerapeutica = dto.ClasseTerapeutica?.Trim();
            s.ListaPortaria344 = string.IsNullOrWhiteSpace(dto.ListaPortaria344) ? null : dto.ListaPortaria344.Trim().ToUpper();
            s.TipoReceita = dto.TipoReceita;
            s.ValidadeReceitaDias = dto.ValidadeReceitaDias;
            s.Adendo = dto.Adendo;
            s.Ativo = dto.Ativo;

            await _db.SaveChangesAsync();

            var novo = ParaDict(s);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            Log.Error(ex, "Erro em SubstanciaService.AtualizarAsync | Id: {Id}", id);
            throw;
        }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var s = await _db.Substancias.FindAsync(id)
                ?? throw new KeyNotFoundException($"Substância {id} não encontrada.");

            var dados = ParaDict(s);
            _db.Substancias.Remove(s);

            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.Substancias.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em SubstanciaService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }

    // ── Importação da planilha oficial Anvisa ─────────────────────────
    //
    // Estrutura esperada (aba "substancias"):
    // Linha 4: header → lista, substancia_portaria, dcb, num_dcb, cas, tipo_receita, cor_receituario,
    //                   metodo_match_dcb, Classe Terapeucica, observacao, fonte_lista_url, fonte_dcb_url
    // Linha 5 em diante: dados.
    //
    // Mapeamentos aplicados:
    //  • lista (A1..C5)              → ListaPortaria344
    //  • substancia_portaria         → Nome (ToUpper)
    //  • num_dcb (int)               → Dcb (código numérico DCB como string). Linhas sem num_dcb OU sem CAS são puladas.
    //  • cas                         → Cas (ToUpper)
    //  • tipo_receita (texto)        → TipoReceita (enum int) via dicionário
    //  • Classe Terapeucica (texto)  → ClasseTerapeutica (texto direto)
    //  • validadeReceitaDias         → 30 (default para todos os tipos 1-4)
    //  • ControleEspecialSngpc       → true (tudo da Portaria 344 é SNGPC)
    //
    // Tipo de receita no ERP:
    //   1 = Receita de Controle Especial em 2 vias (Branca)
    //   2 = Notificação de Receita B (Azul)
    //   3 = Notificação de Receita Especial (Branca)
    //   4 = Notificação de Receita A (Amarela)
    //   5 = Receita Antimicrobiano em 2 vias
    //   6 = Notificação de Receita B2 (Azul — Anorexígenos)
    private static readonly Dictionary<string, int> MAPA_TIPO_RECEITA = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Receita de Controle Especial em 2 vias"] = 1,
        ["Receituário do Programa DST/AIDS ou Receita de Controle Especial em 2 vias"] = 1,
        ["Notificação de Receita B"]  = 2,
        ["Notificação de Receita Especial"] = 3,
        ["Notificação de Receita A"]  = 4,
        ["Notificação de Receita B2"] = 6
    };

    public async Task<SubstanciaImportResultDto> ImportarPlanilhaAsync(Stream xlsxStream, bool limparAntes)
    {
        var resultado = new SubstanciaImportResultDto();
        try
        {
            using var wb = new XLWorkbook(xlsxStream);
            if (!wb.Worksheets.TryGetWorksheet("substancias", out var ws))
                throw new ArgumentException("A planilha deve conter a aba 'substancias'.");

            if (limparAntes)
            {
                // Remove dependências primeiro (ProdutoSubstancia) e depois as substâncias
                var vinculos = await _db.ProdutosSubstancias.ToListAsync();
                if (vinculos.Count > 0) _db.ProdutosSubstancias.RemoveRange(vinculos);
                var todas = await _db.Substancias.ToListAsync();
                resultado.RemovidasAntes = todas.Count;
                if (todas.Count > 0) _db.Substancias.RemoveRange(todas);
                await _db.SaveChangesAsync();
            }

            // Índice pra detectar duplicadas DENTRO do próprio batch (por Nome ToUpper)
            var nomesExistentes = await _db.Substancias
                .Select(s => s.Nome.ToUpper())
                .ToListAsync();
            var nomesSet = new HashSet<string>(nomesExistentes);

            var novas = new List<Substancia>();
            // Dados começam na linha 5 (linha 4 é header)
            foreach (var row in ws.RowsUsed().Where(r => r.RowNumber() >= 5))
            {
                resultado.TotalLinhas++;

                var lista  = row.Cell(1).GetString().Trim();
                var nome   = row.Cell(2).GetString().Trim();
                var numDcbCell = row.Cell(4);     // coluna num_dcb (numérica)
                var cas    = row.Cell(5).GetString().Trim();
                var tipoTx = row.Cell(6).GetString().Trim();
                var classe = row.Cell(9).GetString().Trim();  // "Classe Terapeucica"

                if (string.IsNullOrEmpty(lista) || string.IsNullOrEmpty(nome))
                    continue;

                // O Dcb recebe o código numérico (num_dcb) como string.
                // Linhas sem num_dcb OU sem CAS são puladas (não têm correspondência na base DCB).
                string? dcb = null;
                if (!numDcbCell.IsEmpty())
                {
                    try { dcb = ((int)numDcbCell.GetDouble()).ToString(); }
                    catch { dcb = numDcbCell.GetString().Trim(); }
                }

                if (string.IsNullOrWhiteSpace(dcb) || string.IsNullOrWhiteSpace(cas))
                {
                    resultado.PuladasSemDcbCas++;
                    continue;
                }

                // Mapeia o tipo de receita
                int? tipoReceita = null;
                if (!string.IsNullOrWhiteSpace(tipoTx))
                {
                    if (MAPA_TIPO_RECEITA.TryGetValue(tipoTx, out var tipo))
                        tipoReceita = tipo;
                    else
                        resultado.Avisos.Add($"Linha {row.RowNumber()}: tipo_receita desconhecido '{tipoTx}' — substância '{nome}' importada sem tipo de receita.");
                }

                var nomeUpper = nome.ToUpper();
                if (nomesSet.Contains(nomeUpper))
                {
                    resultado.PuladasDuplicadas++;
                    continue;
                }
                nomesSet.Add(nomeUpper);

                var s = new Substancia
                {
                    Nome  = nomeUpper,
                    Dcb   = dcb!.ToUpper(),
                    Cas   = cas.ToUpper(),
                    ControleEspecialSngpc = true,
                    ClasseTerapeutica = string.IsNullOrWhiteSpace(classe) ? null : classe,
                    ListaPortaria344 = lista.ToUpper(),
                    TipoReceita = tipoReceita,
                    ValidadeReceitaDias = tipoReceita == 5 ? 10 : 30,
                    Adendo = false,
                    Ativo  = true
                };
                novas.Add(s);
            }

            if (novas.Count > 0)
            {
                _db.Substancias.AddRange(novas);
                await _db.SaveChangesAsync();
                resultado.Importadas = novas.Count;

                // Log único de importação (sem poluir com 500 entries individuais)
                await _log.RegistrarAsync(TELA, "IMPORTAÇÃO", ENTIDADE, 0, novo: new Dictionary<string, string?>
                {
                    ["Origem"]         = "Planilha Anvisa (Portaria 344)",
                    ["Total linhas"]   = resultado.TotalLinhas.ToString(),
                    ["Importadas"]     = resultado.Importadas.ToString(),
                    ["Sem DCB/CAS"]    = resultado.PuladasSemDcbCas.ToString(),
                    ["Duplicadas"]     = resultado.PuladasDuplicadas.ToString(),
                    ["Removidas antes"]= resultado.RemovidasAntes.ToString()
                });
            }

            return resultado;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em SubstanciaService.ImportarPlanilhaAsync");
            throw;
        }
    }

    private static Dictionary<string, string?> ParaDict(Substancia s) => new()
    {
        ["Nome"]                 = s.Nome,
        ["DCB"]                  = s.Dcb,
        ["CAS"]                  = s.Cas,
        ["Controle Especial"]    = s.ControleEspecialSngpc ? "Sim" : "Não",
        ["Classe Terapêutica"]   = s.ClasseTerapeutica,
        ["Lista Portaria 344"]   = s.ListaPortaria344,
        ["Tipo Receita"]         = s.TipoReceita?.ToString(),
        ["Validade Receita"]     = s.ValidadeReceitaDias?.ToString(),
        ["Adendo"]               = s.Adendo ? "Sim" : "Não",
        ["Ativo"]                = s.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
