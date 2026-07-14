using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using AgendaPessoal.Components;
using AgendaPessoal.Data;
using AgendaPessoal.Models;
using AgendaPessoal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var cultura = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var builder = WebApplication.CreateBuilder(args);

// Hospedagens gratuitas (Render etc.) informam a porta pela variável PORT
var porta = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(porta))
    builder.WebHost.UseUrls($"http://0.0.0.0:{porta}");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.Cookie.Name = "agenda_auth";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opcoes.Cookie.SameSite = SameSiteMode.Lax;
        opcoes.LoginPath = "/login";
        opcoes.ExpireTimeSpan = TimeSpan.FromDays(30);
        opcoes.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Proteção contra brute-force no login: no máximo 5 tentativas por minuto por IP.
// O app roda atrás do proxy do Render/Neon, então o IP real vem no X-Forwarded-For.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // O proxy da hospedagem não tem IP fixo conhecido; confiar na cadeia encaminhada
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Em vez de uma página 429 em branco, volta ao login com aviso amigável
    opcoes.OnRejected = (contexto, _) =>
    {
        if (!contexto.HttpContext.Response.HasStarted)
            contexto.HttpContext.Response.Redirect("/login?erro=4");
        return ValueTask.CompletedTask;
    };
    opcoes.AddPolicy("login", http =>
    {
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5,
            QueueLimit = 0
        });
    });
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpClient("notificador", c => c.Timeout = TimeSpan.FromSeconds(30));

// Banco: Postgres gratuito (Neon) na nuvem via DATABASE_URL; SQLite local sem configurar nada
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    // datas do app são "hora do Brasil" sem offset; evita a exigência de DateTime UTC do Npgsql
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    builder.Services.AddDbContextFactory<AgendaDbContext>(o => o.UseNpgsql(ConverterDatabaseUrl(databaseUrl)));
}
else
{
    var pastaDados = Path.Combine(builder.Environment.ContentRootPath, "dados");
    Directory.CreateDirectory(pastaDados);
    var caminhoBanco = Path.Combine(pastaDados, "agenda.db");
    builder.Services.AddDbContextFactory<AgendaDbContext>(o => o.UseSqlite($"Data Source={caminhoBanco}"));
}

builder.Services.AddSingleton<ConfiguracaoService>();
builder.Services.AddSingleton<ContaService>();
builder.Services.AddSingleton<INotificador, CallMeBotNotificador>();
builder.Services.AddSingleton<ItemService>();
builder.Services.AddSingleton<TemaService>();
builder.Services.AddSingleton<AniversarioService>();
builder.Services.AddSingleton<LembreteService>();
builder.Services.AddHostedService<LembreteBackgroundService>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization(); // páginas exigem login; /login tem [AllowAnonymous]

// Login: formulários HTML puros fazem POST aqui (cookie não pode ser gravado pelo circuito Blazor)
app.MapPost("/api/conta/entrar", async (HttpContext http, ContaService conta) =>
{
    var form = await http.Request.ReadFormAsync();
    var usuario = form["usuario"].ToString().Trim();
    var senha = form["senha"].ToString();
    if (!await conta.ValidarAsync(usuario, senha))
        return Results.Redirect("/login?erro=1");
    await EntrarAsync(http, usuario);
    return Results.Redirect("/");
}).DisableAntiforgery().RequireRateLimiting("login");

app.MapPost("/api/conta/criar", async (HttpContext http, ContaService conta) =>
{
    if (await conta.TemUsuarioAsync())
        return Results.Redirect("/login"); // conta já existe; criação só no primeiro acesso
    var form = await http.Request.ReadFormAsync();
    var usuario = form["usuario"].ToString().Trim();
    var senha = form["senha"].ToString();
    var confirmar = form["confirmar"].ToString();
    if (usuario.Length < 3 || senha.Length < 6)
        return Results.Redirect("/login?erro=3");
    if (senha != confirmar)
        return Results.Redirect("/login?erro=2");
    await conta.CriarAsync(usuario, senha);
    await EntrarAsync(http, usuario);
    return Results.Redirect("/");
}).DisableAntiforgery().RequireRateLimiting("login");

app.MapGet("/api/conta/sair", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// Chamado por um cron gratuito (ex.: cron-job.org) para acordar o app e disparar lembretes
app.MapGet("/api/lembretes/verificar", async (string? token, LembreteService lembretes, ConfiguracaoService config) =>
{
    var esperado = await config.ObterAsync(ConfiguracaoService.TokenVerificacao);
    if (string.IsNullOrWhiteSpace(esperado) || token != esperado)
        return Results.Unauthorized();
    var enviados = await lembretes.VerificarEDispararAsync();
    return Results.Ok(new { enviados, horario = FusoHorario.Agora() });
});

// Cria o banco e os temas iniciais na primeira execução
using (var escopo = app.Services.CreateScope())
{
    var fabrica = escopo.ServiceProvider.GetRequiredService<IDbContextFactory<AgendaDbContext>>();
    await using var db = await fabrica.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Temas.AnyAsync())
    {
        db.Temas.AddRange(
            new Tema { Nome = "Pessoal", Cor = "#7C4DFF" },
            new Tema { Nome = "Trabalho", Cor = "#1E88E5" },
            new Tema { Nome = "Saúde", Cor = "#43A047" },
            new Tema { Nome = "Estudos", Cor = "#FB8C00" });
        await db.SaveChangesAsync();
    }
}

app.Run();

static async Task EntrarAsync(HttpContext http, string usuario)
{
    var identidade = new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, usuario)],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identidade),
        new AuthenticationProperties { IsPersistent = true });
}

// Converte postgres://usuario:senha@host:porta/banco (formato do Neon/Render) em connection string
static string ConverterDatabaseUrl(string url)
{
    var uri = new Uri(url);
    var credenciais = uri.UserInfo.Split(':', 2);
    var porta = uri.Port > 0 ? uri.Port : 5432;
    var banco = uri.AbsolutePath.TrimStart('/');
    var senha = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : "";
    return $"Host={uri.Host};Port={porta};Database={banco};" +
           $"Username={Uri.UnescapeDataString(credenciais[0])};Password={senha};" +
           "SSL Mode=Require;Trust Server Certificate=true";
}
