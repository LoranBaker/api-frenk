using Frank.Domain.Entities;
using Frank.Infrastructure.Data;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frank.Infrastructure.Repositories;

public class AnswerRepository : IAnswerRepository
{
    private readonly AppDbContext _db;

    public AnswerRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddRangeAsync(
        IEnumerable<Answer> answers,
        CancellationToken ct = default)
    {
        await _db.Answers.AddRangeAsync(answers, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Answer>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
        => await _db.Answers
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(ct);

    public async Task<List<Answer>> GetByUserAndQuestionAsync(
        Guid userId,
        string questionId,
        int limitDays = 30,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-limitDays);
        return await _db.Answers
            .Where(a => a.UserId == userId
                     && a.QuestionId == questionId
                     && a.Timestamp >= cutoff)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetPreUrgeContextsAsync(
        Guid userId,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken ct = default)
    {
        var startDt = DateTime.SpecifyKind(weekStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDt = DateTime.SpecifyKind(weekEnd.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

        return await _db.Answers
            .Where(a => a.UserId == userId
                     && a.QuestionId == "U01"
                     && a.SessionType == "urge"
                     && a.Timestamp >= startDt
                     && a.Timestamp <= endDt
                     && (a.TapValue != null || a.AnswerText != null))
            .OrderBy(a => a.Timestamp)
            .Select(a => a.AnswerText ?? a.TapValue!)
            .ToListAsync(ct);
    }

    public async Task DeleteAllByUserAsync(
    Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.Answers
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);
        _db.Answers.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }
}

public class ChatSessionRepository : BaseRepository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(AppDbContext db) : base(db) { }

    public async Task<ChatSession?> GetTodayAsync(
        Guid userId,
        string sessionType,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.UserId == userId
                                   && s.SessionType == sessionType
                                   && s.Date == today, ct);
    }

    public async Task<ChatSession> CreateAsync(
        Guid userId,
        string sessionType,
        DateOnly date,
        CancellationToken ct = default)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionType = sessionType,
            Date = date,
            StartedAt = DateTime.UtcNow,
            Completed = false
        };

        await _db.ChatSessions.AddAsync(session, ct);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task CompleteAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await _db.ChatSessions
            .FindAsync(new object[] { sessionId }, ct);

        if (session is null) return;

        session.Completed = true;
        session.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAllByUserAsync(
    Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.ChatSessions
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);
        _db.ChatSessions.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }
}

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _db;

    public ConversationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(
        ConversationHistory message,
        CancellationToken ct = default)
    {
        await _db.ConversationHistories.AddAsync(message, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(
        IEnumerable<ConversationHistory> messages,
        CancellationToken ct = default)
    {
        await _db.ConversationHistories.AddRangeAsync(messages, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ConversationHistory>> GetByDateAsync(
        Guid userId,
        DateOnly date,
        CancellationToken ct = default)
        => await _db.ConversationHistories
            .Where(m => m.UserId == userId && m.Date == date)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(ct);

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.ConversationHistories
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);
        _db.ConversationHistories.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ConversationHistory>> GetAllByUserAsync(
    Guid userId, CancellationToken ct = default)
    => await _db.ConversationHistories
        .Where(m => m.UserId == userId)
        .OrderBy(m => m.Timestamp)
        .ToListAsync(ct);
}