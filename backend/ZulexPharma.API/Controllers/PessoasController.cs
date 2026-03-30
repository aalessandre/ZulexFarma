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
