using Pingr.Domain.Entities;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Application.Abstractions.Services.Email;

/// <remarks><c>Body</c> must be in Markdown format.</remarks>
public sealed record EmailTemplate(
    string Subject,
    string Body);

/// <summary>
/// Provides predefined email templates for application notifications.
/// </summary>
public static class EmailTemplates
{
    public static EmailTemplate CriticalErrorLogged(
        Log log) =>
        new(
            "Critical Error Logged - Pingr",
            $"""
             # Critical Error Logged
             A critical error has been registered and requires immediate attention.
             
             ## Error Details:
             - Identifier: {log.Id}
             - Title: {log.Title}
             - Logged At (UTC): {log.CreatedAt}
             
             This email was generated automatically by Pingr.
             """
        );
    
    public static EmailTemplate CriticalErrorResolved(
        Log log) =>
        new(
            "Resolved Critical Error - Pingr",
            $"""
             # Critical Error Resolved

             ## Resolved Error Details:
             - Identifier: {log.Id}
             - Title: {log.Title}
             - Logged At (UTC): {log.CreatedAt}

             This email was generated automatically by Pingr.
             """
        );

    public static EmailTemplate LogDigest(string workspaceName) =>
        new(
            $"{workspaceName} - Log Summary (Last 30 Minutes) - Pingr",
            string.Empty);

    public static EmailTemplate LogDigestFailed(string workspaceName) =>
        new(
            $"{workspaceName} - Log Summary (Last 30 Minutes) - Pingr",
            $"""
             # Log Summary From The Last 30 Minutes
             
             There was a problem with generating the AI summary.
             
             This email was generated automatically by Pingr.
             """);
}

/// <summary>
/// Provides predefined email templates for auth-related notifications.
/// </summary>
public static class AuthEmailTemplates
{
    public static EmailTemplate VerifyEmail(
        string verificationUrl) =>
        new(
            "Verify Your Email - Pingr",
            $"""
             # Verify Your Email
             
             Thank you for creating your Pingr account.
             
             To activate your account, please verify your email by clicking the link below.
             
             {verificationUrl}
             
             If it wasn't you that created this account, you can safely ignore this email.
             
             This email was generated automatically by Pingr.
             """);

    public static readonly EmailTemplate EmailVerified =
        new(
            "Your Email Has Been Verified - Pingr",
            """
            # Your Email Has Been Verified
            
            Thank you for verifying your email address.
            
            Your Pingr account is now active and you can log in.
            
            This email was generated automatically by Pingr.
            """);
}

/// <summary>
/// Provides predefined email templates for workspace related notifications.
/// </summary>
public static class WorkspaceEmailTemplates
{
    public static EmailTemplate OwnerOfNewWorkspace(string workspaceName) =>
        new(
            $"You're now the owner of {workspaceName}  - Pingr",
            $"""
             # Owner of {workspaceName}
                    
             Ownership of {workspaceName} has been transferred to you.
                    
                    
             This email was generated automatically by Pingr.
             """);
}

/// <summary>
/// Provides predefined email templates for monitor related notifications.
/// </summary>
public static class MonitorEmailTemplates
{
    public static EmailTemplate MonitorRecovered(string monitorName) =>
        new(
            $"Monitor {monitorName} has recovered - Pingr",
            string.Empty);
    
    public static EmailTemplate MonitorDown(string monitorName) =>
        new(
            $"Monitor {monitorName} is down - Pingr",
            string.Empty);
}