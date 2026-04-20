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
	public class CommentConfiguration : IEntityTypeConfiguration<Comment>
	{
		public void Configure(EntityTypeBuilder<Comment> modelBuilder)
		{
			modelBuilder
				.HasOne(c => c.ParentComment)
				.WithMany(c => c.Replies)
				.HasForeignKey(c => c.ParentCommentId)
				.OnDelete(DeleteBehavior.NoAction);


			modelBuilder
				.HasMany(x => x.Reactions)
				.WithOne(x => x.Comment)
				.HasForeignKey(x => x.CommentId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasOne(x => x.ParentComment)
				.WithMany(x => x.Replies)
				.HasForeignKey(x => x.ParentCommentId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
