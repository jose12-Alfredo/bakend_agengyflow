using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.DTOs.ClientCompany;
using AgencyFlow.Services;
using AgencyFlow.Authorization;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.Administrators)]
[ApiController]
[Route("api/client-companies")]
public class ClientCompaniesController(
    ClientCompanyService clientCompanyService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] string? nombre = null,
        [FromQuery] string? email = null)
    {
        var result = await clientCompanyService
            .GetAllAsync(page, pageSize, nombre, email);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await clientCompanyService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateClientCompanyDto dto)
    {
        var result = await clientCompanyService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateClientCompanyDto dto)
    {
        var result = await clientCompanyService.UpdateAsync(id, dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await clientCompanyService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
