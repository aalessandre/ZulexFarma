using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using ZulexPharma.Domain.Entities;

namespace ZulexPharma.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _http;
    private readonly int _filialCodigo;

    /// <summary>Se true, não gera Codigo nem registra na SyncFila (usado ao aplicar sync remoto).</summary>
    public bool AplicandoSync { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? http = null, IConfiguration? config = null) : base(options)
    {
        _http = http;
        _filialCodigo = int.TryParse(config?["Filial:Codigo"], out var c) ? c : 1;
    }

    public DbSet<Filial> Filiais => Set<Filial>();
    public DbSet<GrupoUsuario> UsuariosGrupos => Set<GrupoUsuario>();
    public DbSet<GrupoPermissao> UsuariosGruposPermissao => Set<GrupoPermissao>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<LogErro> LogsErro => Set<LogErro>();
    public DbSet<LogAcao> LogsAcao => Set<LogAcao>();
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<PessoaContato> PessoasContato => Set<PessoaContato>();
    public DbSet<PessoaEndereco> PessoasEndereco => Set<PessoaEndereco>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<UsuarioFilialGrupo> UsuarioFilialGrupos => Set<UsuarioFilialGrupo>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<DicionarioTabela> DicionarioTabelas => Set<DicionarioTabela>();
    public DbSet<DicionarioRevisao> DicionarioRevisoes => Set<DicionarioRevisao>();
    public DbSet<DicionarioRelacionamento> DicionarioRelacionamentos => Set<DicionarioRelacionamento>();
    public DbSet<Fabricante> Fabricantes => Set<Fabricante>();
    public DbSet<Substancia> Substancias => Set<Substancia>();
    public DbSet<GrupoPrincipal> GruposPrincipais => Set<GrupoPrincipal>();
    public DbSet<GrupoProduto> GruposProdutos => Set<GrupoProduto>();
    public DbSet<SubGrupo> SubGrupos => Set<SubGrupo>();
    public DbSet<Secao> Secoes => Set<Secao>();
    public DbSet<SyncFila> SyncFila => Set<SyncFila>();
    public DbSet<SequenciaLocal> SequenciasLocais => Set<SequenciaLocal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Filial ────────────────────────────────────────────────────
        modelBuilder.Entity<Filial>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.NomeFilial).HasMaxLength(100).IsRequired();
            e.Property(x => x.RazaoSocial).HasMaxLength(150).IsRequired();
            e.Property(x => x.NomeFantasia).HasMaxLength(100).IsRequired();
            e.Property(x => x.Cnpj).HasMaxLength(18).IsRequired();
            e.HasIndex(x => x.Cnpj).IsUnique();
            e.Property(x => x.InscricaoEstadual).HasMaxLength(30);
            e.Property(x => x.Cep).HasMaxLength(9).IsRequired();
            e.Property(x => x.Rua).HasMaxLength(200).IsRequired();
            e.Property(x => x.Numero).HasMaxLength(10).IsRequired();
            e.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
            e.Property(x => x.Cidade).HasMaxLength(100).IsRequired();
            e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
        });

        // ── GrupoUsuario (tabela: UsuariosGrupos) ─────────────────────
        modelBuilder.Entity<GrupoUsuario>(e =>
        {
            e.ToTable("UsuariosGrupos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        });

        // ── GrupoPermissao (tabela: UsuariosGruposPermissao) ─────────
        modelBuilder.Entity<GrupoPermissao>(e =>
        {
            e.ToTable("UsuariosGruposPermissao");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CodigoTela).HasMaxLength(50).IsRequired();
            e.Property(x => x.NomeTela).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.GrupoUsuario)
             .WithMany(x => x.Permissoes)
             .HasForeignKey(x => x.GrupoUsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Usuario ───────────────────────────────────────────────────
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Login).HasMaxLength(50).IsRequired();
            e.Property(x => x.SenhaHash).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Login).IsUnique();
            e.HasOne(x => x.GrupoUsuario)
             .WithMany(x => x.Usuarios)
             .HasForeignKey(x => x.GrupoUsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Filial)
             .WithMany(x => x.Usuarios)
             .HasForeignKey(x => x.FilialId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Colaborador)
             .WithOne(x => x.Usuario)
             .HasForeignKey<Usuario>(x => x.ColaboradorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── LogErro ───────────────────────────────────────────────────
        modelBuilder.Entity<LogErro>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Mensagem).HasMaxLength(1000);
        });

        // ── LogAcao ───────────────────────────────────────────────────
        modelBuilder.Entity<LogAcao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tela).HasMaxLength(100).IsRequired();
            e.Property(x => x.Acao).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Usuario)
             .WithMany(x => x.LogsAcoes)
             .HasForeignKey(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.UsuarioLiberou)
             .WithMany()
             .HasForeignKey(x => x.UsuarioLiberouId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Pessoa ────────────────────────────────────────────────────
        modelBuilder.Entity<Pessoa>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tipo).HasMaxLength(1).IsRequired();
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.RazaoSocial).HasMaxLength(150);
            e.Property(x => x.CpfCnpj).HasMaxLength(18).IsRequired();
            e.HasIndex(x => x.CpfCnpj).IsUnique();
            e.Property(x => x.InscricaoEstadual).HasMaxLength(30);
            e.Property(x => x.Rg).HasMaxLength(20);
            e.Property(x => x.Observacao).HasMaxLength(500);
        });

        // ── Colaborador ──────────────────────────────────────────────
        modelBuilder.Entity<Colaborador>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Cargo).HasMaxLength(100);
            e.Property(x => x.Salario).HasColumnType("numeric(18,2)");
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Pessoa)
             .WithOne(x => x.Colaborador)
             .HasForeignKey<Colaborador>(x => x.PessoaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Fornecedor ──────────────────────────────────────────────
        modelBuilder.Entity<Fornecedor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Pessoa)
             .WithOne(x => x.Fornecedor)
             .HasForeignKey<Fornecedor>(x => x.PessoaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UsuarioFilialGrupo ───────────────────────────────────────
        modelBuilder.Entity<UsuarioFilialGrupo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasIndex(x => new { x.UsuarioId, x.FilialId, x.GrupoUsuarioId }).IsUnique();
            e.HasOne(x => x.Usuario).WithMany(x => x.FilialGrupos)
             .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Filial).WithMany(x => x.UsuarioFilialGrupos)
             .HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.GrupoUsuario).WithMany(x => x.UsuarioFilialGrupos)
             .HasForeignKey(x => x.GrupoUsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Configuracao ─────────────────────────────────────────────
        modelBuilder.Entity<Configuracao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Chave).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Chave).IsUnique();
            e.Property(x => x.Valor).HasMaxLength(500).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(200);
        });

        // ── PessoaContato ─────────────────────────────────────────────
        modelBuilder.Entity<PessoaContato>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Valor).HasMaxLength(150).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(100);
            e.HasOne(x => x.Pessoa)
             .WithMany(x => x.Contatos)
             .HasForeignKey(x => x.PessoaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PessoaEndereco ────────────────────────────────────────────
        modelBuilder.Entity<PessoaEndereco>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Cep).HasMaxLength(9).IsRequired();
            e.Property(x => x.Rua).HasMaxLength(200).IsRequired();
            e.Property(x => x.Numero).HasMaxLength(10).IsRequired();
            e.Property(x => x.Complemento).HasMaxLength(100);
            e.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
            e.Property(x => x.Cidade).HasMaxLength(100).IsRequired();
            e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
            e.HasOne(x => x.Pessoa)
             .WithMany(x => x.Enderecos)
             .HasForeignKey(x => x.PessoaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Fabricante ──────────────────────────────────────────────
        modelBuilder.Entity<Fabricante>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        });

        // ── Substancia ───────────────────────────────────────────────
        modelBuilder.Entity<Substancia>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.Dcb).HasMaxLength(200).IsRequired();
            e.Property(x => x.Cas).HasMaxLength(50).IsRequired();
            e.Property(x => x.ClasseTerapeutica).HasMaxLength(100);
        });

        // ── DicionarioTabela ─────────────────────────────────────────
        modelBuilder.Entity<DicionarioTabela>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tabela).HasMaxLength(100).IsRequired();
            e.Property(x => x.Escopo).HasMaxLength(20).HasDefaultValue("global");
            e.Property(x => x.InstrucaoIA).HasMaxLength(1000);
            e.HasIndex(x => x.Tabela).IsUnique();
        });

        // ── DicionarioRevisao ────────────────────────────────────────
        modelBuilder.Entity<DicionarioRevisao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tabela).HasMaxLength(100).IsRequired();
            e.Property(x => x.Coluna).HasMaxLength(100).IsRequired();
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.Property(x => x.InstrucaoIA).HasMaxLength(1000);
            e.HasIndex(x => new { x.Tabela, x.Coluna }).IsUnique();
        });

        // ── DicionarioRelacionamento ─────────────────────────────────
        modelBuilder.Entity<DicionarioRelacionamento>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tabela).HasMaxLength(100).IsRequired();
            e.Property(x => x.ColunaFk).HasMaxLength(100).IsRequired();
            e.Property(x => x.TabelaAlvo).HasMaxLength(100).IsRequired();
            e.Property(x => x.OnDelete).HasMaxLength(20).HasDefaultValue("restrict");
            e.Property(x => x.OnUpdate).HasMaxLength(20).HasDefaultValue("noAction");
            e.HasIndex(x => new { x.Tabela, x.ColunaFk }).IsUnique();
        });

        // ── Classificações de Produto ────────────────────────────────
        ConfigurarClassificacao<GrupoPrincipal>(modelBuilder);
        ConfigurarClassificacao<GrupoProduto>(modelBuilder);
        ConfigurarClassificacao<SubGrupo>(modelBuilder);
        ConfigurarClassificacao<Secao>(modelBuilder);

        // Index on Codigo for sync lookups
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.ClrType != typeof(BaseEntity))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("Codigo")
                    .HasFilter("\"Codigo\" IS NOT NULL");
            }
        }

        // ── SyncFila ────────────────────────────────────────────────
        modelBuilder.Entity<SyncFila>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tabela).HasMaxLength(100).IsRequired();
            e.Property(x => x.Operacao).HasMaxLength(1).IsRequired();
            e.Property(x => x.RegistroCodigo).HasMaxLength(50);
            e.Property(x => x.Erro).HasMaxLength(500);
            e.HasIndex(x => x.Enviado);
            e.HasIndex(x => x.FilialOrigemId);
        });

        // ── SequenciaLocal ──────────────────────────────────────────
        modelBuilder.Entity<SequenciaLocal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tabela).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Tabela).IsUnique();
        });
    }

    private static void ConfigurarClassificacao<T>(ModelBuilder mb) where T : ClassificacaoProdutoBase
    {
        mb.Entity<T>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.Priorizar).HasMaxLength(20);
            e.Property(x => x.ComissaoPercentual).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMinimo).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMaximo).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMaximoComSenha).HasColumnType("numeric(5,2)");
            e.Property(x => x.ProjecaoLucro).HasColumnType("numeric(5,2)");
            e.Property(x => x.MarkupPadrao).HasColumnType("numeric(5,2)");
        });
    }

    // Tabelas que NÃO geram Codigo nem entram na SyncFila
    private static readonly HashSet<string> _tabelasSemSync = new()
    {
        "Configuracoes", "DicionarioTabelas", "DicionarioRevisoes", "DicionarioRelacionamentos",
        "SyncFila", "SequenciaLocal"
    };

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (AplicandoSync)
        {
            // Still update timestamps but don't generate Codigo or register in SyncFila
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.AtualizadoEm = DateTime.UtcNow;
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        // FilialOrigemId usa o codigo do SERVIDOR (config), nao a filial do usuario (JWT)
        var filialOrigem = _filialCodigo > 0 ? _filialCodigo : GetFilialIdFromContext();
        var operacoesPendentes = new List<(string tabela, string op, BaseEntity entidade)>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            var tabela = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.FilialOrigemId == null && filialOrigem > 0)
                    entry.Entity.FilialOrigemId = filialOrigem;

                // Gerar Codigo visível (FilialCodigo.Sequencial)
                if (entry.Entity.Codigo == null && !_tabelasSemSync.Contains(tabela))
                    entry.Entity.Codigo = await GerarCodigo(tabela, cancellationToken);

                if (!_tabelasSemSync.Contains(tabela))
                    operacoesPendentes.Add((tabela, "I", entry.Entity));
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = DateTime.UtcNow;
                if (!_tabelasSemSync.Contains(tabela))
                    operacoesPendentes.Add((tabela, "U", entry.Entity));
            }
            else if (entry.State == EntityState.Deleted)
            {
                if (!_tabelasSemSync.Contains(tabela))
                    operacoesPendentes.Add((tabela, "D", entry.Entity));
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Registrar operações na SyncFila APÓS o save (para ter o Id gerado)
        if (operacoesPendentes.Count > 0)
        {
            foreach (var (tabela, op, entidade) in operacoesPendentes)
            {
                SyncFila.Add(new SyncFila
                {
                    Tabela = tabela,
                    Operacao = op,
                    RegistroId = entidade.Id,
                    RegistroCodigo = entidade.Codigo,
                    DadosJson = op != "D" ? JsonSerializer.Serialize(entidade, entidade.GetType(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }) : null,
                    FilialOrigemId = _filialCodigo > 0 ? _filialCodigo : (entidade.FilialOrigemId ?? 0),
                    Enviado = false
                });
            }
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private async Task<string> GerarCodigo(string tabela, CancellationToken ct)
    {
        var conn = Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            INSERT INTO ""SequenciasLocais"" (""Tabela"", ""Ultimo"")
            VALUES ('{tabela}', 1)
            ON CONFLICT (""Tabela"")
            DO UPDATE SET ""Ultimo"" = ""SequenciasLocais"".""Ultimo"" + 1
            RETURNING ""Ultimo""";

        var result = await cmd.ExecuteScalarAsync(ct);
        var ultimo = Convert.ToInt64(result);
        return $"{_filialCodigo}.{ultimo}";
    }

    private long GetFilialIdFromContext()
    {
        try
        {
            var claim = _http?.HttpContext?.User.FindFirst("filialId")?.Value;
            return long.TryParse(claim, out var id) ? id : 0;
        }
        catch { return 0; }
    }
}
