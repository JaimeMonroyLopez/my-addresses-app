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
    public async Task SetDefault_LeavesOnlyTheNewDefault_ListedFirst()
    {
        var client = CreateClient();

        // Address 1 is customer 1's default in the sample data.
        await client.PutAsync("/api/addresses/2/default", null);
        var addresses = await client.GetFromJsonAsync<List<AddressDto>>("/api/addresses");

        Assert.Equal(2, addresses!.Single(a => a.IsDefault).Id);
        Assert.Equal(2, addresses[0].Id);
    }

    [Fact]
    public async Task SetDefault_LeavesOtherCustomersDefaultAlone()
    {
        var client = CreateClient();

        await client.PutAsync("/api/addresses/2/default", null);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/addresses");
        request.Headers.Add("X-Customer-Id", "2");
        var otherCustomersAddresses = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<List<AddressDto>>();

        Assert.True(otherCustomersAddresses!.Single(a => a.Id == 4).IsDefault);
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

    [Fact]
    public async Task Delete_NonDefaultAddress_RemovesIt()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync("/api/addresses/3");
        var addresses = await client.GetFromJsonAsync<List<AddressDto>>("/api/addresses");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(addresses!, a => a.Id == 3);
        Assert.Equal(2, addresses!.Count);
    }

    [Fact]
    public async Task Delete_DefaultAddress_ReturnsConflictAndKeepsIt()
    {
        var client = CreateClient();

        // Address 1 is customer 1's default in the sample data.
        var response = await client.DeleteAsync("/api/addresses/1");
        var addresses = await client.GetFromJsonAsync<List<AddressDto>>("/api/addresses");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(addresses!.Single(a => a.Id == 1).IsDefault);
    }

    [Fact]
    public async Task Delete_AnotherCustomersAddress_ReturnsNotFoundAndKeepsIt()
    {
        var client = CreateClient();

        // Address 5 belongs to customer 2; the client is signed in as customer 1.
        var response = await client.DeleteAsync("/api/addresses/5");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/addresses");
        request.Headers.Add("X-Customer-Id", "2");
        var otherCustomersAddresses = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<List<AddressDto>>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(otherCustomersAddresses!, a => a.Id == 5);
    }
}
