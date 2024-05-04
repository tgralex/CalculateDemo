using AspNetCoreRateLimit;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Configuration;


var builder = WebApplication.CreateBuilder(args);
// Configure logging to use a file-based logger

//builder.Logging.AddFile(options =>
//{
//    options.FileName = "app-logs.txt";
//    options.FileSizeLimit = 1024 * 1024 * 5; // 5 MB
//    options.RetainedFileCountLimit = 7;
//});


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var services = builder.Services;
const string allowCorsOnDevelopmentName = nameof(allowCorsOnDevelopmentName);
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

/**************************************************************/

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");

app.UseCors(allowCorsOnDevelopmentName);
app.UseIpRateLimiting();

app.Run();
