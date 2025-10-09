public interface ICurrencyConverterService
{
    Task<CurrencyConversionResponse> ConvertAsync(CurrencyConversionRequest request);
}