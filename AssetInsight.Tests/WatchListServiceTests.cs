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
	public class WatchListServiceTests
	{
		private Mock<IRepository<WatchList>> _repoMock;
		private WatchListService _service;
		private List<WatchList> _watchList;

		[SetUp]
		public void SetUp()
		{
			_watchList = new List<WatchList>();
			_repoMock = new Mock<IRepository<WatchList>>();

			_repoMock
				.Setup(r => r.All())
				.Returns(() => _watchList.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<WatchList>()))
				.Callback((WatchList w) =>
				{
					w.Id = _watchList.Count + 1;
					_watchList.Add(w);
				})
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var intId = (int)id;
					var item = _watchList.FirstOrDefault(x => x.Id == intId);
					if (item != null)
						_watchList.Remove(item);
				})
				.Returns(Task.CompletedTask);

			_service = new WatchListService(_repoMock.Object);
		}

		[Test]
		public async Task ToggleWatchList_ShouldAdd_WhenNotExisting()
		{
			var result = await _service.ToggleWatchList("user1", "AAPL");

			Assert.That(result, Is.True);
			Assert.That(_watchList.Count, Is.EqualTo(1));
			Assert.That(_watchList[0].UserId, Is.EqualTo("user1"));
			Assert.That(_watchList[0].Symbol, Is.EqualTo("AAPL"));
		}

		[Test]
		public async Task ToggleWatchList_ShouldRemove_WhenAlreadyExists()
		{
			_watchList.Add(new WatchList
			{
				Id = 1,
				UserId = "user1",
				Symbol = "AAPL"
			});

			var result = await _service.ToggleWatchList("user1", "AAPL");

			Assert.That(result, Is.False);
			Assert.That(_watchList, Is.Empty);
		}

		[Test]
		public async Task ToggleWatchList_ShouldOnlyRemoveMatchingUserAndSymbol()
		{
			_watchList.Add(new WatchList { Id = 1, UserId = "user1", Symbol = "AAPL" });
			_watchList.Add(new WatchList { Id = 2, UserId = "user2", Symbol = "AAPL" });

			var result = await _service.ToggleWatchList("user1", "AAPL");

			Assert.That(result, Is.False);
			Assert.That(_watchList.Count, Is.EqualTo(1));
			Assert.That(_watchList[0].UserId, Is.EqualTo("user2"));
		}

		[Test]
		public async Task IsFollowing_ShouldReturnTrue_WhenExists()
		{
			_watchList.Add(new WatchList
			{
				Id = 1,
				UserId = "user1",
				Symbol = "AAPL"
			});

			var result = await _service.IsFollowing("user1", "AAPL");

			Assert.That(result, Is.True);
		}

		[Test]
		public async Task IsFollowing_ShouldReturnFalse_WhenNotExists()
		{
			var result = await _service.IsFollowing("user1", "AAPL");

			Assert.That(result, Is.False);
		}

		[Test]
		public async Task IsFollowing_ShouldBeCaseSensitive()
		{
			_watchList.Add(new WatchList
			{
				Id = 1,
				UserId = "user1",
				Symbol = "AAPL"
			});

			var result = await _service.IsFollowing("USER1", "AAPL");

			Assert.That(result, Is.False);
		}
	}
}