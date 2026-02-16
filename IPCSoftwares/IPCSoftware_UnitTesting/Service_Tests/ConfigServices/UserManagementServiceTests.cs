using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;

namespace IPCSoftware_UnitTesting.Service_Tests.ConfigServices
{
    public class UserManagementServiceTests : IDisposable
    {
        private readonly string _tempFolder;

        public UserManagementServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "UserMgmtTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempFolder))
                    Directory.Delete(_tempFolder, true);
            }
            catch
            {
                // best-effort cleanup
            }
        }

        private UserManagementService CreateService(IAppLogger logger = null)
        {
            var cfg = new ConfigSettings
            {
                DataFolder = _tempFolder,
                UserFileName = "Users.csv"
            };
            return new UserManagementService(Options.Create(cfg), logger ?? new NoOpLogger());
        }

        private class NoOpLogger : IAppLogger
        {
            public void LogInfo(string message, LogType type) { }
            public void LogWarning(string message, LogType type) { }
            public void LogError(string message, LogType type, string memberName = "", string filePath = "", int lineNumber = 0) { }
        }

        [Fact]
        public async Task InitializeAsync_NoFile_CreatesCsvAndLoadsEmptyList()
        {
            // Arrange
            var svc = CreateService();
            var csvPath = Path.Combine(_tempFolder, "Users.csv");
            if (File.Exists(csvPath)) File.Delete(csvPath);

            // Act
            await svc.InitializeAsync();
            var users = await svc.GetAllUsersAsync();

            // Assert
            Assert.NotNull(users);
            Assert.Empty(users);
            Assert.True(File.Exists(csvPath));
            var content = await File.ReadAllTextAsync(csvPath, Encoding.UTF8);
            Assert.Contains("Id,FirstName,LastName,UserName,PasswordHash,PasswordSalt,Role,IsActive,IntegritySignature", content);
        }

        [Fact]
        public async Task AddUserAsync_AddsUser_HashesPasswordAndPersists()
        {
            // Arrange
            var svc = CreateService();
            var csvPath = Path.Combine(_tempFolder, "Users.csv");
            if (File.Exists(csvPath)) File.Delete(csvPath);

            var u = new UserConfigurationModel
            {
                FirstName = "John",
                LastName = "Doe",
                UserName = "jdoe",
                PlainTextPassword = "P@ssw0rd!",
                Role = "Admin",
                IsActive = true
            };

            // Act
            var added = await svc.AddUserAsync(u);

            // Assert
            Assert.NotNull(added);
            Assert.Equal(1, added.Id);
            Assert.False(string.IsNullOrEmpty(added.Password)); // hashed
            Assert.False(string.IsNullOrEmpty(added.PasswordSalt));
            Assert.False(string.IsNullOrEmpty(added.RowSignature));

            var all = await svc.GetAllUsersAsync();
            Assert.Single(all);
            Assert.Equal("jdoe", all[0].UserName);

            // persisted
            Assert.True(File.Exists(csvPath));
            var content = await File.ReadAllTextAsync(csvPath, Encoding.UTF8);
            Assert.Contains("jdoe", content);
            Assert.Contains(added.RowSignature, content);
        }

        [Fact]
        public async Task AddUserAsync2_DuplicateUsername_Throws()
        {
            // Arrange
            var svc = CreateService();
            var u1 = new UserConfigurationModel { FirstName = "A", LastName = "A", UserName = "dup", Role = "User", IsActive = true };
            var u2 = new UserConfigurationModel { FirstName = "B", LastName = "B", UserName = "Dup", Role = "User", IsActive = true };

            // Act
            await svc.AddUserAsync2(u1);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.AddUserAsync2(u2));
        }

        [Fact]
        public async Task GetUserByIdAndUsername_ReturnsCorrect()
        {
            // Arrange
            var svc = CreateService();
            var added = await svc.AddUserAsync(new UserConfigurationModel
            {
                FirstName = "Sam",
                LastName = "Smith",
                UserName = "sam",
                PlainTextPassword = "x",
                Role = "User",
                IsActive = true
            });

            // Act
            var byId = await svc.GetUserByIdAsync(added.Id);
            var byName = await svc.GetUserByUsernameAsync("SAM");

            // Assert
            Assert.NotNull(byId);
            Assert.Equal("sam", byId.UserName);
            Assert.NotNull(byName);
            Assert.Equal(added.Id, byName.Id);
        }

        [Fact]
        public async Task UpdateUserAsync_RehashesPasswordWhenPlainTextProvided_UpdatesFields()
        {
            // Arrange
            var svc = CreateService();
            var added = await svc.AddUserAsync(new UserConfigurationModel
            {
                FirstName = "Old",
                LastName = "Name",
                UserName = "user1",
                PlainTextPassword = "oldpw",
                Role = "User",
                IsActive = true
            });

            var beforePassword = added.Password;
            var beforeSalt = added.PasswordSalt;

            // Act - update some fields and change password
            var update = new UserConfigurationModel
            {
                Id = added.Id,
                FirstName = "NewFirst",
                LastName = "NewLast",
                UserName = "user1",
                PlainTextPassword = "newpw",
                Role = "Admin",
                IsActive = false
            };

            var result = await svc.UpdateUserAsync(update);

            // Assert
            Assert.True(result);
            var fetched = await svc.GetUserByIdAsync(added.Id);
            Assert.Equal("NewFirst", fetched.FirstName);
            Assert.Equal("Admin", fetched.Role);
            Assert.False(fetched.IsActive);
            Assert.NotEqual(beforePassword, fetched.Password);
            Assert.NotEqual(beforeSalt, fetched.PasswordSalt);

            // persisted
            var csvPath = Path.Combine(_tempFolder, "Users.csv");
            var text = await File.ReadAllTextAsync(csvPath, Encoding.UTF8);
            Assert.Contains("NewFirst", text);
            Assert.Contains(fetched.RowSignature, text);
        }

        [Fact]
        public async Task UpdateUserAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            var svc = CreateService();
            var fake = new UserConfigurationModel { Id = 9999, UserName = "no", FirstName = "X", LastName = "Y", Role = "User", IsActive = true };

            // Act
            var result = await svc.UpdateUserAsync(fake);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserAsync2_DuplicateUsername_ReturnsFalse()
        {
            // Arrange
            var svc = CreateService();
            var a = await svc.AddUserAsync2(new UserConfigurationModel { FirstName = "A", LastName = "A", UserName = "alpha", Role = "User", IsActive = true });
            var b = await svc.AddUserAsync2(new UserConfigurationModel { FirstName = "B", LastName = "B", UserName = "beta", Role = "User", IsActive = true });

            // Attempt to change A to username of B
            var updateA = new UserConfigurationModel { Id = a.Id, FirstName = "A", LastName = "A", UserName = "beta", Role = "User", IsActive = true };

            // Act
            var result = await svc.UpdateUserAsync2(updateA);

            // Assert: method catches and returns false on duplicate
            Assert.False(result);

            // Ensure original is still present and not overwritten
            var all = await svc.GetAllUsersAsync();
            Assert.Contains(all, u => u.Id == a.Id && u.UserName == "alpha");
        }

        [Fact]
        public async Task DeleteUserAsync_Success_RemovesAndPersists()
        {
            // Arrange
            var svc = CreateService();
            var added = await svc.AddUserAsync(new UserConfigurationModel
            {
                FirstName = "To",
                LastName = "Delete",
                UserName = "todel",
                PlainTextPassword = "x",
                Role = "User",
                IsActive = true
            });

            // Act
            var deleted = await svc.DeleteUserAsync(added.Id);

            // Assert
            Assert.True(deleted);
            var all = await svc.GetAllUsersAsync();
            Assert.Empty(all);

            var csvPath = Path.Combine(_tempFolder, "Users.csv");
            var content = await File.ReadAllTextAsync(csvPath, Encoding.UTF8);
            Assert.DoesNotContain("todel", content);
        }

        [Fact]
        public async Task DeleteUserAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            var svc = CreateService();

            // Act
            var result = await svc.DeleteUserAsync(123456);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task InitializeAsync_WithExistingCsv_LoadsAndSetsNextId()
        {
            // Arrange: create initial service and add two users
            var svcWriter = CreateService();
            var u1 = await svcWriter.AddUserAsync(new UserConfigurationModel { FirstName = "One", LastName = "One", UserName = "u1", PlainTextPassword = "p1", Role = "User", IsActive = true });
            var u2 = await svcWriter.AddUserAsync(new UserConfigurationModel { FirstName = "Two", LastName = "Two", UserName = "u2", PlainTextPassword = "p2", Role = "User", IsActive = true });

            // Create a new service instance that will load from the same CSV
            var svcReader = CreateService();
            await svcReader.InitializeAsync();
            var loaded = await svcReader.GetAllUsersAsync();

            // Assert loaded correctly
            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, u => u.UserName == "u1");
            Assert.Contains(loaded, u => u.UserName == "u2");

            // Next add should get id = max(existing)+1
            var newUser = new UserConfigurationModel { FirstName = "Three", LastName = "Three", UserName = "u3", PlainTextPassword = "p3", Role = "User", IsActive = true };
            var added = await svcReader.AddUserAsync(newUser);

            int expectedNextId = Math.Max(u1.Id, u2.Id) + 1;
            Assert.Equal(expectedNextId, added.Id);
        }
    }
}