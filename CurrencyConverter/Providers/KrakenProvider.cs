using System.Text.Json.Serialization;

namespace CurrencyConverter.Providers
{
    public class KrakenProvider : ICryptoProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public KrakenProvider(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<decimal?> GetRateAsync(string from, string to)
        {
            var pair = $"{from}{to}";

            var url = string.Format(_config["CryptoApiUrl"], pair);

            KrakenTickerResponse? resp;

            try
            {
                resp = await _http.GetFromJsonAsync<KrakenTickerResponse>(url);
            }
            catch
            {
                return null;
            }

            if (resp?.Result == null || resp.Result.Count == 0)
                return null;

            var ticker = resp.Result.Values.FirstOrDefault();
            if (ticker?.C == null || ticker.C.Length == 0)
                return null;

            return decimal.TryParse(ticker.C[0], out var price) ? price : null;
        }

        private class KrakenTickerResponse
        {
            [JsonPropertyName("result")]
            public Dictionary<string, KrakenTickerItem>? Result { get; set; }
        }

        private class KrakenTickerItem
        {
            [JsonPropertyName("c")]
            public string[]? C { get; set; }
        }
    }
}
