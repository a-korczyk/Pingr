namespace Pingr.Domain.Common;

/// <summary>
/// Represents an application error.
/// </summary>
/// <param name="Code">A unique error code.</param>
/// <param name="Message">A human-readable error message.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents the absence of an error.
    /// Used by a successful <see cref="Result{T}"/> instance.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>
/// Contains application errors related to users.
/// </summary>
public static class UserErrors
{
    public static readonly Error InvalidCredentials =
        new(
            "Users.InvalidCredentials",
            "The provided email or password are invalid.");
    
    public static readonly Error EmailAlreadyExists =
        new(
            "Users.EmailAlreadyExists",
            "A user with the provided email already exists.");
    
    public static readonly Error UnverifiedEmail =
        new(
            "Users.UnverifiedEmail",
            "The provided email is not verified.");
    
    public static readonly Error NotFound = 
        new(
            "Users.UserNotFound",
            "This user couldn't be found.");
    
    public static readonly Error TwoFactorAlreadySetup = 
        new(
            "Users.TwoFactorAlreadySetup",
            "Two factor authentication has already been setup.");
    
    public static readonly Error TwoFactorSetupNotRequested =
        new(
            "Users.TwoFactorSetupNotRequested",
            "Two factor authentication has not been requested to be setup.");

    public static readonly Error TwoFactorRequired =
        new(
            "Users.TwoFactorRequired",
            "This action requires 2FA enabled.");

    public static readonly Error WorkspaceOwner =
        new(
            "Users.WorkspaceOwner",
            "User is a workspace owner.");
}

/// <summary>
/// Contains application errors related to email verification requests.
/// </summary>
public static class EmailVerificationRequestErrors
{
    public static readonly Error NotFound =
        new(
            "EmailVerificationRequests.NotFound",
            "Couldn't find a request with the provided identifier.");
    
    public static readonly Error Invalid =
        new(
            "EmailVerificationRequests.Invalid",
            "The provided token is invalid.");

    public static readonly Error Expired =
        new(
            "EmailVerificationRequests.Expired",
            "The provided token has expired.");
}

/// <summary>
/// Contains application errors related to 2FA.
/// </summary>
public static class TwoFactorErrors
{
    public static readonly Error NoChallengeFound =
        new(
            "TwoFactor.NoChallengeFound",
            "No appropriate challenge could be found.");

    public static readonly Error ExpiredChallenge =
        new(
            "TwoFactor.ExpiredChallenge",
            "The challenge has expired.");

    public static readonly Error InvalidToken =
        new(
            "TwoFactor.InvalidToken",
            "The provided token is invalid.");

    public static readonly Error InvalidTotpCode =
        new(
            "TwoFactor.InvalidTotpCode",
            "The provided Totp code is invalid.");
}

public sealed class WorkspaceErrors
{
    public static readonly Error NotFound =
        new(
            "Workspace.NotFound",
            "Workspace couldn't be found.");
    
    public static readonly Error UserNotFound =
        new(
            "Workspace.UserNotFound",
            "This user couldn't be found.");
    
    public static readonly Error UserAlreadyInWorkspace =
        new(
            "Workspace.UserAlreadyInWorkspace",
            "This user is already a member in the workspace.");

    public static readonly Error ActionForbidden =
        new(
            "Workspace.ActionForbidden",
            "This action is forbidden.");
}

/// <summary>
/// Contains application errors related to refresh tokens.
/// </summary>
public static class RefreshTokenErrors
{
    public static readonly Error NotFound =
        new(
            "RefreshToken.NotFound",
            "No such refresh token could be found.");
    
    public static readonly Error Expired =
        new(
            "RefreshToken.Expired",
            "The provided token has expired.");
    
    public static readonly Error Revoked =
        new(
            "RefreshToken.Revoked",
            "The provided token has been revoked.");
}

/// <summary>
/// Contains application errors related to logs.
/// </summary>
public sealed class LogErrors
{
    public static readonly Error LogWithIdNotFound =
        new(
            "Logs.LogWithIdNotFound",
            "Couldn't find a log with the provided Id.");

    public static readonly Error Forbidden =
        new(
            "Logs.Forbidden",
            "You don't have access this log.");
}

/// <summary>
/// Contains application errors related to monitors.
/// </summary>
public sealed class MonitorErrors 
{
    public static readonly Error Forbidden =
        new(
            "Monitors.Forbidden",
            "You don't have access this monitor.");
    
    public static readonly Error NotFound =
        new(
            "Monitors.NotFound",
            "No monitor with the provided details could be found.");
}

/// <summary>
/// Contains application errors related to validation.
/// </summary>
public static class ValidationErrors
{
    public static readonly Error Failed =
        new(
            "Validation.Failed",
            "Validation failed.");
}

/// <summary>
/// Contains application errors related to the server.
/// </summary>
public static class ServerErrors
{
    public static readonly Error InternalError =
        new(
            "Server.InternalError",
            string.Empty);
}
