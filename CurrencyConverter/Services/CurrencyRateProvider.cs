using Microsoft.Extensions.Caching.Memory;

public class CurrencyRateProvider : ICurrencyRateProvider
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyRateProvider> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public CurrencyRateProvider(IMemoryCache cache, ILogger<CurrencyRateProvider> logger, IConfiguration config, HttpClient httpClient)
    {
        _cache = cache;
        _logger = logger;
        _config = config;
        _httpClient = httpClient;
    }

    public async Task<decimal> GetRateAsync(string sourceCurrency, string destinationCurrency)
    {
        var cacheKey = $"{sourceCurrency}_{destinationCurrency}";
        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
        {
            _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
            return cachedRate;
        }

        // TODO: Implement NBU/Kraken API calls and cross conversion logic here.
        decimal rate = await FetchRateFromApisAsync(sourceCurrency, destinationCurrency);

        var minutes = long.TryParse(_config["CurrencyApi:CacheMinutes"], out var m) ? m : 10;
        _cache.Set(cacheKey, rate, TimeSpan.FromMinutes(minutes));

        return rate;
    }

    private async Task<decimal> FetchRateFromApisAsync(string sourceCurrency, string destinationCurrency)
    {
        // Self conversion
        if (sourceCurrency.Equals(destinationCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        // Use UAH as base currency for cross conversion
        const string baseCurrency = "UAH";

        // Helper to fetch rate to UAH
        async Task<decimal> GetRateToUAH(string currency)
        {
            if (currency.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase))
                return 1m;

            var url = _config["CurrencyApi:NbuUrl"] ?? throw new Exception("Rate API url not found");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var rate = root[0].GetProperty("rate").GetDecimal();
                return rate;
            }
            throw new InvalidOperationException($"Rate for {currency} not found.");
        }

        // Get rates to UAH
        var sourceToUAH = await GetRateToUAH(sourceCurrency);
        var destToUAH = await GetRateToUAH(destinationCurrency);

        // Calculate cross rate
        var rate = destToUAH / sourceToUAH;
        return rate;
    }
}