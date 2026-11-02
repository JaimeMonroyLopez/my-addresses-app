using System.Net;
using System.Net.Http.Json;
using Addresses.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Addresses.Tests;

public class AddressesApiTests
{
    // Each test gets its own in-memory database with the sample data.
    private static HttpClient CreateClient() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("DatabaseName", Guid.NewGuid().ToString()))
            .CreateClient();

    [Fact]
    public async Task GetAddresses_ReturnsTheCustomersAddresses()
    {
        var client = CreateClient();

        var addresses = await client.GetFromJsonAsync<List<AddressDto>>("/api/addresses");

        Assert.NotNull(addresses);
        Assert.Equal(3, addresses.Count);
    }

    [Fact]
    public async Task SetDefault_MarksTheAddressAsDefault()
    {
        var client = CreateClient();

        var response = await client.PutAsync("/api/addresses/2/default", null);
        var addresses = await client.GetFromJsonAsync<List<AddressDto>>("/api/addresses");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(addresses!.Single(a => a.Id == 2).IsDefault);
    }

    [Fact]
    public async Task SetDefault_AnotherCustomersAddress_ReturnsNotFoundAndChangesNothing()
    {
        var client = CreateClient();

        // Address 5 belongs to customer 2; the client is signed in as customer 1.
        var response = await client.PutAsync("/api/addresses/5/default", null);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/addresses");
        request.Headers.Add("X-Customer-Id", "2");
        var otherCustomersAddresses = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<List<AddressDto>>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(otherCustomersAddresses!.Single(a => a.Id == 5).IsDefault);
    }
}
