using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class Trade
	{
		public Guid Id { get; set; }
		public Guid BotId { get; set; }

		public string Symbol { get; set; }

		public decimal Size { get; set; }

		public decimal EntryPrice { get; set; }
		public decimal? ExitPrice { get; set; }

		public decimal? StopLoss { get; set; }
		public decimal? TakeProfit { get; set; }

		//TODO statuses
		//public TradeStatus Status { get; set; }

		public DateTime OpenedAt { get; set; }
		public DateTime? ClosedAt { get; set; }
	}

}
