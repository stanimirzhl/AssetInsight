using AssetInsight.Core;
using AssetInsight.Core.DTOs.TradingStrategy;
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
	public class StrategyServiceTests
	{
		private Mock<IRepository<TradingStrategy>> _repoMock;
		private StrategyService _service;
		private List<TradingStrategy> _strategies;

		private const string ValidJson = "{}";
		private const string InvalidJson = "NOT_JSON";

		[SetUp]
		public void SetUp()
		{
			_strategies = new List<TradingStrategy>();
			_repoMock = new Mock<IRepository<TradingStrategy>>();

			_repoMock.Setup(r => r.AllAsReadOnly())
				.Returns(() => _strategies.AsQueryable().BuildMockDbSet().Object);

			_repoMock.Setup(r => r.All())
				.Returns(() => _strategies.AsQueryable().BuildMockDbSet().Object);

			_repoMock.Setup(r => r.AddAsync(It.IsAny<TradingStrategy>()))
				.Callback((TradingStrategy s) =>
				{
					s.Id = _strategies.Count + 1;
					_strategies.Add(s);
				})
				.Returns(Task.CompletedTask);

			_repoMock.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var intId = (int)id;
					var strategy = _strategies.FirstOrDefault(x => x.Id == intId);
					if (strategy != null) _strategies.Remove(strategy);
				})
				.Returns(Task.CompletedTask);

			_repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

			_service = new StrategyService(_repoMock.Object);
		}

		[Test]
		public async Task GetAllStrategiesAsync_ShouldReturnAllStrategies()
		{
			_strategies.Add(new TradingStrategy { Id = 1, Name = "Global 1" });
			_strategies.Add(new TradingStrategy { Id = 2, Name = "Global 2" });

			var result = await _service.GetAllStrategiesAsync();

			Assert.That(result.Count, Is.EqualTo(2));
		}

		[Test]
		public async Task GetAllUserStrategiesAsync_NullUserId_ShouldReturnOnlySystemStrategies()
		{
			_strategies.Add(new TradingStrategy { Id = 1, UserId = null });
			_strategies.Add(new TradingStrategy { Id = 2, UserId = "user1" });

			var result = await _service.GetAllUserStrategiesAsync(null);

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result.First().UserId, Is.Null);
		}

		[Test]
		public async Task GetAllUserStrategiesAsync_ValidUserId_ShouldReturnUserAndSystemStrategies()
		{
			_strategies.Add(new TradingStrategy { Id = 1, UserId = null });
			_strategies.Add(new TradingStrategy { Id = 2, UserId = "user1" });
			_strategies.Add(new TradingStrategy { Id = 3, UserId = "user2" });

			var result = await _service.GetAllUserStrategiesAsync("user1");

			Assert.That(result.Count, Is.EqualTo(2));
			Assert.That(result.Any(s => s.Id == 1), Is.True);
			Assert.That(result.Any(s => s.Id == 2), Is.True);
		}

		[Test]
		public async Task GetStrategyByIdAsync_ExistingId_ShouldReturnStrategy()
		{
			_strategies.Add(new TradingStrategy { Id = 1, Name = "Test" });

			var result = await _service.GetStrategyByIdAsync(1);

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Name, Is.EqualTo("Test"));
		}

		[Test]
		public void GetStrategyByIdAsync_MissingId_ShouldThrowNoEntityException()
		{
			var ex = Assert.ThrowsAsync<NoEntityException>(async () =>
				await _service.GetStrategyByIdAsync(99));

			Assert.That(ex.Message, Does.Contain("not found"));
		}

		[Test]
		public async Task CreateCustomStrategyAsync_ValidJson_ShouldAddAndSave()
		{
			var dto = new StrategyDto { Name = "New Strat", DefinitionJson = ValidJson };

			await _service.CreateCustomStrategyAsync(dto, "user1");

			Assert.That(_strategies.Count, Is.EqualTo(1));
			Assert.That(_strategies[0].Name, Is.EqualTo("New Strat"));
			Assert.That(_strategies[0].UserId, Is.EqualTo("user1"));

		}

		[Test]
		public void CreateCustomStrategyAsync_InvalidJson_ShouldThrowException()
		{
			var dto = new StrategyDto { Name = "Bad Strat", DefinitionJson = InvalidJson };

			var ex = Assert.ThrowsAsync<Exception>(async () =>
				await _service.CreateCustomStrategyAsync(dto, "user1"));

			Assert.That(ex.Message, Does.Contain("Invalid Strategy Format"));
			Assert.That(_strategies, Is.Empty);
			_repoMock.Verify(r => r.AddAsync(It.IsAny<TradingStrategy>()), Times.Never);
		}

		[Test]
		public async Task UpdateCustomStrategyAsync_ValidData_ShouldUpdateAndSave()
		{
			_strategies.Add(new TradingStrategy { Id = 1, UserId = "user1", Name = "Old", DefinitionJson = ValidJson });

			var dto = new StrategyDto { Name = "New", DefinitionJson = ValidJson };

			await _service.UpdateCustomStrategyAsync(1, dto, "user1");

			Assert.That(_strategies[0].Name, Is.EqualTo("New"));
		}

		[Test]
		public void UpdateCustomStrategyAsync_WrongUser_ShouldThrowUnauthorized()
		{
			_strategies.Add(new TradingStrategy { Id = 1, UserId = "user1", Name = "Old" });

			var dto = new StrategyDto { Name = "New", DefinitionJson = ValidJson };

			Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
				await _service.UpdateCustomStrategyAsync(1, dto, "user2"));
		}

		[Test]
		public async Task DeleteStrategyAsync_ValidUser_ShouldDeleteAndSave()
		{
			_strategies.Add(new TradingStrategy { Id = 1, UserId = "user1" });
			_strategies.Add(new TradingStrategy { Id = 2, UserId = "user2" });

			await _service.DeleteStrategyAsync(1, "user1");

			Assert.That(_strategies.Count, Is.EqualTo(1));
			Assert.That(_strategies[0].Id, Is.EqualTo(2));

			_repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
		}

		[Test]
		public void DeleteStrategyAsync_WrongUser_ShouldThrowUnauthorized()
		{
			_strategies.Add(new TradingStrategy { Id = 1, UserId = "user1" });

			Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
				await _service.DeleteStrategyAsync(1, "user2"));

			Assert.That(_strategies.Count, Is.EqualTo(1));
			_repoMock.Verify(r => r.DeleteAsync(It.IsAny<object>()), Times.Never);
		}
	}
}