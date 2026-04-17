using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Gera o mapa mensal SNGPC consolidando entradas (compras), saídas (vendas),
/// perdas e receitas de produtos controlados de uma filial para uma competência.
///
/// ⚠️ O XML gerado aqui é uma ESTRUTURA SIMPLIFICADA, não o XSD oficial completo da Anvisa.
/// O XSD oficial (disponível em https://www.gov.br/anvisa) tem dezenas de elementos e validações
/// que serão implementados numa iteração futura. Por enquanto, o XML serve como snapshot
/// consolidado dos movimentos, assinável e arquivável.
///
/// O envio ao webservice Anvisa também está em stub — marca como "Enviado" no banco mas
/// não chama serviço externo ainda.
/// </summary>
public class SngpcMapaService : ISngpcMapaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Mapa SNGPC";
    private const string ENTIDADE = "SngpcMapa";

    public SngpcMapaService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<SngpcMapaListDto>> ListarAsync(long? filialId = null, int? ano = null)
    {
        var q = _db.SngpcMapas.AsQueryable();
        if (filialId.HasValue) q = q.Where(m => m.FilialId == filialId.Value);
        if (ano.HasValue) q = q.Where(m => m.CompetenciaAno == ano.Value);

        return await q.OrderByDescending(m => m.CompetenciaAno)
            .ThenByDescending(m => m.CompetenciaMes)
            .Select(m => new SngpcMapaListDto
            {
                Id = m.Id,
                FilialId = m.FilialId,
                CompetenciaMes = m.CompetenciaMes,
                CompetenciaAno = m.CompetenciaAno,
                Status = (int)m.Status,
                StatusNome = m.Status.ToString(),
                DataGeracao = m.DataGeracao,
                DataEnvio = m.DataEnvio,
                ProtocoloAnvisa = m.ProtocoloAnvisa,
                TotalEntradas = m.TotalEntradas,
                TotalSaidas = m.TotalSaidas,
                TotalReceitas = m.TotalReceitas,
                TotalPerdas = m.TotalPerdas
            }).ToListAsync();
    }

    public async Task<SngpcMapaListDto> GerarAsync(GerarMapaSngpcRequest req, long? usuarioId)
    {
        if (req.Mes < 1 || req.Mes > 12) throw new ArgumentException("Mês inválido.");
        if (req.Ano < 2000 || req.Ano > 3000) throw new ArgumentException("Ano inválido.");

        // Verifica se já existe mapa pra essa competência
        var existente = await _db.SngpcMapas.FirstOrDefaultAsync(m =>
            m.FilialId == req.FilialId && m.CompetenciaMes == req.Mes && m.CompetenciaAno == req.Ano);
        if (existente != null && existente.Status == StatusSngpcMapa.Enviado)
            throw new InvalidOperationException("Esta competência já foi enviada à Anvisa. Não é possível regerar.");

        // Janela do mês
        var inicio = DateTime.SpecifyKind(new DateTime(req.Ano, req.Mes, 1), DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);

        // Entradas (MovimentoLote tipo Entrada) de produtos SNGPC
        var entradas = await _db.MovimentosLote
            .Include(m => m.ProdutoLote).ThenInclude(l => l.Produto)
            .Where(m => m.Tipo == TipoMovimentoLote.Entrada
                && m.DataMovimento >= inicio && m.DataMovimento < fim
                && m.ProdutoLote.FilialId == req.FilialId
                && (m.ProdutoLote.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS
                 || m.ProdutoLote.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO))
            .ToListAsync();

        // Saídas (tipo Saida)
        var saidas = await _db.MovimentosLote
            .Include(m => m.ProdutoLote).ThenInclude(l => l.Produto)
            .Where(m => m.Tipo == TipoMovimentoLote.Saida
                && m.DataMovimento >= inicio && m.DataMovimento < fim
                && m.ProdutoLote.FilialId == req.FilialId
                && (m.ProdutoLote.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS
                 || m.ProdutoLote.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO))
            .ToListAsync();

        // Perdas
        var perdas = await _db.Perdas
            .Include(p => p.Produto)
            .Include(p => p.ProdutoLote)
            .Where(p => p.FilialId == req.FilialId
                && p.DataPerda >= inicio && p.DataPerda < fim
                && (p.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS
                 || p.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO))
            .ToListAsync();

        // Receitas (VendaReceita — ligadas a vendas finalizadas)
        var receitas = await _db.VendaReceitas
            .Include(r => r.Venda)
            .Include(r => r.Prescritor)
            .Include(r => r.Itens).ThenInclude(i => i.VendaItem).ThenInclude(vi => vi.Produto)
            .Where(r => r.Venda.FilialId == req.FilialId
                && r.DataEmissao >= inicio && r.DataEmissao < fim)
            .ToListAsync();

        // Filial
        var filial = await _db.Filiais.FirstOrDefaultAsync(f => f.Id == req.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        // ── Monta o XML (schema simplificado, não é o XSD oficial da Anvisa) ──
        var ns = XNamespace.Get("http://www.anvisa.gov.br/sngpc");
        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(ns + "mapaMensalSngpc",
                new XAttribute("versao", "3.0-simplificado"),
                new XElement(ns + "cabecalho",
                    new XElement(ns + "cnpj", OnlyDigits(filial.Cnpj ?? "")),
                    new XElement(ns + "razaoSocial", filial.RazaoSocial),
                    new XElement(ns + "competencia", $"{req.Ano:D4}-{req.Mes:D2}"),
                    new XElement(ns + "geradoEm", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                ),
                new XElement(ns + "entradas",
                    new XAttribute("total", entradas.Count),
                    entradas.Select(e => new XElement(ns + "entrada",
                        new XElement(ns + "data", e.DataMovimento.ToString("yyyy-MM-dd")),
                        new XElement(ns + "produto", e.ProdutoLote.Produto.Nome),
                        new XElement(ns + "classe", e.ProdutoLote.Produto.ClasseTerapeutica),
                        new XElement(ns + "lote", e.ProdutoLote.NumeroLote),
                        new XElement(ns + "validade", e.ProdutoLote.DataValidade?.ToString("yyyy-MM-dd") ?? ""),
                        new XElement(ns + "quantidade", e.Quantidade.ToString("0.####"))
                    ))
                ),
                new XElement(ns + "saidas",
                    new XAttribute("total", saidas.Count),
                    saidas.Select(s => new XElement(ns + "saida",
                        new XElement(ns + "data", s.DataMovimento.ToString("yyyy-MM-dd")),
                        new XElement(ns + "produto", s.ProdutoLote.Produto.Nome),
                        new XElement(ns + "classe", s.ProdutoLote.Produto.ClasseTerapeutica),
                        new XElement(ns + "lote", s.ProdutoLote.NumeroLote),
                        new XElement(ns + "quantidade", s.Quantidade.ToString("0.####")),
                        new XElement(ns + "vendaId", s.VendaId?.ToString() ?? "")
                    ))
                ),
                new XElement(ns + "perdas",
                    new XAttribute("total", perdas.Count),
                    perdas.Select(p => new XElement(ns + "perda",
                        new XElement(ns + "data", p.DataPerda.ToString("yyyy-MM-dd")),
                        new XElement(ns + "produto", p.Produto.Nome),
                        new XElement(ns + "lote", p.ProdutoLote.NumeroLote),
                        new XElement(ns + "quantidade", p.Quantidade.ToString("0.####")),
                        new XElement(ns + "motivo", p.Motivo.ToString()),
                        new XElement(ns + "boletim", p.NumeroBoletim ?? "")
                    ))
                ),
                new XElement(ns + "receitas",
                    new XAttribute("total", receitas.Count),
                    receitas.Select(r => new XElement(ns + "receita",
                        new XElement(ns + "data", r.DataEmissao.ToString("yyyy-MM-dd")),
                        new XElement(ns + "numero", r.NumeroNotificacao ?? ""),
                        new XElement(ns + "tipo", r.Tipo.ToString()),
                        new XElement(ns + "prescritor",
                            new XElement(ns + "nome", r.Prescritor.Nome),
                            new XElement(ns + "tipoConselho", r.Prescritor.TipoConselho),
                            new XElement(ns + "numero", r.Prescritor.NumeroConselho),
                            new XElement(ns + "uf", r.Prescritor.Uf)
                        ),
                        new XElement(ns + "paciente",
                            new XElement(ns + "nome", r.PacienteNome),
                            new XElement(ns + "cpf", r.PacienteCpf ?? "")
                        ),
                        new XElement(ns + "itens",
                            r.Itens.Select(i => new XElement(ns + "item",
                                new XElement(ns + "produto", i.VendaItem.Produto.Nome),
                                new XElement(ns + "quantidade", i.Quantidade.ToString("0.####"))
                            ))
                        )
                    ))
                )
            )
        );

        var xmlStr = xml.ToString(SaveOptions.None);

        // Salva ou atualiza
        if (existente == null)
        {
            existente = new SngpcMapa
            {
                FilialId = req.FilialId,
                CompetenciaMes = req.Mes,
                CompetenciaAno = req.Ano
            };
            _db.SngpcMapas.Add(existente);
        }
        existente.Status = StatusSngpcMapa.Gerado;
        existente.XmlConteudo = xmlStr;
        existente.DataGeracao = DateTime.UtcNow;
        existente.TotalEntradas = entradas.Count;
        existente.TotalSaidas = saidas.Count;
        existente.TotalReceitas = receitas.Count;
        existente.TotalPerdas = perdas.Count;
        existente.UsuarioId = usuarioId;
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "GERAÇÃO", ENTIDADE, existente.Id, novo: new Dictionary<string, string?>
        {
            ["Competência"] = $"{req.Ano:D4}-{req.Mes:D2}",
            ["Entradas"] = entradas.Count.ToString(),
            ["Saídas"] = saidas.Count.ToString(),
            ["Perdas"] = perdas.Count.ToString(),
            ["Receitas"] = receitas.Count.ToString()
        });

        return (await ListarAsync(req.FilialId, req.Ano)).First(m => m.Id == existente.Id);
    }

    public async Task<string> ObterXmlAsync(long id)
    {
        var m = await _db.SngpcMapas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Mapa {id} não encontrado.");
        return m.XmlConteudo ?? "";
    }

    public async Task MarcarEnviadoAsync(long id, string? protocolo, long? usuarioId)
    {
        var m = await _db.SngpcMapas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Mapa {id} não encontrado.");
        if (m.Status != StatusSngpcMapa.Gerado)
            throw new InvalidOperationException("Só é possível enviar mapas gerados.");

        // ⚠️ STUB: aqui deveria chamar o webservice da Anvisa. Por enquanto só registra.
        Log.Information("SngpcMapaService: envio stub para competência {Mes}/{Ano}", m.CompetenciaMes, m.CompetenciaAno);

        m.Status = StatusSngpcMapa.Enviado;
        m.DataEnvio = DateTime.UtcNow;
        m.ProtocoloAnvisa = protocolo;
        m.UsuarioId = usuarioId;
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "ENVIO", ENTIDADE, id, novo: new Dictionary<string, string?>
        {
            ["Protocolo"] = protocolo,
            ["Competência"] = $"{m.CompetenciaAno:D4}-{m.CompetenciaMes:D2}"
        });
    }

    public async Task ExcluirAsync(long id)
    {
        var m = await _db.SngpcMapas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Mapa {id} não encontrado.");
        if (m.Status == StatusSngpcMapa.Enviado)
            throw new InvalidOperationException("Não é possível excluir mapa já enviado.");
        _db.SngpcMapas.Remove(m);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
    }

    private static string OnlyDigits(string s) =>
        new string((s ?? "").Where(char.IsDigit).ToArray());
}
