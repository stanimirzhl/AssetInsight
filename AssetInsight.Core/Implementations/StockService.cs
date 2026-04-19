using AssetInsight.Core.DTOs.Stock;
using AssetInsight.Core.Interfaces;
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
		private readonly IHttpClientFactory httpClientFactory;

		public StockService(IHttpClientFactory httpClientFactory)
		{
			this.httpClientFactory = httpClientFactory;
		}

		public async Task<StockHistoryDtoModel> GetStockHistoryAsync(string symbol, string range = "1mo")
		{
			symbol = symbol.ToUpper();
			var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?range={range}&interval=1d";

			var client = httpClientFactory.CreateClient();

			client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

			var response = await client.GetAsync(url);
			if (!response.IsSuccessStatusCode)
				throw new Exception($"Failed to fetch data for {symbol}");

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

			var model = new StockHistoryDtoModel { Symbol = symbol, CurrentRange = range };

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

			return model;
		}
	}
}
