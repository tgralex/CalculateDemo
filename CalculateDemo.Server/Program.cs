using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpLogging;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Http.Features;
using static System.Net.Mime.MediaTypeNames;

const string allowCorsOnDevelopmentName = nameof(allowCorsOnDevelopmentName);
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Application has started");
try
{
    logger.Info($".Net version: {Environment.Version}");
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

int GetConfigValueOrDefault(IHostApplicationBuilder builder, string path, int defaultValue)
{
    if (int.TryParse(builder.Configuration[path], out var result))
    {
        return result;
    }

    return defaultValue;
}
void BuildServicesAndApp()
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseNLog();
    builder
        .WebHost
        .ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(1);
            serverOptions.Limits.MaxConcurrentConnections = 100;
            serverOptions.Limits.MaxRequestBodySize = GetConfigValueOrDefault(builder, "FormOptions:MultipartBodyLengthLimit", 10000);
            logger.Info($"MaxRequestBodySize is set to: {serverOptions.Limits.MaxRequestBodySize}");
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
        logger.Info("Http logging is enabled");
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
        var limit = GetConfigValueOrDefault(builder, "IpRateLimitOptions:Limit", 10000);
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
        logger.Info($"RateLimitRule is set to: {limit} per {perPeriod}");
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
        app.UseCors(allowCorsOnDevelopmentName);
    }

    app.UseAuthorization();
    app.MapControllers();
    app.MapFallbackToFile("/index.html");

    // handling payload size
    app.Use(async (context, next) =>
    {
        var httpMaxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (httpMaxRequestBodySizeFeature is not null && context.Request.ContentLength > httpMaxRequestBodySizeFeature.MaxRequestBodySize)
        {
            context.Response.ContentType = Text.Plain;
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsync("Request is too long");
        }
        else
        {
            await next(context);
        }
    });


    // Ensures that HTTPS is the only protocol allowed
    app.UseHttpsRedirection();
    app.UseIpRateLimiting();
    app.UseHttpLogging();
}