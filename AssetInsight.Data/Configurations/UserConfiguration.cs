using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Configurations
{
	public class UserConfiguration : IEntityTypeConfiguration<User>
	{
		public void Configure(EntityTypeBuilder<User> modelBuilder)
		{
			modelBuilder
				.HasMany(x => x.SavedPosts)
				.WithOne(x => x.User)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasMany(x => x.PostReactions)
				.WithOne(x => x.User)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasMany(x => x.CommentReactions)
				.WithOne(x => x.User)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasMany(x => x.Posts)
				.WithOne(x => x.Author)
				.HasForeignKey(x => x.AuthorId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder
				.HasMany(x => x.Comments)
				.WithOne(x => x.Author)
				.HasForeignKey(x => x.AuthorId)
				.OnDelete(DeleteBehavior.SetNull);
		}
	}
}
