using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using ZulexPharma.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ─── Profile de sync (pc1/pc2) - carrega appsettings.pc1.json ou pc2.json ─
var syncProfile = Environment.GetEnvironmentVariable("SYNC_PROFILE");
if (!string.IsNullOrEmpty(syncProfile))
{
    builder.Configuration.AddJsonFile($"appsettings.{syncProfile}.json", optional: true, reloadOnChange: true);
}

// ─── Serilog ───────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/zulexpharma-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Database ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── JWT ───────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ──────────────────────────────────────────────────────────────────
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ─── Compressão gzip ──────────────────────────────────────────────────────
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes;
});

// ─── Controllers + OpenAPI ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─── DI Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IAuthService,
                            ZulexPharma.Infrastructure.Services.AuthService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IFilialService,
                            ZulexPharma.Infrastructure.Services.FilialService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IGrupoService,
                            ZulexPharma.Infrastructure.Services.GrupoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IUsuarioService,
                            ZulexPharma.Infrastructure.Services.UsuarioService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ZulexPharma.Infrastructure.Services.FilialContexto>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ILogAcaoService,
                            ZulexPharma.Infrastructure.Services.LogAcaoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IColaboradorService,
                            ZulexPharma.Infrastructure.Services.ColaboradorService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IFornecedorService,
                            ZulexPharma.Infrastructure.Services.FornecedorService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IFabricanteService,
                            ZulexPharma.Infrastructure.Services.FabricanteService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ISubstanciaService,
                            ZulexPharma.Infrastructure.Services.SubstanciaService>();
builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.GrupoPrincipal>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(), "Gerenciar Produtos", "GrupoPrincipal"));

builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.GrupoProduto>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(), "Gerenciar Produtos", "GrupoProduto"));

builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.SubGrupo>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(), "Gerenciar Produtos", "SubGrupo"));

builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.Secao>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(), "Gerenciar Produtos", "Secao"));

builder.Services.AddScoped<ZulexPharma.Infrastructure.Services.SyncService>();
builder.Services.AddHostedService<ZulexPharma.Infrastructure.Services.SyncBackgroundService>();
builder.Services.AddHostedService<ZulexPharma.Infrastructure.Services.UpdateBackgroundService>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ─── Seed do banco ─────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DatabaseSeeder.SeedAsync(db);

        Log.Information("Banco de dados inicializado com sucesso.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Erro ao inicializar o banco de dados.");
        throw;
    }
}

// ─── Pipeline ──────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseResponseCompression();
app.UseMiddleware<ZulexPharma.API.Middleware.ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
