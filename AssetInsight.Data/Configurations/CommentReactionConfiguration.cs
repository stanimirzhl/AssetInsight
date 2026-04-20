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
	public class CommentReactionConfiguration : IEntityTypeConfiguration<CommentReaction>
	{
		public void Configure(EntityTypeBuilder<CommentReaction> modelBuilder)
		{
			modelBuilder
				.HasOne(x => x.Comment)
				.WithMany(x => x.Reactions)
				.HasForeignKey(x => x.CommentId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasOne(x => x.User)
				.WithMany(x => x.CommentReactions)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
