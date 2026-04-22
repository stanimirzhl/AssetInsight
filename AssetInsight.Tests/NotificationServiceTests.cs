using AssetInsight.Core.Implementations;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using AssetInsight.Hubs;
using Microsoft.AspNetCore.SignalR;
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
	public class NotificationServiceTests
	{
		private Mock<IRepository<Notification>> _repoMock;
		private Mock<IHubContext<NotificationHub>> _hubMock;
		private Mock<IClientProxy> _clientProxyMock;

		private List<Notification> _notifications;
		private NotificationService _service;

		[SetUp]
		public void SetUp()
		{
			_notifications = new List<Notification>();

			_repoMock = new Mock<IRepository<Notification>>();

			var mockSet = _notifications.AsQueryable().BuildMockDbSet();

			_repoMock.Setup(r => r.All()).Returns(mockSet.Object);

			_repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
				.Callback<Notification>(n => _notifications.Add(n))
				.Returns(Task.CompletedTask);

			_repoMock.Setup(r => r.SaveChangesAsync())
				.ReturnsAsync(1);

			_clientProxyMock = new Mock<IClientProxy>();

			var clientsMock = new Mock<IHubClients>();
			clientsMock
				.Setup(c => c.User(It.IsAny<string>()))
				.Returns(_clientProxyMock.Object);

			_hubMock = new Mock<IHubContext<NotificationHub>>();
			_hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

			_service = new NotificationService(_repoMock.Object, _hubMock.Object);
		}

		[Test]
		public async Task CreateNotification_ShouldAddNotification_AndSendHubMessage()
		{
			await _service.CreateNotification("user1", "Hello", "/test");

			Assert.That(_notifications.Count, Is.EqualTo(1));
			Assert.That(_notifications.First().Message, Is.EqualTo("Hello"));

			_clientProxyMock.Verify(
				c => c.SendCoreAsync(
					"ReceiveNotification",
					It.Is<object[]>(o => (string)o[0] == "Hello" && (string)o[1] == "/test"),
					default),
				Times.Once);
		}

		[Test]
		public async Task GetLatestAsync_ShouldReturn5Latest()
		{
			var userId = "u1";

			for (int i = 0; i < 10; i++)
			{
				_notifications.Add(new Notification
				{
					Id = i + 1,
					ReceiverId = userId,
					Message = $"msg{i}",
					CreatedAt = DateTime.UtcNow.AddMinutes(i)
				});
			}

			var result = await _service.GetLatestAsync(userId);

			Assert.That(result.Count, Is.EqualTo(5));
			Assert.That(result.First().Message, Is.EqualTo("msg9"));
		}

		[Test]
		public async Task GetAllByIdAsync_ShouldReturnAllUserNotifications()
		{
			_notifications.Add(new Notification { ReceiverId = "u1" });
			_notifications.Add(new Notification { ReceiverId = "u1" });
			_notifications.Add(new Notification { ReceiverId = "u2" });

			var result = await _service.GetAllByIdAsync("u1");

			Assert.That(result.Count, Is.EqualTo(2));
		}

		[Test]
		public async Task MarkAsReadAsync_ShouldMarkSingleNotification()
		{
			var n = new Notification { Id = 1, ReceiverId = "u1", IsRead = false };
			_notifications.Add(n);

			await _service.MarkAsReadAsync(1, "u1");

			Assert.That(n.IsRead, Is.True);
		}

		[Test]
		public async Task MarkAllAsReadAsync_ShouldMarkAllUnread()
		{
			_notifications.Add(new Notification { ReceiverId = "u1", IsRead = false });
			_notifications.Add(new Notification { ReceiverId = "u1", IsRead = false });
			_notifications.Add(new Notification { ReceiverId = "u1", IsRead = true });

			await _service.MarkAllAsReadAsync("u1");

			Assert.That(_notifications.Count(n => !n.IsRead), Is.EqualTo(0));
		}

		[Test]
		public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
		{
			_notifications.Add(new Notification { ReceiverId = "u1", IsRead = false });
			_notifications.Add(new Notification { ReceiverId = "u1", IsRead = false });
			_notifications.Add(new Notification { ReceiverId = "u1", IsRead = true });

			var result = await _service.GetUnreadCountAsync("u1");

			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public async Task MarkAsReadAsync_ShouldDoNothing_WhenNotificationNotFound()
		{
			_notifications.Add(new Notification { Id = 1, ReceiverId = "user2", IsRead = false });

			await _service.MarkAsReadAsync(1, "user1"); 
			await _service.MarkAsReadAsync(99, "user2");
			Assert.That(_notifications[0].IsRead, Is.False);
			_repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
		}

		[Test]
		public async Task GetLatestAsync_ShouldReturnAll_WhenFewerThan5Exist()
		{
			var userId = "u1";

			for (int i = 0; i < 3; i++)
			{
				_notifications.Add(new Notification
				{
					Id = i + 1,
					ReceiverId = userId,
					Message = $"msg{i}",
					CreatedAt = DateTime.UtcNow
				});
			}

			var result = await _service.GetLatestAsync(userId);

			Assert.That(result.Count, Is.EqualTo(3));
		}
	}
}