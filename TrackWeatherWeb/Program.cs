using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using TrackWeatherWeb;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.Infrastructure;
using TrackWeatherWeb.Options;
using TrackWeatherWeb.Repositories;
using TrackWeatherWeb.Repositories.Interfaces;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog: sinks and levels come from appsettings
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services), preserveStaticLogger: true);

    // Options validated at startup, so a missing secret fails fast with a clear message
    builder.Services.AddOptions<JwtOptions>()
        .BindConfiguration(JwtOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddOptions<OpenWeatherOptions>().BindConfiguration(OpenWeatherOptions.SectionName);
    builder.Services.AddOptions<YandexMapsOptions>().BindConfiguration(YandexMapsOptions.SectionName);

    // Localization: Russian and English, resources live in Resources/SharedResource*.resx
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        CultureInfo[] cultures = [.. SupportedLanguages.All.Select(language => new CultureInfo(language))];
        options.DefaultRequestCulture = new RequestCulture(SupportedLanguages.Default);
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;
        options.ApplyCurrentCultureToResponseHeaders = true;
        // An explicit choice in the switcher wins over the browser language
        options.RequestCultureProviders =
        [
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider()
        ];
    });

    // Russian text stays readable in the HTML instead of turning into numeric entities
    builder.Services.Configure<WebEncoderOptions>(options =>
        options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

    // Add services to the container.
    builder.Services.AddRazorPages()
        .AddDataAnnotationsLocalization(options =>
            options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));
    builder.Services.AddControllers()
        .AddDataAnnotationsLocalization(options =>
            options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ApiExceptionHandler>();

    // Swagger version + swagger's name
    builder.Services.AddSwaggerGen(x =>
    {
        x.SwaggerDoc("v1", new OpenApiInfo { Title = "TrackWeather API", Version = "v1" });
        x.EnableAnnotations();
        x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT from POST /api/auth/login"
        });
        x.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

    // Keeps auth cookies valid across container restarts when a keys folder is configured
    var dataProtection = builder.Services.AddDataProtection().SetApplicationName("TrackWeather");
    var keysPath = builder.Configuration["DataProtection:KeysPath"];
    if (!string.IsNullOrWhiteSpace(keysPath))
    {
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    }

    // Browsers authenticate with a cookie, API clients with a JWT; the header decides which one applies
    builder.Services.AddAuthentication(AuthDefaults.CookieOrBearerScheme)
        .AddPolicyScheme(AuthDefaults.CookieOrBearerScheme, AuthDefaults.CookieOrBearerScheme, options =>
        {
            options.ForwardDefaultSelector = context =>
                context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? JwtBearerDefaults.AuthenticationScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name = AuthDefaults.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/Login";
            options.AccessDeniedPath = "/Error/403";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            // The API answers with a status code instead of redirecting to the login form
            options.Events.OnRedirectToLogin = context => RedirectOrStatus(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => RedirectOrStatus(context, StatusCodes.Status403Forbidden);
        })
        .AddJwtBearer();

    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((options, jwt) =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwt.Value.Issuer,
                ValidAudience = jwt.Value.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Key))
            };
        });

    // Everything requires a signed-in user unless it is explicitly marked [AllowAnonymous]
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

    // Slows down password guessing on login and registration
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(AuthDefaults.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitsPerMinute", 20),
                Window = TimeSpan.FromMinutes(1)
            }));
    });

    builder.Services.AddScoped<IAccountRepo, AccountRepo>();
    builder.Services.AddScoped<ITransportRepo, TransportRepo>();
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient<IWeatherRepo, WeatherRepo>((services, client) =>
    {
        client.BaseAddress = new Uri(services.GetRequiredService<IOptions<OpenWeatherOptions>>().Value.BaseUrl);
    }).AddStandardResilienceHandler();
    builder.Services.AddTransient<DatabaseInitializer>();

    var app = builder.Build();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    }

    // Configure the HTTP request pipeline.
    app.UseSecurityHeaders();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // API errors stay ProblemDetails, the site shows a friendly page
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/api"),
        branch => branch.UseStatusCodePages());
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        branch => branch.UseStatusCodePagesWithReExecute("/Error/{0}"));

    app.UseStaticFiles();
    app.UseRequestLocalization();

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(x =>
        {
            x.SwaggerEndpoint("/swagger/v1/swagger.json", "TrackWeather API");
        });
    }

    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapRazorPages();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    // Clean and Close logger
    await Log.CloseAndFlushAsync();
}

static Task RedirectOrStatus(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = statusCode;
    }
    else
    {
        context.Response.Redirect(context.RedirectUri);
    }
    return Task.CompletedTask;
}

// Entry point for WebApplicationFactory in the integration tests
public partial class Program;
