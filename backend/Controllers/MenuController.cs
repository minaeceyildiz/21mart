using ApiProject.Models;
using ApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly ICafeService _cafeService;

    public MenuController(ICafeService cafeService)
    {
        _cafeService = cafeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<MenuItem>>> GetMenuItems()
    {
        var menuItems = await _cafeService.GetMenuItemsAsync();
        return Ok(menuItems);
    }
}

