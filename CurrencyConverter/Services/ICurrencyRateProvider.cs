public interface ICurrencyRateProvider
{
    Task<decimal> GetRateAsync(string sourceCurrency, string destinationCurrency);
}