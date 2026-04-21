using AssetInsight.Core;
using AssetInsight.Core.DTOs.Post;
using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using MockQueryable.Moq;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class PostServiceTests
	{
		private Mock<IRepository<Post>> _mockRepository;
		private PostService _postService;
		private List<Post> _testPosts;

		[SetUp]
		public void SetUp()
		{
			_testPosts = new List<Post>();

			_mockRepository = new Mock<IRepository<Post>>();

			//var queryable = _testPosts.AsQueryable();

			_mockRepository
				.Setup(r => r.AllAsReadOnly())
				.Returns(() =>
				{
					return _testPosts
						.AsQueryable()
						.BuildMockDbSet()
						.Object;
				});

			_mockRepository
				.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
				.ReturnsAsync((Guid id) =>
					_testPosts.FirstOrDefault(p => p.Id == id));

			_mockRepository
				.Setup(r => r.AddAsync(It.IsAny<Post>()))
				.Callback((Post post) =>
				{
					post.Id = Guid.NewGuid();
					_testPosts.Add(post);
				})
				.Returns(Task.CompletedTask);

			_mockRepository.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var guid = (Guid)id;
					var post = _testPosts.FirstOrDefault(p => p.Id == guid);
					if (post != null) _testPosts.Remove(post);
				})
				.Returns(Task.CompletedTask);

			_mockRepository
				.Setup(r => r.SaveChangesAsync())
				.ReturnsAsync(1);

			_postService = new PostService(_mockRepository.Object);
		}

		private Post CreateTestPost(
			Guid id,
			string title = "Test Title",
			string content = "Test Content",
			string authorId = "user1",
			string authorName = "User One",
			DateTime? createdAt = null,
			bool isLocked = false,
			List<Comment> comments = null,
			List<PostReaction> reactions = null,
			List<SavedPost> savedPosts = null)
		{
			var post = new Post
			{
				Id = id,
				Title = title,
				Content = content,
				AuthorId = authorId,
				CreatedAt = createdAt ?? DateTime.UtcNow,
				IsLocked = isLocked,
				Comments = comments ?? new List<Comment>(),
				Reactions = reactions ?? new List<PostReaction>(),
				SavedPosts = savedPosts ?? new List<SavedPost>()
			};

			if (authorId != null && authorName != null)
			{
				post.Author = new User { Id = authorId, UserName = authorName };
			}

			return post;
		}

		[Test]
		public async Task AddAsync_ValidPostDto_ShouldAddPostAndReturnId()
		{
			var postDto = new PostDto
			{
				Title = "New Post",
				Content = "Content of new post",
				AuthorId = "author123"
			};

			var result = await _postService.AddAsync(postDto);

			Assert.That(result, Is.Not.EqualTo(Guid.Empty));
			Assert.That(_testPosts, Has.Count.EqualTo(1));
			var addedPost = _testPosts.First();
			Assert.That(addedPost.Title, Is.EqualTo(postDto.Title));
			Assert.That(addedPost.Content, Is.EqualTo(postDto.Content));
			Assert.That(addedPost.AuthorId, Is.EqualTo(postDto.AuthorId));
			Assert.That(addedPost.Id, Is.EqualTo(result));

			_mockRepository.Verify(r => r.AddAsync(It.IsAny<Post>()), Times.Once);
			_mockRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
		}

		[Test]
		public async Task GetByIdAsync_ExistingPost_ShouldReturnPostDto()
		{
			var postId = Guid.NewGuid();
			var post = CreateTestPost(postId, "Test Post", "Content", "user1", "TestUser");
			_testPosts.Add(post);

			var result = await _postService.GetByIdAsync(postId);

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Id, Is.EqualTo(postId));
			Assert.That(result.Title, Is.EqualTo("Test Post"));
			Assert.That(result.Content, Is.EqualTo("Content"));
			Assert.That(result.AuthorId, Is.EqualTo("user1"));
			Assert.That(result.AuthorName, Is.EqualTo("TestUser"));
			Assert.That(result.CreatedAt, Is.EqualTo(post.CreatedAt));
			Assert.That(result.EditedAt, Is.Null);
			Assert.That(result.IsLocked, Is.False);
		}

		[Test]
		public async Task GetByIdAsync_AuthorIsNull_ShouldReturnDeletedAuthorName()
		{
			var postId = Guid.NewGuid();
			var post = CreateTestPost(postId, "Test", "Content", null, null);
			_testPosts.Add(post);

			var result = await _postService.GetByIdAsync(postId);

			Assert.That(result.AuthorName, Is.EqualTo("[deleted]"));
		}

		[Test]
		public void GetByIdAsync_NonExistingPost_ShouldThrowNoEntityException()
		{
			var nonExistentId = Guid.NewGuid();

			var ex = Assert.ThrowsAsync<NoEntityException>(async () =>
				await _postService.GetByIdAsync(nonExistentId));

			Assert.That(ex.Message, Does.Contain($"No entity found with id: {nonExistentId}"));
		}

		[Test]
		public async Task EditAsync_ExistingPost_ShouldUpdateTitleContentAndEditedAt()
		{
			var postId = Guid.NewGuid();
			var originalPost = CreateTestPost(postId, "Old Title", "Old Content");
			_testPosts.Add(originalPost);

			var editDto = new PostDto
			{
				Id = postId,
				Title = "New Title",
				Content = "New Content"
			};

			await _postService.EditAsync(editDto);

			var updatedPost = _testPosts.First(p => p.Id == postId);
			Assert.That(updatedPost.Title, Is.EqualTo("New Title"));
			Assert.That(updatedPost.Content, Is.EqualTo("New Content"));
			Assert.That(updatedPost.EditedAt, Is.Not.Null.And.InRange(DateTime.Now.AddSeconds(-2), DateTime.Now));

			_mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
		}

		[Test]
		public void EditAsync_NonExistingPost_ShouldNotThrow_WhenGetByIdReturnsNull()
		{
			var nonExistentId = Guid.NewGuid();
			var editDto = new PostDto { Id = nonExistentId, Title = "New", Content = "New" };

			Assert.ThrowsAsync<NullReferenceException>(async () =>
				await _postService.EditAsync(editDto));
		}

		[Test]
		public async Task DeleteAsync_ExistingPost_ShouldDeleteAndSaveChanges()
		{
			var postId = Guid.NewGuid();
			var post = CreateTestPost(postId);
			_testPosts.Add(post);

			await _postService.DeleteAsync(postId);

			Assert.That(_testPosts, Is.Empty);
			_mockRepository.Verify(r => r.DeleteAsync(postId), Times.Once);
			_mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
		}

		[Test]
		public void DeleteAsync_NonExistingPost_ShouldThrowNoEntityException()
		{
			var nonExistentId = Guid.NewGuid();

			var ex = Assert.ThrowsAsync<NoEntityException>(async () =>
				await _postService.DeleteAsync(nonExistentId));

			Assert.That(ex.Message, Does.Contain($"No entity found with id: {nonExistentId}"));
			_mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
			_mockRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
		}

		[Test]
		public async Task GetAllPagedPostsAsync_ShouldReturnPagedPostsOrderedByCreatedAtDescending()
		{
			var now = DateTime.UtcNow;
			var post1 = CreateTestPost(Guid.NewGuid(), "Post 1", "Content", "user1", "User1", now.AddDays(-2));
			var post2 = CreateTestPost(Guid.NewGuid(), "Post 2", "Content", "user2", "User2", now.AddDays(-1));
			var post3 = CreateTestPost(Guid.NewGuid(), "Post 3", "Content", "user1", "User1", now);

			post1.Comments = new List<Comment> { new Comment(), new Comment() };
			post2.Reactions = new List<PostReaction> { new PostReaction(), new PostReaction(), new PostReaction() };
			post3.Comments = new List<Comment> { new Comment() };
			post3.Reactions = new List<PostReaction> { new PostReaction() };

			_testPosts.AddRange(new[] { post1, post2, post3 });

			var result = await _postService.GetAllPagedPostsAsync(1, 2, null);

			Assert.That(result.Items, Has.Count.EqualTo(2));
			Assert.That(result.PageIndex, Is.EqualTo(1));
			Assert.That(result.PageSize, Is.EqualTo(2));
			Assert.That(result.TotalCount, Is.EqualTo(3));
			Assert.That(result.TotalPages, Is.EqualTo(2));

			var first = result.Items.First();
			var second = result.Items.Last();
			Assert.That(first.Id, Is.EqualTo(post3.Id));
			Assert.That(second.Id, Is.EqualTo(post2.Id));

			Assert.That(first.CommentsCount, Is.EqualTo(1));
			Assert.That(first.ReactionsCount, Is.EqualTo(1));
			Assert.That(second.CommentsCount, Is.EqualTo(0));
			Assert.That(second.ReactionsCount, Is.EqualTo(3));
		}

		[Test]
		public async Task GetAllPagedPostsAsync_SecondPage_ShouldReturnCorrectItems()
		{
			var now = DateTime.UtcNow;
			for (int i = 1; i <= 5; i++)
			{
				_testPosts.Add(CreateTestPost(Guid.NewGuid(), $"Post {i}", "Content", "user", "User", now.AddDays(-i)));
			}

			var result = await _postService.GetAllPagedPostsAsync(2, 2, null);

			Assert.That(result.Items, Has.Count.EqualTo(2));
			Assert.That(result.PageIndex, Is.EqualTo(2));
			Assert.That(result.TotalCount, Is.EqualTo(5));

			var expectedIds = _testPosts.OrderByDescending(p => p.CreatedAt).Skip(2).Take(2).Select(p => p.Id);
			Assert.That(result.Items.Select(i => i.Id), Is.EquivalentTo(expectedIds));
		}

		[Test]
		public async Task GetAllPagedPostsByUserNameAsync_ShouldFilterByUserNameAndSortByRecent()
		{
			var user1 = "Alice";
			var user2 = "Bob";
			var now = DateTime.UtcNow;

			var post1 = CreateTestPost(Guid.NewGuid(), "Alice Post 1", "Content", "user1", user1, now.AddDays(-3));
			var post2 = CreateTestPost(Guid.NewGuid(), "Bob Post", "Content", "user2", user2, now.AddDays(-2));
			var post3 = CreateTestPost(Guid.NewGuid(), "Alice Post 2", "Content", "user1", user1, now.AddDays(-1));
			post1.Reactions = new List<PostReaction> { new PostReaction() };
			post3.Reactions = new List<PostReaction> { new PostReaction(), new PostReaction() };

			_testPosts.AddRange(new[] { post1, post2, post3 });

			var result = await _postService.GetAllPagedPostsByUserNameAsync(user1, 1, 10, "recent");

			Assert.That(result.Items, Has.Count.EqualTo(2));
			Assert.That(result.TotalCount, Is.EqualTo(2));
			Assert.That(result.Items.First().Id, Is.EqualTo(post3.Id));
			Assert.That(result.Items.Last().Id, Is.EqualTo(post1.Id));
		}

		[Test]
		public async Task GetAllPagedPostsByUserNameAsync_ShouldSortByTopWhenSortByIsTop()
		{
			var user1 = "Alice";
			var now = DateTime.UtcNow;

			var post1 = CreateTestPost(Guid.NewGuid(), "Post 1", "Content", "user1", user1, now.AddDays(-3));
			var post2 = CreateTestPost(Guid.NewGuid(), "Post 2", "Content", "user1", user1, now.AddDays(-2));
			var post3 = CreateTestPost(Guid.NewGuid(), "Post 3", "Content", "user1", user1, now.AddDays(-1));

			post1.Reactions = new List<PostReaction> { new PostReaction() };
			post2.Reactions = new List<PostReaction> { new PostReaction(), new PostReaction(), new PostReaction() };
			post3.Reactions = new List<PostReaction> { new PostReaction(), new PostReaction() };

			_testPosts.AddRange(new[] { post1, post2, post3 });

			var result = await _postService.GetAllPagedPostsByUserNameAsync(user1, 1, 10, "top");

			Assert.That(result.Items, Has.Count.EqualTo(3));
			Assert.That(result.Items.Select(i => i.Id).ToArray(),
				Is.EqualTo(new[] { post2.Id, post3.Id, post1.Id }));
		}

		[Test]
		public async Task GetAllPagedPostsByUserNameAsync_NoPostsForUser_ShouldReturnEmptyPage()
		{
			_testPosts.Add(CreateTestPost(Guid.NewGuid(), "Post", "Content", "user1", "Alice"));

			var result = await _postService.GetAllPagedPostsByUserNameAsync("Bob", 1, 10, "recent");

			Assert.That(result.Items, Is.Empty);
			Assert.That(result.TotalCount, Is.EqualTo(0));
		}

		[Test]
		public async Task GetSavedPostsPagedAsync_ShouldReturnOnlySavedPostsForUser()
		{
			var userId = "user123";
			var otherUserId = "user456";

			var post1 = CreateTestPost(Guid.NewGuid(), "Saved by user", "Content", "author1", "Author1");
			var post2 = CreateTestPost(Guid.NewGuid(), "Not saved", "Content", "author2", "Author2");
			var post3 = CreateTestPost(Guid.NewGuid(), "Saved by other", "Content", "author3", "Author3");

			post1.SavedPosts = new List<SavedPost> { new SavedPost { UserId = userId } };
			post2.SavedPosts = new List<SavedPost>();
			post3.SavedPosts = new List<SavedPost> { new SavedPost { UserId = otherUserId } };

			_testPosts.AddRange(new[] { post1, post2, post3 });

			var result = await _postService.GetSavedPostsPagedAsync(userId, 1, 10, "recent");

			Assert.That(result.Items, Has.Count.EqualTo(1));
			Assert.That(result.TotalCount, Is.EqualTo(1));
			Assert.That(result.Items.First().Id, Is.EqualTo(post1.Id));
		}

		[Test]
		public async Task GetSavedPostsPagedAsync_ShouldSortByTopCorrectly()
		{
			var userId = "user123";
			var now = DateTime.UtcNow;

			var post1 = CreateTestPost(Guid.NewGuid(), "Post 1", "Content", "author1", "Author1", now.AddDays(-2));
			var post2 = CreateTestPost(Guid.NewGuid(), "Post 2", "Content", "author2", "Author2", now.AddDays(-1));

			post1.Reactions = new List<PostReaction> { new PostReaction() };
			post2.Reactions = new List<PostReaction> { new PostReaction(), new PostReaction() };

			post1.SavedPosts = new List<SavedPost> { new SavedPost { UserId = userId } };
			post2.SavedPosts = new List<SavedPost> { new SavedPost { UserId = userId } };

			_testPosts.AddRange(new[] { post1, post2 });

			var result = await _postService.GetSavedPostsPagedAsync(userId, 1, 10, "top");

			Assert.That(result.Items.First().Id, Is.EqualTo(post2.Id));
			Assert.That(result.Items.Last().Id, Is.EqualTo(post1.Id));
		}

		[Test]
		public async Task GetUpvotedPostsPagedAsync_ShouldReturnOnlyPostsWithUserUpvote()
		{
			var userId = "user123";
			var otherUserId = "user456";

			var post1 = CreateTestPost(Guid.NewGuid(), "Upvoted by user", "Content");
			var post2 = CreateTestPost(Guid.NewGuid(), "Not upvoted", "Content");
			var post3 = CreateTestPost(Guid.NewGuid(), "Upvoted by other", "Content");
			var post4 = CreateTestPost(Guid.NewGuid(), "Downvoted by user", "Content");

			post1.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = true }
			};
			post2.Reactions = new List<PostReaction>();
			post3.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = otherUserId, IsUpVote = true }
			};
			post4.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = false }
			};

			_testPosts.AddRange(new[] { post1, post2, post3, post4 });

			var result = await _postService.GetUpvotedPostsPagedAsync(userId, 1, 10, "recent");

			Assert.That(result.Items, Has.Count.EqualTo(1));
			Assert.That(result.Items.First().Id, Is.EqualTo(post1.Id));
		}

		[Test]
		public async Task GetUpvotedPostsPagedAsync_ShouldSortByTop()
		{
			var userId = "user123";
			var now = DateTime.UtcNow;

			var post1 = CreateTestPost(Guid.NewGuid(), "Post 1", "Content", createdAt: now.AddDays(-2));
			var post2 = CreateTestPost(Guid.NewGuid(), "Post 2", "Content", createdAt: now.AddDays(-1));

			post1.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = true },
				new PostReaction { UserId = "other", IsUpVote = true }
			};
			post2.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = true }
			};

			_testPosts.AddRange(new[] { post1, post2 });

			var result = await _postService.GetUpvotedPostsPagedAsync(userId, 1, 10, "top");

			Assert.That(result.Items.First().Id, Is.EqualTo(post1.Id));
		}

		[Test]
		public async Task GetDownvotedPostsPagedAsync_ShouldReturnOnlyPostsWithUserDownvote()
		{
			var userId = "user123";
			var otherUserId = "user456";

			var post1 = CreateTestPost(Guid.NewGuid(), "Downvoted by user", "Content");
			var post2 = CreateTestPost(Guid.NewGuid(), "Upvoted by user", "Content");
			var post3 = CreateTestPost(Guid.NewGuid(), "Downvoted by other", "Content");
			var post4 = CreateTestPost(Guid.NewGuid(), "No reaction", "Content");

			post1.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = false }
			};
			post2.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = true }
			};
			post3.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = otherUserId, IsUpVote = false }
			};
			post4.Reactions = new List<PostReaction>();

			_testPosts.AddRange(new[] { post1, post2, post3, post4 });

			var result = await _postService.GetDownvotedPostsPagedAsync(userId, 1, 10, "recent");

			Assert.That(result.Items, Has.Count.EqualTo(1));
			Assert.That(result.Items.First().Id, Is.EqualTo(post1.Id));
		}

		[Test]
		public async Task GetDownvotedPostsPagedAsync_ShouldSortByTop()
		{
			var userId = "user123";
			var now = DateTime.UtcNow;

			var post1 = CreateTestPost(Guid.NewGuid(), "Post 1", "Content", createdAt: now.AddDays(-2));
			var post2 = CreateTestPost(Guid.NewGuid(), "Post 2", "Content", createdAt: now.AddDays(-1));

			post1.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = false },
				new PostReaction { UserId = "other", IsUpVote = true }
			};
			post2.Reactions = new List<PostReaction>
			{
				new PostReaction { UserId = userId, IsUpVote = false }
			};

			_testPosts.AddRange(new[] { post1, post2 });

			var result = await _postService.GetDownvotedPostsPagedAsync(userId, 1, 10, "top");

			Assert.That(result.Items.First().Id, Is.EqualTo(post1.Id));
		}
	}
}