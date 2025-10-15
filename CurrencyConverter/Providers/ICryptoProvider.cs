namespace CurrencyConverter.Providers
{
    public interface ICryptoProvider
    {
        Task<decimal?> GetRateAsync(string from, string to);
    }
}
