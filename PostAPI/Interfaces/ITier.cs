using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface ITier
    {
        Task<List<ReaderTier>> GetReaderTiers();
    }
}
