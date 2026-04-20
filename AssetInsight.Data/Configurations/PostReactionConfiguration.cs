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
	public class PostReactionConfiguration : IEntityTypeConfiguration<PostReaction>
	{
		public void Configure(EntityTypeBuilder<PostReaction> modelBuilder)
		{
			modelBuilder
				.HasOne(x => x.Post)
				.WithMany(x => x.Reactions)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasOne(x => x.User)
				.WithMany(x => x.PostReactions)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
