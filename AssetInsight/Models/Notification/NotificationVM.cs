namespace AssetInsight.Models.Notification
{
	public class NotificationVM
	{
		public int Id { get; set; }
		public string ReceiverId { get; set; }
		public string Message { get; set; }
		public string TargetUrl { get; set; }
		public bool IsRead { get; set; } = false;
		public DateTime CreatedAt { get; set; }
	}
}
