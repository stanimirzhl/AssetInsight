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
	public class PostConfiguration : IEntityTypeConfiguration<Post>
	{
		public void Configure(EntityTypeBuilder<Post> modelBuilder)
		{
			modelBuilder
				.HasMany(x => x.Reactions)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasMany(x => x.Comments)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasMany(x => x.PostTags)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasMany(x => x.SavedPosts)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
