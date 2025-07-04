using AutoFixture;
using AutoFixture.AutoMoq;
using FakeServer.Controllers;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Text.Json;
using Xunit;

namespace FakeServer.Tests.Controllers
{
    public class HealthCheckControllerTests
    {
        private readonly IFixture _fixture;

        public HealthCheckControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
        }

        [Fact]
        public void Get_ReturnsOk_WhenHealthIsHealthy()
        {
            // Arrange
            var mockDataStore = _fixture.Freeze<Mock<IDataStore>>();
            mockDataStore.Setup(ds => ds.Reload());

            var checker = new HealthCheckController.HealthChecker(mockDataStore.Object);
            var controller = new HealthCheckController(checker);

            // Act
            var result = controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            Assert.Equal("Healthy", dict["status"].GetString());
            Assert.NotNull(dict["uptime"].GetString());
            Assert.NotNull(dict["version"].GetString());
        }

        [Fact]
        public void Get_Returns503_WhenHealthIsUnhealthy()
        {
            // Arrange
            var mockDataStore = _fixture.Freeze<Mock<IDataStore>>();
            mockDataStore.Setup(ds => ds.Reload()).Throws(new Exception("DB Unavailable"));

            var checker = new HealthCheckController.HealthChecker(mockDataStore.Object);
            var controller = new HealthCheckController(checker);

            // Act
            var result = controller.Get();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);

            var json = JsonSerializer.Serialize(objectResult.Value);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            Assert.Equal("Unhealthy", dict["status"].GetString());
            Assert.NotNull(dict["version"].GetString());
        }
    }
}
