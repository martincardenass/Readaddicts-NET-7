using Microsoft.EntityFrameworkCore;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class TierRepository : ITier
    {
        private readonly AppDbContext _context;

        public TierRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<ReaderTier>> GetReaderTiers()
        {
            // * Store tiers ids that users have
            var users = await _context.Users
                .Where(t => t.Tier_Id != null)
                .Select(t => t.Tier_Id)
                .ToListAsync();

            // * Return a list of tiers, only tiers that users have (dont return unused tiers)
            var tiers = await _context.ReaderTiers
                .Where(t => users.Contains(t.Tier_Id))
                .ToListAsync();

            return tiers;
        }
    }
}
