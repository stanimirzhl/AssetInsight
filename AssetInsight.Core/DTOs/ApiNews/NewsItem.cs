namespace AssetInsight.Models.ApiNews
{
	public class NewsItem
	{
		public long Id { get; set; }
		public string Category { get; set; }
		public string Ticker { get; set; }
		public string Headline { get; set; }
		public string Summary { get; set; }
		public string Url { get; set; }
		public string ImageUrl { get; set; }
		public string Source { get; set; }
		public bool IsPositive { get; set; }
		public DateTime PublishedAt { get; set; }
	}
}
