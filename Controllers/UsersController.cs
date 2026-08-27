using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.DTOs.User;
using AgencyFlow.Services;

namespace AgencyFlow.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] string? nombre = null,
        [FromQuery] string? apellido = null,
        [FromQuery] string? email = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] Guid? clientCompanyId = null)
    {
        var result = await _userService.GetAllAsync(
            page,
            pageSize,
            nombre,
            apellido,
            email,
            roleId,
            clientCompanyId);

        return Ok(result);
    }

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] string? nombre = null,
        [FromQuery] string? apellido = null,
        [FromQuery] Guid? clientCompanyId = null)
    {
        var result = await _userService.GetClientsAsync(
            page,
            pageSize,
            nombre,
            apellido,
            clientCompanyId);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _userService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto)
    {
        var result = await _userService.LoginAsync(dto);

        if (result == null)
        {
            return Unauthorized(new
            {
                message = "Credenciales incorrectas."
            });
        }

        return Ok(result);
    }
}
