using System.Net;
using System.Text.Json;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var usuario = context.User?.Identity?.Name ?? "Anônimo";
        var tela = context.Request.Path.Value ?? "";
        var metodo = context.Request.Method;

        Log.Error(ex,
            "Erro não tratado | Usuário: {Usuario} | Tela: {Tela} | Método: {Metodo} | Mensagem: {Mensagem}",
            usuario, tela, metodo, ex.Message);

        // Persiste no banco se possível
        try
        {
            var db = context.RequestServices.GetService<AppDbContext>();
            if (db != null)
            {
                db.LogsErro.Add(new LogErro
                {
                    UsuarioLogin = usuario,
                    Tela = tela,
                    Funcao = metodo,
                    Mensagem = ex.Message,
                    StackTrace = ex.StackTrace
                });
                await db.SaveChangesAsync();
            }
        }
        catch
        {
            // Se falhar ao logar, apenas continua
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            success = false,
            message = "Ocorreu um erro interno. A equipe de suporte foi notificada.",
            detalhe = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                ? ex.Message
                : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
