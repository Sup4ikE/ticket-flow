const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// MVP: dev-server origins only, any method/header. Tighten before anything is deployed.
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

app.MapReverseProxy();

// Liveness only: the Gateway has no state of its own. Compose makes it wait for the APIs' /health instead.
app.MapHealthChecks("/health");

app.Run();
