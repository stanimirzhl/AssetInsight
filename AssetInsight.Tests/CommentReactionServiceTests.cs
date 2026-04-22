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
	public class CommentReactionServiceTests
	{
		private Mock<IRepository<CommentReaction>> _repoMock;
		private CommentReactionService _service;
		private List<CommentReaction> _reactions;

		[SetUp]
		public void SetUp()
		{
			_reactions = new List<CommentReaction>();
			_repoMock = new Mock<IRepository<CommentReaction>>();

			_repoMock
				.Setup(r => r.All())
				.Returns(() => _reactions.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<CommentReaction>()))
				.Callback((CommentReaction r) =>
				{
					r.Id = _reactions.Count + 1;
					_reactions.Add(r);
				})
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var intId = (int)id;
					var reaction = _reactions.FirstOrDefault(x => x.Id == intId);
					if (reaction != null)
						_reactions.Remove(reaction);
				})
				.Returns(Task.CompletedTask);

			_service = new CommentReactionService(_repoMock.Object);
		}

		[Test]
		public async Task GetCommentReactionScoreAsync_ShouldReturnCorrectScore()
		{
			var commentId = Guid.NewGuid();

			_reactions.Add(new CommentReaction
			{
				Id = 1,
				CommentId = commentId,
				IsUpVote = true
			});

			_reactions.Add(new CommentReaction
			{
				Id = 2,
				CommentId = commentId,
				IsUpVote = false
			});

			var result = await _service.GetCommentReactionScoreAsync(commentId);

			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public async Task GetCommentReactionScoreAsync_ShouldReturnPositiveScore()
		{
			var commentId = Guid.NewGuid();

			_reactions.Add(new CommentReaction { Id = 1, CommentId = commentId, IsUpVote = true });
			_reactions.Add(new CommentReaction { Id = 2, CommentId = commentId, IsUpVote = true });

			var result = await _service.GetCommentReactionScoreAsync(commentId);

			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public async Task GetCommentReactionScoreAsync_ShouldReturnNegativeScore()
		{
			var commentId = Guid.NewGuid();

			_reactions.Add(new CommentReaction { Id = 1, CommentId = commentId, IsUpVote = false });
			_reactions.Add(new CommentReaction { Id = 2, CommentId = commentId, IsUpVote = false });

			var result = await _service.GetCommentReactionScoreAsync(commentId);

			Assert.That(result, Is.EqualTo(-2));
		}

		[Test]
		public async Task ToggleReactionAsync_ShouldAddUpvote_WhenNoExistingReaction()
		{
			var commentId = Guid.NewGuid();

			var (score, status) = await _service.ToggleReactionAsync(commentId, "user1", true);

			Assert.That(status, Is.EqualTo("upvoted"));
			Assert.That(_reactions.Count, Is.EqualTo(1));
			Assert.That(_reactions[0].IsUpVote, Is.True);
		}

		[Test]
		public async Task ToggleReactionAsync_ShouldAddDownvote_WhenNoExistingReaction()
		{
			var commentId = Guid.NewGuid();

			var (score, status) = await _service.ToggleReactionAsync(commentId, "user1", false);

			Assert.That(status, Is.EqualTo("downvoted"));
			Assert.That(_reactions.Count, Is.EqualTo(1));
			Assert.That(_reactions[0].IsUpVote, Is.False);
		}

		[Test]
		public async Task ToggleReactionAsync_ShouldRemoveReaction_WhenSameVote()
		{
			var commentId = Guid.NewGuid();

			_reactions.Add(new CommentReaction
			{
				Id = 1,
				CommentId = commentId,
				UserId = "user1",
				IsUpVote = true
			});

			var (score, status) = await _service.ToggleReactionAsync(commentId, "user1", true);

			Assert.That(status, Is.EqualTo("none"));
			Assert.That(_reactions, Is.Empty);
		}

		[Test]
		public async Task ToggleReactionAsync_ShouldSwitchFromUpvoteToDownvote()
		{
			var commentId = Guid.NewGuid();

			_reactions.Add(new CommentReaction
			{
				Id = 1,
				CommentId = commentId,
				UserId = "user1",
				IsUpVote = true
			});

			var (score, status) = await _service.ToggleReactionAsync(commentId, "user1", false);

			Assert.That(status, Is.EqualTo("downvoted"));
			Assert.That(_reactions.Count, Is.EqualTo(1));
			Assert.That(_reactions[0].IsUpVote, Is.False);
		}

		[Test]
		public async Task ToggleReactionAsync_ShouldSwitchFromDownvoteToUpvote()
		{
			var commentId = Guid.NewGuid();

			_reactions.Add(new CommentReaction
			{
				Id = 1,
				CommentId = commentId,
				UserId = "user1",
				IsUpVote = false
			});

			var (score, status) = await _service.ToggleReactionAsync(commentId, "user1", true);

			Assert.That(status, Is.EqualTo("upvoted"));
			Assert.That(_reactions[0].IsUpVote, Is.True);
		}

		[Test]
		public async Task ToggleReactionAsync_ShouldAddUpvote_WhenNoExistingReaction2()
		{
			var commentId = Guid.NewGuid();

			var (score, status) = await _service.ToggleReactionAsync(commentId, "user1", true);

			Assert.That(status, Is.EqualTo("upvoted"));
			Assert.That(score, Is.EqualTo(1));
			Assert.That(_reactions.Count, Is.EqualTo(1));
			Assert.That(_reactions[0].IsUpVote, Is.True);

			_repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
		}
	}
}