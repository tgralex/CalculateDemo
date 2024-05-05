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
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    logger.Debug("Application has ended");
    LogManager.Shutdown();
}
void BuildServicesAndApp()
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseNLog();
    builder
        .WebHost
        .ConfigureKestrel(serverOptions =>
        {
            var strSize = builder.Configuration["FormOptions:MultipartBodyLengthLimit"];
            long.TryParse(strSize, out var size);
            if (size == 0)
            {
                size = 10_000;
            }

            serverOptions.Limits.MaxRequestBodySize = size;
        });

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
    AddIpRateLimitationService(services, builder);

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

void AddIpRateLimitationService(IServiceCollection services, IHostApplicationBuilder builder)
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
        var perPeriod = builder.Configuration["IpRateLimitOptions:PerPeriod"];
        var strLimit = builder.Configuration["IpRateLimitOptions:Limit"];
        int.TryParse(strLimit, out var limit);
        if (limit == 0)
        {
            limit = 1;
        }
        options.GeneralRules = new List<RateLimitRule>
        {
            // one request per second
            new()
            {
                Endpoint = "PUT:/api/Calculate",
                Period = perPeriod,
                Limit = limit 
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