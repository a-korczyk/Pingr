using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.AI;

namespace Pingr.Infrastructure.Services;

/// <summary>
/// Defines methods for communicating with a chat model.
/// </summary>
public interface IChatService 
{
    /// <summary>
    /// Sends a prompt.
    /// </summary>
    /// <param name="prompt">The prompt to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="options">Custom prompt <see cref="ChatOptions"/>.</param>
    /// <returns>The chat response from the model.</returns>
    Task<ChatResponse> SendAsync(
        string prompt,
        CancellationToken cancellationToken,
        ChatOptions? options = null);
}

/// <inheritdoc/>
public sealed class ChatService(
    IChatClient chatClient) : IChatService 
{
    /// <inheritdoc/>
    /// <remarks>
    /// By default, the reasoning effort is set to low
    /// and temperature to 0.2f .
    /// </remarks>
    public async Task<ChatResponse> SendAsync(
        string prompt,
        CancellationToken cancellationToken,
        ChatOptions? options = null)
    {
        return await chatClient.GetResponseAsync(
            prompt,
            options: options ?? new ChatOptions
            {
                Reasoning = new ReasoningOptions
                {
                    Output = ReasoningOutput.None,
                    Effort = ReasoningEffort.Low
                },
                Temperature = 0.2f
            },
            cancellationToken);
    }
}

/// <summary>
/// Chat model configuration options.
/// </summary>
public sealed class ChatServiceOptions 
{
    public const string SectionName = "Ai";

    [Required] 
    public string BaseAddress { get; init; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Model { get; init; } = string.Empty;
}