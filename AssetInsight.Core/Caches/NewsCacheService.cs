using AssetInsight.Models.ApiNews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Caches
{
	public class NewsCacheService
	{
		private List<NewsItem> _cachedNews = new List<NewsItem>();
		private readonly object _lock = new object();

		public void UpdateNews(List<NewsItem> newItems)
		{
			lock (_lock)
			{
				_cachedNews = newItems;
			}
		}

		public List<NewsItem> GetLatestNews()
		{
			lock (_lock)
			{
				return new List<NewsItem>(_cachedNews);
			}
		}
	}
}
