using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using CurrencyConverter.Services;
using CurrencyConverter.Providers;

namespace CurrencyConverter.Tests
{
    public class ConversionServiceTests
    {
        private static IConfiguration BuildConfig(int cacheMinutes = 60)
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("CacheMinutes", cacheMinutes.ToString()) })
                .Build();

        [Fact]
        public async Task ConvertAsync_ReturnsSameCurrency_WhenSourceEqualsDestination()
        {
            var fiatMock = new Mock<IFiatProvider>();
            var cryptoMock = new Mock<ICryptoProvider>();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var config = BuildConfig();

            var service = new ConversionService(NullLogger<ConversionService>.Instance, config, fiatMock.Object, cryptoMock.Object, cache);

            var request = new CurrencyConversionRequest { SourceCurrency = "USD", DestinationCurrency = "usd", SourceAmount = 10m };
            var resp = await service.ConvertAsync(request);

            Assert.Equal("USD", resp.SourceCurrency);
            Assert.Equal("USD", resp.DestinationCurrency);
            Assert.Equal(1m, resp.Rate);
            Assert.Equal(10m, resp.DestinationAmount);
        }

        [Fact]
        public async Task ConvertAsync_UsesFiat_WhenBothFiatAvailable()
        {
            var fiatMock = new Mock<IFiatProvider>();
            fiatMock.Setup(p => p.GetRateAsync("USD")).ReturnsAsync(1m);
            fiatMock.Setup(p => p.GetRateAsync("EUR")).ReturnsAsync(0.5m);

            var cryptoMock = new Mock<ICryptoProvider>();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var config = BuildConfig();

            var service = new ConversionService(NullLogger<ConversionService>.Instance, config, fiatMock.Object, cryptoMock.Object, cache);

            var request = new CurrencyConversionRequest { SourceCurrency = "usd", DestinationCurrency = "eur", SourceAmount = 2m };
            var resp = await service.ConvertAsync(request);

            // rate = fiatFrom / fiatTo = 1 / 0.5 = 2
            Assert.Equal(2m, resp.Rate);
            Assert.Equal(4m, resp.DestinationAmount);
            fiatMock.Verify(p => p.GetRateAsync("USD"), Times.Once);
            fiatMock.Verify(p => p.GetRateAsync("EUR"), Times.Once);
        }

        [Fact]
        public async Task ConvertAsync_UsesCryptoDirect_WhenAvailable()
        {
            var fiatMock = new Mock<IFiatProvider>();
            fiatMock.Setup(p => p.GetRateAsync(It.IsAny<string>())).ReturnsAsync((decimal?)null);

            var cryptoMock = new Mock<ICryptoProvider>();
            cryptoMock.Setup(p => p.GetRateAsync("BTC", "ETH")).ReturnsAsync(3.5m);

            var cache = new MemoryCache(new MemoryCacheOptions());
            var config = BuildConfig();

            var service = new ConversionService(NullLogger<ConversionService>.Instance, config, fiatMock.Object, cryptoMock.Object, cache);

            var request = new CurrencyConversionRequest { SourceCurrency = "btc", DestinationCurrency = "eth", SourceAmount = 1m };
            var resp = await service.ConvertAsync(request);

            Assert.Equal(3.5m, resp.Rate);
            Assert.Equal(3.5m, resp.DestinationAmount);
            cryptoMock.Verify(p => p.GetRateAsync("BTC", "ETH"), Times.Once);
        }

        [Fact]
        public async Task ConvertAsync_UsesCache_OnSubsequentCalls()
        {
            var fiatMock = new Mock<IFiatProvider>();
            fiatMock.Setup(p => p.GetRateAsync("USD")).ReturnsAsync(1m);
            fiatMock.Setup(p => p.GetRateAsync("EUR")).ReturnsAsync(0.5m);

            var cryptoMock = new Mock<ICryptoProvider>();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var config = BuildConfig();

            var service = new ConversionService(NullLogger<ConversionService>.Instance, config, fiatMock.Object, cryptoMock.Object, cache);

            var request = new CurrencyConversionRequest { SourceCurrency = "USD", DestinationCurrency = "EUR", SourceAmount = 1m };

            await service.ConvertAsync(request); // cold -> providers called
            var invocationsAfterFirst = fiatMock.Invocations.Count + cryptoMock.Invocations.Count;

            await service.ConvertAsync(request); // should hit cache -> no new provider calls
            var invocationsAfterSecond = fiatMock.Invocations.Count + cryptoMock.Invocations.Count;

            Assert.Equal(invocationsAfterFirst, invocationsAfterSecond);
        }

        [Fact]
        public async Task ConvertAsync_Throws_WhenNoRateAvailable()
        {
            var fiatMock = new Mock<IFiatProvider>();
            fiatMock.Setup(p => p.GetRateAsync(It.IsAny<string>())).ReturnsAsync((decimal?)null);

            var cryptoMock = new Mock<ICryptoProvider>();
            cryptoMock.Setup(p => p.GetRateAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((decimal?)null);

            var cache = new MemoryCache(new MemoryCacheOptions());
            var config = BuildConfig();

            var service = new ConversionService(NullLogger<ConversionService>.Instance, config, fiatMock.Object, cryptoMock.Object, cache);

            var request = new CurrencyConversionRequest { SourceCurrency = "FOO", DestinationCurrency = "BAR", SourceAmount = 1m };

            await Assert.ThrowsAsync<System.Exception>(() => service.ConvertAsync(request));
        }
    }
}
