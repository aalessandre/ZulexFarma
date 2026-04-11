namespace ZulexPharma.Application.DTOs.Colaboradores;

// ── Grid ─────────────────────────────────────────────────────────────
public class ColaboradorListDto
{
    public long   Id              { get; set; }
    public string? Codigo         { get; set; }
    public string Nome            { get; set; } = string.Empty;
    public string Cpf             { get; set; } = string.Empty;
    public string? Rg             { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Cargo          { get; set; }
    public decimal? Salario       { get; set; }
    public string? Email          { get; set; }
    public string? Telefone       { get; set; }
    public string? Cidade         { get; set; }
    public string? Uf             { get; set; }
    public DateTime CriadoEm     { get; set; }
    public bool   Ativo           { get; set; }
}

// ── Detalhe (retorno do GET /{id}) ───────────────────────────────────
public class ColaboradorDetalheDto
{
    public long   Id              { get; set; }
    public string? Codigo         { get; set; }
    public string Nome            { get; set; } = string.Empty;
    public string Cpf             { get; set; } = string.Empty;
    public string? Rg             { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Genero         { get; set; }
    public string? Cargo          { get; set; }
    public DateTime? DataAdmissao { get; set; }
    public decimal? Salario       { get; set; }
    public string? Observacao     { get; set; }
    public bool   Ativo           { get; set; }
    public bool   PermitirAbrirCaixa { get; set; }
    public DateTime CriadoEm     { get; set; }

    public List<EnderecoFormDto> Enderecos { get; set; } = new();
    public List<ContatoFormDto>  Contatos  { get; set; } = new();

    public AcessoDetalheDto? Acesso { get; set; }
}

// ── Formulário (entrada POST / PUT) ──────────────────────────────────
public class ColaboradorFormDto
{
    // Dados pessoais
    public string Nome            { get; set; } = string.Empty;
    public string Cpf             { get; set; } = string.Empty;
    public string? Rg             { get; set; }
    public DateTime? DataNascimento { get; set; }

    // Emprego
    public string? Genero         { get; set; }
    public string? Cargo          { get; set; }
    public DateTime? DataAdmissao { get; set; }
    public decimal? Salario       { get; set; }

    // Endereços e contatos
    public List<EnderecoFormDto> Enderecos { get; set; } = new();
    public List<ContatoFormDto>  Contatos  { get; set; } = new();

    // Acesso ao sistema (opcional)
    public AcessoFormDto? Acesso  { get; set; }

    public string? Observacao     { get; set; }
    public bool    Ativo          { get; set; } = true;
    public bool    PermitirAbrirCaixa { get; set; }
}

// ── Acesso ──────────────────────────────────────────────────────────
public class AcessoFormDto
{
    public string Login           { get; set; } = string.Empty;
    public string? Senha          { get; set; }
    public bool   IsAdministrador { get; set; }

    public int SessaoMaximaMinutos { get; set; }
    public int InatividadeMinutos { get; set; }

    public long FilialPadraoId { get; set; }

    /// <summary>Para cada filial, qual grupo está atribuído. Ausência = SEM ACESSO.</summary>
    public List<FilialGrupoFormDto> FilialGrupos { get; set; } = new();
}

public class FilialGrupoFormDto
{
    public long FilialId       { get; set; }
    public long GrupoUsuarioId { get; set; }
}

public class AcessoDetalheDto
{
    public long   UsuarioId       { get; set; }
    public string Login           { get; set; } = string.Empty;
    public bool   IsAdministrador { get; set; }

    public int SessaoMaximaMinutos { get; set; }
    public int InatividadeMinutos { get; set; }

    public long FilialPadraoId { get; set; }
    public string NomeFilialPadrao { get; set; } = string.Empty;

    public List<FilialGrupoDetalheDto> FilialGrupos { get; set; } = new();
}

public class FilialGrupoDetalheDto
{
    public long   FilialId       { get; set; }
    public string NomeFilial     { get; set; } = string.Empty;
    public long   GrupoUsuarioId { get; set; }
    public string NomeGrupo      { get; set; } = string.Empty;
}

// ── Sub-DTOs ─────────────────────────────────────────────────────────
public class EnderecoFormDto
{
    public long?  Id          { get; set; }
    public string Tipo        { get; set; } = "PRINCIPAL";
    public string Cep         { get; set; } = string.Empty;
    public string Rua         { get; set; } = string.Empty;
    public string Numero      { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro      { get; set; } = string.Empty;
    public string Cidade      { get; set; } = string.Empty;
    public string Uf          { get; set; } = string.Empty;
    public bool   Principal   { get; set; }
}

public class ContatoFormDto
{
    public long?  Id        { get; set; }
    public string Tipo      { get; set; } = string.Empty;
    public string Valor     { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool   Principal { get; set; }
}
