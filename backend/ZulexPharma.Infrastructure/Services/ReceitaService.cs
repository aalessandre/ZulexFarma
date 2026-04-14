using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ReceitaService : IReceitaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Receitas";
    private const string ENTIDADE = "Receita";

    public ReceitaService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<ReceitaListDto>> ListarAsync(long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var q = _db.Receitas.Include(r => r.Itens).AsQueryable();
        if (filialId.HasValue) q = q.Where(r => r.FilialId == filialId.Value);
        if (dataInicio.HasValue) q = q.Where(r => r.DataEmissao >= dataInicio.Value);
        if (dataFim.HasValue) q = q.Where(r => r.DataEmissao <= dataFim.Value);

        return await q.OrderByDescending(r => r.DataEmissao)
            .Select(r => new ReceitaListDto
            {
                Id = r.Id,
                FilialId = r.FilialId,
                VendaId = r.VendaId,
                MedicoNome = r.MedicoNome,
                MedicoCrm = r.MedicoCrm,
                PacienteNome = r.PacienteNome,
                NumeroReceita = r.NumeroReceita,
                DataEmissao = r.DataEmissao,
                TipoReceita = r.TipoReceita,
                TotalItens = r.Itens.Count,
                CriadoEm = r.CriadoEm
            }).ToListAsync();
    }

    public async Task<ReceitaDetalheDto> ObterAsync(long id)
    {
        var r = await _db.Receitas
            .Include(x => x.Itens).ThenInclude(i => i.Produto)
            .Include(x => x.Itens).ThenInclude(i => i.ProdutoLote)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Receita {id} não encontrada.");

        return new ReceitaDetalheDto
        {
            Id = r.Id,
            FilialId = r.FilialId,
            VendaId = r.VendaId,
            MedicoNome = r.MedicoNome,
            MedicoCrm = r.MedicoCrm,
            MedicoUf = r.MedicoUf,
            MedicoCpf = r.MedicoCpf,
            PacienteNome = r.PacienteNome,
            PacienteCpf = r.PacienteCpf,
            PacienteEndereco = r.PacienteEndereco,
            PacienteCep = r.PacienteCep,
            PacienteCidade = r.PacienteCidade,
            PacienteUf = r.PacienteUf,
            NumeroReceita = r.NumeroReceita,
            DataEmissao = r.DataEmissao,
            TipoReceita = r.TipoReceita,
            Observacao = r.Observacao,
            Itens = r.Itens.Select(i => new ReceitaItemDto
            {
                Id = i.Id,
                ProdutoId = i.ProdutoId,
                ProdutoNome = i.Produto?.Nome,
                ProdutoLoteId = i.ProdutoLoteId,
                NumeroLote = i.ProdutoLote?.NumeroLote,
                Quantidade = i.Quantidade,
                Posologia = i.Posologia
            }).ToList()
        };
    }

    public async Task<ReceitaListDto> CriarAsync(ReceitaFormDto dto)
    {
        ValidarDto(dto);
        var r = new Receita
        {
            FilialId = dto.FilialId,
            VendaId = dto.VendaId,
            MedicoNome = dto.MedicoNome.Trim().ToUpper(),
            MedicoCrm = dto.MedicoCrm?.Trim(),
            MedicoUf = dto.MedicoUf?.Trim().ToUpper(),
            MedicoCpf = dto.MedicoCpf?.Trim(),
            PacienteNome = dto.PacienteNome.Trim().ToUpper(),
            PacienteCpf = dto.PacienteCpf?.Trim(),
            PacienteEndereco = dto.PacienteEndereco?.Trim(),
            PacienteCep = dto.PacienteCep?.Trim(),
            PacienteCidade = dto.PacienteCidade?.Trim().ToUpper(),
            PacienteUf = dto.PacienteUf?.Trim().ToUpper(),
            NumeroReceita = dto.NumeroReceita?.Trim(),
            DataEmissao = DateTime.SpecifyKind(dto.DataEmissao, DateTimeKind.Utc),
            TipoReceita = dto.TipoReceita?.Trim(),
            Observacao = dto.Observacao
        };
        foreach (var i in dto.Itens)
        {
            r.Itens.Add(new ReceitaItem
            {
                ProdutoId = i.ProdutoId,
                ProdutoLoteId = i.ProdutoLoteId,
                Quantidade = i.Quantidade,
                Posologia = i.Posologia
            });
        }
        _db.Receitas.Add(r);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, r.Id, novo: new Dictionary<string, string?>
        {
            ["Médico"] = r.MedicoNome,
            ["Paciente"] = r.PacienteNome,
            ["Itens"] = r.Itens.Count.ToString()
        });

        return (await ListarAsync(r.FilialId)).First(x => x.Id == r.Id);
    }

    public async Task AtualizarAsync(long id, ReceitaFormDto dto)
    {
        ValidarDto(dto);
        var r = await _db.Receitas.Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Receita {id} não encontrada.");

        r.MedicoNome = dto.MedicoNome.Trim().ToUpper();
        r.MedicoCrm = dto.MedicoCrm?.Trim();
        r.MedicoUf = dto.MedicoUf?.Trim().ToUpper();
        r.MedicoCpf = dto.MedicoCpf?.Trim();
        r.PacienteNome = dto.PacienteNome.Trim().ToUpper();
        r.PacienteCpf = dto.PacienteCpf?.Trim();
        r.PacienteEndereco = dto.PacienteEndereco?.Trim();
        r.PacienteCep = dto.PacienteCep?.Trim();
        r.PacienteCidade = dto.PacienteCidade?.Trim().ToUpper();
        r.PacienteUf = dto.PacienteUf?.Trim().ToUpper();
        r.NumeroReceita = dto.NumeroReceita?.Trim();
        r.DataEmissao = DateTime.SpecifyKind(dto.DataEmissao, DateTimeKind.Utc);
        r.TipoReceita = dto.TipoReceita?.Trim();
        r.Observacao = dto.Observacao;

        _db.ReceitasItens.RemoveRange(r.Itens);
        r.Itens.Clear();
        foreach (var i in dto.Itens)
        {
            r.Itens.Add(new ReceitaItem
            {
                ProdutoId = i.ProdutoId,
                ProdutoLoteId = i.ProdutoLoteId,
                Quantidade = i.Quantidade,
                Posologia = i.Posologia
            });
        }
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id);
    }

    public async Task ExcluirAsync(long id)
    {
        var r = await _db.Receitas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Receita {id} não encontrada.");
        _db.Receitas.Remove(r);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
    }

    private static void ValidarDto(ReceitaFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MedicoNome)) throw new ArgumentException("Nome do médico é obrigatório.");
        if (string.IsNullOrWhiteSpace(dto.PacienteNome)) throw new ArgumentException("Nome do paciente é obrigatório.");
        if (dto.Itens.Count == 0) throw new ArgumentException("Informe ao menos um produto na receita.");
    }
}
