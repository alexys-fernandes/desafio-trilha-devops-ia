using HabitApp.Application;
using HabitApp.Application.Dtos;
using Microsoft.Extensions.Configuration;

namespace HabitApp.Domain.Services.Tests;

public class AICoachApplicationServiceTests
{
    [Fact]
    public async Task GetCoachResponseAsync_ShouldReturnHelpfulResponse_WhenNoApiKeyConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI_ENABLED"] = "true",
                ["AI_PROVIDER"] = "gemini"
            })
            .Build();

        var service = new AICoachApplicationService(configuration);

        var result = await service.SendMessageAsync(new AICoachRequestDto
        {
            UserId = 1,
            Message = "Estou desanimado"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Response));
        Assert.Equal("mock", result.Provider, ignoreCase: true);
        Assert.True(result.Success);
    }
}
