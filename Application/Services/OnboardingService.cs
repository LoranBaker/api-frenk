using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IUserRepository _users;
    private readonly IPreUrgeVersionRepository _preUrgeVersions;
    private readonly IUserSummaryRepository _summaries;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        IUserRepository users,
        IPreUrgeVersionRepository preUrgeVersions,
        IUserSummaryRepository summaries,
        ILogger<OnboardingService> logger)
    {
        _users = users;
        _preUrgeVersions = preUrgeVersions;
        _summaries = summaries;
        _logger = logger;
    }

    /// <summary>
    /// Saves all onboarding answers in one logical operation.
    /// Idempotent — safe to call multiple times if user restarts onboarding.
    /// </summary>
    public async Task<SuccessResponse> SaveAsync(
        Guid userId,
        OnboardingRequest request,
        CancellationToken ct = default)
    {
        // 1. Upsert user_profiles
        var profile = new UserProfile
        {
            UserId = userId,
            FirstName = request.FirstName.Trim(),
            AddictionTypes = request.AddictionTypes,
            RiskWindows = request.RiskWindows,
            PreUrgeDescription = request.PreUrgeDescription.Trim(),
            CoreMotivation = request.CoreMotivation.Trim(),
            EnvironmentRisks = request.EnvironmentRisks,
            ReplacementStack = request.ReplacementStack,
            HabitDuration = request.HabitDuration,
            IdentityFraming = request.IdentityFraming,
            FutureSelfText = request.FutureSelfText?.Trim(),
            ChangeStage = "precontemplation",
            CreatedAt = DateTime.UtcNow
        };

        await _users.UpsertAsync(profile, ct);

        // 2. Create pre_urge_versions row (version 1)
        // Only create if no version exists yet — idempotent
        var existing = await _preUrgeVersions.GetLatestAsync(userId, ct);
        if (existing is null)
        {
            var version = new PreUrgeVersion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VersionText = request.PreUrgeDescription.Trim(),
                VersionNum = 1,
                Source = "onboarding",
                CreatedAt = DateTime.UtcNow
            };

            await _preUrgeVersions.AddAsync(version, ct);
            await _preUrgeVersions.SaveChangesAsync(ct);
        }

        // 3. Create user_summaries row with defaults
        var summary = await _summaries.GetByUserIdAsync(userId, ct);
        if (summary is null)
        {
            await _summaries.UpsertAsync(new UserSummary
            {
                UserId = userId,
                DailyUrgeCount = 0,
                UpdatedAt = DateTime.UtcNow
            }, ct);
        }

        _logger.LogInformation(
            "Onboarding saved for user {UserId}. Addictions: {Addictions}",
            userId, string.Join(", ", request.AddictionTypes));

        return new SuccessResponse();
    }

    public async Task<UserProfileResponse> GetProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        return new UserProfileResponse(
            UserId: profile.UserId,
            FirstName: profile.FirstName,
            AddictionTypes: profile.AddictionTypes,
            RiskWindows: profile.RiskWindows,
            ReplacementStack: profile.ReplacementStack,
            HabitDuration: profile.HabitDuration,
            IdentityFraming: profile.IdentityFraming,
            FutureSelfText: profile.FutureSelfText,
            ChangeStage: profile.ChangeStage,
            CreatedAt: profile.CreatedAt
        );
    }
}