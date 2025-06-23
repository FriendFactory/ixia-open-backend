using AuthServer.Services.UserManaging.NicknameSuggestion;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Frever.Auth.Core.Test;

[Collection("Nickname suggestion test")]
public class NicknameSuggestionTest
{
    [Fact(DisplayName = "👍👎 Suggest Nickname base functionality")]
    public async Task SuggestNickname()
    {
        // Arrange
        var testInstance = CreateTestServiceInstance();

        // Act
        var suggestions = await testInstance.SuggestNickname(string.Empty, 3);

        // Assert
        suggestions.Should()
                   .NotBeEmpty()
                   .And.HaveCount(3)
                   .And.OnlyHaveUniqueItems()
                   .And.AllSatisfy(e => { e.Should().MatchRegex("[A-Z]{1}[a-z]{1,}[A-Z]{1}[a-z]{1,}[0-9]{1,3}"); });
    }

    [Fact(DisplayName = "👍👎 Nicknames should be unique")]
    public async Task SuggestNicknamesShouldBeUnique()
    {
        // Arrange
        var data = new Mock<INicknameSuggestionData>();
        data.Setup(s => s.GetNouns()).Returns(Task.FromResult(new[] {"Earth", "Moon", "Venus"}));
        data.Setup(s => s.GetAdjectives()).Returns(Task.FromResult(new[] {"Full", "Half", "Empty"}));

        var testInstance = CreateTestServiceInstance(data.Object);

        // Act
        var suggestions = await testInstance.SuggestNickname(string.Empty, 3);

        // Assert
        suggestions.Should()
                   .NotBeEmpty()
                   .And.HaveCount(3)
                   .And.OnlyHaveUniqueItems()
                   .And.AllSatisfy(e => { e.Should().MatchRegex("[A-Z]{1}[a-z]{1,}[A-Z]{1}[a-z]{1,}[0-9]{1,3}"); });
    }

    private INicknameSuggestionService CreateTestServiceInstance(
        INicknameSuggestionData data = null,
        Mock<INicknameSuggestionRepository> repo = null
    )
    {
        if (repo == null)
        {
            repo = new Mock<INicknameSuggestionRepository>();
            repo.Setup(r => r.AllNicknameByPrefix(It.IsAny<string>())).Returns(Enumerable.Empty<string>().BuildMock());
        }

        data ??= new HardcodedNicknameSuggestionData();

        var service = new NicknameSuggestionService(data, repo.Object);

        return service;
    }
}