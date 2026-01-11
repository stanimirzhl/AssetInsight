using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetInsight.Data
{
	public class AssetInsightDbContext : IdentityDbContext<User>
	{
		public AssetInsightDbContext(DbContextOptions<AssetInsightDbContext> options) : base(options)
		{

		}

		public virtual DbSet<Account> Accounts { get; set; }

		public virtual DbSet<AccountTransaction> Transactions { get; set; }

		public virtual DbSet<Trade> Trades { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
		}
	}
}
