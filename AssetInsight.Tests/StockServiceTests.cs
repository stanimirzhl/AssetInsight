using AssetInsight.Core.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class StockServiceTests
	{
		private Mock<IHttpClientFactory> _httpClientFactoryMock;
		private Mock<IConfiguration> _configMock;
		private Mock<ILogger<StockService>> _loggerMock;
		private Mock<HttpMessageHandler> _httpMessageHandlerMock;

		[SetUp]
		public void SetUp()
		{
			_httpClientFactoryMock = new Mock<IHttpClientFactory>();
			_configMock = new Mock<IConfiguration>();
			_loggerMock = new Mock<ILogger<StockService>>();
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();

			_configMock.Setup(c => c["Finnhub:ApiKey"]).Returns("fake_api_key");
		}

		private StockService CreateService()
		{
			var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
			_httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

			return new StockService(_httpClientFactoryMock.Object, _configMock.Object, _loggerMock.Object);
		}

		private void SetupHttpResponse(HttpStatusCode statusCode, string jsonContent)
		{
			_httpMessageHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>()
				)
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = statusCode,
					Content = new StringContent(jsonContent)
				});
		}

		[Test]
		public async Task GetStockHistoryAsync_ValidResponse_ShouldReturnParsedData()
		{
			var jsonResponse = @"
            {
              ""chart"": {
                ""result"": [
                  {
                    ""timestamp"": [1600000000, 1600086400],
                    ""indicators"": {
                      ""quote"": [
                        {
                          ""open"": [150.0, 155.0],
                          ""high"": [152.0, 158.0],
                          ""low"": [149.0, 154.0],
                          ""close"": [151.0, 157.0],
                          ""volume"": [10000, 20000]
                        }
                      ]
                    }
                  }
                ]
              }
            }";

			SetupHttpResponse(HttpStatusCode.OK, jsonResponse);
			var service = CreateService();

			var result = await service.GetStockHistoryAsync("AAPL", "1mo");

			Assert.That(result.Symbol, Is.EqualTo("AAPL"));
			Assert.That(result.CurrentRange, Is.EqualTo("1mo"));
			Assert.That(result.History.Count, Is.EqualTo(2));

			Assert.That(result.History[0].Open, Is.EqualTo(150.0m));
			Assert.That(result.History[0].ClosePrice, Is.EqualTo(151.0m));
			Assert.That(result.History[0].Volume, Is.EqualTo(10000));
		}

		[TestCase("1d", "1d")]
		[TestCase("5d", "1d")]
		[TestCase("1mo", "1d")]
		[TestCase("3mo", "1d")]
		[TestCase("6mo", "1d")]
		[TestCase("1y", "1wk")]
		[TestCase("2y", "1wk")]
		[TestCase("5y", "1mo")]
		[TestCase("10y", "1mo")]
		[TestCase("max", "1mo")]
		[TestCase("random_garbage", "1d")]
		public async Task GetStockHistoryAsync_VariousRanges_ShouldUseCorrectIntervalInUrl(string range, string expectedInterval)
		{
			var symbol = "AAPL";
			var expectedUrl = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?range={range.ToLower()}&interval={expectedInterval}";

			var validJsonResponse = @"
			{
			  ""chart"": {
			    ""result"": [
			      {
			        ""timestamp"": [1600000000],
			        ""indicators"": { ""quote"": [ { ""open"": [150.0], ""high"": [152.0], ""low"": [149.0], ""close"": [151.0], ""volume"": [10000] } ] }
			      }
			    ]
			  }
			}";

			SetupHttpResponse(HttpStatusCode.OK, validJsonResponse);
			var service = CreateService();

			await service.GetStockHistoryAsync(symbol, range);

			_httpMessageHandlerMock.Protected().Verify(
				"SendAsync",
				Times.Once(),
				ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == expectedUrl),
				ItExpr.IsAny<CancellationToken>()
			);
		}

		[Test]
		public async Task GetStockHistoryAsync_WithNullClosePrice_ShouldSkipDataPoint()
		{
			var jsonResponse = @"
            {
              ""chart"": {
                ""result"": [
                  {
                    ""timestamp"": [1600000000, 1600086400],
                    ""indicators"": {
                      ""quote"": [
                        {
                          ""open"": [150.0, null],
                          ""high"": [152.0, null],
                          ""low"": [149.0, null],
                          ""close"": [151.0, null],
                          ""volume"": [10000, null]
                        }
                      ]
                    }
                  }
                ]
              }
            }";

			SetupHttpResponse(HttpStatusCode.OK, jsonResponse);
			var service = CreateService();

			var result = await service.GetStockHistoryAsync("AAPL", "1mo");

			Assert.That(result.History.Count, Is.EqualTo(1), "Should skip the data point with a null close price.");
			Assert.That(result.History[0].ClosePrice, Is.EqualTo(151.0m));
		}

		[Test]
		public void GetStockHistoryAsync_ApiReturnsError_ShouldThrowException()
		{
			SetupHttpResponse(HttpStatusCode.NotFound, "Not Found");
			var service = CreateService();

			var ex = Assert.ThrowsAsync<Exception>(async () => await service.GetStockHistoryAsync("INVALID_TICKER"));
			Assert.That(ex.Message, Does.Contain("Invalid ticker or data unavailable"));
			Assert.That(ex.InnerException.Message, Does.Contain("returned NotFound"));
		}

		[Test]
		public void GetStockHistoryAsync_InvalidJsonFormat_ShouldThrowException()
		{
			SetupHttpResponse(HttpStatusCode.OK, "{ \"broken\": \"json\" }");
			var service = CreateService();

			var ex = Assert.ThrowsAsync<Exception>(async () => await service.GetStockHistoryAsync("AAPL"));
			Assert.That(ex.Message, Does.Contain("Invalid ticker or data unavailable"));
		}

		[Test]
		public async Task GetCompanyNewsAsync_ValidResponse_ShouldReturnParsedNews()
		{
			var jsonResponse = @"
            [
              {
                ""id"": 12345,
                ""category"": ""technology"",
                ""related"": ""AAPL"",
                ""headline"": ""Apple releases new product"",
                ""summary"": ""A brief summary of the news."",
                ""url"": ""https://news.com/123"",
                ""image"": ""https://news.com/img.jpg"",
                ""source"": ""TechNews"",
                ""datetime"": 1600000000
              }
            ]";

			SetupHttpResponse(HttpStatusCode.OK, jsonResponse);
			var service = CreateService();

			var result = await service.GetCompanyNewsAsync("AAPL");

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0].Id, Is.EqualTo(12345));
			Assert.That(result[0].Headline, Is.EqualTo("Apple releases new product"));
			Assert.That(result[0].Ticker, Is.EqualTo("AAPL"));
		}

		[Test]
		public async Task GetCompanyNewsAsync_MoreThan10Items_ShouldLimitTo10()
		{
			var singleItem = @"{""id"": 1, ""category"": """", ""related"": """", ""headline"": """", ""summary"": """", ""url"": """", ""image"": """", ""source"": """", ""datetime"": 1600000000}";
			var jsonResponse = $"[{string.Join(",", Enumerable.Repeat(singleItem, 15))}]";

			SetupHttpResponse(HttpStatusCode.OK, jsonResponse);
			var service = CreateService();

			var result = await service.GetCompanyNewsAsync("AAPL");

			Assert.That(result.Count, Is.EqualTo(10), "Should only take the first 10 news items.");
		}

		[Test]
		public async Task GetCompanyNewsAsync_MissingApiKey_ShouldReturnEmptyList_AndLogWarning()
		{
			_configMock.Setup(c => c["Finnhub:ApiKey"]).Returns(string.Empty);
			var service = CreateService();

			var result = await service.GetCompanyNewsAsync("AAPL");

			Assert.That(result, Is.Empty);

			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping company news fetch")),
					It.IsAny<Exception>(),
					It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
				Times.Once);
		}

		[Test]
		public async Task GetCompanyNewsAsync_ApiReturnsError_ShouldReturnEmptyList_WithoutThrowing()
		{
			SetupHttpResponse(HttpStatusCode.InternalServerError, "Error");
			var service = CreateService();

			var result = await service.GetCompanyNewsAsync("AAPL");

			Assert.That(result, Is.Empty);
		}

		[Test]
		public async Task GetCompanyNewsAsync_InvalidJsonFormat_ShouldReturnEmptyList_WithoutThrowing()
		{
			SetupHttpResponse(HttpStatusCode.OK, "This is not JSON");
			var service = CreateService();


			var result = await service.GetCompanyNewsAsync("AAPL");

			Assert.That(result, Is.Empty);
		}
	}
}