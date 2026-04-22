using AssetInsight.Core;
using AssetInsight.Core.Implementations;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class CommentServiceTests
	{
		private Mock<IRepository<Comment>> _repoMock;
		private CommentService _service;
		private List<Comment> _comments;

		[SetUp]
		public void SetUp()
		{
			_comments = new List<Comment>();
			_repoMock = new Mock<IRepository<Comment>>();

			_repoMock
				.Setup(r => r.All())
				.Returns(() => _comments.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<Comment>()))
				.Callback((Comment c) =>
				{
					c.Id = Guid.NewGuid();
					c.CreatedAt = DateTime.UtcNow;
					_comments.Add(c);
				})
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
				.ReturnsAsync((Guid id) => _comments.FirstOrDefault(x => x.Id == id));

			_repoMock
				.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var guid = (Guid)id;
					var comment = _comments.FirstOrDefault(x => x.Id == guid);
					if (comment != null)
						_comments.Remove(comment);
				})
				.Returns(Task.CompletedTask);

			_service = new CommentService(_repoMock.Object);
		}

		[Test]
		public async Task AddAsync_ShouldCreateComment()
		{
			var postId = Guid.NewGuid();

			var result = await _service.AddAsync(postId, "Hello world", "user1");

			Assert.That(_comments.Count, Is.EqualTo(1));
			Assert.That(result.Content, Is.EqualTo("Hello world"));
			Assert.That(result.ParentCommentId, Is.Null);
			Assert.That(_comments[0].PostId, Is.EqualTo(postId));
			Assert.That(_comments[0].AuthorId, Is.EqualTo("user1"));
		}

		[Test]
		public async Task EditAsync_ShouldUpdateContent()
		{
			var postId = Guid.NewGuid();

			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId,
				Content = "old"
			};

			_comments.Add(comment);

			await _service.EditAsync(postId, comment.Id, "new content");

			Assert.That(comment.Content, Is.EqualTo("new content"));
			Assert.That(comment.EditedAt, Is.Not.Null);
		}

		[Test]
		public void EditAsync_ShouldThrow_WhenCommentNotFound()
		{
			Assert.ThrowsAsync<NoEntityException>(async () =>
				await _service.EditAsync(Guid.NewGuid(), Guid.NewGuid(), "text"));
		}

		[Test]
		public void EditAsync_ShouldThrow_WhenWrongPost()
		{
			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				PostId = Guid.NewGuid(),
				Content = "old"
			};

			_comments.Add(comment);

			Assert.ThrowsAsync<InvalidOperationException>(async () =>
				await _service.EditAsync(Guid.NewGuid(), comment.Id, "new"));
		}

		[Test]
		public async Task DeleteAsync_ShouldRemove_WhenNoReplies()
		{
			var postId = Guid.NewGuid();

			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId
			};

			_comments.Add(comment);

			await _service.DeleteAsync(postId, comment.Id);

			Assert.That(_comments, Is.Empty);
		}

		[Test]
		public async Task DeleteAsync_ShouldMarkDeleted_WhenHasReplies()
		{
			var postId = Guid.NewGuid();

			var parent = new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId
			};

			var reply = new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId,
				ParentCommentId = parent.Id
			};

			_comments.Add(parent);
			_comments.Add(reply);

			await _service.DeleteAsync(postId, parent.Id);

			Assert.That(parent.IsDeleted, Is.True);
			Assert.That(_comments.Count, Is.EqualTo(2));
		}

		[Test]
		public void DeleteAsync_ShouldThrow_WhenWrongPost()
		{
			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				PostId = Guid.NewGuid()
			};

			_comments.Add(comment);

			Assert.ThrowsAsync<InvalidOperationException>(async () =>
				await _service.DeleteAsync(Guid.NewGuid(), comment.Id));
		}

		[Test]
		public async Task GetByIdAsync_ShouldReturnCommentDto()
		{
			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				Content = "test",
				AuthorId = "user1",
				PostId = Guid.NewGuid()
			};

			_comments.Add(comment);

			var result = await _service.GetByIdAsync(comment.Id);

			Assert.That(result.Id, Is.EqualTo(comment.Id));
			Assert.That(result.Content, Is.EqualTo("test"));
			Assert.That(result.AuthorId, Is.EqualTo("user1"));
		}

		[Test]
		public async Task GetRootCommentsPaginated_ShouldReturnOnlyRootComments()
		{
			var postId = Guid.NewGuid();

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId,
				ParentCommentId = null,
				Content = "root1",
				CreatedAt = DateTime.UtcNow
			});

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId,
				ParentCommentId = Guid.NewGuid(),
				Content = "reply"
			});

			var result = await _service.GetRootCommentsPaginated(postId, 1, "user1");

			Assert.That(result.Items.Count, Is.EqualTo(1));
			Assert.That(result.Items[0].Content, Is.EqualTo("root1"));
		}

		[Test]
		public async Task GetRootCommentsPaginated_ShouldSortByNewest()
		{
			var postId = Guid.NewGuid();

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId,
				ParentCommentId = null,
				Content = "old",
				CreatedAt = DateTime.UtcNow.AddDays(-1)
			});

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				PostId = postId,
				ParentCommentId = null,
				Content = "new",
				CreatedAt = DateTime.UtcNow
			});

			var result = await _service.GetRootCommentsPaginated(postId, 1, "user1", "newest");

			Assert.That(result.Items.First().Content, Is.EqualTo("new"));
		}

		[Test]
		public async Task GetPagedCommentsByUserAsync_ShouldReturnOnlyUserComments()
		{
			var userId = "user1";

			var post = new Post
			{
				Id = Guid.NewGuid(),
				Title = "Test Post",
				CreatedAt = DateTime.UtcNow
			};

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				AuthorId = userId,
				Content = "mine",
				CreatedAt = DateTime.UtcNow,

				Author = new User
				{
					Id = userId,
					UserName = "john"
				},

				Post = post,
				Reactions = new List<CommentReaction>(),
				ParentComment = null
			});

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				AuthorId = "other",
				Content = "not mine",
				CreatedAt = DateTime.UtcNow,

				Author = new User
				{
					Id = "other",
					UserName = "bob"
				},

				Post = post,
				Reactions = new List<CommentReaction>(),
				ParentComment = null
			});

			var result = await _service.GetPagedCommentsByUserAsync(userId, 1, 10, "new");

			Assert.That(result.Items.Count, Is.EqualTo(1));
			Assert.That(result.Items[0].Content, Is.EqualTo("mine"));
		}

		[Test]
		public async Task GetPagedCommentsByUserAsync_ShouldSortByCreatedAtDescending()
		{
			var userId = "user1";

			var post = new Post
			{
				Id = Guid.NewGuid(),
				Title = "Test Post",
				CreatedAt = DateTime.UtcNow
			};

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				AuthorId = userId,
				Content = "old",
				CreatedAt = DateTime.UtcNow.AddDays(-2),

				Author = new User
				{
					Id = userId,
					UserName = "john"
				},

				Post = post,
				Reactions = new List<CommentReaction>(),
				ParentComment = null
			});

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				AuthorId = userId,
				Content = "new",
				CreatedAt = DateTime.UtcNow,

				Author = new User
				{
					Id = userId,
					UserName = "john"
				},

				Post = post,
				Reactions = new List<CommentReaction>(),
				ParentComment = null
			});

			var result = await _service.GetPagedCommentsByUserAsync(userId, 1, 10, "new");

			Assert.That(result.Items.First().Content, Is.EqualTo("new"));
		}

		[Test]
		public async Task GetRepliesByParentId_ShouldReturnDirectReplies()
		{
			var parentId = Guid.NewGuid();

			var reply = new Comment
			{
				Id = Guid.NewGuid(),
				ParentCommentId = parentId,
				Content = "reply"
			};

			_comments.Add(reply);

			var result = await _service.GetRepliesByParentId(parentId, "user1");

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0].Content, Is.EqualTo("reply"));
		}

		[Test]
		public async Task GetRepliesByParentId_ShouldReturnDirectRepliesOnly()
		{
			var parentId = Guid.NewGuid();

			var parent = new Comment
			{
				Id = parentId,
				Content = "parent"
			};

			var child = new Comment
			{
				Id = Guid.NewGuid(),
				ParentCommentId = parentId,
				Content = "child",
				Replies = new List<Comment>()
			};

			var nested = new Comment
			{
				Id = Guid.NewGuid(),
				ParentCommentId = child.Id,
				Content = "nested"
			};

			child.Replies.Add(nested);

			_comments.Add(parent);
			_comments.Add(child);
			_comments.Add(nested);

			var result = await _service.GetRepliesByParentId(parentId, "user1");

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0].Content, Is.EqualTo("child"));
			Assert.That(result[0].Replies.Count, Is.EqualTo(1));
			Assert.That(result[0].Replies.First().Content, Is.EqualTo("nested"));
		}

		[Test]
		public async Task GetRepliesByParentId_ShouldReturnEmpty_WhenNoReplies()
		{
			var result = await _service.GetRepliesByParentId(Guid.NewGuid(), "user1");

			Assert.That(result, Is.Empty);
		}

		[Test]
		public async Task GetPostCommentCountAsync_ShouldReturnCorrectCount()
		{
			var postId = Guid.NewGuid();

			_comments.Add(new Comment { Id = Guid.NewGuid(), PostId = postId });
			_comments.Add(new Comment { Id = Guid.NewGuid(), PostId = postId });
			_comments.Add(new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid() });

			var result = await _service.GetPostCommentCountAsync(postId);

			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public async Task GetByIdAsync_WhenAuthorExists_ShouldReturnAuthorName()
		{
			var commentId = Guid.NewGuid();
			_comments.Add(new Comment
			{
				Id = commentId,
				Content = "test",
				AuthorId = "user1",
				// Explicitly setting the Author navigation property
				Author = new User { UserName = "JohnDoe" }
			});

			var result = await _service.GetByIdAsync(commentId);

			Assert.That(result.Id, Is.EqualTo(commentId));
			Assert.That(result.AuthorName, Is.EqualTo("JohnDoe"));
		}

		[Test]
		public async Task GetByIdAsync_WhenAuthorIsNull_ShouldReturnDeleted()
		{
			var commentId = Guid.NewGuid();
			_comments.Add(new Comment
			{
				Id = commentId,
				Content = "test",
				AuthorId = "user1",
				Author = null
			});

			var result = await _service.GetByIdAsync(commentId);

			Assert.That(result.Id, Is.EqualTo(commentId));
			Assert.That(result.AuthorName, Is.EqualTo("[deleted]"));
		}

		[Test]
		public async Task AddAsync_ShouldReturnDtoWithCorrectData()
		{
			var postId = Guid.NewGuid();
			var parentId = Guid.NewGuid();
			var authorId = "user1";

			var result = await _service.AddAsync(postId, "New comment", authorId, parentId);

			Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
			Assert.That(result.Content, Is.EqualTo("New comment"));
			Assert.That(result.ReplyCount, Is.EqualTo(0));
			Assert.That(result.ParentCommentId, Is.EqualTo(parentId));
			Assert.That(result.IsAuthor, Is.True);

			Assert.That(result.AuthorName, Is.EqualTo("[deleted]"));
		}

		[Test]
		public async Task GetPagedCommentsByUserAsync_TernaryBranches_ShouldMapCorrectly()
		{
			var userId = "user1";
			var post = new Post { Id = Guid.NewGuid(), Title = "Title" };
			var author = new User { Id = userId, UserName = "User1" };

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				AuthorId = userId,
				Post = post,
				Author = author,
				ParentComment = null
			});

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				AuthorId = userId,
				Post = post,
				Author = author,
				ParentComment = new Comment { Author = null }
			});

			_comments.Add(new Comment
			{
				Id = Guid.NewGuid(),
				AuthorId = userId,
				Post = post,
				Author = author,
				ParentComment = new Comment { Author = new User { UserName = "ParentUser" } }
			});

			var result = await _service.GetPagedCommentsByUserAsync(userId, 1, 10, "new");

			var items = result.Items.ToList();

			Assert.That(items, Has.Count.EqualTo(3));
			Assert.That(items.Any(i => i.ParentCommentAuthorName == null), Is.True, "Failed to map null parent");
			Assert.That(items.Any(i => i.ParentCommentAuthorName == "[deleted]"), Is.True, "Failed to map deleted parent author");
			Assert.That(items.Any(i => i.ParentCommentAuthorName == "ParentUser"), Is.True, "Failed to map valid parent author");
		}
	}
}