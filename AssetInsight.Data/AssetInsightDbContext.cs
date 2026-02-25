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

		public virtual DbSet<Post> Posts { get; set; }

		public virtual DbSet<Tag> Tags { get; set; }

		public virtual DbSet<Comment> Comments { get; set; }

		public virtual DbSet<SavedPost> SavedPosts { get; set; }

		public virtual DbSet<PostTag> PostTags { get; set; }

		public virtual DbSet<PostReaction> PostReactions { get; set; }

		public virtual DbSet<CommentReaction> CommentReactions { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Post>()
				.HasMany(x => x.Reactions)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Post>()
				.HasMany(x => x.Comments)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Post>()
				.HasMany(x => x.PostTags)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Post>()
				.HasMany(x => x.SavedPosts)
				.WithOne(x => x.Post)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Comment>()
				.HasMany(x => x.Replies)
				.WithOne(x => x.ParentComment)
				.HasForeignKey(x => x.ParentCommentId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Comment>()
				.HasMany(x => x.Reactions)
				.WithOne(x => x.Comment)
				.HasForeignKey(x => x.CommentId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<User>()
				.HasMany(x => x.SavedPosts)
				.WithOne(x => x.User)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<User>()
				.HasMany(x => x.PostReactions)
				.WithOne(x => x.User)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<User>()
				.HasMany(x => x.CommentReactions)
				.WithOne(x => x.User)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<User>()
				.HasMany(x => x.Posts)
				.WithOne(x => x.Author)
				.HasForeignKey(x => x.AuthorId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<User>()
				.HasMany(x => x.Comments)
				.WithOne(x => x.Author)
				.HasForeignKey(x => x.AuthorId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<CommentReaction>()
				.HasOne(x => x.Comment)
				.WithMany(x => x.Reactions)
				.HasForeignKey(x => x.CommentId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<CommentReaction>()
				.HasOne(x => x.User)
				.WithMany(x => x.CommentReactions)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<PostReaction>()
				.HasOne(x => x.Post)
				.WithMany(x => x.Reactions)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<PostReaction>()
				.HasOne(x => x.User)
				.WithMany(x => x.PostReactions)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<PostTag>()
				.HasOne(x => x.Tag)
				.WithMany(x => x.PostTags)
				.HasForeignKey(x => x.TagId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<PostTag>()
				.HasOne(x => x.Post)
				.WithMany(x => x.PostTags)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<SavedPost>()
				.HasOne(x => x.User)
				.WithMany(x => x.SavedPosts)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<SavedPost>()
				.HasOne(x => x.Post)
				.WithMany(x => x.SavedPosts)
				.HasForeignKey(x => x.PostId)
				.OnDelete(DeleteBehavior.Cascade);


			base.OnModelCreating(modelBuilder);
		}
	}
}
