using Google.GenAI;
using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace HabitApp.Application;

public class AICoachApplicationService(IConfiguration configuration) : IAICoachApplicationService
{
    private readonly IConfiguration _configuration = configuration;

    private static Client CreateClient(string apiKey) => new(apiKey: apiKey);

    public async Task<AICoachResponseDto> SendMessageAsync(AICoachRequestDto request)
    {
        var enabled = _configuration["AI_ENABLED"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        var provider = _configuration["AI_PROVIDER"] ?? "gemini";

        if (!enabled)
        {
            return new AICoachResponseDto
            {
                Success = false,
                Provider = provider,
                Response = "O coach de IA está desativado no momento.",
                Error = "AI_ENABLED is false"
            };
        }

        var apiKey = _configuration["GEMINI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AICoachResponseDto
            {
                Success = true,
                Provider = "mock",
                Response = $"Você pediu: \"{request.Message}\". Como ainda não há chave de IA configurada, esta é uma resposta de teste. Para retomar seus hábitos, comece com um passo pequeno hoje: 5 minutos ou uma marcação simples.",
                Error = null
            };
        }

        var prompt = BuildPrompt(request);
        var model = _configuration["GEMINI_MODEL"] ?? "gemini-3.5-flash";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var client = CreateClient(apiKey);

            var response = await client.Models.GenerateContentAsync(
                model: model,
                contents: prompt,
                cancellationToken: cts.Token
            );

            return new AICoachResponseDto
            {
                Success = true,
                Provider = provider,
                Response = response.Text ?? "Resposta recebida da IA.",
                Error = null
            };
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            return new AICoachResponseDto
            {
                Success = false,
                Provider = provider,
                Response = "A resposta da IA demorou demais para chegar.",
                Error = "Timeout ao comunicar com a IA. Tente novamente."
            };
        }
        catch (Exception ex)
        {
            return new AICoachResponseDto
            {
                Success = false,
                Provider = provider,
                Response = "Não foi possível acessar a IA no momento.",
                Error = ex.Message
            };
        }
    }

    public async Task<object> CheckHealthAsync()
    {
        var enabled = _configuration["AI_ENABLED"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        var provider = _configuration["AI_PROVIDER"] ?? "gemini";

        if (!enabled)
        {
            return new
            {
                success = false,
                provider,
                available = false,
                message = "O coach de IA está desativado."
            };
        }

        var apiKey = _configuration["GEMINI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new
            {
                success = false,
                provider,
                available = false,
                message = "A chave da IA não está configurada."
            };
        }

        try
        {
            var client = CreateClient(apiKey);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await client.Models.GenerateContentAsync(
                model: _configuration["GEMINI_MODEL"] ?? "gemini-3.5-flash",
                contents: "Responda apenas com a palavra OK.",
                cancellationToken: cts.Token
            );

            return new
            {
                success = true,
                provider,
                available = !string.IsNullOrWhiteSpace(response.Text),
                message = "Coach disponível"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                success = false,
                provider,
                available = false,
                message = ex.Message
            };
        }
    }

    private string BuildPrompt(AICoachRequestDto request)
    {
        var systemPrompt = NormalizePrompt(_configuration["AI_COACH_SYSTEM_PROMPT"]);
        return $"{systemPrompt}\n\nPergunta: {request.Message}\nResponda em português e com até 120 palavras.";
    }

    private static string NormalizePrompt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Você é um coach de hábitos motivador, curto, prático e empático. Responda com orientações objetivas baseadas nos dados do usuário.";
        }

        return value
            .Replace("\\n", Environment.NewLine)
            .Replace("\\r", string.Empty);
    }

    private static bool TryGetMessage(JsonElement element, out string message)
    {
        message = string.Empty;

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryGetPropertyValue(element, "message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
            {
                message = messageElement.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(message);
            }

            if (TryGetPropertyValue(element, "error", out var errorElement))
            {
                if (TryGetMessage(errorElement, out message))
                {
                    return true;
                }
            }

            if (TryGetPropertyValue(element, "details", out var detailsElement) && detailsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var detail in detailsElement.EnumerateArray())
                {
                    if (TryGetMessage(detail, out message))
                    {
                        return true;
                    }
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            message = element.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(message);
        }

        return false;
    }

    private static bool TryGetPropertyValue(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }
}
