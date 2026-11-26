using Bookshelf.Auth;
using Bookshelf.Components;
using Bookshelf.Data;
using Bookshelf.Data.Books;
using Bookshelf.Endpoints;
using Bookshelf.Logging;
using Bookshelf.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;

// Bootstrap logger covers startup until the configured logger replaces it
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRazorComponents()
        .AddInteractiveWebAssemblyComponents()
        .AddAuthenticationStateSerialization(o => o.SerializeAllClaims = true);

    builder.Services.AddValidation();
    builder.Services.AddProblemDetails();

    builder.Services.AddOpenApi();

    var connectionString = builder.Configuration.GetConnectionString("Bookshelf")
        ?? throw new InvalidOperationException("Connection string 'Bookshelf' not found.");

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.PostgreSQL(connectionString, LogColumns.TableName, LogColumns.Writers, needAutoCreateTable: true));

    if (builder.Environment.IsDevelopment())
    {
        // Surface sink failures (e.g. a bad connection) instead of silently dropping logs
        Serilog.Debugging.SelfLog.Enable(Console.Error);
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddIdentityCore<AppUser>(o =>
        {
            o.Password.RequiredLength = 10;
            o.User.RequireUniqueEmail = true;
            o.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
        })
        .AddSignInManager()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

    builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
        .AddIdentityCookies();
    builder.Services.AddAuthorization();

    builder.Services.ConfigureApplicationCookie(o =>
    {
        o.LoginPath = "/login";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = 401;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

    builder.Services.AddMudServices();

    builder.Services.AddHttpClient<OpenLibraryClient>(client =>
    {
        client.BaseAddress = new Uri("https://openlibrary.org/");
        // Open Library asks API clients to identify themselves.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Bookshelf/1.0");
        client.Timeout = TimeSpan.FromSeconds(20);
    });
    builder.Services.AddScoped<BookRepository>();
    builder.Services.AddOptions<BookCatalogOptions>()
        .BindConfiguration(BookCatalogOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddScoped<BookCatalogService>();

    // Interactive pages are prerendered on the server, so components that inject HttpClient need one here too
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // Ensure API calls return a JSON problem response instead of the HTML error page
        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), api => api.UseExceptionHandler());
        app.UseHsts();
    }

    app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), pages =>
        pages.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

    app.UseSerilogRequestLogging(o => o.GetLevel = (ctx, _, ex) =>
        ex is not null || ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
        // Static files (CSS, _framework/*.wasm, ...) would add dozens of rows per page load
        : Path.HasExtension(ctx.Request.Path.Value) ? LogEventLevel.Verbose
        : LogEventLevel.Information);

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi("/api/openapi/{documentName}.json");

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/api/openapi/v1.json", "Bookshelf v1");
        });
    }

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(Bookshelf.Client._Imports).Assembly);

    app.MapAuthEndpoints();
    app.MapBookEndpoints();

    app.Run();
}
// EF Core tooling stops the host deliberately with HostAbortedException
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    // Report the crash to whatever is supervising the process
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
