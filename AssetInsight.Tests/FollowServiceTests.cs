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
	public class FollowServiceTests
	{
		private Mock<IRepository<Follow>> _repoMock;
		private FollowService _service;
		private List<Follow> _follows;

		[SetUp]
		public void SetUp()
		{
			_follows = new List<Follow>();
			_repoMock = new Mock<IRepository<Follow>>();

			_repoMock
				.Setup(r => r.All())
				.Returns(() => _follows.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<Follow>()))
				.Callback((Follow f) =>
				{
					f.Id = _follows.Count + 1;
					_follows.Add(f);
				})
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var intId = (int)id;
					var follow = _follows.FirstOrDefault(x => x.Id == intId);
					if (follow != null)
						_follows.Remove(follow);
				})
				.Returns(Task.CompletedTask);

			_service = new FollowService(_repoMock.Object);
		}

		[Test]
		public async Task ToggleFollowAsync_ShouldAddFollow_WhenNotExisting()
		{
			var followerId = "user1";
			var followeeId = "user2";

			var result = await _service.ToggleFollowAsync(followerId, followeeId);

			Assert.That(result, Is.True);
			Assert.That(_follows.Count, Is.EqualTo(1));
			Assert.That(_follows[0].FollowerId, Is.EqualTo(followerId));
			Assert.That(_follows[0].FollowedUserId, Is.EqualTo(followeeId));
		}

		[Test]
		public async Task ToggleFollowAsync_ShouldRemoveFollow_WhenAlreadyExists()
		{
			var followerId = "user1";
			var followeeId = "user2";

			_follows.Add(new Follow
			{
				Id = 1,
				FollowerId = followerId,
				FollowedUserId = followeeId
			});

			var result = await _service.ToggleFollowAsync(followerId, followeeId);

			Assert.That(result, Is.False);
			Assert.That(_follows, Is.Empty);
		}

		[Test]
		public async Task IsFollowing_ShouldReturnTrue_WhenFollowExists()
		{
			var followerId = "user1";
			var followeeId = "user2";

			_follows.Add(new Follow
			{
				Id = 1,
				FollowerId = followerId,
				FollowedUserId = followeeId
			});

			var result = await _service.IsFollowing(followerId, followeeId);

			Assert.That(result, Is.True);
		}

		[Test]
		public async Task IsFollowing_ShouldReturnFalse_WhenFollowDoesNotExist()
		{
			var result = await _service.IsFollowing("user1", "user2");

			Assert.That(result, Is.False);
		}

		[Test]
		public async Task GetFollowersIdsAsync_ShouldReturnAllFollowers()
		{
			var userId = "target";

			_follows.Add(new Follow { Id = 1, FollowerId = "u1", FollowedUserId = userId });
			_follows.Add(new Follow { Id = 2, FollowerId = "u2", FollowedUserId = userId });
			_follows.Add(new Follow { Id = 3, FollowerId = "u3", FollowedUserId = "other" });

			var result = await _service.GetFollowersIdsAsync(userId);

			Assert.That(result.Count, Is.EqualTo(2));
			Assert.That(result, Does.Contain("u1"));
			Assert.That(result, Does.Contain("u2"));
			Assert.That(result, Does.Not.Contain("u3"));
		}

		[Test]
		public async Task GetFollowersIdsAsync_ShouldReturnEmpty_WhenNoFollowers()
		{
			var result = await _service.GetFollowersIdsAsync("nonexistent");

			Assert.That(result, Is.Empty);
		}
	}
}