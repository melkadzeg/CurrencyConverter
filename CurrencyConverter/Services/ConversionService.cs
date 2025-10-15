using CurrencyConverter.Providers;

namespace CurrencyConverter.Services
{
    public class ConversionService
    {
        private readonly ILogger<ConversionService> _logger;
        private readonly IFiatProvider _nbu;
        private readonly ICryptoProvider _crypto;

        public ConversionService(
            ILogger<ConversionService> logger,
            IFiatProvider nbu,
            ICryptoProvider crypto)
        {
            _logger = logger;
            _nbu = nbu;
            _crypto = crypto;
        }

        public async Task<CurrencyConversionResponse> ConvertAsync(CurrencyConversionRequest request)
        {
            var from = request.SourceCurrency.ToUpperInvariant();
            var to = request.DestinationCurrency.ToUpperInvariant();
            var amount = request.SourceAmount;

            _logger.LogInformation("Converting {Amount} {From} → {To}", amount, from, to);

            var rate = await ResolveRateAsync(from, to);

            if (rate == null)
            {
                _logger.LogWarning("No exchange rate found for {From}->{To}", from, to);
                throw new InvalidOperationException($"Rate not available for {from}->{to}");
            }

            var result = new CurrencyConversionResponse
            {
                SourceCurrency = from,
                DestinationCurrency = to,
                SourceAmount = amount,
                Rate = rate.Value,
                DestinationAmount = amount * rate.Value
            };

            _logger.LogInformation("{From}->{To} rate {Rate} result {Dest}",
                from, to, result.Rate, result.DestinationAmount);

            return result;
        }

        private async Task<decimal?> ResolveRateAsync(string from, string to)
        {
            if (from == to)
                return 1m;

            // 1. Try NBU cross (UAH base)
            var nbuFrom = await _nbu.GetRateAsync(from);
            var nbuTo = await _nbu.GetRateAsync(to);
            if (nbuFrom != null && nbuTo != null)
                return nbuFrom / nbuTo;

            // 2. Try direct Kraken
            var direct = await _crypto.GetRateAsync(from, to);
            if (direct != null)
                return direct;

            // 3. Try Kraken with USD base
            var fromUsd = await _crypto.GetRateAsync(from, "USD");
            var toUsd = await _crypto.GetRateAsync(to, "USD");
            if (fromUsd != null && toUsd != null)
                return fromUsd / toUsd;

            // 4. Mix Kraken and NBU
            if (fromUsd != null && nbuTo != null)
            {
                var usdToUah = await _nbu.GetRateAsync("USD");
                return (fromUsd * usdToUah) / nbuTo;
            }

            if (toUsd != null && nbuFrom != null)
            {
                var usdToUah = await _nbu.GetRateAsync("USD");
                return nbuFrom / (toUsd * usdToUah);
            }

            return null;
        }
    }
}
