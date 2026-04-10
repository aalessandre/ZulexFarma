using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.ContasReceber;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContasReceberController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContasReceberController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? filialId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? tipoPagamento = null,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null,
        [FromQuery] string? busca = null)
    {
        try
        {
            var query = _db.ContasReceber
                .Include(c => c.Cliente).ThenInclude(c => c!.Pessoa)
                .Include(c => c.TipoPagamento)
                .Include(c => c.AdquirenteBandeira).ThenInclude(b => b!.Adquirente)
                .AsQueryable();

            if (filialId.HasValue) query = query.Where(c => c.FilialId == filialId);

            if (status == "aberta") query = query.Where(c => c.Status == StatusContaReceber.Aberta);
            else if (status == "recebida") query = query.Where(c => c.Status == StatusContaReceber.Recebida);
            else if (status == "cancelada") query = query.Where(c => c.Status == StatusContaReceber.Cancelada);
            else if (status == "vencida") query = query.Where(c => c.Status == StatusContaReceber.Aberta && c.DataVencimento < DataHoraHelper.Hoje());

            if (!string.IsNullOrWhiteSpace(tipoPagamento))
            {
                if (tipoPagamento == "cartao") query = query.Where(c => c.TipoPagamento != null && c.TipoPagamento.Modalidade == ModalidadePagamento.VendaCartao);
                else if (tipoPagamento == "pix") query = query.Where(c => c.TipoPagamento != null && c.TipoPagamento.Modalidade == ModalidadePagamento.VendaPix);
                else if (tipoPagamento == "prazo") query = query.Where(c => c.TipoPagamento != null && c.TipoPagamento.Modalidade == ModalidadePagamento.VendaPrazo);
                else if (tipoPagamento == "vista") query = query.Where(c => c.TipoPagamento != null && c.TipoPagamento.Modalidade == ModalidadePagamento.VendaVista);
                else if (tipoPagamento == "voucher") query = query.Where(c => c.TipoPagamento != null && c.TipoPagamento.Modalidade == ModalidadePagamento.Voucher);
            }

            if (dataInicio.HasValue) query = query.Where(c => c.DataEmissao >= dataInicio.Value.Date);
            if (dataFim.HasValue) query = query.Where(c => c.DataEmissao <= dataFim.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToUpper();
                query = query.Where(c => c.Descricao.ToUpper().Contains(termo)
                    || (c.NSU != null && c.NSU.Contains(termo))
                    || (c.Cliente != null && c.Cliente.Pessoa.Nome.ToUpper().Contains(termo)));
            }

            var hoje = DataHoraHelper.Hoje();
            var lista = await query.OrderByDescending(c => c.DataEmissao)
                .Take(500)
                .Select(c => new ContaReceberListDto
                {
                    Id = c.Id, Codigo = c.Codigo, Descricao = c.Descricao,
                    ClienteNome = c.Cliente != null ? c.Cliente.Pessoa.Nome : null,
                    TipoPagamentoNome = c.TipoPagamento != null ? c.TipoPagamento.Nome : null,
                    Modalidade = c.Modalidade,
                    Valor = c.Valor, ValorLiquido = c.ValorLiquido,
                    Tarifa = c.Tarifa, ValorTarifa = c.ValorTarifa,
                    NumParcela = c.NumParcela, TotalParcelas = c.TotalParcelas,
                    DataEmissao = c.DataEmissao, DataVencimento = c.DataVencimento,
                    DataRecebimento = c.DataRecebimento, ValorRecebido = c.ValorRecebido,
                    Status = c.Status,
                    StatusDescricao = c.Status == StatusContaReceber.Aberta ? "Aberta"
                        : c.Status == StatusContaReceber.Recebida ? "Recebida"
                        : c.Status == StatusContaReceber.Cancelada ? "Cancelada" : "Vencida",
                    NSU = c.NSU, TxId = c.TxId,
                    BandeiraNome = c.AdquirenteBandeira != null ? c.AdquirenteBandeira.Bandeira : null,
                    AdquirenteNome = c.AdquirenteBandeira != null ? c.AdquirenteBandeira.Adquirente.Nome : null,
                    Vencido = c.Status == StatusContaReceber.Aberta && c.DataVencimento < hoje
                })
                .ToListAsync();

            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasReceberController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar contas a receber." }); }
    }

    [HttpPost("{id:long}/receber")]
    public async Task<IActionResult> Receber(long id, [FromBody] ReceberRequest request)
    {
        try
        {
            var cr = await _db.ContasReceber.FindAsync(id)
                ?? throw new KeyNotFoundException("Conta não encontrada.");
            if (cr.Status != StatusContaReceber.Aberta) throw new ArgumentException("Conta não está aberta.");

            cr.Status = StatusContaReceber.Recebida;
            cr.DataRecebimento = DataHoraHelper.Agora();
            cr.ValorRecebido = request.ValorRecebido > 0 ? request.ValorRecebido : cr.ValorLiquido;
            cr.ContaBancariaId = request.ContaBancariaId;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasReceberController.Receber"); return StatusCode(500, new { success = false, message = "Erro ao receber conta." }); }
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long id)
    {
        try
        {
            var cr = await _db.ContasReceber.FindAsync(id)
                ?? throw new KeyNotFoundException("Conta não encontrada.");
            if (cr.Status == StatusContaReceber.Recebida) throw new ArgumentException("Conta já recebida não pode ser cancelada.");
            cr.Status = StatusContaReceber.Cancelada;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasReceberController.Cancelar"); return StatusCode(500, new { success = false, message = "Erro ao cancelar conta." }); }
    }

    public class ReceberRequest
    {
        public decimal ValorRecebido { get; set; }
        public long? ContaBancariaId { get; set; }
    }
}
