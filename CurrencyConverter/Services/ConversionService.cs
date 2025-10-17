using CurrencyConverter.Providers;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Services
{
    public class ConversionService
    {
        private readonly ILogger<ConversionService> _logger;
        private readonly IFiatProvider _fiatProvider;
        private readonly ICryptoProvider _cryptProvider;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public ConversionService(
            ILogger<ConversionService> logger,
            IConfiguration configuration,
            IFiatProvider fiatProvider,
            ICryptoProvider cryptoProvider,
            IMemoryCache cache)
        {
            _logger = logger;
            _fiatProvider = fiatProvider;
            _cryptProvider = cryptoProvider;
            _cache = cache;
            _config = configuration;
        }

        public async Task<CurrencyConversionResponse> ConvertAsync(CurrencyConversionRequest request)
        {
            var from = request.SourceCurrency.ToUpperInvariant();
            var to = request.DestinationCurrency.ToUpperInvariant();
            var amount = request.SourceAmount;

            _logger.LogInformation($"Converting {amount} {from} → {to}");

            var pairKey = $"{from}_{to}";

            if (!_cache.TryGetValue(pairKey, out decimal rate))
            {
                var resolved = await ResolveRateAsync(from, to);
                if (resolved == null)
                {
                    throw new Exception($"Rate not available for {from}->{to}");
                }

                rate = resolved.Value;

                // Cache for x minutes
                _cache.Set(pairKey, rate, TimeSpan.FromMinutes(_config.GetValue<int>("CacheMinutes")));
            }

            return new CurrencyConversionResponse
            {
                SourceCurrency = from,
                DestinationCurrency = to,
                SourceAmount = amount,
                Rate = rate,
                DestinationAmount = amount * rate
            };
        }

        private async Task<decimal?> ResolveRateAsync(string from, string to)
        {
            if (from == to)
                return 1m;

            // 1. Try Fiat 
            var fiatFrom = await _fiatProvider.GetRateAsync(from);
            var fiatTo = await _fiatProvider.GetRateAsync(to);
            if (fiatFrom != null && fiatTo != null)
                return fiatFrom / fiatTo;

            // 2. Try direct Crypto
            var direct = await _cryptProvider.GetRateAsync(from, to);
            if (direct != null)
                return direct;

            // 3. Try Crpyo with USD
            var cryptoFrom = await _cryptProvider.GetRateAsync(from, "USD");
            var cryptoTo = await _cryptProvider.GetRateAsync(to, "USD");
            if (cryptoFrom != null && cryptoTo != null)
                return cryptoFrom / cryptoTo;

            // 4. Mix Fiat and Crypto
            if (cryptoFrom != null && fiatTo != null)
            {
                var usdToUah = await _fiatProvider.GetRateAsync("USD");
                return (cryptoFrom * usdToUah) / fiatTo;
            }

            if (cryptoTo != null && fiatFrom != null)
            {
                var usdToUah = await _fiatProvider.GetRateAsync("USD");
                return fiatFrom / (cryptoTo * usdToUah);
            }

            return null;
        }
    }
}
