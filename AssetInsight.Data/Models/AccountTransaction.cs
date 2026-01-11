using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class AccountTransaction
	{
		public Guid Id { get; set; }

		public Guid AccountId { get; set; }
		[ForeignKey(nameof(AccountId))]
		public Account Account { get; set; }

		public decimal Amount { get; set; } // + deposit, - withdrawal, - fee

		//TODO
		//public TransactionType Type { get; set; }    // Deposit, Withdrawal

		public DateTime CreatedAt { get; set; }
	}

}
