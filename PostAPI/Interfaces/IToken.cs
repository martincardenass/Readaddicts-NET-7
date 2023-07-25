namespace PostAPI.Interfaces
{
    public interface IToken
    {
        Task<int> ExtractIdFromToken();
    }
}
