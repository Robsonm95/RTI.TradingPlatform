using Microsoft.AspNetCore.Mvc;
using RTI.OrderAccumulator.DTOs;
using RTI.OrderAccumulator.Services;

namespace RTI.OrderAccumulator.Controllers;

[ApiController]
[Route("exposures")]
public class ExposuresController : ControllerBase
{
    private readonly ExposureService _exposureService;

    public ExposuresController(ExposureService exposureService)
    {
        _exposureService = exposureService;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string? tradeDate)
    {
        var date = string.IsNullOrEmpty(tradeDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow.Date)
            : DateOnly.Parse(tradeDate);

        var exposures = _exposureService.GetExposureAll(date);

        return Ok(new ExposureResponse
        {
            Exposures = exposures,
            MaxLimit = 100_000_000m
        });
    }
}
