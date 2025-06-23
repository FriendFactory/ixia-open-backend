using AuthServer.Features.CredentialUpdate;
using FluentAssertions;
using Xunit;

namespace Frever.Auth.Core.Test;

public class CredentialsFormatterTest
{
    [Theory(DisplayName = "👍👍Check email mask")]
    [InlineData("test1234@email", "t***4@email")]
    [InlineData("test123@email", "t***3@email")]
    [InlineData("t23@email", "t***3@email")]
    [InlineData("t3@email", "t3@email")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void MaskEmail(string email, string result)
    {
        // Arrange

        // Act
        var act = CredentialsFormatter.MaskEmail(email);

        // Assert
        act.Should().Be(result);
    }

    [Theory(DisplayName = "👍👍Check phone number mask")]
    [InlineData("+12345", "+1 2345")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void MaskPhoneNumber(string phoneNumber, string result)
    {
        // Arrange

        // Act
        var act = CredentialsFormatter.MaskPhoneNumber(phoneNumber);

        // Assert
        act.Should().Be(result);
    }
}