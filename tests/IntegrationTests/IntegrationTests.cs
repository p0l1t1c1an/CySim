namespace CySim.Tests.IntegrationTests;
using CySim;
using CySim.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

public class IndexPageTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program>
        _factory;

    public IndexPageTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();   
    }
    
    private void InitializeDatabase() 
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }
 

    /* 
     * This just goes through the list of controller endpoints to ensure reachability.
     * If authentication was set up correctly, we would error on some endpoints we lack access to.
     * We actually need authentication integration tests to ensure correct access.
     * This would use preset accounts in an in-memory sqlite db.
     */  
    
    [Theory]
    [InlineData("/")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Logout")]
    [InlineData("/Identity/Account/Manage")]
    [InlineData("/Machine/Machine")]
    [InlineData("/MachineInfo/MachineInfo")]
    [InlineData("/MachineStatus/MachineStatus")]
    [InlineData("/Scoreboard/Scoreboard")]
    [InlineData("/Scenario")]
    [InlineData("/Tutorial")]
    [InlineData("/TeamRegistration")]
    public async Task GetEndpoints(string url)
    {
        InitializeDatabase();

        // Connect to url
        var response = await _client.GetAsync(url);

        // Assert that connection passed (200-299 status code) and html was recieved
        response.EnsureSuccessStatusCode();

        var responseContentType = response.Content.Headers.ContentType;
        Assert.NotNull(responseContentType); 

        var typeString = responseContentType.ToString();
        Assert.Equal("text/html; charset=utf-8", typeString);
    }
    
    /* 
     * Other tests could be done within this class.
     * Ideas: 
     *  - Ensure error page for non-existent endpoint
     *  - Ensure Correct redirects
     *  - Essentially test different requests 
     *  - Tests can NOT involve data (with this setup of test WebApplication)
     */
}

