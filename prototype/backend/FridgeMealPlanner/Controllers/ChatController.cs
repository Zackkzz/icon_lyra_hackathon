using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Services;
using Microsoft.AspNetCore.Mvc;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OpenRouterService _openRouter;
    private readonly ToolExecutor _toolExecutor;

    public ChatController(OpenRouterService openRouter, ToolExecutor toolExecutor)
    {
        _openRouter = openRouter;
        _toolExecutor = toolExecutor;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new ChatResponse("Message is required"));

        var toolDefinitions = ToolExecutor.GetToolDefinitions();

        var response = await _openRouter.ChatAsync(
            request.Message,
            toolDefinitions,
            async (toolName, arguments) => await _toolExecutor.ExecuteAsync(toolName, arguments)
        );

        return Ok(new ChatResponse(response));
    }
}
