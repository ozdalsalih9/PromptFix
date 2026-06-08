using System.Threading.RateLimiting;
using PromptFix.Api;
using PromptFix.Api.Configuration;
using PromptFix.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<IOllamaService, OllamaService>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddSingleton<IModelConcurrencyGate, ModelConcurrencyGate>();
builder.Services.AddScoped<IPromptOptimizerService, PromptOptimizerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(ApiConstants.CorsPolicy, policy =>
    {
        var origins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

        var allowedOriginSet = origins.ToHashSet(StringComparer.OrdinalIgnoreCase);

        policy
            .SetIsOriginAllowed(origin =>
                allowedOriginSet.Contains(origin) ||
                origin.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ApiConstants.RateLimitPolicy, httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimit:PermitLimit", 30),
            Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimit:WindowMinutes", 1)),
            QueueLimit = 0
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(ApiConstants.CorsPolicy);
app.UseRateLimiter();
app.MapControllers();

app.Run();

public partial class Program;
