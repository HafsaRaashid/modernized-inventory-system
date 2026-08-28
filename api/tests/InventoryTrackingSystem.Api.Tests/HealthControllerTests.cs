using InventoryTrackingSystem.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InventoryTrackingSystem.Api.Tests;

/// <summary>
/// One trivial test proving the backend test runner (test-backend pillar)
/// executes end to end against this foundation. It exercises only the
/// health-check pillar's own endpoint — no domain rule, no backlog
/// capability.
/// </summary>
public class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsOk()
    {
        var controller = new HealthController();

        var result = controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }
}
