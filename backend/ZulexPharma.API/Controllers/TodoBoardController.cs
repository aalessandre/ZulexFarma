using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodoBoardController : ControllerBase
{
    private static readonly string _filePath = Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "todo-board.json");

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [HttpGet]
    public IActionResult Listar()
    {
        var items = Ler();
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    public IActionResult Criar([FromBody] TodoItemDto dto)
    {
        var items = Ler();
        var novo = new TodoItem
        {
            Id = items.Count > 0 ? items.Max(i => i.Id) + 1 : 1,
            Titulo = dto.Titulo?.Trim() ?? "",
            Descricao = dto.Descricao?.Trim(),
            Tipo = dto.Tipo,
            Prioridade = dto.Prioridade,
            Status = dto.Status ?? "aberto",
            Modulo = dto.Modulo?.Trim(),
            CriadoPor = dto.CriadoPor?.Trim() ?? "",
            AtribuidoPara = dto.AtribuidoPara?.Trim(),
            DataLimite = dto.DataLimite,
            DataConclusao = null,
            CriadoEm = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
        };
        items.Add(novo);
        Salvar(items);
        return Created("", new { success = true, data = novo });
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] TodoItemDto dto)
    {
        var items = Ler();
        var item = items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound(new { success = false, message = "Item não encontrado." });

        item.Titulo = dto.Titulo?.Trim() ?? item.Titulo;
        item.Descricao = dto.Descricao?.Trim();
        item.Tipo = dto.Tipo ?? item.Tipo;
        item.Prioridade = dto.Prioridade ?? item.Prioridade;
        item.Status = dto.Status ?? item.Status;
        item.Modulo = dto.Modulo?.Trim();
        item.AtribuidoPara = dto.AtribuidoPara?.Trim();
        item.DataLimite = dto.DataLimite;

        if (item.Status == "concluido" && item.DataConclusao == null)
            item.DataConclusao = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
        if (item.Status != "concluido")
            item.DataConclusao = null;

        Salvar(items);
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        var items = Ler();
        var removed = items.RemoveAll(i => i.Id == id);
        if (removed == 0) return NotFound(new { success = false, message = "Item não encontrado." });
        Salvar(items);
        return Ok(new { success = true });
    }

    [HttpPut("{id:int}/status")]
    public IActionResult AlterarStatus(int id, [FromBody] StatusDto dto)
    {
        var items = Ler();
        var item = items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound(new { success = false, message = "Item não encontrado." });

        item.Status = dto.Status ?? item.Status;
        if (item.Status == "concluido" && item.DataConclusao == null)
            item.DataConclusao = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
        if (item.Status != "concluido")
            item.DataConclusao = null;

        Salvar(items);
        return Ok(new { success = true, data = item });
    }

    private List<TodoItem> Ler()
    {
        try
        {
            if (!System.IO.File.Exists(_filePath)) return new();
            var json = System.IO.File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<TodoItem>>(json, _jsonOpts) ?? new();
        }
        catch { return new(); }
    }

    private void Salvar(List<TodoItem> items)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        System.IO.File.WriteAllText(_filePath, JsonSerializer.Serialize(items, _jsonOpts));
    }

    public record StatusDto(string? Status);

    public class TodoItemDto
    {
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public string? Tipo { get; set; }
        public string? Prioridade { get; set; }
        public string? Status { get; set; }
        public string? Modulo { get; set; }
        public string? CriadoPor { get; set; }
        public string? AtribuidoPara { get; set; }
        public string? DataLimite { get; set; }
    }

    public class TodoItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string? Descricao { get; set; }
        public string? Tipo { get; set; }
        public string? Prioridade { get; set; }
        public string Status { get; set; } = "aberto";
        public string? Modulo { get; set; }
        public string? CriadoPor { get; set; }
        public string? AtribuidoPara { get; set; }
        public string? DataLimite { get; set; }
        public string? DataConclusao { get; set; }
        public string? CriadoEm { get; set; }
    }
}
