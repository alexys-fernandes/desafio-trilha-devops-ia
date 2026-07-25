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

        var service = new AICoachApplicationService(configuration, new HttpClient());

        var result = await service.GetCoachResponseAsync(new AICoachRequestDto
        {
            UserId = 1,
            Message = "Estou desanimado",
            ContextSummary = "Quero voltar a estudar todos os dias"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Response));
        Assert.Equal("mock", result.Provider, ignoreCase: true);
        Assert.True(result.Success);
    }
}
