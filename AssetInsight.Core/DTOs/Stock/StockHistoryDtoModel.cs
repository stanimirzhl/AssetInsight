using AssetInsight.Models.ApiNews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.DTOs.Stock
{
	public class StockHistoryDtoModel
	{
		public string Symbol { get; set; }
		public string CurrentRange { get; set; }
		public bool IsFollowing { get; set; }
		public List<ChartDataPoint> History { get; set; } = new();
		public List<NewsItem> CompanyNews { get; set; } = new();
	}
}
