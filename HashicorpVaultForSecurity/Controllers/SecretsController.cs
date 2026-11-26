using Microsoft.AspNetCore.Mvc;

namespace HashicorpVaultForSecurity.Controllers;

[ApiController]
[Route("[controller]")]
public class SecretsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SecretsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("configurations")]
    public IActionResult GetConfigurations(CancellationToken cancellationToken)
    {
        string? connectionString = _configuration.GetConnectionString("Default");
        SmtpOptions? smtp = _configuration.GetSection("Smtp").Get<SmtpOptions>();
        string[]? hosts = _configuration.GetSection("CorsOrigins").Get<string[]>();

        return Ok(new { connectionString, smtp, hosts });
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public bool UseSsl { get; set; }
}
