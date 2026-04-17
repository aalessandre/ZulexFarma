using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using ZulexPharma.Infrastructure.Data;

// Permitir DateTime sem Kind (Unspecified) no PostgreSQL — evita erro com datas do frontend
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Aumentar limite de request body para upload ABCFarma (~25MB JSON)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 60_000_000);

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
builder.Services.AddControllers(o => o.MaxModelBindingCollectionSize = int.MaxValue)
    .AddJsonOptions(o => {
        o.JsonSerializerOptions.MaxDepth = 128;
        // Aceita enums tanto como string ("ReceitaC1") quanto como número — o frontend envia string.
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
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
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.INcmService,
    ZulexPharma.Infrastructure.Services.NcmService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IFornecedorService,
                            ZulexPharma.Infrastructure.Services.FornecedorService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IFabricanteService,
                            ZulexPharma.Infrastructure.Services.FabricanteService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IPrescritorService,
                            ZulexPharma.Infrastructure.Services.PrescritorService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IVendaReceitaService,
                            ZulexPharma.Infrastructure.Services.VendaReceitaService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ICampanhaFidelidadeService,
                            ZulexPharma.Infrastructure.Services.CampanhaFidelidadeService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IPremioFidelidadeService,
                            ZulexPharma.Infrastructure.Services.PremioFidelidadeService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IPlanoContaService,
                            ZulexPharma.Infrastructure.Services.PlanoContaService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IContaBancariaService,
                            ZulexPharma.Infrastructure.Services.ContaBancariaService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IContaPagarService,
                            ZulexPharma.Infrastructure.Services.ContaPagarService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ITipoPagamentoService,
                            ZulexPharma.Infrastructure.Services.TipoPagamentoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IConvenioService,
                            ZulexPharma.Infrastructure.Services.ConvenioService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IPromocaoService,
                            ZulexPharma.Infrastructure.Services.PromocaoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IClienteService,
                            ZulexPharma.Infrastructure.Services.ClienteService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IHierarquiaDescontoService,
                            ZulexPharma.Infrastructure.Services.HierarquiaDescontoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IHierarquiaComissaoService,
                            ZulexPharma.Infrastructure.Services.HierarquiaComissaoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IVendaService,
                            ZulexPharma.Infrastructure.Services.VendaService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IVendaFiscalService,
                            ZulexPharma.Infrastructure.Services.VendaFiscalService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ZulexPharma.Infrastructure.Services.IbptService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IAdquirenteService,
                            ZulexPharma.Infrastructure.Services.AdquirenteService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ICaixaMovimentoService,
                            ZulexPharma.Infrastructure.Services.CaixaMovimentoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ISubstanciaService,
                            ZulexPharma.Infrastructure.Services.SubstanciaService>();
builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.GrupoPrincipal>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.IProdutoLoteService>(),
    "Gerenciar Produtos", "GrupoPrincipal"));

builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.GrupoProduto>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.IProdutoLoteService>(),
    "Gerenciar Produtos", "GrupoProduto"));

builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.SubGrupo>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.IProdutoLoteService>(),
    "Gerenciar Produtos", "SubGrupo"));

builder.Services.AddScoped(sp => new ZulexPharma.Infrastructure.Services.ClassificacaoProdutoService<ZulexPharma.Domain.Entities.Secao>(
    sp.GetRequiredService<ZulexPharma.Infrastructure.Data.AppDbContext>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.ILogAcaoService>(),
    sp.GetRequiredService<ZulexPharma.Application.Interfaces.IProdutoLoteService>(),
    "Gerenciar Produtos", "Secao"));

builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IProdutoFamiliaService,
                            ZulexPharma.Infrastructure.Services.ProdutoFamiliaService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IProdutoService,
                            ZulexPharma.Infrastructure.Services.ProdutoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IProdutoLocalService,
                            ZulexPharma.Infrastructure.Services.ProdutoLocalService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ICompraService,
                            ZulexPharma.Infrastructure.Services.CompraService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IProdutoLoteService,
                            ZulexPharma.Infrastructure.Services.ProdutoLoteService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IInventarioSngpcService,
                            ZulexPharma.Infrastructure.Services.InventarioSngpcService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IPerdaService,
                            ZulexPharma.Infrastructure.Services.PerdaService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IEstoqueSngpcService,
                            ZulexPharma.Infrastructure.Services.EstoqueSngpcService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ISngpcMapaService,
                            ZulexPharma.Infrastructure.Services.SngpcMapaService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ICompraSngpcService,
                            ZulexPharma.Infrastructure.Services.CompraSngpcService>();

// ── Gestor Tributário ──
builder.Services.AddHttpClient("Avant");
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IGestorTributarioProvider,
                            ZulexPharma.Infrastructure.Services.GestorTributario.AvantGestorTributarioProvider>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IGestorTributarioService,
                            ZulexPharma.Infrastructure.Services.GestorTributario.GestorTributarioService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IIcmsUfService,
                            ZulexPharma.Infrastructure.Services.IcmsUfService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.IAtualizacaoPrecoService,
                            ZulexPharma.Infrastructure.Services.AtualizacaoPrecoService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.ISefazService,
                            ZulexPharma.Infrastructure.Services.SefazService>();
builder.Services.AddScoped<ZulexPharma.Application.Interfaces.INaturezaOperacaoService,
                            ZulexPharma.Infrastructure.Services.NaturezaOperacaoService>();

builder.Services.AddHostedService<ZulexPharma.Infrastructure.Services.UpdateBackgroundService>();
builder.Services.AddHostedService<ZulexPharma.Infrastructure.Services.SyncBackgroundService>();
builder.Services.AddHostedService<ZulexPharma.Infrastructure.Services.IbptBackgroundService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

// ─── Seed do banco ─────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var filialCodigo = int.TryParse(config["Filial:Codigo"], out var fc) ? fc : 0;
        await DatabaseSeeder.SeedAsync(db, filialCodigo);

        Log.Information("Banco de dados inicializado com sucesso.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Erro ao inicializar o banco de dados.");
        throw;
    }
}

app.UseResponseCompression();

// ─── No-cache: ERP não pode servir dados em cache ─────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseMiddleware<ZulexPharma.API.Middleware.ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
