using AssetInsight.Core.Caches;
using AssetInsight.Models;
using AssetInsight.Models.ApiNews;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AssetInsight.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly NewsCacheService newsCache;

		public HomeController(ILogger<HomeController> logger,
			NewsCacheService newsCache)
		{
			_logger = logger;
			this.newsCache = newsCache;
		}

		public IActionResult Index()
		{
			try
			{
				ViewBag.MarketNews = newsCache.GetLatestNews();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to retrieve market news from cache.");
				ViewBag.MarketNews = new List<NewsItem>();
			}
			return View();
		}

		[HttpGet]
		public IActionResult GetPaginatedNews(string category = "all", int page = 1)
		{
			int pageSize = 10;
			var allNews = newsCache.GetLatestNews();

			if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
			{
				allNews = allNews.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
			}

			int totalItems = allNews.Count;
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var pagedNews = allNews.Skip((page - 1) * pageSize).Take(pageSize).ToList();

			return Json(new
			{
				data = pagedNews,
				totalPages = totalPages,
				currentPage = page
			});
		}

		public IActionResult Privacy()
		{
			return View();
		}

		public IActionResult ToS()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
