using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpLogging;
using NLog;
using NLog.Web;

const string allowCorsOnDevelopmentName = nameof(allowCorsOnDevelopmentName);
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Application has started");
try
{
    BuildServicesAndApp();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    logger.Debug("Application has ended");
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}
void BuildServicesAndApp()
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseNLog();

    BuildServices(builder);

    var app = builder.Build();
    BuildApp(app);

    app.Run();
}

void BuildServices(IHostApplicationBuilder builder)
{
    var services = builder.Services;

    // Add services to the container.
    services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    AddCorsServiceForDevelopment(services);
    AddIpRateLimitationService(services);

    services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
        logging.RequestHeaders.Add("sec-ch-ua");
        logging.ResponseHeaders.Add("MyResponseHeader");
        logging.MediaTypeOptions.AddText("application/javascript");
        logging.RequestBodyLogLimit = 4096;
        logging.ResponseBodyLogLimit = 4096;
        logging.CombineLogs = true;
    });
}

void AddCorsServiceForDevelopment(IServiceCollection services)
{
    services.AddCors(options =>
    {
        // allowing CORS policy from client development port
        options.AddPolicy(allowCorsOnDevelopmentName,
            policyBuilder =>
            {
                policyBuilder
                    .WithOrigins("https://localhost:4200")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
    });
}

void AddIpRateLimitationService(IServiceCollection services)
{
    // Adding IP Rate Limitation (requires MemoryCache)
    services.AddMemoryCache();
    services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = 429;
        options.RealIpHeader = "X-Real-IP";
        options.ClientIdHeader = "X-ClientId";
        options.GeneralRules = new List<RateLimitRule>
        {
            // one request per second
            new()
            {
                Endpoint = "PUT:/api/Calculate",
                Period = "1s",
                Limit = 1,
            }
        };
    });
    services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    services.AddInMemoryRateLimiting();
}

void BuildApp(WebApplication app)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();
    app.MapControllers();
    app.MapFallbackToFile("/index.html");

    // Ensures that HTTPS is the only protocol allowed
    app.UseHttpsRedirection();
    app.UseCors(allowCorsOnDevelopmentName);
    app.UseIpRateLimiting();
    app.UseHttpLogging();
}