using Frank.Domain.Entities;
using Frank.Infrastructure.Data;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frank.Infrastructure.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _db;

    public QuestionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Question?> GetByIdAsync(
        string questionId,
        CancellationToken ct = default)
        => await _db.Questions
            .Include(q => q.TapOptions.Where(t => t.Active))
            .FirstOrDefaultAsync(q => q.Id == questionId && q.Active, ct);

    public async Task<List<Question>> GetByFrameAsync(
        string sessionType,
        string frame,
        Guid userId,
        int cooldownDays,
        CancellationToken ct = default)
    {
        var query = _db.Questions
            .Where(q => q.SessionType == sessionType
                     && q.Frame == frame
                     && q.Active
                     && q.Phase == "mvp");

        // Apply cooldown filter
        if (userId != Guid.Empty && cooldownDays > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-cooldownDays);
            var recentlyUsed = await _db.QuestionUsages
                .Where(u => u.UserId == userId
                         && u.ShownAt >= cutoff
                         && u.SessionType == sessionType)
                .Select(u => u.QuestionId)
                .ToListAsync(ct);

            query = query.Where(q => !recentlyUsed.Contains(q.Id));
        }

        return await query
            .OrderByDescending(q => q.Weight)
            .ToListAsync(ct);
    }

    public async Task<List<Question>> GetByTagAsync(
    string tag,
    CancellationToken ct = default)
    => await _db.Questions
        .Where(q => q.Active
                 && q.PatternTags.Contains(tag))
        .OrderByDescending(q => q.Weight)
        .ToListAsync(ct);

    public async Task<List<Question>> GetBySessionTypeAsync(
    string sessionType,
    CancellationToken ct = default)
    {
        var query = _db.Questions
            .Include(q => q.TapOptions.Where(t => t.Active))
            .Where(q => q.SessionType == sessionType
                     && q.Active
                     && q.Phase == "mvp");

        // Halt has two subtypes:
        //   screening    → H01-H05, shown to user in the urge HALT flow
        //   intervention → H01B-HX4, served by GetByHaltRootAsync after root detected
        // This endpoint must only ever return the 5 screening questions.
        if (sessionType == "halt")
            query = query.Where(q => q.Subtype == "screening");

        return await query
            .OrderBy(q => q.Id)
            .ToListAsync(ct);
    }

    public async Task<List<QuestionSelectionRule>> GetSelectionRulesAsync(
        string sessionType,
        CancellationToken ct = default)
        => await _db.QuestionSelectionRules
            .Where(r => r.SessionType == sessionType && r.Active)
            .OrderBy(r => r.Slot)
            .ToListAsync(ct);

    public async Task RecordUsageAsync(
        Guid userId,
        string questionId,
        string sessionType,
        CancellationToken ct = default)
    {
        await _db.QuestionUsages.AddAsync(new QuestionUsage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionId = questionId,
            SessionType = sessionType,
            ShownAt = DateTime.UtcNow
        }, ct);

        await _db.SaveChangesAsync(ct);
    }
    public async Task<List<Question>> GetByHaltRootAsync(
     string haltRoot, CancellationToken ct = default)
     => await _db.Questions
         .Where(q => q.SessionType == "urge"
                  && q.Active
                  && q.Phase == "mvp"
                  && q.PatternTags.Contains(haltRoot))
         .ToListAsync(ct);
}

public class AppSettingRepository : IAppSettingRepository
{
    private readonly AppDbContext _db;

    public AppSettingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetValueAsync(
        string key,
        CancellationToken ct = default)
    {
        var setting = await _db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting?.Value;
    }

    public async Task<Dictionary<string, string>> GetAllAsync(
        CancellationToken ct = default)
        => await _db.AppSettings
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
}

public class TapOptionRepository : ITapOptionRepository
{
    private readonly AppDbContext _db;

    public TapOptionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TapOption>> GetAllActiveAsync(CancellationToken ct = default)
      => await _db.TapOptions
          .Where(t => t.Active)
          .OrderBy(t => t.QuestionId)
          .ThenBy(t => t.Order)
          .ToListAsync(ct);

    public async Task<List<TapOption>> GetByQuestionIdAsync(
        string questionId,
        CancellationToken ct = default)
        => await _db.TapOptions
            .Where(t => t.QuestionId == questionId && t.Active)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);

    public async Task<List<TapOption>> GetByQuestionIdsAsync(
        IEnumerable<string> questionIds,
        CancellationToken ct = default)
        => await _db.TapOptions
            .Where(t => questionIds.Contains(t.QuestionId) && t.Active)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);

 
}