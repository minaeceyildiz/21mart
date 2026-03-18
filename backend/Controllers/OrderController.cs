using ApiProject.Models.DTOs;
using ApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly ICafeService _cafeService;
    private readonly IOrderManagementService _orderManagementService;

    public OrderController(ICafeService cafeService, IOrderManagementService orderManagementService)
    {
        _cafeService = cafeService;
        _orderManagementService = orderManagementService;
    }

    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var order = await _cafeService.CreateOrderAsync(userId.Value, createOrderDto);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Sipariş oluşturulurken bir hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<ActionResult> GetMyOrders()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var orders = await _orderManagementService.GetMyOrdersAsync(userId.Value);
        return Ok(orders);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}

