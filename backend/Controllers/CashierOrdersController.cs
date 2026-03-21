using ApiProject.Models;
using ApiProject.Models.DTOs;
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

    [HttpGet("unpaid-risk-overview")]
    public async Task<ActionResult<CashierUnpaidRiskOverviewDto>> GetUnpaidRiskOverview()
    {
        var data = await _orderManagementService.GetCashierUnpaidRiskOverviewAsync();
        return Ok(data);
    }

    [HttpGet("unpaid-by-user")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetUnpaidByUser([FromQuery] int userId)
    {
        if (userId <= 0) return BadRequest(new { message = "Geçerli bir kullanıcı seçin." });
        var list = await _orderManagementService.GetOpenNotPaidOrdersForUserAsync(userId);
        return Ok(list);
    }

    [HttpPost("settle-all-unpaid")]
    public async Task<ActionResult> SettleAllUnpaid([FromQuery] int userId)
    {
        if (userId <= 0) return BadRequest(new { message = "Geçerli bir kullanıcı seçin." });
        try
        {
            var n = await _orderManagementService.SettleAllUnpaidDebtsForUserAsync(userId);
            return Ok(new { settledCount = n, message = n > 0 ? $"{n} kayıt tahsil edildi." : "Açık borç yok." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/settle-debt")]
    public async Task<ActionResult<OrderResponseDto>> SettleDebt(int id)
    {
        try
        {
            var updated = await _orderManagementService.SettleNotPaidDebtAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult> GetOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] bool? isPaid,
        [FromQuery] string? userSearch)
    {
        var orders = await _orderManagementService.GetCashierOrdersAsync(status, isPaid, userSearch);
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

