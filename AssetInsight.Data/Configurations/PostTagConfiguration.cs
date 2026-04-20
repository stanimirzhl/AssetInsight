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
	public class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
	{
		public void Configure(EntityTypeBuilder<PostTag> modelBuilder)
		{
			modelBuilder
				.HasOne(x => x.Tag)
				.WithMany(x => x.PostTags)
				.HasForeignKey(x => x.TagId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder
				.HasOne(x => x.Post)
				.WithMany(x => x.PostTags)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
