using IdeatecAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/dev/robot")]
[Authorize]
public class DevRobotController : ControllerBase
{
    private readonly RetryLogStore _logStore;

    public DevRobotController(RetryLogStore logStore) => _logStore = logStore;

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(_logStore.Status);

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int count = 100)
        => Ok(_logStore.GetLogs(count));

    [HttpPost("stop")]
    public IActionResult Stop()
    {
        _logStore.Status.IsRunning = false;
        _logStore.Add(new RetryLogEntry(DateTime.Now, "ciclo", "PAUSADO", Detalle: "Detenido manualmente"));
        return Ok(new { mensaje = "Robot pausado." });
    }

    [HttpPost("start")]
    public IActionResult Start()
    {
        _logStore.Status.IsRunning = true;
        _logStore.Add(new RetryLogEntry(DateTime.Now, "ciclo", "REANUDADO", Detalle: "Reanudado manualmente"));
        return Ok(new { mensaje = "Robot reanudado." });
    }

    [HttpPut("config")]
    public IActionResult UpdateConfig([FromBody] UpdateRobotConfigDto dto)
    {
        if (dto.IntervaloMinutos is < 1 or > 120)
            return BadRequest(new { mensaje = "IntervaloMinutos debe estar entre 1 y 120." });

        _logStore.Status.IntervaloMinutos       = dto.IntervaloMinutos;
        _logStore.Status.IncluirEnviadoEnResumen = dto.IncluirEnviadoEnResumen;

        _logStore.Add(new RetryLogEntry(
            DateTime.Now, "ciclo", "CONFIG",
            Detalle: $"Intervalo: {dto.IntervaloMinutos}min | IncluirResumen: {dto.IncluirEnviadoEnResumen}"));

        return Ok(_logStore.Status);
    }
}

public record UpdateRobotConfigDto(int IntervaloMinutos, bool IncluirEnviadoEnResumen);
