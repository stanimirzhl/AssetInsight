using AssetInsight.Core.Caches;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using AssetInsight.Models.ApiNews;
using AssetInsight.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AssetInsight.Tests.Services
{
	[TestFixture]
	public class NewsBackgroundServiceTests
	{
		private NewsCacheService _newsCache;
		private Mock<ILogger<NewsBackgroundService>> _loggerMock;
		private Mock<IConfiguration> _configMock;
		private Mock<IServiceScopeFactory> _scopeFactoryMock;
		private Mock<IHttpClientFactory> _httpClientFactoryMock;
		private Mock<HttpMessageHandler> _httpMessageHandlerMock;

		private Mock<IRepository<WatchList>> _watchListRepoMock;
		private Mock<INotificationService> _notificationServiceMock;

		[SetUp]
		public void SetUp()
		{
			_newsCache = new NewsCacheService();

			_loggerMock = new Mock<ILogger<NewsBackgroundService>>();
			_configMock = new Mock<IConfiguration>();
			_scopeFactoryMock = new Mock<IServiceScopeFactory>();
			_httpClientFactoryMock = new Mock<IHttpClientFactory>();
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();

			_watchListRepoMock = new Mock<IRepository<WatchList>>();
			_notificationServiceMock = new Mock<INotificationService>();

			_configMock.Setup(c => c["Finnhub:ApiKey"]).Returns("test_key");

			var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
			_httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

			var scopeMock = new Mock<IServiceScope>();
			var serviceProviderMock = new Mock<IServiceProvider>();

			serviceProviderMock.Setup(sp => sp.GetService(typeof(IRepository<WatchList>)))
				.Returns(_watchListRepoMock.Object);
			serviceProviderMock.Setup(sp => sp.GetService(typeof(INotificationService)))
				.Returns(_notificationServiceMock.Object);

			scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
			_scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);
		}

		private TestableNewsBackgroundService CreateService()
		{
			return new TestableNewsBackgroundService(
				_newsCache,
				_loggerMock.Object,
				_configMock.Object,
				_scopeFactoryMock.Object,
				_httpClientFactoryMock.Object
			);
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
		public async Task ExecuteAsync_MissingApiKey_ShouldExitImmediately()
		{
			_configMock.Setup(c => c["Finnhub:ApiKey"]).Returns(string.Empty);
			var service = CreateService();

			await service.RunExecuteAsync(CancellationToken.None);

			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("disabled because the Finnhub API Key is missing")),
					It.IsAny<Exception>(),
					It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
				Times.Once);

			_httpMessageHandlerMock.Protected().Verify(
				"SendAsync",
				Times.Never(),
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>());
		}

		[Test]
		public async Task ExecuteAsync_ValidRun_ShouldUpdateCache_And_NotifyUsers()
		{
			var recentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var jsonResponse = $@"
			[
			  {{
				""id"": 1,
				""category"": ""general"",
				""related"": ""AAPL"",
				""headline"": ""Apple shares surge dramatically!"",
				""summary"": """",
				""url"": ""http://news.com/1"",
				""image"": """",
				""source"": """",
				""datetime"": {recentUnixTime}
			  }}
			]";

			SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

			var watchList = new List<WatchList>
			{
				new WatchList { Symbol = "AAPL", UserId = "user1" },
				new WatchList { Symbol = "MSFT", UserId = "user2" }
			};
			_watchListRepoMock.Setup(r => r.AllAsReadOnly()).Returns(watchList.AsQueryable().BuildMockDbSet().Object);

			var service = CreateService();

			var cts = new CancellationTokenSource(300);

			await service.RunExecuteAsync(cts.Token);

			var cachedNews = _newsCache.GetLatestNews();
			Assert.That(cachedNews, Is.Not.Empty);
			Assert.That(cachedNews.Any(i => i.Ticker == "AAPL" && i.IsPositive), Is.True);

			_notificationServiceMock.Verify(n => n.CreateNotification(
				"user1",
				It.Is<string>(msg => msg.Contains("Apple shares surge")),
				"http://news.com/1"
			), Times.Once);

			_notificationServiceMock.Verify(n => n.CreateNotification(
				"user2",
				It.IsAny<string>(),
				It.IsAny<string>()
			), Times.Never);
		}

		[Test]
		public async Task ExecuteAsync_HttpError_ShouldNotCrash_And_SkipUpdate()
		{
			SetupHttpResponse(HttpStatusCode.InternalServerError, "");
			var service = CreateService();

			var cts = new CancellationTokenSource(100);

			await service.RunExecuteAsync(cts.Token);

			Assert.That(_newsCache.GetLatestNews(), Is.Empty);
		}

		[Test]
		public async Task ExecuteAsync_JsonParseError_ShouldNotCrash()
		{
			SetupHttpResponse(HttpStatusCode.OK, "BROKEN JSON");
			var service = CreateService();

			var cts = new CancellationTokenSource(100);

			await service.RunExecuteAsync(cts.Token);

			Assert.That(_newsCache.GetLatestNews, Is.Empty);
		}
	}

	public class TestableNewsBackgroundService : NewsBackgroundService
	{
		public TestableNewsBackgroundService(
			NewsCacheService newsCache,
			ILogger<NewsBackgroundService> logger,
			IConfiguration configuration,
			IServiceScopeFactory scopeFactory,
			IHttpClientFactory httpClientFactory)
			: base(newsCache, logger, configuration, scopeFactory, httpClientFactory)
		{
		}

		public Task RunExecuteAsync(CancellationToken token)
		{
			return ExecuteAsync(token);
		}
	}
}