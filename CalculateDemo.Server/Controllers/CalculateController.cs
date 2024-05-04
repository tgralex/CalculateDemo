using CalculateDemo.Server.Classes;
using Microsoft.AspNetCore.Mvc;

namespace CalculateDemo.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalculateController : ControllerBase
{
    private readonly ILogger<CalculateController> _logger;

    public CalculateController(ILogger<CalculateController> logger)
    {
        _logger = logger;
    }

    [HttpPut]
    public async Task<IActionResult> Calculate(StringExpression expressionObj)
    {
        var expression = expressionObj.Expression;
        _logger.LogInformation(expression);
        await Task.Delay(100); // this is just to emulate a small delay on execution - TA
        return Ok(new Calculation(expression));
    }
    
    [HttpGet("/Test")]
    [HttpGet("/api/Test")]
    [HttpGet("[action]")]
    public IActionResult Test()
    {
        return Ok("Hello from CalculateDemo Server");
    }
}

public class StringExpression
{
    public string Expression { get; set; } = "";
}