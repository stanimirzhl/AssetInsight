using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Configurations
{
	public class FollowConfiguration : IEntityTypeConfiguration<Follow>
	{
		public void Configure(EntityTypeBuilder<Follow> modelBuilder)
		{
			modelBuilder
				.HasOne(x => x.Follower)
				.WithMany(x => x.Following)
				.HasForeignKey(x => x.FollowerId)
				.OnDelete(DeleteBehavior.NoAction);

			modelBuilder
				.HasOne(x => x.FollowedUser)
				.WithMany(x => x.Followers)
				.HasForeignKey(x => x.FollowedUserId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
