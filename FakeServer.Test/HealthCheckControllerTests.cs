using NSubstitute;
using Xunit;
using JsonFlatFileDataStore;
using FakeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace FakeServer.Test;

public class HealthCheckControllerTests
{
    private readonly IDataStore _mockDataStore;
    private readonly HealthCheckController _controller;

    public HealthCheckControllerTests()
    {
        _mockDataStore = Substitute.For<IDataStore>();
        _controller = new HealthCheckController(_mockDataStore);
    }

    [Fact]
    public void Get_ReturnsHealthyStatus_WhenDataStoreIsAccessible()
    {
        // Arrange
        // For the healthy case, we just need to ensure Reload() doesn't throw an exception
        // This is the default behavior for NSubstitute, so no explicit configuration is needed

        // Act
        var result = _controller.Get() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

        var responseDict = result.Value as Dictionary<string, string>;
        Assert.NotNull(responseDict);
        Assert.True(responseDict.ContainsKey("status"), "Response should contain 'status' key");
        Assert.Equal("Healthy", responseDict["status"]);
        Assert.True(responseDict.ContainsKey("uptime"), "Response should contain 'uptime' key");
        Assert.NotNull(responseDict["uptime"]);
        Assert.True(responseDict.ContainsKey("version"), "Response should contain 'version' key");
        Assert.NotNull(responseDict["version"]);
    }

    [Fact]
    public void Get_ReturnsUnhealthyStatus_WhenDataStoreIsInaccessible()
    {
        // Arrange
        // Configure _mockDataStore.Reload() to throw an exception to simulate failure
        _mockDataStore.When(ds => ds.Reload()).Do(x => { throw new Exception("Data store error"); });

        // Act
        var result = _controller.Get() as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);

        // The controller now returns a Dictionary<string, object>
        var responseDict = result.Value as Dictionary<string, string>;
        Assert.NotNull(responseDict);
        Assert.True(responseDict.ContainsKey("status"), "Response should contain 'status' key");
        Assert.Equal("Unhealthy", responseDict["status"]);
        Assert.True(responseDict.ContainsKey("version"), "Response should contain 'version' key");
        Assert.NotNull(responseDict["version"]);
    }
}