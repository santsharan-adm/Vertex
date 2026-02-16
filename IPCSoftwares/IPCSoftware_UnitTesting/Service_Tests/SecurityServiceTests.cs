using System;
using System.Text;
using IPCSoftware.Services;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests;

public class SecurityServiceTests
{
    [Fact]
    public void HashPassword_ReturnsNonEmptyHashAndSalt()
    {
        // Arrange
        string password = "P@ssw0rd!";

        // Act
        string hash = SecurityService.HashPassword(password, out string salt);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.False(string.IsNullOrWhiteSpace(salt));
    }

    [Fact]
    public void HashPassword_ProducesDifferentSaltAndHash_ForSamePassword()
    {
        // Arrange
        string password = "SamePassword";

        // Act
        string hash1 = SecurityService.HashPassword(password, out string salt1);
        string hash2 = SecurityService.HashPassword(password, out string salt2);

        // Assert salts should differ and hashes should differ (PBKDF2 with random salt)
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPasswordAndFalseForIncorrect()
    {
        // Arrange
        string password = "CorrectHorseBatteryStaple";
        string storedHash = SecurityService.HashPassword(password, out string storedSalt);

        // Act & Assert
        Assert.True(SecurityService.VerifyPassword(password, storedHash, storedSalt));
        Assert.False(SecurityService.VerifyPassword("wrong-password", storedHash, storedSalt));
    }

    [Fact]
    public void VerifyPassword_FailsWhenSaltIsModified()
    {
        // Arrange
        string password = "PasswordToTest";
        string storedHash = SecurityService.HashPassword(password, out string storedSalt);

        // Modify the salt bytes to produce a different salt
        byte[] saltBytes = Convert.FromBase64String(storedSalt);
        saltBytes[0] ^= 0xFF; // flip bits in first byte
        string modifiedSalt = Convert.ToBase64String(saltBytes);

        // Act
        bool resultWithModifiedSalt = SecurityService.VerifyPassword(password, storedHash, modifiedSalt);

        // Assert
        Assert.False(resultWithModifiedSalt);
    }

    [Fact]
    public void HashPassword_EmptyPassword_IsHandledAndVerifiable()
    {
        // Arrange
        string password = string.Empty;

        // Act
        string hash = SecurityService.HashPassword(password, out string salt);

        // Assert hash produced and VerifyPassword recognizes it
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.True(SecurityService.VerifyPassword(password, hash, salt));
        Assert.False(SecurityService.VerifyPassword("not-empty", hash, salt));
    }

    [Fact]
    public void GenerateRowSignature_IsDeterministic_ForSameInputs()
    {
        // Arrange
        int id = 123;
        string username = "Alice";
        string role = "Admin";
        string passwordHash = "some-password-hash";
        bool isActive = true;

        // Act
        string sig1 = SecurityService.GenerateRowSignature(id, username, role, passwordHash, isActive);
        string sig2 = SecurityService.GenerateRowSignature(id, username, role, passwordHash, isActive);

        // Assert
        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void GenerateRowSignature_IsCaseInsensitiveForUsername()
    {
        // Arrange
        int id = 7;
        string usernameLower = "bob";
        string usernameUpper = "BOB";
        string role = "User";
        string passwordHash = "phash";
        bool isActive = false;

        // Act
        string sigLower = SecurityService.GenerateRowSignature(id, usernameLower, role, passwordHash, isActive);
        string sigUpper = SecurityService.GenerateRowSignature(id, usernameUpper, role, passwordHash, isActive);

        // Assert (username is lowercased inside the implementation)
        Assert.Equal(sigLower, sigUpper);
    }

    [Fact]
    public void GenerateRowSignature_ChangesWhenAnyFieldChanges()
    {
        // Arrange
        int id = 1;
        string username = "carol";
        string role = "Reader";
        string passwordHash = "hashA";
        bool isActive = true;

        string baseSig = SecurityService.GenerateRowSignature(id, username, role, passwordHash, isActive);

        // Change id
        string sigId = SecurityService.GenerateRowSignature(2, username, role, passwordHash, isActive);
        Assert.NotEqual(baseSig, sigId);

        // Change role
        string sigRole = SecurityService.GenerateRowSignature(id, username, "Writer", passwordHash, isActive);
        Assert.NotEqual(baseSig, sigRole);

        // Change passwordHash
        string sigPassword = SecurityService.GenerateRowSignature(id, username, role, "differentHash", isActive);
        Assert.NotEqual(baseSig, sigPassword);

        // Change isActive
        string sigActive = SecurityService.GenerateRowSignature(id, username, role, passwordHash, !isActive);
        Assert.NotEqual(baseSig, sigActive);
    }
}