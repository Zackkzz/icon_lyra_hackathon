using System.Net.Http.Json;
using System.Text.Json;

namespace FridgeMealPlanner.Services;

public class OpenRouterService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenRouterService(HttpClient http)
    {
        _http = http;
        _apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
        _model = Environment.GetEnvironmentVariable("OPENROUTER_MODEL") ?? "openai/gpt-4o-mini";
        _http.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://fridge-planner.local");
        _http.DefaultRequestHeaders.Add("X-Title", "Fridge Meal Planner");
    }

    public async Task<string> ChatAsync(
        string userMessage,
        List<object> toolDefinitions,
        Func<string, string, Task<string>> executeTool)
    {
        var messages = new List<object>
        {
            new { role = "system", content = "You are a helpful fridge meal planner assistant. You can look up what's in the user's fridge, suggest recipes, create meal plans, and generate shopping lists. Always use the available tools to get real data before answering. Be friendly and concise." },
            new { role = "user", content = userMessage }
        };

        var tools = toolDefinitions;

        for (int iteration = 0; iteration < 10; iteration++)
        {
            var requestBody = new
            {
                model = _model,
                messages,
                tools,
                tool_choice = "auto"
            };

            var response = await _http.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var choices = json.GetProperty("choices");

            if (choices.GetArrayLength() == 0)
                return "I couldn't process that request.";

            var choice = choices[0];
            var message = choice.GetProperty("message");

            // Check for tool calls
            if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
            {
                messages.Add(new
                {
                    role = "assistant",
                    content = (string?)null,
                    tool_calls = toolCalls
                });

                foreach (var toolCall in toolCalls.EnumerateArray())
                {
                    var function = toolCall.GetProperty("function");
                    var toolName = function.GetProperty("name").GetString()!;
                    var arguments = function.GetProperty("arguments").GetString() ?? "{}";
                    var toolCallId = toolCall.GetProperty("id").GetString()!;

                    var toolResult = await executeTool(toolName, arguments);

                    messages.Add(new
                    {
                        role = "tool",
                        tool_call_id = toolCallId,
                        content = toolResult
                    });
                }
            }
            else
            {
                // No tool calls – return the assistant's content
                var content = message.GetProperty("content");
                return content.GetString() ?? "I'm not sure how to help with that.";
            }
        }

        return "I went through too many steps trying to process your request. Could you try asking more specifically?";
    }
}
