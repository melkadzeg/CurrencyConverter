public class CurrencyConverterService : ICurrencyConverterService
{
    private readonly ILogger<CurrencyConverterService> _logger;
    private readonly ICurrencyRateProvider _rateProvider;

    public CurrencyConverterService(ILogger<CurrencyConverterService> logger, ICurrencyRateProvider rateProvider)
    {
        _logger = logger;
        _rateProvider = rateProvider;
    }

    public async Task<CurrencyConversionResponse> ConvertAsync(CurrencyConversionRequest request)
    {
        _logger.LogInformation("Converting {SourceAmount} {SourceCurrency} to {DestinationCurrency}", 
            request.SourceAmount, request.SourceCurrency, request.DestinationCurrency);

        if (request.SourceCurrency == request.DestinationCurrency)
        {
            return new CurrencyConversionResponse
            {
                SourceCurrency = request.SourceCurrency,
                DestinationCurrency = request.DestinationCurrency,
                SourceAmount = request.SourceAmount,
                DestinationAmount = request.SourceAmount,
                Rate = 1m
            };
        }

        var rate = await _rateProvider.GetRateAsync(request.SourceCurrency, request.DestinationCurrency);
        var destinationAmount = request.SourceAmount * rate;

        return new CurrencyConversionResponse
        {
            SourceCurrency = request.SourceCurrency,
            DestinationCurrency = request.DestinationCurrency,
            SourceAmount = request.SourceAmount,
            DestinationAmount = destinationAmount,
            Rate = rate
        };
    }
}