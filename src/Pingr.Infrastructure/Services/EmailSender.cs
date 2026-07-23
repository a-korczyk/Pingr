using System.ComponentModel.DataAnnotations;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using MailKit.Net.Smtp;
using Markdig;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Pingr.Infrastructure.Services;

/// <summary>
/// Sends emails using a SMTP client.
/// </summary>
public sealed class EmailSender(
    IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;
    
    public async Task SendAsync(
        EmailMessageDetails messageDetails,
        CancellationToken cancellationToken)
    {
        using var smtpClient = new SmtpClient();
        
        await smtpClient.ConnectAsync(_options.Host, _options.Port, cancellationToken: cancellationToken);
        await smtpClient.AuthenticateAsync(_options.Username,  _options.Password, cancellationToken);
        
        var message = BuildMessage(messageDetails);
        await smtpClient.SendAsync(message, cancellationToken);

        await smtpClient.DisconnectAsync(true, cancellationToken);
    }
    
    /// <summary>
    /// Builds an email message using the provided details.
    /// </summary>
    /// <param name="messageDetails">The details to include in the message.</param>
    /// <returns>A <see cref="MimeMessage"/> including the provided details.</returns>
    private MimeMessage BuildMessage(
        EmailMessageDetails messageDetails)
    {
        var message = new MimeMessage();
        
        // Set addresses
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(messageDetails.RecipientName, messageDetails.RecipientEmail));
        
        // Build body
        var bodyBuilder = new BodyBuilder
        {
            TextBody = Markdown.ToPlainText(messageDetails.MarkdownBody),
            HtmlBody = Markdown.ToHtml(messageDetails.MarkdownBody)
        };
        message.Body = bodyBuilder.ToMessageBody();
        
        message.Subject = messageDetails.Subject;

        return message;
    }
}


/// <summary>
/// SMTP configuration options.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Email";
    
    /// <summary>
    /// SMTP server host name.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Host { get; init; } = string.Empty;
    
    /// <summary>
    /// SMTP server port number.
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; init; }
    
    /// <summary>
    /// SMTP username.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Username { get; init; } = string.Empty;
    
    /// <summary>
    /// SMTP password.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Password { get; init; } = string.Empty;
    
    /// <summary>
    /// Sender email address.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string FromAddress { get; init; } = string.Empty;
    
    /// <summary>
    /// Sender display name.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FromName { get; init; } = string.Empty;
}