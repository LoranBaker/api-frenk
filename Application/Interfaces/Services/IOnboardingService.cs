using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

public interface IOnboardingService
{
    /// <summary>
    /// Saves all onboarding answers in one transaction:
    ///   - Upserts user_profiles row
    ///   - Creates pre_urge_versions row (version 1, source=onboarding)
    ///   - Creates user_summaries row with defaults
    /// Idempotent — safe to call multiple times if user restarts onboarding.
    /// </summary>
    Task<SuccessResponse> SaveAsync(
        Guid userId,
        OnboardingRequest request,
        CancellationToken ct = default);

    Task<UserProfileResponse> GetProfileAsync(
        Guid userId,
        CancellationToken ct = default);
}