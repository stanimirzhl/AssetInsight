using AssetInsight.Core.DTOs.BackTest;
using AssetInsight.Core.DTOs.Stock;
using AssetInsight.Core.Implementations;
using AssetInsight.Core.StrategyEngine.Context;
using AssetInsight.Core.StrategyEngine.Nodes;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class BacktestServiceTests
	{
		private BacktestService _service;

		[SetUp]
		public void SetUp()
		{
			_service = new BacktestService();
		}

		private List<ChartDataPoint> GenerateMockHistory(params decimal[] prices)
		{
			var history = new List<ChartDataPoint>();
			var startDate = new DateTime(2023, 1, 1);

			for (int i = 0; i < prices.Length; i++)
			{
				history.Add(new ChartDataPoint
				{
					Date = startDate.AddDays(i),
					ClosePrice = prices[i]
				});
			}
			return history;
		}

		[Test]
		public void Run_EmptyHistory_ShouldReturnInitialBalance()
		{
			var strategy = new StrategyDefinition();

			var result = _service.Run(new List<ChartDataPoint>(), strategy, 1000m);

			Assert.That(result.FinalBalance, Is.EqualTo(1000m));
			Assert.That(result.Logs.First(), Does.Contain("No historical data"));
		}

		[Test]
		public void Run_ProfitableStrategy_ShouldIncreaseBalance()
		{
			var history = GenerateMockHistory(10m, 20m, 30m);

			var buyMock = new Mock<IStrategyNode>();
			buyMock.SetupSequence(n => n.Evaluate(It.IsAny<IndicatorContext>()))
				.Returns(true)  
				.Returns(false) 
				.Returns(false);

			var sellMock = new Mock<IStrategyNode>();
			sellMock.SetupSequence(n => n.Evaluate(It.IsAny<IndicatorContext>()))
				.Returns(false) 
				.Returns(true)  
				.Returns(false);

			var strategy = new StrategyDefinition { Buy = buyMock.Object, Sell = sellMock.Object };

			var result = _service.Run(history, strategy, 100m);

			Assert.That(result.FinalBalance, Is.EqualTo(200m));
			Assert.That(result.Logs.Count, Is.EqualTo(2));
			Assert.That(result.Logs[0], Does.Contain("BUY 10 shares at 10.00"));
			Assert.That(result.Logs[1], Does.Contain("SELL 10 shares at 20.00"));
		}

		[Test]
		public void Run_LosingStrategy_ShouldDecreaseBalance()
		{
			var history = GenerateMockHistory(50m, 25m);

			var buyMock = new Mock<IStrategyNode>();
			buyMock.SetupSequence(n => n.Evaluate(It.IsAny<IndicatorContext>())).Returns(true).Returns(false);

			var sellMock = new Mock<IStrategyNode>();
			sellMock.SetupSequence(n => n.Evaluate(It.IsAny<IndicatorContext>())).Returns(false).Returns(true);

			var strategy = new StrategyDefinition { Buy = buyMock.Object, Sell = sellMock.Object };

			var result = _service.Run(history, strategy, 100m);

			Assert.That(result.FinalBalance, Is.EqualTo(50m));
		}

		[Test]
		public void Run_HoldUntilEnd_ShouldCalculateFinalValueCorrectly()
		{
			var history = GenerateMockHistory(10m, 50m);

			var buyMock = new Mock<IStrategyNode>();
			buyMock.SetupSequence(n => n.Evaluate(It.IsAny<IndicatorContext>())).Returns(true).Returns(false);

			var strategy = new StrategyDefinition { Buy = buyMock.Object, Sell = null };

			var result = _service.Run(history, strategy, 100m);

			Assert.That(result.FinalBalance, Is.EqualTo(500m));
		}

		[Test]
		public void PrecomputeIndicators_UnsupportedIndicator_ShouldThrowException()
		{
			var history = GenerateMockHistory(10m, 20m);

			var badNode = new ConditionNode
			{
				Indicator = "MAGIC",
				Period = 14
			};

			var strategy = new StrategyDefinition { Buy = badNode };

			var ex = Assert.Throws<Exception>(() => _service.Run(history, strategy, 100m));

			Assert.That(ex.Message, Does.Contain("Indicator MAGIC is not supported"));
		}

		[Test]
		public void PrecomputeIndicators_ValidIndicators_ShouldNotThrow()
		{
			var history = GenerateMockHistory(Enumerable.Repeat(10m, 20).ToArray());

			var groupNode = new GroupNode
			{
				Children = new List<IStrategyNode>
				{
				    new ConditionNode { Indicator = "SMA", Period = 10, Operator = ">", Value = 5 },
					new ConditionNode { Indicator = "PRICE", Period = 1, Operator = "<", CompareIndicator = "RSI", ComparePeriod = 14 }
				}
			};

			var strategy = new StrategyDefinition { Buy = groupNode };

			Assert.DoesNotThrow(() => _service.Run(history, strategy, 100m));
		}
	}
}