using Frank.Application.DTOs.Responses;
using Frank.Domain.Entities;

namespace Frank.Application.Interfaces.Services;

public interface IMiniMirrorService
{
    /// <summary>
    /// Checks if user qualifies for mini mirror and generates it if so.
    /// 
    /// Trigger conditions (ALL must be true):
    ///   - Day 3-7 after signup
    ///   - 3+ urge presses with pre_urge_context written
    ///   - Never shown before (one time only, ever)
    /// 
    /// Returns null if conditions not met.
    /// Returns MiniMirrorResponse with quotes + observation if triggered.
    /// </summary>
    Task<MiniMirrorResponse?> TryGenerateAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<MiniMirrorEvent?> GetShownAsync(Guid userId, CancellationToken ct = default);
}