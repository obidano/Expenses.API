using System;
using System.Linq;
using System.Threading.Tasks;
using Expenses.API.framework.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expenses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController(AppDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Lightweight endpoint to wake up the database by touching a small table.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Minimal DB touch to wake the connection
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");

            return Ok(new
            {
                status = "ok",
                timestamp = DateTime.UtcNow
            });
        }
        catch
        {
            // Do not fail the warm-up; return ok even if the query errors.
            return Ok(new
            {
                status = "ok",
                timestamp = DateTime.UtcNow
            });
        }
    }
}


