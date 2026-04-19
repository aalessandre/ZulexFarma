using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;
using ZulexPharma.Application.DTOs.Filiais;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FiliaisController : ControllerBase
{
    private const string SISTEMA_LOGIN = "SISTEMA";
    private const string HEADER_SENHA_DIA = "X-Senha-Sistema";

    private readonly IFilialService _service;
    private readonly ILogAcaoService _log;
    private readonly ISenhaDiaService _senhaDia;
    private readonly AppDbContext _db;

    public FiliaisController(IFilialService service, ILogAcaoService log,
                             ISenhaDiaService senhaDia, AppDbContext db)
    {
        _service = service;
        _log = log;
        _senhaDia = senhaDia;
        _db = db;
    }

    private long UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    /// <summary>
    /// Gate de escrita (RN-12): só permite se for SISTEMA, ou admin com senha do dia válida.
    /// Retorna IActionResult com o erro (403/401) se falhar, ou null se ok.
    /// </summary>
    private async Task<IActionResult?> ValidarAcessoEscritaAsync(string operacao)
    {
        var userId = UsuarioId;
        if (userId == 0) return Unauthorized(new { success = false, message = "Sessão inválida." });

        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (usuario == null) return Unauthorized(new { success = false, message = "Usuário não encontrado." });

        // Bypass: usuário SISTEMA (softwarehouse)
        if (string.Equals(usuario.Login, SISTEMA_LOGIN, StringComparison.OrdinalIgnoreCase))
            return null;

        // Gate: precisa ser administrador
        if (!usuario.IsAdministrador)
            return StatusCode(403, new { success = false, message = "Apenas administradores podem alterar filiais." });

        // Gate: senha do dia obrigatória
        var senha = Request.Headers[HEADER_SENHA_DIA].ToString();
        if (string.IsNullOrWhiteSpace(senha))
            return Unauthorized(new { success = false, codigo = "SENHA_DIA_EXIGIDA", message = "Senha do dia exigida para esta operação." });

        if (!_senhaDia.Validar(senha))
            return Unauthorized(new { success = false, codigo = "SENHA_DIA_INVALIDA", message = "Senha do dia inválida ou expirada." });

        return null;
    }

    private async Task RegistrarLiberacaoAsync(string operacao, long registroId)
    {
        try
        {
            await _log.RegistrarAsync("Filiais", $"LIBERAÇÃO_SENHA_DIA_{operacao}", "Filial", registroId,
                novo: new Dictionary<string, string?> { ["usuarioId"] = UsuarioId.ToString() });
        }
        catch (Exception ex) { Log.Warning(ex, "Falha ao registrar liberação por senha do dia"); }
    }

    [HttpGet]
    [Permissao("filiais", "c")]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _service.ListarAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar filiais." });
        }
    }

    [HttpPost]
    [Permissao("filiais", "i")]
    public async Task<IActionResult> Criar([FromBody] FilialFormDto dto)
    {
        try
        {
            var acessoErro = await ValidarAcessoEscritaAsync("INSERT");
            if (acessoErro != null) return acessoErro;

            var data = await _service.CriarAsync(dto);
            await RegistrarLiberacaoAsync("INSERT", data.Id);
            return Created(string.Empty, new { success = true, data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar filial." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("filiais", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] FilialFormDto dto)
    {
        try
        {
            var acessoErro = await ValidarAcessoEscritaAsync("UPDATE");
            if (acessoErro != null) return acessoErro;

            await _service.AtualizarAsync(id, dto);
            await RegistrarLiberacaoAsync("UPDATE", id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Filial não encontrada." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar filial." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("filiais", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var acessoErro = await ValidarAcessoEscritaAsync("DELETE");
            if (acessoErro != null) return acessoErro;

            var resultado = await _service.ExcluirAsync(id);
            await RegistrarLiberacaoAsync("DELETE", id);
            return Ok(new { success = true, resultado });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Filial não encontrada." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir filial." });
        }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("filiais", "c")]
    public async Task<IActionResult> ObterLog(long id,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var data = await _log.ListarPorRegistroAsync("Filial", id, dataInicio, dataFim);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.ObterLog | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." });
        }
    }
}
