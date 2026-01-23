using System.Collections.Generic;
using System.Threading.Tasks;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using Moq;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserManagementService> _userService = new();
        private readonly Mock<IAppLogger> _logger = new();

        private AuthService CreateSut() => new AuthService(_userService.Object, _logger.Object);

        [Fact]
        public async Task LoginAsync_ReturnsSuccess_WhenUserExistsActiveAndPasswordMatches()
        {
            var user = new UserConfigurationModel
            {
                UserName = "john",
                Password = "pwd",
                Role = "Admin",
                IsActive = true
            };
            _userService.Setup(s => s.GetUserByUsernameAsync("john")).ReturnsAsync(user);

            var sut = CreateSut();

            var result = await sut.LoginAsync("john", "pwd");

            Assert.True(result.Success);
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFalse_WhenUserNotFound()
        {
            _userService.Setup(s => s.GetUserByUsernameAsync("ghost")).ReturnsAsync((UserConfigurationModel)null!);
            var sut = CreateSut();

            var result = await sut.LoginAsync("ghost", "pwd");

            Assert.False(result.Success);
            Assert.Null(result.Role);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFalse_WhenUserInactive()
        {
            var user = new UserConfigurationModel { UserName = "john", Password = "pwd", Role = "User", IsActive = false };
            _userService.Setup(s => s.GetUserByUsernameAsync("john")).ReturnsAsync(user);
            var sut = CreateSut();

            var result = await sut.LoginAsync("john", "pwd");

            Assert.False(result.Success);
            Assert.Null(result.Role);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFalse_WhenPasswordDoesNotMatch()
        {
            var user = new UserConfigurationModel { UserName = "john", Password = "pwd", Role = "User", IsActive = true };
            _userService.Setup(s => s.GetUserByUsernameAsync("john")).ReturnsAsync(user);
            var sut = CreateSut();

            var result = await sut.LoginAsync("john", "wrong");

            Assert.False(result.Success);
            Assert.Null(result.Role);
        }

        [Fact]
        public async Task EnsureDefaultUserExistsAsync_AddsAdmin_WhenMissing()
        {
            _userService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<UserConfigurationModel>());
            _userService.Setup(s => s.AddUserAsync(It.IsAny<UserConfigurationModel>())).ReturnsAsync((UserConfigurationModel u) => u);
            var sut = CreateSut();

            await sut.EnsureDefaultUserExistsAsync();

            _userService.Verify(s => s.AddUserAsync(It.Is<UserConfigurationModel>(u =>
                u.UserName == "admin" &&
                u.Role == "Admin" &&
                u.IsActive)), Times.Once);
        }

        [Fact]
        public async Task EnsureDefaultUserExistsAsync_DoesNothing_WhenAdminExists()
        {
            var existingAdmin = new UserConfigurationModel { UserName = "admin", Role = "Admin", IsActive = true };
            _userService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<UserConfigurationModel> { existingAdmin });
            var sut = CreateSut();

            await sut.EnsureDefaultUserExistsAsync();

            _userService.Verify(s => s.AddUserAsync(It.IsAny<UserConfigurationModel>()), Times.Never);
        }
    }
}
