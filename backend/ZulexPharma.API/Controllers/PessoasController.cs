using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PessoasController : ControllerBase
{
    private readonly AppDbContext _db;

    public PessoasController(AppDbContext db) => _db = db;

    /// <summary>
    /// Pesquisa pessoas por nome ou CPF/CNPJ (mínimo 3 caracteres). Limite 30 resultados.
    /// Filtro opcional: tipo = fornecedor | cliente | todos
    /// </summary>
    [HttpGet("pesquisar")]
    public async Task<IActionResult> Pesquisar([FromQuery] string termo, [FromQuery] string? tipo = null)
    {
        if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 3)
            return Ok(new { success = true, data = Array.Empty<object>() });

        var termoNorm = termo.Trim().ToUpper();

        var query = _db.Pessoas
            .Include(p => p.Fornecedor)
            .Where(p => p.Ativo && (p.Nome.ToUpper().Contains(termoNorm) || p.CpfCnpj.Contains(termoNorm)));

        if (tipo == "fornecedor")
            query = query.Where(p => p.Fornecedor != null);
        else if (tipo == "cliente")
            query = query.Where(p => p.Fornecedor == null);

        var lista = await query
            .OrderBy(p => p.Nome)
            .Take(30)
            .Select(p => new
            {
                id = p.Id,
                nome = p.Nome,
                cpfCnpj = p.CpfCnpj,
                tipoPessoa = p.Tipo,
                ehFornecedor = p.Fornecedor != null
            })
            .ToListAsync();

        return Ok(new { success = true, data = lista });
    }

    /// <summary>
    /// Busca uma Pessoa por CPF/CNPJ. Retorna dados + flags de vínculos.
    /// Usado pelo frontend para reutilizar Pessoa existente ao cadastrar Colaborador/Fornecedor.
    /// </summary>
    [HttpGet("buscar-cpfcnpj")]
    public async Task<IActionResult> BuscarPorCpfCnpj([FromQuery] string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return Ok(new { success = true, data = (object?)null });

        var pessoa = await _db.Pessoas
            .Include(p => p.Colaborador)
            .Include(p => p.Fornecedor)
            .Include(p => p.Contatos)
            .Include(p => p.Enderecos)
            .FirstOrDefaultAsync(p => p.CpfCnpj == valor.Trim());

        if (pessoa == null)
            return Ok(new { success = true, data = (object?)null });

        return Ok(new
        {
            success = true,
            data = new
            {
                pessoaId = pessoa.Id,
                tipo = pessoa.Tipo,
                nome = pessoa.Nome,
                razaoSocial = pessoa.RazaoSocial,
                cpfCnpj = pessoa.CpfCnpj,
                inscricaoEstadual = pessoa.InscricaoEstadual,
                rg = pessoa.Rg,
                dataNascimento = pessoa.DataNascimento,
                observacao = pessoa.Observacao,
                temColaborador = pessoa.Colaborador != null,
                temFornecedor = pessoa.Fornecedor != null,
                enderecos = pessoa.Enderecos.Select(e => new
                {
                    id = e.Id,
                    tipo = e.Tipo,
                    cep = e.Cep,
                    rua = e.Rua,
                    numero = e.Numero,
                    complemento = e.Complemento,
                    bairro = e.Bairro,
                    cidade = e.Cidade,
                    uf = e.Uf,
                    principal = e.Principal
                }),
                contatos = pessoa.Contatos.Select(c => new
                {
                    id = c.Id,
                    tipo = c.Tipo,
                    valor = c.Valor,
                    descricao = c.Descricao,
                    principal = c.Principal
                })
            }
        });
    }
}
