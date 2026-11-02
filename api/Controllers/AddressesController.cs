using Addresses.Api.Auth;
using Addresses.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Addresses.Api.Controllers;

public record AddressDto(int Id, string Label, string Street, string City, bool IsDefault);

[ApiController]
[Authorize]
[Route("api/addresses")]
public class AddressesController(AppDbContext db) : ControllerBase
{
    // GET /api/addresses — the signed-in customer's addresses, default first.
    [HttpGet]
    public async Task<List<AddressDto>> GetAddresses()
    {
        var customerId = User.GetCustomerId();
        return await db.Addresses
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Label)
            .Select(a => new AddressDto(a.Id, a.Label, a.Street, a.City, a.IsDefault))
            .ToListAsync();
    }

    // PUT /api/addresses/{id}/default — marks an address as the default one.
    [HttpPut("{id:int}/default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var address = await FindCustomersAddress(id);
        if (address is null)
        {
            return NotFound();
        }

        // Only one default per customer: unmark the previous one in the same save.
        var previousDefaults = await db.Addresses
            .Where(a => a.CustomerId == address.CustomerId && a.IsDefault && a.Id != id)
            .ToListAsync();
        foreach (var previous in previousDefaults)
        {
            previous.IsDefault = false;
        }

        address.IsDefault = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/addresses/{id} — deletes one of the customer's addresses, never the default one.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var address = await FindCustomersAddress(id);
        if (address is null)
        {
            return NotFound();
        }

        if (address.IsDefault)
        {
            return Conflict("The default address cannot be deleted.");
        }

        db.Addresses.Remove(address);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Another customer's address comes back null, the same as a missing one, so callers answer 404
    // and its existence is not revealed.
    private Task<Address?> FindCustomersAddress(int id)
    {
        var customerId = User.GetCustomerId();
        return db.Addresses.SingleOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);
    }
}
