using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Starts the process of deleting a user's account.
/// </summary>
public sealed record StartDeleteUserCommand(
    Guid UserId) : IRequest<Result<StartDeleteUserResponse>>;

public sealed class StartDeleteUserCommandHandler(
    IWorkspaceRepository workspaceRepository,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork, 
    IUserRepository userRepository,
    ICurrentUser currentUser) : IRequestHandler<StartDeleteUserCommand, Result<StartDeleteUserResponse>>
{
    public async Task<Result<StartDeleteUserResponse>> Handle(StartDeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Prevent deleting other users
        if (request.UserId != currentUser.GetUserId())
            return UserErrors.NotFound;
        
        var user = await userRepository.GetByIdAsync(
            currentUser.GetUserId(),
            cancellationToken);

        // Check if user has 2FA enabled
        if (user.TwoFactorEnabled is false)
            return UserErrors.TwoFactorRequired;

        var ownedWorkspaces = await workspaceRepository.GetByOwnerUserIdAsync(
            user.Id,
            new(
                Pagination.DefaultPage,
                Pagination.DefaultPageSize),
            cancellationToken);

        // Check if user owns any workspaces
        if (ownedWorkspaces.Any())
            return UserErrors.WorkspaceOwner;
        
        var existingChallenge = await twoFactorChallengeRepository.GetAsync(user.Id, cancellationToken);
        var twoFactorToken = tokenGenerator.GenerateToken();
        
        if (existingChallenge is not null)
        {
            existingChallenge.Update(
                tokenGenerator.HashToken(twoFactorToken),
                TwoFactorChallengePurpose.DeleteAccount);
        }
        else
        {
            await twoFactorChallengeRepository.AddAsync(
                new(
                    user.Id,
                    tokenGenerator.HashToken(twoFactorToken),
                    TwoFactorChallengePurpose.DeleteAccount),
                cancellationToken);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StartDeleteUserResponse(
            true,
            twoFactorToken);
    }
}

public sealed record StartDeleteUserResponse(
    bool TwoFactorRequired,
    string? TwoFactorToken);

public sealed class StartDeleteUserCommandValidator : AbstractValidator<StartDeleteUserCommand>
{
    public StartDeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId must not be empty.");
    }
}