using AssetInsight.Core.DTOs.Stock;
using AssetInsight.Models.ApiNews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IStockService
	{
		Task<StockHistoryDtoModel> GetStockHistoryAsync(string symbol, string range);
		Task<List<NewsItem>> GetCompanyNewsAsync(string symbol);
	}
}
