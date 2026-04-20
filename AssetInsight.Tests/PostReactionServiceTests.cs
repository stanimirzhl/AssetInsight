using AssetInsight.Core.Implementations;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class PostReactionServiceTests
	{
		private Mock<IRepository<PostReaction>> _repoMock;
		private List<PostReaction> _reactions;
		private PostReactionService _service;

		[SetUp]
		public void SetUp()
		{
			_reactions = new List<PostReaction>();

			_repoMock = new Mock<IRepository<PostReaction>>();

			var mockSet = _reactions.AsQueryable().BuildMockDbSet();

			_repoMock.Setup(r => r.All()).Returns(mockSet.Object);

			_repoMock.Setup(r => r.AddAsync(It.IsAny<PostReaction>()))
				.Callback<PostReaction>(r => _reactions.Add(r))
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var intId = (int)id;

					var img = _reactions.FirstOrDefault(x => x.Id == intId);
					if (img != null)
						_reactions.Remove(img);
				})
				.Returns(Task.CompletedTask);

			_repoMock.Setup(r => r.SaveChangesAsync())
				.ReturnsAsync(1);

			_service = new PostReactionService(_repoMock.Object);
		}

		[Test]
		public async Task GetPostReactionScoreAsync_ShouldReturnCorrectScore()
		{
			var postId = Guid.NewGuid();

			_reactions.AddRange(new[]
			{
				new PostReaction { Id = 1, PostId = postId, IsUpVote = true },
				new PostReaction { Id = 2, PostId = postId, IsUpVote = false },
				new PostReaction { Id = 3, PostId = postId, IsUpVote = true }
			});

			var result = await _service.GetPostReactionScoreAsync(postId);

			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public async Task ToggleReaction_AddNewUpvote_ShouldAddAndReturnStatus()
		{
			var postId = Guid.NewGuid();

			var result = await _service.ToggleReactionAsync(postId, "user1", true);

			Assert.That(result.status, Is.EqualTo("upvoted"));
			Assert.That(_reactions.Count, Is.EqualTo(1));
			Assert.That(_reactions.First().IsUpVote, Is.True);
		}

		[Test]
		public async Task ToggleReaction_RemoveSameReaction_ShouldDelete()
		{
			var postId = Guid.NewGuid();

			_reactions.Add(new PostReaction
			{
				Id = 1,
				PostId = postId,
				UserId = "user1",
				IsUpVote = true
			});

			var result = await _service.ToggleReactionAsync(postId, "user1", true);

			Assert.That(result.status, Is.EqualTo("none"));
			Assert.That(_reactions, Is.Empty);
		}

		[Test]
		public async Task ToggleReaction_ChangeReaction_ShouldUpdate()
		{
			var postId = Guid.NewGuid();

			_reactions.Add(new PostReaction
			{
				Id = 1,
				PostId = postId,
				UserId = "user1",
				IsUpVote = true
			});

			var result = await _service.ToggleReactionAsync(postId, "user1", false);

			Assert.That(result.status, Is.EqualTo("downvoted"));
			Assert.That(_reactions.First().IsUpVote, Is.False);
		}

		[Test]
		public async Task ToggleReaction_ShouldRecalculateScore()
		{
			var postId = Guid.NewGuid();

			_reactions.Add(new PostReaction { Id = 1, PostId = postId, IsUpVote = true });
			_reactions.Add(new PostReaction { Id = 2, PostId = postId, IsUpVote = true });

			var result = await _service.ToggleReactionAsync(postId, "user3", false);

			Assert.That(result.score, Is.EqualTo(1));
		}
	}
}