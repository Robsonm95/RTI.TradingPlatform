using Microsoft.AspNetCore.Mvc;
using RTI.OrderGenerator.DTOs;
using RTI.OrderGenerator.Services;
using RTI.OrderGenerator.Validation;

namespace RTI.OrderGenerator.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderFixService _orderFixService;

    public OrdersController(
        OrderFixService orderFixService)
    {
        _orderFixService = orderFixService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
    CreateOrderRequest request)
    {
        var errors =
            CreateOrderValidator.Validate(request);

        if (errors.Any())
            return BadRequest(errors);
        
        try
        {
            var result =
                await _orderFixService
                    .SendOrder(request);

            return Ok(new OrderResponse
            {
                ClOrdId = result.ClOrdId,
                Success = result.Success,
                Status = result.Status,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new OrderResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = ex.Message
                });
        }
    }
}