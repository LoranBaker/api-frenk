using Frank.Domain.Entities;
using Frank.Infrastructure.Data;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FrankApi.Infrastructure.Repositories
{
    public class MiniMirrorRepository : IMiniMirrorRepository
    {
        private readonly AppDbContext _db;

        public MiniMirrorRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> WasTriggeredAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.MiniMirrorEvents
            .AnyAsync(m => m.UserId == userId, ct);

        public async Task AddAsync(
            MiniMirrorEvent evt,
            CancellationToken ct = default)
            => await _db.MiniMirrorEvents.AddAsync(evt, ct);

        public async Task<MiniMirrorEvent?> GetByUserIdAsync(
            Guid userId,
            CancellationToken ct = default)
            => await _db.MiniMirrorEvents
                .FirstOrDefaultAsync(m => m.UserId == userId, ct);

        public async Task MarkOpenedAsync(
            Guid eventId,
            CancellationToken ct = default)
        {
            var evt = await _db.MiniMirrorEvents
                .FirstOrDefaultAsync(m => m.Id == eventId, ct);
            if (evt is not null)
            {
                evt.Opened = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
            => await _db.SaveChangesAsync(ct);
    }
}
