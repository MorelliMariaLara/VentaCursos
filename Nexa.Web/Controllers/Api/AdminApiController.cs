using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers.Api;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin")]
public class AdminApiController : ControllerBase
{
    private readonly StoreService _store;

    public AdminApiController(StoreService store) => _store = store;

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var s = await _store.StatsAsync();
        return Ok(new
        {
            users = s.Users,
            courses = s.Courses,
            enrollments = s.Enrollments,
            orders = s.Orders,
            revenue = s.Revenue,
        });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders() =>
        Ok(new { orders = await _store.ListOrdersAsync() });

    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        var users = await _store.ListUsersAsync();
        return Ok(new
        {
            users = users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                role = u.Role,
                createdAt = u.CreatedAt,
            }),
        });
    }

    [HttpGet("courses")]
    public async Task<IActionResult> Courses() =>
        Ok(new { courses = (await _store.ListCoursesAsync(true)).Select(CourseMapper.ToPublic) });

    [HttpPost("courses")]
    public async Task<IActionResult> Upsert([FromBody] Course body)
    {
        try
        {
            var course = await _store.UpsertCourseAsync(body ?? new Course());
            return Ok(new { course = CourseMapper.ToPublic(course) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("courses/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _store.DeleteCourseAsync(id);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
