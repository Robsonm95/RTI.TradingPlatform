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
    public IActionResult Get()
    {
        var symbols = new[] { "PETR4", "VALE3", "VIIA4" };
        var exposures = new Dictionary<string, decimal>();

        foreach (var symbol in symbols)
        {
            exposures[symbol] = _exposureService.GetExposure(symbol);
        }

        return Ok(new ExposureResponse
        {
            Exposures = exposures,
            MaxLimit = 100_000_000m
        });
    }
}
