namespace ZulexPharma.Application.DTOs.Fornecedores;

// ── Grid ─────────────────────────────────────────────────────────────
public class FornecedorListDto
{
    public long   Id               { get; set; }
    public string? Codigo          { get; set; }
    public string Tipo             { get; set; } = "J";
    public string Nome             { get; set; } = string.Empty;
    public string? RazaoSocial     { get; set; }
    public string CpfCnpj          { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? Email           { get; set; }
    public string? Telefone        { get; set; }
    public string? Cidade          { get; set; }
    public string? Uf              { get; set; }
    public DateTime CriadoEm      { get; set; }
    public bool   Ativo            { get; set; }
}

// ── Detalhe (retorno do GET /{id}) ───────────────────────────────────
public class FornecedorDetalheDto
{
    public long   Id               { get; set; }
    public string? Codigo          { get; set; }
    public string Tipo             { get; set; } = "J";
    public string Nome             { get; set; } = string.Empty;
    public string? RazaoSocial     { get; set; }
    public string CpfCnpj          { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? Rg              { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Observacao      { get; set; }
    public bool   Ativo            { get; set; }
    public DateTime CriadoEm      { get; set; }

    public List<EnderecoFormDto> Enderecos { get; set; } = new();
    public List<ContatoFormDto>  Contatos  { get; set; } = new();
}

// ── Formulário (entrada POST / PUT) ──────────────────────────────────
public class FornecedorFormDto
{
    public string Tipo             { get; set; } = "J";
    public string Nome             { get; set; } = string.Empty;
    public string? RazaoSocial     { get; set; }
    public string CpfCnpj          { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? Rg              { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Observacao      { get; set; }
    public bool   Ativo            { get; set; } = true;

    public List<EnderecoFormDto> Enderecos { get; set; } = new();
    public List<ContatoFormDto>  Contatos  { get; set; } = new();
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
