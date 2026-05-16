using Microsoft.AspNetCore.Mvc;
using RTI.OrderAccumulator.DTOs;
using RTI.OrderAccumulator.Services;

namespace RTI.OrderAccumulator.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly ExposureService _exposureService;

    public OrdersController(ExposureService exposureService)
    {
        _exposureService = exposureService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string symbol, [FromQuery] string? tradeDate)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return BadRequest("Symbol is required");
        }

        var date = string.IsNullOrEmpty(tradeDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow.Date)
            : DateOnly.Parse(tradeDate);

        var orders = await _exposureService.GetOrdersBySymbolAsync(symbol, date);

        return Ok(new OrderListResponse
        {
            Symbol = symbol,
            TradeDate = date,
            Orders = orders.Select(o => new OrderDto
            {
                ClOrdId = o.ClOrdId,
                Symbol = o.Symbol,
                Side = o.Side,
                Quantity = o.Quantity,
                Price = o.Price,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            }).ToList()
        });
    }
}