using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class EntregaService : IEntregaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private readonly IGeocodingService _geocoding;
    private const string TELA = "Entregas";
    private const string ENTIDADE = "Entrega";

    public EntregaService(AppDbContext db, ILogAcaoService log, IGeocodingService geocoding)
    {
        _db = db;
        _log = log;
        _geocoding = geocoding;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Preview / Criar
    // ═══════════════════════════════════════════════════════════════════

    public async Task<EntregaPreviewDto> CalcularAsync(long filialId, long enderecoEntregaId)
    {
        var (distancia, endereco, faixa) = await CalcularDistanciaEFaixaAsync(filialId, enderecoEntregaId);

        return new EntregaPreviewDto
        {
            DistanciaKm = distancia,
            ValorEntrega = faixa.Valor,
            EntregaFaixaId = faixa.Id,
            Bairro = endereco.Bairro,
            Cidade = endereco.Cidade,
            Latitude = endereco.Latitude!.Value,
            Longitude = endereco.Longitude!.Value
        };
    }

    public async Task<EntregaDetalheDto> CriarAsync(EntregaFormDto dto, long? usuarioId)
    {
        var venda = await _db.Vendas
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == dto.VendaId)
            ?? throw new KeyNotFoundException($"Venda {dto.VendaId} não encontrada.");

        if (venda.ClienteId == null)
            throw new ArgumentException("Venda de entrega exige cliente cadastrado.");

        var jaExiste = await _db.Entregas.AnyAsync(e => e.VendaId == dto.VendaId);
        if (jaExiste)
            throw new InvalidOperationException("Esta venda já tem uma entrega cadastrada.");

        long enderecoId;
        if (dto.EnderecoEntregaId.HasValue) enderecoId = dto.EnderecoEntregaId.Value;
        else
        {
            var enderecoPrincipal = await _db.Set<Cliente>()
                .Where(c => c.Id == venda.ClienteId.Value)
                .SelectMany(c => c.Pessoa.Enderecos)
                .OrderByDescending(e => e.Principal).ThenBy(e => e.Id)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
            if (enderecoPrincipal == 0)
                throw new ArgumentException("Cliente não tem endereço cadastrado.");
            enderecoId = enderecoPrincipal;
        }

        var (distancia, endereco, faixa) = await CalcularDistanciaEFaixaAsync(venda.FilialId, enderecoId);

        var entrega = new Entrega
        {
            VendaId = venda.Id,
            FilialId = venda.FilialId,
            ClienteId = venda.ClienteId.Value,
            EnderecoEntregaId = enderecoId,
            // Snapshot do endereço
            Cep = endereco.Cep,
            Rua = endereco.Rua,
            Numero = endereco.Numero,
            Complemento = endereco.Complemento,
            Bairro = endereco.Bairro,
            Cidade = endereco.Cidade,
            Uf = endereco.Uf,
            CodigoIbgeMunicipio = endereco.CodigoIbgeMunicipio,
            Latitude = endereco.Latitude!.Value,
            Longitude = endereco.Longitude!.Value,
            // Faixa + valor
            DistanciaKm = distancia,
            EntregaFaixaId = faixa.Id,
            ValorEntrega = faixa.Valor,
            Status = StatusEntrega.Pendente,
            DataPedido = DataHoraHelper.Agora(),
            DataPrevista = dto.DataPrevista,
            Observacao = dto.Observacao
        };
        entrega.Eventos.Add(new EntregaEvento
        {
            Tipo = TipoEntregaEvento.StatusChange,
            Status = StatusEntrega.Pendente,
            Texto = "Entrega criada.",
            UsuarioId = usuarioId
        });
        _db.Entregas.Add(entrega);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, entrega.Id, novo: new()
        {
            ["VendaId"] = venda.Id.ToString(),
            ["ValorEntrega"] = entrega.ValorEntrega.ToString("0.00"),
            ["DistanciaKm"] = entrega.DistanciaKm.ToString("0.###")
        });

        return await ObterAsync(entrega.Id);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Listagem / detalhe
    // ═══════════════════════════════════════════════════════════════════

    public async Task<List<EntregaListDto>> ListarAsync(long? filialId = null, StatusEntrega? status = null,
        DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var q = _db.Entregas
            .Include(e => e.Cliente).ThenInclude(c => c!.Pessoa)
            .Include(e => e.Entregador).ThenInclude(en => en!.Pessoa)
            .AsQueryable();

        if (filialId.HasValue) q = q.Where(e => e.FilialId == filialId.Value);
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        if (dataInicio.HasValue) q = q.Where(e => e.DataPedido >= dataInicio.Value);
        if (dataFim.HasValue) q = q.Where(e => e.DataPedido <= dataFim.Value);

        return await q.OrderByDescending(e => e.DataPedido)
            .Select(e => new EntregaListDto
            {
                Id = e.Id,
                VendaId = e.VendaId,
                ClienteId = e.ClienteId,
                ClienteNome = e.Cliente != null && e.Cliente.Pessoa != null ? e.Cliente.Pessoa.Nome : "",
                ClienteTelefone = "",
                EntregadorId = e.EntregadorId,
                EntregadorNome = e.Entregador != null && e.Entregador.Pessoa != null ? e.Entregador.Pessoa.Nome : null,
                Status = e.Status,
                StatusNome = e.Status.ToString(),
                ValorEntrega = e.ValorEntrega,
                DistanciaKm = e.DistanciaKm,
                Bairro = e.Bairro,
                Cidade = e.Cidade,
                Uf = e.Uf,
                DataPedido = e.DataPedido,
                DataSaida = e.DataSaida,
                DataEntrega = e.DataEntrega,
                TokenRastreamento = e.TokenRastreamento
            })
            .ToListAsync();
    }

    public async Task<EntregaDetalheDto> ObterAsync(long id)
    {
        var e = await _db.Entregas
            .Include(x => x.Cliente).ThenInclude(c => c!.Pessoa)
            .Include(x => x.Entregador).ThenInclude(en => en!.Pessoa)
            .Include(x => x.Eventos).ThenInclude(ev => ev.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Entrega {id} não encontrada.");

        return new EntregaDetalheDto
        {
            Id = e.Id,
            VendaId = e.VendaId,
            FilialId = e.FilialId,
            ClienteId = e.ClienteId,
            ClienteNome = e.Cliente?.Pessoa?.Nome ?? "",
            EnderecoEntregaId = e.EnderecoEntregaId,
            EntregadorId = e.EntregadorId,
            EntregadorNome = e.Entregador?.Pessoa?.Nome,
            Status = e.Status,
            StatusNome = e.Status.ToString(),
            ValorEntrega = e.ValorEntrega,
            DistanciaKm = e.DistanciaKm,
            EntregaFaixaId = e.EntregaFaixaId,
            Cep = e.Cep,
            Rua = e.Rua,
            Numero = e.Numero,
            Complemento = e.Complemento,
            Bairro = e.Bairro,
            Cidade = e.Cidade,
            Uf = e.Uf,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            DataPedido = e.DataPedido,
            DataPrevista = e.DataPrevista,
            DataSaida = e.DataSaida,
            DataEntrega = e.DataEntrega,
            TokenRastreamento = e.TokenRastreamento,
            Observacao = e.Observacao,
            Eventos = e.Eventos.OrderBy(ev => ev.CriadoEm).Select(ev => new EntregaEventoDto
            {
                Id = ev.Id,
                Tipo = ev.Tipo,
                Status = ev.Status,
                Latitude = ev.Latitude,
                Longitude = ev.Longitude,
                Texto = ev.Texto,
                UsuarioLogin = ev.Usuario?.Login,
                CriadoEm = ev.CriadoEm
            }).ToList()
        };
    }

    public async Task<EntregaRastreioPublicoDto> ObterPorTokenAsync(Guid tokenRastreamento)
    {
        var e = await _db.Entregas
            .Include(x => x.Filial)
            .Include(x => x.Entregador).ThenInclude(en => en!.Pessoa)
            .Include(x => x.Eventos)
            .FirstOrDefaultAsync(x => x.TokenRastreamento == tokenRastreamento)
            ?? throw new KeyNotFoundException("Entrega não encontrada.");

        return new EntregaRastreioPublicoDto
        {
            Status = e.Status,
            StatusNome = e.Status.ToString(),
            FilialNome = e.Filial?.NomeFantasia ?? "",
            EntregadorNome = e.Entregador?.Pessoa?.Nome,
            Bairro = e.Bairro,
            Cidade = e.Cidade,
            DistanciaKm = e.DistanciaKm,
            DataPedido = e.DataPedido,
            DataSaida = e.DataSaida,
            DataEntrega = e.DataEntrega,
            DataPrevista = e.DataPrevista,
            Eventos = e.Eventos
                .Where(ev => ev.Tipo == TipoEntregaEvento.StatusChange)
                .OrderBy(ev => ev.CriadoEm)
                .Select(ev => new EntregaEventoPublicoDto
                {
                    Status = ev.Status,
                    StatusNome = ev.Status?.ToString(),
                    CriadoEm = ev.CriadoEm
                })
                .ToList()
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Atribuir entregador + Mudar status
    // ═══════════════════════════════════════════════════════════════════

    public async Task AtribuirEntregadorAsync(long id, long entregadorId, long? usuarioId)
    {
        var entrega = await _db.Entregas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Entrega {id} não encontrada.");

        if (entrega.Status != StatusEntrega.Pendente && entrega.Status != StatusEntrega.EmPreparacao)
            throw new InvalidOperationException("Só é possível atribuir entregador antes do despacho.");

        var entregador = await _db.Set<Colaborador>()
            .Include(c => c.Pessoa)
            .FirstOrDefaultAsync(c => c.Id == entregadorId)
            ?? throw new KeyNotFoundException($"Colaborador {entregadorId} não encontrado.");

        entrega.EntregadorId = entregadorId;
        var nomeEntregador = entregador.Pessoa?.Nome ?? $"#{entregador.Id}";
        _db.EntregaEventos.Add(new EntregaEvento
        {
            EntregaId = entrega.Id,
            Tipo = TipoEntregaEvento.Observacao,
            Texto = $"Entregador atribuído: {nomeEntregador}",
            UsuarioId = usuarioId
        });
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "ATRIBUIÇÃO", ENTIDADE, id, novo: new() { ["Entregador"] = nomeEntregador });
    }

    public async Task MudarStatusAsync(long id, StatusEntrega novoStatus, long? usuarioId, string? observacao = null)
    {
        var entrega = await _db.Entregas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Entrega {id} não encontrada.");

        if (!TransicaoValida(entrega.Status, novoStatus))
            throw new InvalidOperationException($"Transição inválida: {entrega.Status} → {novoStatus}.");

        var agora = DataHoraHelper.Agora();
        entrega.Status = novoStatus;

        if (novoStatus == StatusEntrega.SaiuParaEntrega) entrega.DataSaida = agora;
        if (novoStatus == StatusEntrega.Entregue || novoStatus == StatusEntrega.Devolvida) entrega.DataEntrega = agora;

        _db.EntregaEventos.Add(new EntregaEvento
        {
            EntregaId = entrega.Id,
            Tipo = TipoEntregaEvento.StatusChange,
            Status = novoStatus,
            Texto = observacao,
            UsuarioId = usuarioId
        });
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, $"STATUS → {novoStatus}", ENTIDADE, id);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers internos
    // ═══════════════════════════════════════════════════════════════════

    private async Task<(decimal Distancia, PessoaEndereco Endereco, EntregaFaixa Faixa)>
        CalcularDistanciaEFaixaAsync(long filialId, long enderecoId)
    {
        var filial = await _db.Filiais.FindAsync(filialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");
        if (filial.Latitude == null || filial.Longitude == null)
            throw new InvalidOperationException(
                "Filial sem coordenadas cadastradas. Vá em Configurações → Filial e clique em 'Buscar Coordenadas'.");

        var endereco = await _db.Set<PessoaEndereco>().FindAsync(enderecoId)
            ?? throw new KeyNotFoundException("Endereço não encontrado.");

        if (endereco.Latitude == null || endereco.Longitude == null)
        {
            var res = await _geocoding.GeocodificarAsync(new GeocodingRequestDto
            {
                Rua = endereco.Rua,
                Numero = endereco.Numero,
                Bairro = endereco.Bairro,
                Cidade = endereco.Cidade,
                Uf = endereco.Uf,
                Cep = endereco.Cep
            });
            if (!res.Encontrado || res.Latitude == null || res.Longitude == null)
                throw new InvalidOperationException(
                    res.Mensagem ?? "Endereço sem coordenadas. Ajuste o endereço ou informe lat/lng manualmente.");
            endereco.Latitude = res.Latitude.Value;
            endereco.Longitude = res.Longitude.Value;
            await _db.SaveChangesAsync();
        }

        var distancia = (decimal)HaversineKm(
            (double)filial.Latitude.Value, (double)filial.Longitude.Value,
            (double)endereco.Latitude.Value, (double)endereco.Longitude.Value);

        var faixa = await _db.EntregaFaixas
            .Where(f => f.FilialId == filialId && f.RaioMaxKm >= distancia)
            .OrderBy(f => f.RaioMaxKm)
            .FirstOrDefaultAsync();
        if (faixa == null)
            throw new InvalidOperationException(
                $"Endereço está a {distancia:0.##} km, fora da área de entrega. Ajuste as faixas ou informe lat/lng manualmente.");

        return (Math.Round(distancia, 3), endereco, faixa);
    }

    /// <summary>Distância em km entre dois pontos (fórmula de Haversine).</summary>
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double ToRad(double d) => d * Math.PI / 180.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static bool TransicaoValida(StatusEntrega atual, StatusEntrega novo)
    {
        return (atual, novo) switch
        {
            (StatusEntrega.Pendente, StatusEntrega.EmPreparacao) => true,
            (StatusEntrega.Pendente, StatusEntrega.SaiuParaEntrega) => true,
            (StatusEntrega.Pendente, StatusEntrega.Cancelada) => true,
            (StatusEntrega.EmPreparacao, StatusEntrega.SaiuParaEntrega) => true,
            (StatusEntrega.EmPreparacao, StatusEntrega.Cancelada) => true,
            (StatusEntrega.SaiuParaEntrega, StatusEntrega.Entregue) => true,
            (StatusEntrega.SaiuParaEntrega, StatusEntrega.Devolvida) => true,
            _ => false
        };
    }
}
