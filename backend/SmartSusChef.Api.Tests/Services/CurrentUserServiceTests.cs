using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartSusChef.Api.Services;
using Xunit;

namespace SmartSusChef.Api.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void Properties_ShouldReturnDefaults_WhenHttpContextMissing()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var service = new CurrentUserService(accessor.Object);

        // Act & Assert
        Assert.Equal(0, service.StoreId);
        Assert.Equal(Guid.Empty, service.UserId);
        Assert.Equal(string.Empty, service.Role);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void Properties_ShouldReturnValues_FromClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("StoreId", "12"),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Manager")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);
        var service = new CurrentUserService(accessor.Object);

        // Act & Assert
        Assert.Equal(12, service.StoreId);
        Assert.Equal(userId, service.UserId);
        Assert.Equal("Manager", service.Role);
        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public void Properties_ShouldHandle_InvalidClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("StoreId", "not-an-int"),
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);
        var service = new CurrentUserService(accessor.Object);

        // Act & Assert
        Assert.Equal(0, service.StoreId);
        Assert.Equal(Guid.Empty, service.UserId);
    }
}
