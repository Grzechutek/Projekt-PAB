// src/GameHub.WebAPI/Program.cs
using Serilog;
using Serilog.Events;
using GameHub.Infrastructure.Persistence;
using GameHub.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

// ── 1. Bootstrap logger — działa zanim DI będzie gotowe ──────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Uruchamianie GameHub.WebAPI...");

    var builder = WebApplication.CreateBuilder(args);

    // ── 2. Serilog — konfiguracja docelowa ───────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");

        cfg
            // Czytaj ustawienia z appsettings (opcjonalne nadpisanie)
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)

            // Wzbogać każdy log o nazwę maszyny i wątku
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()

            // ── Sink: konsola (dev-friendly) ─────────────────────────────
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
                    "{NewLine}{Exception}")

            // ── Sink: plik dzienny — wszystkie logi ≥ Information ────────
            .WriteTo.File(
                path: Path.Combine(logsPath, "gamehub-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
                    "{SourceContext} {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information)

            // ── Sink: plik dzienny — tylko błędy (Warning+) ──────────────
            .WriteTo.File(
                path: Path.Combine(logsPath, "errors-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
                    "{SourceContext} {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Warning)

            // Poziomy minimalne
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning);
    });

    // ── 3. Serwisy ───────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Connection string z appsettings.json
    var connectionString = builder.Configuration
        .GetConnectionString("DefaultConnection");

    // Rejestracja DbContext — Scoped = nowa instancja per request HTTP
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(connectionString));

    // Rejestracja UnitOfWork — też Scoped, żeby dzielił ten sam DbContext
    builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

    // ── 4. Pipeline HTTP ─────────────────────────────────────────────────────
    var app = builder.Build();

    // Loguj każde żądanie HTTP (middleware Serilog)
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} odpowiedział {StatusCode} " +
            "w {Elapsed:0.0000} ms";
        opts.GetLevel = (ctx, elapsed, ex) =>
            ex != null || ctx.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : ctx.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "GameHub.WebAPI zakończył działanie z nieoczekiwanym błędem.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;