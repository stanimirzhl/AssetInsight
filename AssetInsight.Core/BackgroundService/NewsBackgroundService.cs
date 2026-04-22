using AssetInsight.Core.Caches;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using AssetInsight.Models.ApiNews;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AssetInsight.Services
{
	public class NewsBackgroundService : BackgroundService
	{
		private readonly NewsCacheService _newsCache;
		private readonly ILogger<NewsBackgroundService> _logger;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly HttpClient _httpClient;
		private readonly string? _finnhubApiKey;

		private readonly TimeSpan _updateInterval = TimeSpan.FromHours(2);
		private readonly string[] _categories = { "general", "forex", "crypto", "merger" };

		private DateTime _lastNotificationTime = DateTime.UtcNow.AddHours(-2);

		public NewsBackgroundService(
			NewsCacheService newsCache,
			ILogger<NewsBackgroundService> logger,
			IConfiguration configuration,
			IServiceScopeFactory scopeFactory,
			IHttpClientFactory httpClientFactory)
		{
			_newsCache = newsCache;
			_logger = logger;
			_httpClient = httpClientFactory.CreateClient();
			_scopeFactory = scopeFactory;

			_finnhubApiKey = configuration["Finnhub:ApiKey"];
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			if (string.IsNullOrWhiteSpace(_finnhubApiKey))
			{
				_logger.LogWarning("NewsBackgroundService is disabled because the Finnhub API Key is missing from configuration.");
				return;
			}

			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("Fetching categorized market news from Finnhub...");
				var allFetchedNews = new List<NewsItem>();
				var currentRunTime = DateTime.UtcNow;

				foreach (var category in _categories)
				{
					try
					{
						var categoryNews = await GetRealNewsFromFinnhubAsync(category);
						allFetchedNews.AddRange(categoryNews);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, $"Failed to fetch news for category: {category}");
					}
				}

				if (allFetchedNews.Count > 0)
				{
					allFetchedNews.Sort((x, y) => y.PublishedAt.CompareTo(x.PublishedAt));
					_newsCache.UpdateNews(allFetchedNews);

					await NotifyWatchListUsers(allFetchedNews);
					_logger.LogInformation($"Cache updated with {allFetchedNews.Count} total items.");
				}

				_lastNotificationTime = currentRunTime;

				try
				{
					await Task.Delay(_updateInterval, stoppingToken);
				}
				catch (TaskCanceledException)
				{
					break;
				}
			}
		}

		private async Task<List<NewsItem>> GetRealNewsFromFinnhubAsync(string category)
		{
			var newsList = new List<NewsItem>();
			string url = $"https://finnhub.io/api/v1/news?category={category}&token={_finnhubApiKey}";

			try
			{
				var response = await _httpClient.GetAsync(url);
				response.EnsureSuccessStatusCode();

				var jsonResponse = await response.Content.ReadAsStringAsync();
				using var document = JsonDocument.Parse(jsonResponse);

				foreach (var element in document.RootElement.EnumerateArray())
				{
					string headline = element.GetProperty("headline").GetString() ?? "Market Update";

					newsList.Add(new NewsItem
					{
						Id = element.GetProperty("id").GetInt64(),
						Category = element.GetProperty("category").GetString(),
						Ticker = element.GetProperty("related").GetString() != "" ? element.GetProperty("related").GetString() : "MKT",
						Headline = headline,
						Summary = element.GetProperty("summary").GetString(),
						Url = element.GetProperty("url").GetString(),
						ImageUrl = element.GetProperty("image").GetString(),
						Source = element.GetProperty("source").GetString(),
						PublishedAt = DateTimeOffset.FromUnixTimeSeconds(element.GetProperty("datetime").GetInt64()).UtcDateTime,
						IsPositive = DetermineSentiment(headline)
					});
				}
			}
			catch (HttpRequestException httpEx)
			{
				_logger.LogWarning(httpEx, $"HTTP request failed for category: {category}. Status Code: {httpEx.StatusCode}");
			}
			catch (JsonException jsonEx)
			{
				_logger.LogWarning(jsonEx, $"Failed to parse JSON for category: {category}.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"An unexpected error occurred while fetching news for category: {category}.");
			}

			return newsList;
		}

		private async Task NotifyWatchListUsers(List<NewsItem> fetchedNews)
		{
			var newTickerNews = fetchedNews
				.Where(n => n.PublishedAt > _lastNotificationTime &&
							!string.IsNullOrEmpty(n.Ticker) &&
							n.Ticker != "MKT")
				.ToList();

			if (!newTickerNews.Any()) return;

			using (var scope = _scopeFactory.CreateScope())
			{
				var watchListRepo = scope.ServiceProvider.GetRequiredService<IRepository<WatchList>>();
				var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

				var uniqueTickers = newTickerNews.Select(n => n.Ticker).Distinct().ToList();

				var interestedWatchers = watchListRepo.AllAsReadOnly()
					.Where(w => uniqueTickers.Contains(w.Symbol))
					.ToList();

				foreach (var watcher in interestedWatchers)
				{
					var article = newTickerNews.First(n => n.Ticker == watcher.Symbol);

					string message = $"New article for {watcher.Symbol}: {article.Headline}";
					string url = $"{article.Url}";

					await notificationService.CreateNotification(watcher.UserId, message, url);
				}
			}
		}

		private bool DetermineSentiment(string text)
		{
			var lowerText = text.ToLower();
			string[] positiveWords = { "surge", "jump", "soar", "gain", "up", "beat", "rally" };
			string[] negativeWords = { "plunge", "drop", "fall", "lose", "down", "miss", "crash" };
			int score = 0;
			foreach (var word in positiveWords) { if (lowerText.Contains(word)) score++; }
			foreach (var word in negativeWords) { if (lowerText.Contains(word)) score--; }
			return score >= 0;
		}
	}
}