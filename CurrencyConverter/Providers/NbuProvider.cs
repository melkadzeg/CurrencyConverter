using System.Text.Json.Serialization;

namespace CurrencyConverter.Providers
{
    public class NbuProvider : IFiatProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public NbuProvider(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<decimal?> GetRateAsync(string currency)
        {
            if (currency.Equals("UAH", StringComparison.OrdinalIgnoreCase))
                return 1m;


            var url = string.Format(_config["FiatApiUrl"], currency);

            try
            {
                var data = await _http.GetFromJsonAsync<List<NbuResponse>>(url);
                return data?.FirstOrDefault()?.Rate;
            }
            catch
            {
                return null;
            }
        }

        private class NbuResponse
        {
            [JsonPropertyName("rate")]
            public decimal Rate { get; set; }
        }
    }
}
