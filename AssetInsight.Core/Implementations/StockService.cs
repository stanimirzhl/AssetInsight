using AssetInsight.Core.DTOs.Stock;
using AssetInsight.Core.Interfaces;
using AssetInsight.Models.ApiNews;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class StockService : IStockService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly string _finnhubApiKey;
		private readonly ILogger<StockService> _logger;

		public StockService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<StockService> logger)
		{
			_httpClientFactory = httpClientFactory;

			_finnhubApiKey = configuration["Finnhub:ApiKey"];
			_logger = logger;
		}

		public async Task<StockHistoryDtoModel> GetStockHistoryAsync(string symbol, string range = "1mo")
		{
			symbol = symbol.ToUpper();

			string interval = range.ToLower() switch
			{
				"1d" or "5d" or "1mo" => "1d",
				"3mo" or "6mo" => "1d",
				"1y" or "2y" => "1wk",
				"5y" or "10y" or "max" => "1mo",
				_ => "1d"
			};

			var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?range={range}&interval={interval}";
			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

			var model = new StockHistoryDtoModel { Symbol = symbol, CurrentRange = range };

			try
			{
				var response = await client.GetAsync(url);

				if (!response.IsSuccessStatusCode)
					throw new Exception($"Yahoo API returned {response.StatusCode} for {symbol}");

				var jsonStr = await response.Content.ReadAsStringAsync();
				using var json = JsonDocument.Parse(jsonStr);

				var result = json.RootElement.GetProperty("chart").GetProperty("result")[0];
				var timestamps = result.GetProperty("timestamp").EnumerateArray().ToList();
				var quote = result.GetProperty("indicators").GetProperty("quote")[0];

				var opens = quote.GetProperty("open").EnumerateArray().ToList();
				var highs = quote.GetProperty("high").EnumerateArray().ToList();
				var lows = quote.GetProperty("low").EnumerateArray().ToList();
				var closes = quote.GetProperty("close").EnumerateArray().ToList();
				var volumes = quote.GetProperty("volume").EnumerateArray().ToList();

				for (int i = 0; i < timestamps.Count; i++)
				{
					if (closes[i].ValueKind == JsonValueKind.Null) continue;

					model.History.Add(new ChartDataPoint
					{
						Date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime,
						Open = opens[i].GetDecimal(),
						High = highs[i].GetDecimal(),
						Low = lows[i].GetDecimal(),
						ClosePrice = closes[i].GetDecimal(),
						Volume = volumes[i].GetInt64()
					});
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Invalid ticker or data unavailable for '{symbol}'.", ex);
			}

			return model;
		}

		public async Task<List<NewsItem>> GetCompanyNewsAsync(string symbol)
		{
			var newsList = new List<NewsItem>();

			if (string.IsNullOrWhiteSpace(_finnhubApiKey))
			{
				_logger.LogWarning("Skipping company news fetch for {Symbol} because Finnhub API key is missing.", symbol);
				return newsList;
			}

			try
			{
				var to = DateTime.UtcNow.ToString("yyyy-MM-dd");
				var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
				var url = $"https://finnhub.io/api/v1/company-news?symbol={symbol}&from={from}&to={to}&token={_finnhubApiKey}";

				var client = _httpClientFactory.CreateClient();
				var response = await client.GetAsync(url);

				if (!response.IsSuccessStatusCode) return newsList;

				var jsonResponse = await response.Content.ReadAsStringAsync();
				using var document = JsonDocument.Parse(jsonResponse);

				foreach (var element in document.RootElement.EnumerateArray().Take(10))
				{
					newsList.Add(new NewsItem
					{
						Id = element.GetProperty("id").GetInt64(),
						Category = element.GetProperty("category").GetString(),
						Ticker = element.GetProperty("related").GetString(),
						Headline = element.GetProperty("headline").GetString(),
						Summary = element.GetProperty("summary").GetString(),
						Url = element.GetProperty("url").GetString(),
						ImageUrl = element.GetProperty("image").GetString(),
						Source = element.GetProperty("source").GetString(),
						PublishedAt = DateTimeOffset.FromUnixTimeSeconds(element.GetProperty("datetime").GetInt64()).UtcDateTime
					});
				}
			}
			catch
			{
				return newsList;
			}

			return newsList;
		}
	}
}
