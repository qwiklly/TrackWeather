using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.Repositories;
using TrackWeatherWeb.HttpServices;
using TrackWeatherWeb.States;
using TrackWeatherWeb.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.Debug()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Swagger version + swagger's name
builder.Services.AddSwaggerGen(x =>
{
    x.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
    x.EnableAnnotations();
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Define the connection scheme (HTTP/HTTPS) depending on the environment
var httpBaseAddress = builder.Configuration["BaseUrls:Http"];
var httpsBaseAddress = builder.Configuration["BaseUrls:Https"];
// Сheck if the docker container exists
var isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var baseAddress = isRunningInDocker ? httpBaseAddress : httpsBaseAddress;

// JWT Auth base on scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !isRunningInDocker; 

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = baseAddress,
        ValidAudience = baseAddress,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddScoped<IAccount, Account>();
builder.Services.AddScoped<ITransport, Transport>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress!) });
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthenticationProvider>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}
//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAntiforgery();

app.UseSwagger();
app.UseSwaggerUI(x =>
{
    x.SwaggerEndpoint("/swagger/v1/swagger.json", "My API");
});

app.UseGlobalExceptionMiddleware();
app.MapRazorPages();
app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

// Clean and Close logger
Log.CloseAndFlush();

