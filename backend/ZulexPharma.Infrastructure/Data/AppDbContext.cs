using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZulexPharma.Domain.Entities;

namespace ZulexPharma.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _http;
    private readonly long _filialLocalId;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? http = null, IConfiguration? config = null) : base(options)
    {
        _http = http;
        _filialLocalId = long.TryParse(config?["Sync:FilialLocalId"], out var f) ? f : 0;
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
    public DbSet<SyncControle> SyncControles => Set<SyncControle>();
    public DbSet<DicionarioRevisao> DicionarioRevisoes => Set<DicionarioRevisao>();
    public DbSet<Fabricante> Fabricantes => Set<Fabricante>();
    public DbSet<Substancia> Substancias => Set<Substancia>();
    public DbSet<GrupoPrincipal> GruposPrincipais => Set<GrupoPrincipal>();
    public DbSet<GrupoProduto> GruposProdutos => Set<GrupoProduto>();
    public DbSet<SubGrupo> SubGrupos => Set<SubGrupo>();
    public DbSet<Secao> Secoes => Set<Secao>();

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
             .OnDelete(DeleteBehavior.Cascade);
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
             .OnDelete(DeleteBehavior.SetNull);
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
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Fornecedor ──────────────────────────────────────────────
        modelBuilder.Entity<Fornecedor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Pessoa)
             .WithOne(x => x.Fornecedor)
             .HasForeignKey<Fornecedor>(x => x.PessoaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UsuarioFilialGrupo ───────────────────────────────────────
        modelBuilder.Entity<UsuarioFilialGrupo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasIndex(x => new { x.UsuarioId, x.FilialId, x.GrupoUsuarioId }).IsUnique();
            e.HasOne(x => x.Usuario).WithMany(x => x.FilialGrupos)
             .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
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

        // ── SyncControle ─────────────────────────────────────────────
        modelBuilder.Entity<SyncControle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tabela).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.MensagemErro).HasMaxLength(500);
            e.HasIndex(x => new { x.FilialId, x.Tabela }).IsUnique();
        });

        // ── DicionarioRevisao ────────────────────────────────────────
        modelBuilder.Entity<DicionarioRevisao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tabela).HasMaxLength(100).IsRequired();
            e.Property(x => x.Coluna).HasMaxLength(100).IsRequired();
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasIndex(x => new { x.Tabela, x.Coluna }).IsUnique();
        });

        // ── Classificações de Produto ────────────────────────────────
        ConfigurarClassificacao<GrupoPrincipal>(modelBuilder);
        ConfigurarClassificacao<GrupoProduto>(modelBuilder);
        ConfigurarClassificacao<SubGrupo>(modelBuilder);
        ConfigurarClassificacao<Secao>(modelBuilder);
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

    /// <summary>
    /// Quando true, SaveChangesAsync NÃO incrementa VersaoSync nem altera FilialOrigemId.
    /// Usado pelo SyncBackgroundService ao aplicar registros recebidos do servidor central.
    /// </summary>
    public bool SuspenderAutoSync { get; set; }

    // Garante VersaoSync monotonicamente crescente (mesmo em saves no mesmo milissegundo)
    private static long _lastVersaoSync = 0;
    private static readonly object _versaoLock = new();

    private static long ProximaVersaoSync()
    {
        lock (_versaoLock)
        {
            var agora = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastVersaoSync = Math.Max(agora, _lastVersaoSync + 1);
            return _lastVersaoSync;
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!SuspenderAutoSync)
        {
            // FilialOrigemId: usa FilialLocalId da config (identifica a farmácia/servidor),
            // com fallback para o filialId do JWT do usuário
            var filialId = _filialLocalId > 0 ? _filialLocalId : GetFilialIdFromContext();

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.AtualizadoEm = DateTime.UtcNow;
                    entry.Entity.VersaoSync = ProximaVersaoSync();
                }
                else if (entry.State == EntityState.Added)
                {
                    entry.Entity.VersaoSync = ProximaVersaoSync();
                    if (entry.Entity.FilialOrigemId == null && filialId > 0)
                        entry.Entity.FilialOrigemId = filialId;
                }
            }
        }
        return base.SaveChangesAsync(cancellationToken);
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
