namespace CurrencyConverter.Providers
{
    public interface IFiatProvider
    {
        Task<decimal?> GetRateAsync(string currency);
    }
}
