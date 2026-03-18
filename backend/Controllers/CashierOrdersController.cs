using ApiProject.Models;
using ApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/cashier/orders")]
[Authorize(Roles = "Staff")]
public class CashierOrdersController : ControllerBase
{
    private readonly IOrderManagementService _orderManagementService;

    public CashierOrdersController(IOrderManagementService orderManagementService)
    {
        _orderManagementService = orderManagementService;
    }

    [HttpGet]
    public async Task<ActionResult> GetOrders([FromQuery] OrderStatus? status, [FromQuery] bool? isPaid)
    {
        var orders = await _orderManagementService.GetCashierOrdersAsync(status, isPaid);
        return Ok(orders);
    }

    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult> Approve(int id)
    {
        try
        {
            var updated = await _orderManagementService.ApproveAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/preparing")]
    public async Task<ActionResult> Preparing(int id)
    {
        try
        {
            var updated = await _orderManagementService.PreparingAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/ready")]
    public async Task<ActionResult> Ready(int id)
    {
        try
        {
            var updated = await _orderManagementService.ReadyAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/paid")]
    public async Task<ActionResult> Paid(int id)
    {
        try
        {
            var updated = await _orderManagementService.PaidAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/notpaid")]
    public async Task<ActionResult> NotPaid(int id)
    {
        try
        {
            var updated = await _orderManagementService.NotPaidAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id)
    {
        try
        {
            var updated = await _orderManagementService.CancelAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

