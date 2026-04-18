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
    public DbSet<ColaboradorComissaoAgrupador> ColaboradorComissaoAgrupadores => Set<ColaboradorComissaoAgrupador>();
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
    public DbSet<ComissaoFaixaDesconto> ComissaoFaixasDesconto => Set<ComissaoFaixaDesconto>();
    public DbSet<ProdutoFamilia> ProdutoFamilias => Set<ProdutoFamilia>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<ProdutoBarras> ProdutosBarras => Set<ProdutoBarras>();
    public DbSet<ProdutoMs> ProdutosMs => Set<ProdutoMs>();
    public DbSet<ProdutoSubstancia> ProdutosSubstancias => Set<ProdutoSubstancia>();
    public DbSet<ProdutoFornecedor> ProdutosFornecedores => Set<ProdutoFornecedor>();
    public DbSet<ProdutoFiscal> ProdutosFiscal => Set<ProdutoFiscal>();
    public DbSet<ProdutoDados> ProdutosDados => Set<ProdutoDados>();
    public DbSet<ProdutoLocal> ProdutosLocais => Set<ProdutoLocal>();
    public DbSet<SyncFila> SyncFila => Set<SyncFila>();
    public DbSet<SequenciaLocal> SequenciasLocais => Set<SequenciaLocal>();
    public DbSet<Ncm> Ncms => Set<Ncm>();
    public DbSet<NcmFederal> NcmFederais => Set<NcmFederal>();
    public DbSet<NcmIcmsUf> NcmIcmsUfs => Set<NcmIcmsUf>();
    public DbSet<NcmStUf> NcmStUfs => Set<NcmStUf>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraProduto> ComprasProdutos => Set<CompraProduto>();
    public DbSet<CompraProdutoLote> ComprasProdutosLotes => Set<CompraProdutoLote>();
    public DbSet<CompraFiscal> ComprasFiscal => Set<CompraFiscal>();
    public DbSet<ProdutoLote> ProdutosLotes => Set<ProdutoLote>();
    public DbSet<MovimentoLote> MovimentosLote => Set<MovimentoLote>();
    public DbSet<InventarioSngpc> InventariosSngpc => Set<InventarioSngpc>();
    public DbSet<InventarioSngpcItem> InventariosSngpcItens => Set<InventarioSngpcItem>();
    public DbSet<SngpcMapa> SngpcMapas => Set<SngpcMapa>();
    public DbSet<GestorTributarioJob> GestorTributarioJobs => Set<GestorTributarioJob>();
    public DbSet<GestorTributarioUsoMensal> GestorTributarioUsoMensais => Set<GestorTributarioUsoMensal>();
    public DbSet<IcmsUf> IcmsUfs => Set<IcmsUf>();
    public DbSet<CertificadoDigital> CertificadosDigitais => Set<CertificadoDigital>();
    public DbSet<SefazNota> SefazNotas => Set<SefazNota>();
    public DbSet<AbcFarmaBase> AbcFarmaBase => Set<AbcFarmaBase>();
    public DbSet<AtualizacaoPreco> AtualizacoesPreco => Set<AtualizacaoPreco>();
    public DbSet<AtualizacaoPrecoItem> AtualizacoesPrecoItens => Set<AtualizacaoPrecoItem>();
    public DbSet<PlanoConta> PlanosContas => Set<PlanoConta>();
    public DbSet<ContaBancaria> ContasBancarias => Set<ContaBancaria>();
    public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();
    public DbSet<TipoPagamento> TiposPagamento => Set<TipoPagamento>();
    public DbSet<Convenio> Convenios => Set<Convenio>();
    public DbSet<ConvenioDesconto> ConvenioDescontos => Set<ConvenioDesconto>();
    public DbSet<ConvenioBloqueio> ConvenioBloqueios => Set<ConvenioBloqueio>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();
    public DbSet<PromocaoFilial> PromocaoFiliais => Set<PromocaoFilial>();
    public DbSet<PromocaoPagamento> PromocaoPagamentos => Set<PromocaoPagamento>();
    public DbSet<PromocaoConvenio> PromocaoConvenios => Set<PromocaoConvenio>();
    public DbSet<PromocaoProduto> PromocaoProdutos => Set<PromocaoProduto>();
    public DbSet<PromocaoFaixa> PromocaoFaixas => Set<PromocaoFaixa>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ClienteConvenio> ClienteConvenios => Set<ClienteConvenio>();
    public DbSet<ClienteAutorizacao> ClienteAutorizacoes => Set<ClienteAutorizacao>();
    public DbSet<ClienteDesconto> ClienteDescontos => Set<ClienteDesconto>();
    public DbSet<ClienteBloqueio> ClienteBloqueios => Set<ClienteBloqueio>();
    public DbSet<ClienteUsoContinuo> ClienteUsosContinuos => Set<ClienteUsoContinuo>();
    public DbSet<HierarquiaDesconto> HierarquiaDescontos => Set<HierarquiaDesconto>();
    public DbSet<HierarquiaDescontoItem> HierarquiaDescontoItens => Set<HierarquiaDescontoItem>();
    public DbSet<HierarquiaDescontoSecao> HierarquiaDescontoSecoes => Set<HierarquiaDescontoSecao>();
    public DbSet<HierarquiaDescontoColaborador> HierarquiaDescontoColaboradores => Set<HierarquiaDescontoColaborador>();
    public DbSet<HierarquiaDescontoConvenio> HierarquiaDescontoConvenios => Set<HierarquiaDescontoConvenio>();
    public DbSet<HierarquiaDescontoCliente> HierarquiaDescontoClientes => Set<HierarquiaDescontoCliente>();
    public DbSet<HierarquiaComissao> HierarquiasComissao => Set<HierarquiaComissao>();
    public DbSet<HierarquiaComissaoItem> HierarquiaComissaoItens => Set<HierarquiaComissaoItem>();
    public DbSet<HierarquiaComissaoSecao> HierarquiaComissaoSecoes => Set<HierarquiaComissaoSecao>();
    public DbSet<HierarquiaComissaoColaborador> HierarquiaComissaoColaboradores => Set<HierarquiaComissaoColaborador>();
    public DbSet<Caixa> Caixas => Set<Caixa>();
    public DbSet<CaixaMovimento> CaixaMovimentos => Set<CaixaMovimento>();
    public DbSet<CaixaFechamentoDeclarado> CaixaFechamentoDeclarados => Set<CaixaFechamentoDeclarado>();
    public DbSet<MovimentoContaBancaria> MovimentosContaBancaria => Set<MovimentoContaBancaria>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<VendaItem> VendaItens => Set<VendaItem>();
    public DbSet<VendaItemDesconto> VendaItemDescontos => Set<VendaItemDesconto>();
    public DbSet<VendaPagamento> VendaPagamentos => Set<VendaPagamento>();
    public DbSet<VendaReceita> VendaReceitas => Set<VendaReceita>();
    public DbSet<VendaReceitaItem> VendaReceitaItens => Set<VendaReceitaItem>();
    public DbSet<VendaFiscal> VendaFiscais => Set<VendaFiscal>();
    public DbSet<VendaItemFiscal> VendaItemFiscais => Set<VendaItemFiscal>();
    public DbSet<Prescritor> Prescritores => Set<Prescritor>();
    public DbSet<Adquirente> Adquirentes => Set<Adquirente>();
    public DbSet<AdquirenteBandeira> AdquirenteBandeiras => Set<AdquirenteBandeira>();
    public DbSet<AdquirenteTarifa> AdquirenteTarifas => Set<AdquirenteTarifa>();
    public DbSet<ContaReceber> ContasReceber => Set<ContaReceber>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<IbptTax> IbptTaxes => Set<IbptTax>();
    public DbSet<CampanhaFidelidade> CampanhasFidelidade => Set<CampanhaFidelidade>();
    public DbSet<CampanhaFidelidadeFilial> CampanhasFidelidadeFiliais => Set<CampanhaFidelidadeFilial>();
    public DbSet<CampanhaFidelidadePagamento> CampanhasFidelidadePagamentos => Set<CampanhaFidelidadePagamento>();
    public DbSet<CampanhaFidelidadeItem> CampanhasFidelidadeItens => Set<CampanhaFidelidadeItem>();
    public DbSet<PremioFidelidade> PremiosFidelidade => Set<PremioFidelidade>();
    public DbSet<NaturezaOperacao> NaturezasOperacao => Set<NaturezaOperacao>();
    public DbSet<NaturezaOperacaoRegra> NaturezaOperacaoRegras => Set<NaturezaOperacaoRegra>();
    public DbSet<Municipio> Municipios => Set<Municipio>();

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
            e.Property(x => x.AliquotaIcms).HasColumnType("numeric(5,2)");
            e.Property(x => x.CodigoIbgeMunicipio).HasMaxLength(10);
            e.HasOne(x => x.ContaCofre).WithMany().HasForeignKey(x => x.ContaCofreId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Municipio).WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Municipio (tabela IBGE seed) ─────────────────────────────
        modelBuilder.Entity<Municipio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CodigoIbge).HasMaxLength(7).IsRequired();
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.NomeNormalizado).HasMaxLength(100).IsRequired();
            e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
            e.HasIndex(x => x.CodigoIbge).IsUnique();
            e.HasIndex(x => new { x.Uf, x.NomeNormalizado });
        });

        // ── IcmsUf ────────────────────────────────────────────────────
        modelBuilder.Entity<IcmsUf>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
            e.Property(x => x.NomeEstado).HasMaxLength(50).IsRequired();
            e.Property(x => x.AliquotaInterna).HasColumnType("numeric(5,2)");
            e.HasIndex(x => x.Uf).IsUnique();
        });

        // ── SefazNota (cache — NÃO replica) ──────────────────────────
        modelBuilder.Entity<SefazNota>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ChaveNfe).HasMaxLength(44).IsRequired();
            e.Property(x => x.Cnpj).HasMaxLength(18);
            e.Property(x => x.RazaoSocial).HasMaxLength(200);
            e.Property(x => x.NumeroNf).HasMaxLength(20);
            e.Property(x => x.SerieNf).HasMaxLength(5);
            e.Property(x => x.Situacao).HasMaxLength(20);
            e.Property(x => x.TipoDocumento).HasMaxLength(20);
            e.Property(x => x.TipoManifestacao).HasMaxLength(30);
            e.HasIndex(x => new { x.FilialId, x.ChaveNfe }).IsUnique();
            e.HasIndex(x => x.FilialId);
        });

        // ── CertificadoDigital ────────────────────────────────────────
        modelBuilder.Entity<CertificadoDigital>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Cnpj).HasMaxLength(18);
            e.Property(x => x.RazaoSocial).HasMaxLength(200);
            e.Property(x => x.Emissor).HasMaxLength(300);
            e.HasIndex(x => x.FilialId).IsUnique();
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
            e.Property(x => x.Apelido).HasMaxLength(150);
            e.Property(x => x.Cargo).HasMaxLength(100);
            e.Property(x => x.Salario).HasColumnType("numeric(18,2)");
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Pessoa)
             .WithOne(x => x.Colaborador)
             .HasForeignKey<Colaborador>(x => x.PessoaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ColaboradorComissaoAgrupador ─────────────────────────────
        modelBuilder.Entity<ColaboradorComissaoAgrupador>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.AgrupadorNome).HasMaxLength(200);
            e.Property(x => x.ComissaoPercentual).HasColumnType("numeric(5,2)");
            e.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ColaboradorId);
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
            e.Property(x => x.CodigoIbgeMunicipio).HasMaxLength(10);
            e.HasOne(x => x.Pessoa)
             .WithMany(x => x.Enderecos)
             .HasForeignKey(x => x.PessoaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Municipio).WithMany().HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
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
            e.Property(x => x.ListaPortaria344).HasMaxLength(4);
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

        // ── NCM ──────────────────────────────────────────────────────
        modelBuilder.Entity<Ncm>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CodigoNcm).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.CodigoNcm).IsUnique();
            e.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
            e.Property(x => x.ExTipi).HasMaxLength(5);
            e.Property(x => x.UnidadeTributavel).HasMaxLength(6);
        });

        modelBuilder.Entity<NcmFederal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CstIpi).HasMaxLength(3);
            e.Property(x => x.CstPis).HasMaxLength(2);
            e.Property(x => x.CstCofins).HasMaxLength(2);
            e.Property(x => x.AliquotaIi).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIpi).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaPis).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaCofins).HasColumnType("numeric(5,2)");
            e.HasOne(x => x.Ncm).WithMany(x => x.Federais)
             .HasForeignKey(x => x.NcmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NcmIcmsUf>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
            e.Property(x => x.CstIcms).HasMaxLength(3);
            e.Property(x => x.Csosn).HasMaxLength(4);
            e.Property(x => x.AliquotaIcms).HasColumnType("numeric(5,2)");
            e.Property(x => x.ReducaoBaseCalculo).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaFcp).HasColumnType("numeric(5,2)");
            e.Property(x => x.Cbenef).HasMaxLength(10);
            e.HasOne(x => x.Ncm).WithMany(x => x.IcmsUfs)
             .HasForeignKey(x => x.NcmId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NcmId, x.Uf }).IsUnique();
        });

        modelBuilder.Entity<NcmStUf>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.UfOrigem).HasMaxLength(2).IsRequired();
            e.Property(x => x.UfDestino).HasMaxLength(2).IsRequired();
            e.Property(x => x.Mva).HasColumnType("numeric(5,2)");
            e.Property(x => x.MvaAjustado).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIcmsSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.ReducaoBaseCalculoSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.Cest).HasMaxLength(9);
            e.HasOne(x => x.Ncm).WithMany(x => x.StUfs)
             .HasForeignKey(x => x.NcmId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NcmId, x.UfOrigem, x.UfDestino }).IsUnique();
        });

        // ── Classificações de Produto ────────────────────────────────
        ConfigurarClassificacao<GrupoPrincipal>(modelBuilder);
        ConfigurarClassificacao<GrupoProduto>(modelBuilder);
        ConfigurarClassificacao<SubGrupo>(modelBuilder);
        ConfigurarClassificacao<Secao>(modelBuilder);

        // ── Comissão Faixas Desconto ────────────────────────────────
        modelBuilder.Entity<ComissaoFaixaDesconto>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.TipoEntidade).HasMaxLength(30).IsRequired();
            e.Property(x => x.DescontoInicial).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoFinal).HasColumnType("numeric(5,2)");
            e.Property(x => x.ComissaoPercentual).HasColumnType("numeric(5,2)");
            e.HasIndex(x => new { x.TipoEntidade, x.EntidadeId });
        });

        // ── Produto Família ─────────────────────────────────────────
        modelBuilder.Entity<ProdutoFamilia>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        });

        // ── Produto Local ───────────────────────────────────────────
        modelBuilder.Entity<ProdutoLocal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        });

        // ── Produto ─────────────────────────────────────────────────
        modelBuilder.Entity<Produto>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(300).IsRequired();
            e.Property(x => x.CodigoBarras).HasMaxLength(20);
            e.Property(x => x.Lista).HasMaxLength(20).HasDefaultValue("Indefinida");
            e.Property(x => x.PrecoFp).HasColumnType("numeric(10,4)");
            e.Property(x => x.ClasseTerapeutica).HasMaxLength(30);

            e.HasOne(x => x.Fabricante).WithMany().HasForeignKey(x => x.FabricanteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.GrupoPrincipal).WithMany().HasForeignKey(x => x.GrupoPrincipalId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.GrupoProduto).WithMany().HasForeignKey(x => x.GrupoProdutoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SubGrupo).WithMany().HasForeignKey(x => x.SubGrupoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Ncm).WithMany().HasForeignKey(x => x.NcmId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProdutoBarras>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Barras).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Produto).WithMany(p => p.Barras).HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProdutoMs>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.NumeroMs).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Produto).WithMany(p => p.RegistrosMs).HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProdutoSubstancia>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Produto).WithMany(p => p.Substancias).HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Substancia).WithMany().HasForeignKey(x => x.SubstanciaId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProdutoId, x.SubstanciaId }).IsUnique();
        });

        modelBuilder.Entity<ProdutoFornecedor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CodigoProdutoFornecedor).HasMaxLength(24);
            e.Property(x => x.NomeProduto).HasMaxLength(300);
            e.HasOne(x => x.Produto).WithMany(p => p.Fornecedores).HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProdutoId, x.FilialId, x.FornecedorId }).IsUnique();
        });

        modelBuilder.Entity<ProdutoFiscal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Produto).WithMany(p => p.Fiscais).HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Ncm).WithMany().HasForeignKey(x => x.NcmId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.ProdutoId, x.FilialId }).IsUnique();
            e.Property(x => x.Cest).HasMaxLength(9);
            e.Property(x => x.OrigemMercadoria).HasMaxLength(1);
            e.Property(x => x.CstIcms).HasMaxLength(3);
            e.Property(x => x.Csosn).HasMaxLength(4);
            e.Property(x => x.CstPis).HasMaxLength(2);
            e.Property(x => x.CstCofins).HasMaxLength(2);
            e.Property(x => x.CstIpi).HasMaxLength(3);
            e.Property(x => x.AliquotaIcms).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaPis).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaCofins).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIpi).HasColumnType("numeric(5,2)");

            // Campos extras vindos do Gestor Tributário
            e.Property(x => x.AliquotaFcp).HasColumnType("numeric(5,2)");
            e.Property(x => x.ModBc).HasMaxLength(2);
            e.Property(x => x.PercentualReducaoBc).HasColumnType("numeric(6,2)");
            e.Property(x => x.CodigoBeneficio).HasMaxLength(10);
            e.Property(x => x.DispositivoLegalIcms).HasMaxLength(200);
            e.Property(x => x.MvaOriginal).HasColumnType("numeric(7,2)");
            e.Property(x => x.MvaAjustado4).HasColumnType("numeric(7,2)");
            e.Property(x => x.MvaAjustado7).HasColumnType("numeric(7,2)");
            e.Property(x => x.MvaAjustado12).HasColumnType("numeric(7,2)");
            e.Property(x => x.AliquotaIcmsSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaFcpSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIcmsInternoEntrada).HasColumnType("numeric(5,2)");
            e.Property(x => x.NaturezaReceita).HasMaxLength(50);
            e.Property(x => x.EnquadramentoIpi).HasMaxLength(10);
            e.Property(x => x.CstPisEntrada).HasMaxLength(2);
            e.Property(x => x.CstCofinsEntrada).HasMaxLength(2);
            e.Property(x => x.CstIpiEntrada).HasMaxLength(3);
            e.Property(x => x.AliquotaIpiEntrada).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIpiIndustria).HasColumnType("numeric(5,2)");
            // Reforma Tributária 2026+
            e.Property(x => x.CstIs).HasMaxLength(3);
            e.Property(x => x.ClassTribIs).HasMaxLength(10);
            e.Property(x => x.AliquotaIs).HasColumnType("numeric(5,2)");
            e.Property(x => x.CstIbsCbs).HasMaxLength(3);
            e.Property(x => x.ClassTribIbsCbs).HasMaxLength(10);
            e.Property(x => x.AliquotaIbsUf).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIbsMun).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaCbs).HasColumnType("numeric(5,2)");
            e.Property(x => x.AtualizadoGestorTributarioProvider).HasMaxLength(30);
        });

        // ── GestorTributarioJob ───────────────────────────────────
        modelBuilder.Entity<GestorTributarioJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Provider).HasMaxLength(30).IsRequired();
            e.Property(x => x.MensagemErro).HasMaxLength(2000);
            e.Property(x => x.FiltroJson).HasColumnType("text");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.Status, x.CriadoEm });
        });

        // ── GestorTributarioUsoMensal ─────────────────────────────
        modelBuilder.Entity<GestorTributarioUsoMensal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Provider).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.Ano, x.Mes, x.Provider }).IsUnique();
        });

        modelBuilder.Entity<ProdutoDados>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Produto).WithMany(p => p.Dados).HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProdutoId, x.FilialId }).IsUnique();

            // Estoque
            e.Property(x => x.EstoqueAtual).HasColumnType("numeric(10,3)");
            e.Property(x => x.EstoqueMinimo).HasColumnType("numeric(10,3)");
            e.Property(x => x.EstoqueMaximo).HasColumnType("numeric(10,3)");
            e.Property(x => x.Demanda).HasColumnType("numeric(10,3)");
            e.Property(x => x.CurvaAbc).HasMaxLength(1);

            // Preços
            e.Property(x => x.UltimaCompraUnitario).HasColumnType("numeric(10,4)");
            e.Property(x => x.UltimaCompraSt).HasColumnType("numeric(10,4)");
            e.Property(x => x.UltimaCompraOutros).HasColumnType("numeric(10,4)");
            e.Property(x => x.UltimaCompraIpi).HasColumnType("numeric(10,4)");
            e.Property(x => x.UltimaCompraFpc).HasColumnType("numeric(10,4)");
            e.Property(x => x.UltimaCompraBoleto).HasColumnType("numeric(10,4)");
            e.Property(x => x.UltimaCompraDifal).HasColumnType("numeric(10,4)");
            e.Property(x => x.UltimaCompraFrete).HasColumnType("numeric(10,4)");
            e.Property(x => x.CustoMedio).HasColumnType("numeric(10,4)");
            e.Property(x => x.ProjecaoLucro).HasColumnType("numeric(5,2)");
            e.Property(x => x.Markup).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorVenda).HasColumnType("numeric(10,4)");
            e.Property(x => x.Pmc).HasColumnType("numeric(10,4)");
            e.Property(x => x.PrecoFabrica).HasColumnType("numeric(10,4)");
            e.Property(x => x.ValorPromocao).HasColumnType("numeric(10,4)");
            e.Property(x => x.ValorPromocaoPrazo).HasColumnType("numeric(10,4)");
            e.Property(x => x.EstoqueDeposito).HasColumnType("numeric(10,4)");

            // Descontos / Geral
            e.Property(x => x.DescontoMinimo).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMaxSemSenha).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMaxComSenha).HasColumnType("numeric(5,2)");
            e.Property(x => x.Comissao).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorIncentivo).HasColumnType("numeric(10,4)");
            e.Property(x => x.NomeEtiqueta).HasMaxLength(110);
            e.Property(x => x.Mensagem).HasMaxLength(200);
            e.Property(x => x.Lote).HasMaxLength(30);
            e.Property(x => x.BaseCalculo).HasMaxLength(20);
        });

        // ── Compras (cabeçalho NF entrada) ─────────────────────────
        modelBuilder.Entity<Compra>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ChaveNfe).HasMaxLength(44).IsRequired();
            e.Property(x => x.NumeroNf).HasMaxLength(20).IsRequired();
            e.Property(x => x.SerieNf).HasMaxLength(5);
            e.Property(x => x.NaturezaOperacao).HasMaxLength(100);
            e.Property(x => x.ValorProdutos).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorSt).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorFcpSt).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorFrete).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorSeguro).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorDesconto).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorIpi).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorPis).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorCofins).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorOutros).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorNota).HasColumnType("numeric(12,2)");
            e.Property(x => x.XmlConteudo).HasColumnType("text");
            e.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ChaveNfe).IsUnique();
            e.HasIndex(x => x.FilialId);
        });

        // ── ComprasProdutos (itens da NF) ──────────────────────────
        modelBuilder.Entity<CompraProduto>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CodigoProdutoFornecedor).HasMaxLength(60);
            e.Property(x => x.CodigoBarrasXml).HasMaxLength(14);
            e.Property(x => x.DescricaoXml).HasMaxLength(300);
            e.Property(x => x.NcmXml).HasMaxLength(10);
            e.Property(x => x.CestXml).HasMaxLength(9);
            e.Property(x => x.CfopXml).HasMaxLength(4);
            e.Property(x => x.UnidadeXml).HasMaxLength(6);
            e.Property(x => x.Quantidade).HasColumnType("numeric(12,4)");
            e.Property(x => x.ValorUnitario).HasColumnType("numeric(12,6)");
            e.Property(x => x.ValorTotal).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorDesconto).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorFrete).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorOutros).HasColumnType("numeric(12,2)");
            e.Property(x => x.ValorItemNota).HasColumnType("numeric(12,2)");
            e.Property(x => x.Lote).HasMaxLength(30);
            e.Property(x => x.CodigoAnvisa).HasMaxLength(20);
            e.Property(x => x.PrecoMaximoConsumidor).HasColumnType("numeric(10,2)");
            e.Property(x => x.InfoAdicional).HasMaxLength(500);
            e.HasOne(x => x.Compra).WithMany(c => c.Produtos).HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.CompraId, x.NumeroItem }).IsUnique();
            e.Property(x => x.SugestaoVenda).HasColumnType("numeric(12,2)");
            e.Property(x => x.SugestaoMarkup).HasColumnType("numeric(7,2)");
            e.Property(x => x.SugestaoProjecao).HasColumnType("numeric(7,2)");
            e.Property(x => x.SugestaoCustoMedio).HasColumnType("numeric(12,4)");
        });

        // ── CompraProdutoLote (rastros/lotes do XML da compra) ────
        modelBuilder.Entity<CompraProdutoLote>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.NumeroLote).HasMaxLength(30).IsRequired();
            e.Property(x => x.Quantidade).HasColumnType("numeric(12,4)");
            e.Property(x => x.RegistroMs).HasMaxLength(30);
            e.Property(x => x.NumeroLoteOriginal).HasMaxLength(30);
            e.Property(x => x.RegistroMsOriginal).HasMaxLength(30);
            e.HasOne(x => x.CompraProduto).WithMany(c => c.Lotes).HasForeignKey(x => x.CompraProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.EditadoPorUsuario).WithMany().HasForeignKey(x => x.EditadoPorUsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.CompraProdutoId);
        });

        // ── ProdutoLote (lote do produto por filial) ──────────────
        modelBuilder.Entity<ProdutoLote>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.NumeroLote).HasMaxLength(30).IsRequired();
            e.Property(x => x.SaldoAtual).HasColumnType("numeric(12,4)");
            e.Property(x => x.RegistroMs).HasMaxLength(30);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Compra).WithMany().HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.SetNull);
            // Índice de consulta rápida por (filial, produto, vencimento) — base do FEFO
            e.HasIndex(x => new { x.FilialId, x.ProdutoId, x.DataValidade });
            e.HasIndex(x => new { x.FilialId, x.ProdutoId, x.NumeroLote });
        });

        // ── MovimentoLote (histórico de cada lote) ────────────────
        modelBuilder.Entity<MovimentoLote>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Quantidade).HasColumnType("numeric(12,4)");
            e.Property(x => x.SaldoAposMovimento).HasColumnType("numeric(12,4)");
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.ProdutoLote).WithMany(p => p.Movimentos).HasForeignKey(x => x.ProdutoLoteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Compra).WithMany().HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Venda).WithMany().HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CompraProdutoLote).WithMany().HasForeignKey(x => x.CompraProdutoLoteId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.ProdutoLoteId, x.DataMovimento });
        });

        // ── InventarioSngpc (cabeçalho) ───────────────────────────
        modelBuilder.Entity<InventarioSngpc>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Descricao).HasMaxLength(200);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.FilialId, x.DataInventario });
        });

        modelBuilder.Entity<InventarioSngpcItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.NumeroLote).HasMaxLength(30).IsRequired();
            e.Property(x => x.Quantidade).HasColumnType("numeric(12,4)");
            e.Property(x => x.RegistroMs).HasMaxLength(30);
            e.Property(x => x.Observacao).HasMaxLength(300);
            e.HasOne(x => x.Inventario).WithMany(i => i.Itens).HasForeignKey(x => x.InventarioSngpcId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── SngpcMapa (relatório mensal Anvisa) ──────────────────
        modelBuilder.Entity<SngpcMapa>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.XmlConteudo).HasColumnType("text");
            e.Property(x => x.ProtocoloAnvisa).HasMaxLength(100);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.FilialId, x.CompetenciaAno, x.CompetenciaMes }).IsUnique();
        });

        // ── ComprasFiscal (dados fiscais por item) ─────────────────
        modelBuilder.Entity<CompraFiscal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.OrigemMercadoria).HasMaxLength(1);
            e.Property(x => x.CstIcms).HasMaxLength(3);
            e.Property(x => x.BaseIcms).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaIcms).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorIcms).HasColumnType("numeric(12,2)");
            e.Property(x => x.ModalidadeBcSt).HasMaxLength(1);
            e.Property(x => x.MvaSt).HasColumnType("numeric(7,2)");
            e.Property(x => x.BaseSt).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorSt).HasColumnType("numeric(12,2)");
            e.Property(x => x.BaseFcpSt).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaFcpSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorFcpSt).HasColumnType("numeric(12,2)");
            e.Property(x => x.CstPis).HasMaxLength(2);
            e.Property(x => x.BasePis).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaPis).HasColumnType("numeric(7,4)");
            e.Property(x => x.ValorPis).HasColumnType("numeric(12,2)");
            e.Property(x => x.CstCofins).HasMaxLength(2);
            e.Property(x => x.BaseCofins).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaCofins).HasColumnType("numeric(7,4)");
            e.Property(x => x.ValorCofins).HasColumnType("numeric(12,2)");
            e.Property(x => x.CstIbsCbs).HasMaxLength(3);
            e.Property(x => x.ClasseTributariaIbsCbs).HasMaxLength(10);
            e.Property(x => x.BaseIbsCbs).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaIbsUf).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorIbsUf).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaIbsMun).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorIbsMun).HasColumnType("numeric(12,2)");
            e.Property(x => x.AliquotaCbs).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorCbs).HasColumnType("numeric(12,2)");
            e.HasOne(x => x.CompraProduto).WithOne(p => p.Fiscal).HasForeignKey<CompraFiscal>(x => x.CompraProdutoId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── AbcFarmaBase (NÃO herda BaseEntity, NÃO replica) ────────
        modelBuilder.Entity<AbcFarmaBase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Ean).HasMaxLength(14).IsRequired();
            e.Property(x => x.RegistroAnvisa).HasMaxLength(20);
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.Property(x => x.Composicao).HasMaxLength(500);
            e.Property(x => x.NomeFabricante).HasMaxLength(200);
            e.Property(x => x.ClasseTerapeutica).HasMaxLength(200);
            e.Property(x => x.Ncm).HasMaxLength(10);
            e.HasIndex(x => x.Ean);
            e.HasIndex(x => x.RegistroAnvisa);
        });

        // ── AtualizacaoPreco (histórico de atualizações) ───────────
        modelBuilder.Entity<AtualizacaoPreco>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.Property(x => x.NomeUsuario).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.FilialId);
        });

        // ── AtualizacaoPrecoItem ───────────────────────────────────
        modelBuilder.Entity<AtualizacaoPrecoItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ProdutoNome).HasMaxLength(200);
            e.HasOne(x => x.AtualizacaoPreco).WithMany(a => a.Itens).HasForeignKey(x => x.AtualizacaoPrecoId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Caixa ─────────────────────────────────────────────────
        modelBuilder.Entity<Caixa>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ValorAbertura).HasPrecision(18, 2);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.Property(x => x.ModeloFechamento).HasMaxLength(30);
            e.HasOne(x => x.Filial).WithMany().HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Status);
        });

        // ── CaixaMovimento ───────────────────────────────────────────
        modelBuilder.Entity<CaixaMovimento>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.Descricao).HasMaxLength(200);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Caixa).WithMany(x => x.Movimentos).HasForeignKey(x => x.CaixaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.VendaPagamento).WithMany().HasForeignKey(x => x.VendaPagamentoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ContaReceber).WithMany().HasForeignKey(x => x.ContaReceberId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ContaPagar).WithMany().HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.CaixaId);
            e.HasIndex(x => x.Tipo);
            e.HasIndex(x => x.StatusConferencia);
        });

        // ── CaixaFechamentoDeclarado ─────────────────────────────────
        modelBuilder.Entity<CaixaFechamentoDeclarado>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ValorDeclarado).HasPrecision(18, 2);
            e.HasOne(x => x.Caixa).WithMany(x => x.Declarados).HasForeignKey(x => x.CaixaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CaixaId, x.TipoPagamentoId }).IsUnique();
        });

        // ── MovimentoContaBancaria ───────────────────────────────────
        modelBuilder.Entity<MovimentoContaBancaria>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.Descricao).HasMaxLength(200);
            e.HasOne(x => x.ContaBancaria).WithMany().HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CaixaMovimento).WithMany().HasForeignKey(x => x.CaixaMovimentoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Caixa).WithMany().HasForeignKey(x => x.CaixaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.ContaBancariaId, x.DataMovimento });
        });

        // ── Venda ─────────────────────────────────────────────────
        modelBuilder.Entity<Venda>(e =>
        {
            e.ToTable("Vendas");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.NrCesta).HasMaxLength(20);
            e.Property(x => x.TotalBruto).HasPrecision(18, 2);
            e.Property(x => x.TotalDesconto).HasPrecision(18, 2);
            e.Property(x => x.TotalLiquido).HasPrecision(18, 2);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.Property(x => x.NumeroBoletim).HasMaxLength(50);
            e.HasOne(x => x.Filial).WithMany().HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FilialDestino).WithMany().HasForeignKey(x => x.FilialDestinoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Caixa).WithMany().HasForeignKey(x => x.CaixaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.NaturezaOperacao).WithMany().HasForeignKey(x => x.NaturezaOperacaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DestinatarioPessoa).WithMany().HasForeignKey(x => x.DestinatarioPessoaId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.NrCesta);
            e.HasIndex(x => new { x.FilialId, x.TipoOperacao });
            e.HasIndex(x => new { x.FilialId, x.StatusFiscal });
        });
        modelBuilder.Entity<VendaItem>(e =>
        {
            e.ToTable("VendaItens");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ProdutoCodigo).HasMaxLength(50);
            e.Property(x => x.ProdutoNome).HasMaxLength(300);
            e.Property(x => x.Fabricante).HasMaxLength(200);
            e.Property(x => x.PrecoVenda).HasPrecision(18, 2);
            // Quantidade é int, não precisa de precision
            e.Property(x => x.PercentualDesconto).HasPrecision(8, 4);
            e.Property(x => x.PercentualPromocao).HasPrecision(8, 4);
            e.Property(x => x.ValorDesconto).HasPrecision(18, 2);
            e.Property(x => x.PrecoUnitario).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasOne(x => x.Venda).WithMany(x => x.Itens).HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<VendaItemDesconto>(e =>
        {
            e.ToTable("VendaItemDescontos");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tipo).IsRequired();
            e.Property(x => x.Percentual).HasPrecision(8, 4);
            e.Property(x => x.Origem).HasMaxLength(100).IsRequired();
            e.Property(x => x.Regra).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.VendaItem).WithMany(x => x.Descontos).HasForeignKey(x => x.VendaItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LiberadoPor).WithMany().HasForeignKey(x => x.LiberadoPorId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<VendaPagamento>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.Troco).HasPrecision(18, 2);
            e.Property(x => x.TrocoPara).HasMaxLength(50);
            e.HasOne(x => x.Venda).WithMany(x => x.Pagamentos).HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Prescritor ───────────────────────────────────────────
        modelBuilder.Entity<Prescritor>(e =>
        {
            e.ToTable("Prescritores");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.TipoConselho).HasMaxLength(10).IsRequired();
            e.Property(x => x.NumeroConselho).HasMaxLength(20).IsRequired();
            e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
            e.Property(x => x.Cpf).HasMaxLength(14);
            e.Property(x => x.Especialidade).HasMaxLength(100);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.HasIndex(x => new { x.TipoConselho, x.NumeroConselho, x.Uf });
            e.HasIndex(x => x.Nome);
        });

        // ── VendaReceita ─────────────────────────────────────────
        modelBuilder.Entity<VendaReceita>(e =>
        {
            e.ToTable("VendaReceitas");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tipo).IsRequired();
            e.Property(x => x.NumeroNotificacao).HasMaxLength(30);
            e.Property(x => x.Cid).HasMaxLength(10);
            e.Property(x => x.PacienteNome).HasMaxLength(200).IsRequired();
            e.Property(x => x.PacienteCpf).HasMaxLength(14);
            e.Property(x => x.PacienteRg).HasMaxLength(20);
            e.Property(x => x.PacienteSexo).HasMaxLength(1);
            e.Property(x => x.PacienteEndereco).HasMaxLength(200);
            e.Property(x => x.PacienteNumero).HasMaxLength(10);
            e.Property(x => x.PacienteBairro).HasMaxLength(100);
            e.Property(x => x.PacienteCidade).HasMaxLength(100);
            e.Property(x => x.PacienteUf).HasMaxLength(2);
            e.Property(x => x.PacienteCep).HasMaxLength(9);
            e.Property(x => x.PacienteTelefone).HasMaxLength(20);
            e.Property(x => x.CompradorNome).HasMaxLength(200);
            e.Property(x => x.CompradorCpf).HasMaxLength(14);
            e.Property(x => x.CompradorRg).HasMaxLength(20);
            e.Property(x => x.CompradorEndereco).HasMaxLength(200);
            e.Property(x => x.CompradorNumero).HasMaxLength(10);
            e.Property(x => x.CompradorBairro).HasMaxLength(100);
            e.Property(x => x.CompradorCidade).HasMaxLength(100);
            e.Property(x => x.CompradorUf).HasMaxLength(2);
            e.Property(x => x.CompradorCep).HasMaxLength(9);
            e.Property(x => x.CompradorTelefone).HasMaxLength(20);
            e.HasOne(x => x.Venda).WithMany(x => x.Receitas).HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Filial).WithMany().HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Prescritor).WithMany().HasForeignKey(x => x.PrescritorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.FilialId);
        });

        // ── VendaReceitaItem ─────────────────────────────────────
        modelBuilder.Entity<VendaReceitaItem>(e =>
        {
            e.ToTable("VendaReceitaItens");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Quantidade).HasPrecision(18, 3);
            e.HasOne(x => x.VendaReceita).WithMany(x => x.Itens).HasForeignKey(x => x.VendaReceitaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.VendaItem).WithMany().HasForeignKey(x => x.VendaItemId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProdutoLote).WithMany().HasForeignKey(x => x.ProdutoLoteId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Adquirente ───────────────────────────────────────────
        modelBuilder.Entity<Adquirente>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        });
        modelBuilder.Entity<AdquirenteBandeira>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Bandeira).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Adquirente).WithMany(x => x.Bandeiras).HasForeignKey(x => x.AdquirenteId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AdquirenteTarifa>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Tarifa).HasPrecision(8, 4);
            e.HasOne(x => x.AdquirenteBandeira).WithMany(x => x.Tarifas).HasForeignKey(x => x.AdquirenteBandeiraId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ContaBancaria).WithMany().HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── ContaReceber ─────────────────────────────────────────
        modelBuilder.Entity<ContaReceber>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.ValorLiquido).HasPrecision(18, 2);
            e.Property(x => x.Tarifa).HasPrecision(8, 4);
            e.Property(x => x.ValorTarifa).HasPrecision(18, 2);
            e.Property(x => x.ValorRecebido).HasPrecision(18, 2);
            e.Property(x => x.ValorJuros).HasPrecision(18, 2);
            e.Property(x => x.ValorDesconto).HasPrecision(18, 2);
            e.Property(x => x.NSU).HasMaxLength(50);
            e.Property(x => x.TxId).HasMaxLength(100);
            e.Property(x => x.Modalidade).HasMaxLength(50);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Filial).WithMany().HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Venda).WithMany().HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.VendaPagamento).WithMany().HasForeignKey(x => x.VendaPagamentoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Pessoa).WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PlanoConta).WithMany().HasForeignKey(x => x.PlanoContaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ContaBancaria).WithMany().HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AdquirenteBandeira).WithMany().HasForeignKey(x => x.AdquirenteBandeiraId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AdquirenteTarifa).WithMany().HasForeignKey(x => x.AdquirenteTarifaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Voucher).WithMany().HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DataVencimento);
        });

        // ── Voucher ──────────────────────────────────────────────
        modelBuilder.Entity<Voucher>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.ValorUtilizado).HasPrecision(18, 2);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VendaOrigem).WithMany().HasForeignKey(x => x.VendaOrigemId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── IbptTax ───────────────────────────────────────────────
        modelBuilder.Entity<IbptTax>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Ncm).HasMaxLength(10).IsRequired();
            e.Property(x => x.Ex).HasMaxLength(5);
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.AliqNacional).HasColumnType("numeric(7,2)");
            e.Property(x => x.AliqImportado).HasColumnType("numeric(7,2)");
            e.Property(x => x.AliqEstadual).HasColumnType("numeric(7,2)");
            e.Property(x => x.AliqMunicipal).HasColumnType("numeric(7,2)");
            e.Property(x => x.Chave).HasMaxLength(100);
            e.Property(x => x.Versao).HasMaxLength(20);
            e.Property(x => x.Fonte).HasMaxLength(30);
            e.Property(x => x.Uf).HasMaxLength(2);
            e.HasIndex(x => new { x.Ncm, x.Uf, x.Ex }).HasDatabaseName("IX_IbptTax_Ncm_Uf_Ex");
        });

        // ── Fidelidade ────────────────────────────────────────────
        modelBuilder.Entity<CampanhaFidelidade>(e =>
        {
            e.ToTable("CampanhasFidelidade");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.Tipo).IsRequired();
            e.Property(x => x.ModoContagem).IsRequired();
            e.Property(x => x.ValorBase).HasPrecision(18, 2);
            e.Property(x => x.PercentualCashback).HasPrecision(5, 2);
            e.Property(x => x.ValorPorPonto).HasPrecision(10, 4);
            e.HasIndex(x => new { x.Tipo, x.Ativo });
        });
        modelBuilder.Entity<CampanhaFidelidadeFilial>(e =>
        {
            e.ToTable("CampanhasFidelidadeFiliais");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.CampanhaFidelidade).WithMany(x => x.Filiais).HasForeignKey(x => x.CampanhaFidelidadeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Filial).WithMany().HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CampanhaFidelidadeId, x.FilialId }).IsUnique();
        });
        modelBuilder.Entity<CampanhaFidelidadePagamento>(e =>
        {
            e.ToTable("CampanhasFidelidadePagamentos");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.CampanhaFidelidade).WithMany(x => x.Pagamentos).HasForeignKey(x => x.CampanhaFidelidadeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CampanhaFidelidadeId, x.TipoPagamentoId }).IsUnique();
        });
        modelBuilder.Entity<CampanhaFidelidadeItem>(e =>
        {
            e.ToTable("CampanhasFidelidadeItens");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.CampanhaFidelidade).WithMany(x => x.Itens).HasForeignKey(x => x.CampanhaFidelidadeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GrupoPrincipal).WithMany().HasForeignKey(x => x.GrupoPrincipalId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GrupoProduto).WithMany().HasForeignKey(x => x.GrupoProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SubGrupo).WithMany().HasForeignKey(x => x.SubGrupoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Secao).WithMany().HasForeignKey(x => x.SecaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ProdutoFamilia).WithMany().HasForeignKey(x => x.ProdutoFamiliaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Fabricante).WithMany().HasForeignKey(x => x.FabricanteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.ValorVendaReferencia).HasColumnType("numeric(18,2)");
            e.Property(x => x.PercentualCashbackItem).HasColumnType("numeric(5,2)");
            e.Property(x => x.ValorCashbackItem).HasColumnType("numeric(18,2)");
        });
        modelBuilder.Entity<PremioFidelidade>(e =>
        {
            e.ToTable("PremiosFidelidade");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.Categoria).HasMaxLength(100);
            e.Property(x => x.ImagemUrl).HasMaxLength(500);
            e.HasIndex(x => x.Nome);
        });

        // ── HierarquiaDesconto ────────────────────────────────────
        modelBuilder.Entity<HierarquiaDesconto>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        });
        modelBuilder.Entity<HierarquiaDescontoItem>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaDesconto).WithMany(x => x.Itens).HasForeignKey(x => x.HierarquiaDescontoId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<HierarquiaDescontoSecao>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaDescontoItem).WithMany(x => x.Secoes).HasForeignKey(x => x.HierarquiaDescontoItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Secao).WithMany().HasForeignKey(x => x.SecaoId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<HierarquiaDescontoColaborador>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaDesconto).WithMany(x => x.Colaboradores).HasForeignKey(x => x.HierarquiaDescontoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<HierarquiaDescontoConvenio>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaDesconto).WithMany(x => x.Convenios).HasForeignKey(x => x.HierarquiaDescontoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Convenio).WithMany().HasForeignKey(x => x.ConvenioId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<HierarquiaDescontoCliente>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaDesconto).WithMany(x => x.Clientes).HasForeignKey(x => x.HierarquiaDescontoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── HierarquiaComissao ───────────────────────────────────
        modelBuilder.Entity<HierarquiaComissao>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        });
        modelBuilder.Entity<HierarquiaComissaoItem>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaComissao).WithMany(x => x.Itens).HasForeignKey(x => x.HierarquiaComissaoId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<HierarquiaComissaoSecao>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaComissaoItem).WithMany(x => x.Secoes).HasForeignKey(x => x.HierarquiaComissaoItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Secao).WithMany().HasForeignKey(x => x.SecaoId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<HierarquiaComissaoColaborador>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.HierarquiaComissao).WithMany(x => x.Colaboradores).HasForeignKey(x => x.HierarquiaComissaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Cliente ───────────────────────────────────────────────
        modelBuilder.Entity<Cliente>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.LimiteCredito).HasPrecision(18, 2);
            e.Property(x => x.DescontoGeral).HasPrecision(5, 2);
            e.Property(x => x.Aviso).HasMaxLength(200);
            e.Property(x => x.Observacao).HasMaxLength(1000);
            e.Property(x => x.SenhaVendaPrazo).HasMaxLength(50);
            e.HasOne(x => x.Pessoa).WithOne(p => p.Cliente).HasForeignKey<Cliente>(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ClienteConvenio>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Matricula).HasMaxLength(50);
            e.Property(x => x.Cartao).HasMaxLength(50);
            e.Property(x => x.Limite).HasPrecision(18, 2);
            e.HasOne(x => x.Cliente).WithMany(x => x.Convenios).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Convenio).WithMany().HasForeignKey(x => x.ConvenioId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClienteAutorizacao>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Cliente).WithMany(x => x.Autorizacoes).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClienteDesconto>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.AgrupadorOuProdutoNome).HasMaxLength(200);
            e.Property(x => x.DescontoMinimo).HasPrecision(5, 2);
            e.Property(x => x.DescontoMaxSemSenha).HasPrecision(5, 2);
            e.Property(x => x.DescontoMaxComSenha).HasPrecision(5, 2);
            e.HasOne(x => x.Cliente).WithMany(x => x.Descontos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClienteBloqueio>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Cliente).WithMany(x => x.Bloqueios).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClienteId, x.TipoPagamentoId }).IsUnique();
        });
        modelBuilder.Entity<ClienteUsoContinuo>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Fabricante).HasMaxLength(200);
            e.Property(x => x.ColaboradorNome).HasMaxLength(200);
            e.HasOne(x => x.Cliente).WithMany(x => x.UsosContinuos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Promocao ──────────────────────────────────────────────
        modelBuilder.Entity<Promocao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.Tipo).IsRequired();
            e.Property(x => x.ReducaoVendaPrazo).HasPrecision(5, 2);
        });

        modelBuilder.Entity<PromocaoFilial>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Promocao).WithMany(x => x.Filiais).HasForeignKey(x => x.PromocaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Filial).WithMany().HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromocaoPagamento>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Promocao).WithMany(x => x.Pagamentos).HasForeignKey(x => x.PromocaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromocaoConvenio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Promocao).WithMany(x => x.Convenios).HasForeignKey(x => x.PromocaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Convenio).WithMany().HasForeignKey(x => x.ConvenioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromocaoProduto>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PercentualPromocao).HasPrecision(8, 4);
            e.Property(x => x.ValorPromocao).HasPrecision(18, 2);
            e.Property(x => x.PercentualLucro).HasPrecision(8, 4);
            e.Property(x => x.PercentualAposLimite).HasPrecision(8, 4);
            e.Property(x => x.ValorAposLimite).HasPrecision(18, 2);
            e.HasOne(x => x.Promocao).WithMany(x => x.Produtos).HasForeignKey(x => x.PromocaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromocaoFaixa>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PercentualDesconto).HasPrecision(8, 4);
            e.HasOne(x => x.Promocao).WithMany(x => x.Faixas).HasForeignKey(x => x.PromocaoId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Convenio ───────────────────────────────────────────────
        modelBuilder.Entity<Convenio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Aviso).HasMaxLength(200);
            e.Property(x => x.Observacao).HasMaxLength(1000);
            e.Property(x => x.ModoFechamento).IsRequired();
            e.Property(x => x.LimiteCredito).HasPrecision(18, 2);
            e.HasOne(x => x.Pessoa).WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.PessoaId).IsUnique();
        });

        modelBuilder.Entity<ConvenioDesconto>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.AgrupadorNome).HasMaxLength(200);
            e.Property(x => x.DescontoMinimo).HasPrecision(5, 2);
            e.Property(x => x.DescontoMaxSemSenha).HasPrecision(5, 2);
            e.Property(x => x.DescontoMaxComSenha).HasPrecision(5, 2);
            e.HasOne(x => x.Convenio).WithMany(x => x.Descontos).HasForeignKey(x => x.ConvenioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConvenioBloqueio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasOne(x => x.Convenio).WithMany(x => x.Bloqueios).HasForeignKey(x => x.ConvenioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoPagamento).WithMany().HasForeignKey(x => x.TipoPagamentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ConvenioId, x.TipoPagamentoId }).IsUnique();
        });

        // ── TipoPagamento ──────────────────────────────────────────
        modelBuilder.Entity<TipoPagamento>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.Modalidade).IsRequired();
            e.Property(x => x.DescontoMinimo).HasPrecision(5, 2);
            e.Property(x => x.DescontoMaxSemSenha).HasPrecision(5, 2);
            e.Property(x => x.DescontoMaxComSenha).HasPrecision(5, 2);
        });

        // ── ContaPagar ─────────────────────────────────────────────
        modelBuilder.Entity<ContaPagar>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.Desconto).HasPrecision(18, 2);
            e.Property(x => x.Juros).HasPrecision(18, 2);
            e.Property(x => x.Multa).HasPrecision(18, 2);
            e.Property(x => x.ValorFinal).HasPrecision(18, 2);
            e.Property(x => x.NrDocumento).HasMaxLength(100);
            e.Property(x => x.NrNotaFiscal).HasMaxLength(100);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.Property(x => x.RecorrenciaParcela).HasMaxLength(10);
            e.HasOne(x => x.Pessoa).WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PlanoConta).WithMany().HasForeignKey(x => x.PlanoContaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Filial).WithMany().HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DataVencimento);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.RecorrenciaGrupo);
        });

        // ── PlanoConta ─────────────────────────────────────────────
        modelBuilder.Entity<PlanoConta>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.Nivel).IsRequired();
            e.Property(x => x.Natureza).IsRequired();
            e.Property(x => x.Ordem).IsRequired();
            e.HasOne(x => x.ContaPai)
             .WithMany()
             .HasForeignKey(x => x.ContaPaiId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ContaPaiId);
        });

        // ── ContaBancaria ──────────────────────────────────────────
        modelBuilder.Entity<ContaBancaria>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.TipoConta).IsRequired();
            e.Property(x => x.Banco).HasMaxLength(100);
            e.Property(x => x.Agencia).HasMaxLength(20);
            e.Property(x => x.AgenciaDigito).HasMaxLength(5);
            e.Property(x => x.NumeroConta).HasMaxLength(30);
            e.Property(x => x.ContaDigito).HasMaxLength(5);
            e.Property(x => x.ChavePix).HasMaxLength(200);
            e.Property(x => x.SaldoInicial).HasPrecision(18, 2);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.PlanoConta)
             .WithMany()
             .HasForeignKey(x => x.PlanoContaId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Filial)
             .WithMany()
             .HasForeignKey(x => x.FilialId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Configurações globais para todas as entidades BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.ClrType != typeof(BaseEntity))
            {
                // Index on Codigo for sync lookups
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("Codigo")
                    .HasFilter("\"Codigo\" IS NOT NULL");

                // SyncGuid: default para registros existentes + index para reconciliação
                modelBuilder.Entity(entityType.ClrType)
                    .Property("SyncGuid")
                    .HasDefaultValueSql("gen_random_uuid()");
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("SyncGuid");
            }
        }

        // ── NaturezaOperacao ──────────────────────────────────────────
        modelBuilder.Entity<NaturezaOperacao>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.CstPisPadrao).HasMaxLength(5);
            e.Property(x => x.CstCofinsPadrao).HasMaxLength(5);
            e.Property(x => x.CstIpiPadrao).HasMaxLength(5);
            e.Property(x => x.EnquadramentoIpiPadrao).HasMaxLength(10);
            e.Property(x => x.Observacao).HasMaxLength(500);
        });

        // ── NaturezaOperacaoRegra ────────────────────────────────────
        modelBuilder.Entity<NaturezaOperacaoRegra>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CfopInterno).HasMaxLength(5);
            e.Property(x => x.CfopInterestadual).HasMaxLength(5);
            e.Property(x => x.CstIcmsInterno).HasMaxLength(5);
            e.Property(x => x.CstIcmsInterestadual).HasMaxLength(5);
            e.Property(x => x.CodigoBeneficioInterno).HasMaxLength(15);
            e.Property(x => x.CodigoBeneficioInterestadual).HasMaxLength(15);
            e.HasOne(x => x.NaturezaOperacao).WithMany(x => x.Regras).HasForeignKey(x => x.NaturezaOperacaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.NaturezaOperacaoId);
        });

        // ── VendaFiscal (1:1 Venda, só quando emite documento) ─────
        modelBuilder.Entity<VendaFiscal>(e =>
        {
            e.ToTable("VendaFiscais");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ChaveAcesso).HasMaxLength(44);
            e.Property(x => x.Protocolo).HasMaxLength(20);
            e.Property(x => x.NatOp).HasMaxLength(200);
            e.Property(x => x.MotivoStatus).HasMaxLength(500);
            e.Property(x => x.PlacaVeiculo).HasMaxLength(10);
            e.Property(x => x.UfVeiculo).HasMaxLength(2);
            e.Property(x => x.VolumeEspecie).HasMaxLength(50);
            e.Property(x => x.ChaveNfeReferenciada).HasMaxLength(44);
            e.Property(x => x.Observacao).HasMaxLength(2000);
            // Totais
            e.Property(x => x.ValorProdutos).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorDesconto).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorFrete).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorSeguro).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorOutros).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorIcms).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorIcmsSt).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorIpi).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorPis).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorCofins).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorNota).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorTotalTributos).HasColumnType("numeric(18,2)");
            e.Property(x => x.VolumePesoLiquido).HasColumnType("numeric(18,3)");
            e.Property(x => x.VolumePesoBruto).HasColumnType("numeric(18,3)");
            // FKs
            e.HasOne(x => x.Venda).WithOne(x => x.Fiscal).HasForeignKey<VendaFiscal>(x => x.VendaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.NaturezaOperacao).WithMany().HasForeignKey(x => x.NaturezaOperacaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TransportadoraPessoa).WithMany().HasForeignKey(x => x.TransportadoraPessoaId).OnDelete(DeleteBehavior.SetNull);
            // Indexes
            e.HasIndex(x => x.VendaId).IsUnique();
            e.HasIndex(x => x.ChaveAcesso).IsUnique().HasFilter("\"ChaveAcesso\" <> ''");
        });

        // ── VendaItemFiscal (1:1 VendaItem, snapshot fiscal) ───────
        modelBuilder.Entity<VendaItemFiscal>(e =>
        {
            e.ToTable("VendaItemFiscais");
            e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.CodigoProduto).HasMaxLength(60);
            e.Property(x => x.CodigoBarras).HasMaxLength(20);
            e.Property(x => x.DescricaoProduto).HasMaxLength(300);
            e.Property(x => x.Ncm).HasMaxLength(10);
            e.Property(x => x.Cest).HasMaxLength(10);
            e.Property(x => x.Cfop).HasMaxLength(5);
            e.Property(x => x.Unidade).HasMaxLength(6);
            e.Property(x => x.CodigoAnvisa).HasMaxLength(20);
            e.Property(x => x.RastroLote).HasMaxLength(40);
            e.Property(x => x.OrigemMercadoria).HasMaxLength(2);
            e.Property(x => x.CstIcms).HasMaxLength(5);
            e.Property(x => x.Csosn).HasMaxLength(5);
            e.Property(x => x.ModBcIcms).HasMaxLength(2);
            e.Property(x => x.MotivoDesoneracaoIcms).HasMaxLength(5);
            e.Property(x => x.CodigoBeneficioFiscal).HasMaxLength(20);
            e.Property(x => x.ModBcIcmsSt).HasMaxLength(2);
            e.Property(x => x.CstPis).HasMaxLength(5);
            e.Property(x => x.CstCofins).HasMaxLength(5);
            e.Property(x => x.CstIpi).HasMaxLength(5);
            e.Property(x => x.EnquadramentoIpi).HasMaxLength(10);
            e.Property(x => x.RastroQuantidade).HasColumnType("numeric(18,4)");
            e.Property(x => x.CustoUnitario).HasColumnType("numeric(18,4)");
            // Monetarios
            e.Property(x => x.ValorFrete).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorSeguro).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorOutros).HasColumnType("numeric(18,2)");
            e.Property(x => x.BaseIcms).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorIcms).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorIcmsDesonerado).HasColumnType("numeric(18,2)");
            e.Property(x => x.BaseIcmsSt).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorIcmsSt).HasColumnType("numeric(18,2)");
            e.Property(x => x.BaseFcp).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorFcp).HasColumnType("numeric(18,2)");
            e.Property(x => x.BaseFcpSt).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorFcpSt).HasColumnType("numeric(18,2)");
            e.Property(x => x.BasePis).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorPis).HasColumnType("numeric(18,2)");
            e.Property(x => x.BaseCofins).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorCofins).HasColumnType("numeric(18,2)");
            e.Property(x => x.BaseIpi).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorIpi).HasColumnType("numeric(18,2)");
            e.Property(x => x.ValorTotalTributos).HasColumnType("numeric(18,2)");
            // Percentuais
            e.Property(x => x.AliquotaIcms).HasColumnType("numeric(5,2)");
            e.Property(x => x.PercentualReducaoBc).HasColumnType("numeric(5,2)");
            e.Property(x => x.MvaSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIcmsSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaFcp).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaFcpSt).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaPis).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaCofins).HasColumnType("numeric(5,2)");
            e.Property(x => x.AliquotaIpi).HasColumnType("numeric(5,2)");
            // FKs
            e.HasOne(x => x.VendaItem).WithOne(x => x.Fiscal).HasForeignKey<VendaItemFiscal>(x => x.VendaItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.VendaItemId).IsUnique();
        });

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
            e.Property(x => x.BaseCalculo).HasMaxLength(20).HasDefaultValue("CUSTO_COMPRA");
            e.Property(x => x.ComissaoPercentual).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMinimo).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMaximo).HasColumnType("numeric(5,2)");
            e.Property(x => x.DescontoMaximoComSenha).HasColumnType("numeric(5,2)");
            e.Property(x => x.ProjecaoLucro).HasColumnType("numeric(5,2)");
            e.Property(x => x.MarkupPadrao).HasColumnType("numeric(5,2)");
        });
    }

    // Tabelas que NÃO geram Codigo nem entram na SyncFila
    // Tabelas que NÃO geram SyncFila (infraestrutura de sync apenas).
    // REGRA: todas as filiais veem dados de todas as filiais. TUDO replica.
    // Nota: DicionarioTabelas/Revisoes/Relacionamentos não herdam BaseEntity,
    // então não passam pelo interceptor — não precisam estar aqui.
    private static readonly HashSet<string> _tabelasSemSync = new()
    {
        "SyncFila", "SequenciasLocais", "AbcFarmaBase", "CertificadosDigitais", "SefazNotas"
    };

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (AplicandoSync)
        {
            // Still update timestamps but don't generate Codigo or register in SyncFila
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.AtualizadoEm = Domain.Helpers.DataHoraHelper.Agora();
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
                entry.Entity.AtualizadoEm = Domain.Helpers.DataHoraHelper.Agora();
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
        return $"{_filialCodigo}{ultimo}";
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
