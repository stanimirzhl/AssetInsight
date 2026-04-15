using AssetInsight.Core;
using AssetInsight.Core.DTOs.Comment;
using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace AssetInsight.Controllers
{
	public class CommentController : Controller
	{
		private readonly ILogger<CommentController> logger;
		private readonly ICommentService commentService;
		private readonly INotificationService notificationService;

		public CommentController(ILogger<CommentController> logger,
			ICommentService commentService,
			INotificationService notificationService)
		{
			this.logger = logger;
			this.commentService = commentService;
			this.notificationService = notificationService;
		}

		public async Task<IActionResult> GetMoreComments(Guid postId, int pageIndex, string sortBy)
		{
			var comments = await commentService.GetRootCommentsPaginated(postId, pageIndex, this.GetUserId(), sortBy);
			if (comments == null || !comments.Items.Any()) return NoContent();

			return PartialView("~/Views/Post/_CommentListPartial.cshtml", comments.Items);
		}

		public async Task<IActionResult> GetReplies(Guid parentId /*int skip = 0, int take = 5*/)
		{
			var replies = await commentService.GetRepliesByParentId(parentId, this.GetUserId()/*, skip, take*/);

			return PartialView("~/Views/Post/_CommentListPartial.cshtml", replies);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([FromBody] JsonElement json)
		{
			try
			{
				if (!json.TryGetProperty("content", out var contentElement) ||
					string.IsNullOrWhiteSpace(contentElement.GetString()))
				{
					return BadRequest("Comment content is required.");
				}

				string content = contentElement.GetString()!;

				if (!json.TryGetProperty("postId", out var postElement) ||
					!Guid.TryParse(postElement.GetString(), out Guid postId))
				{
					return BadRequest("Invalid Post ID.");
				}

				Guid? parentId = null;
				if (json.TryGetProperty("parentId", out var parentElement) &&
					parentElement.ValueKind != JsonValueKind.Null &&
					Guid.TryParse(parentElement.GetString(), out Guid parsedParentId))
				{
					parentId = parsedParentId;
				}

				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					var returnUrl = Url.Action("Details", "Post", new { id = postId });
					var loginUrl = Url.Page("/Account/Login", null, new { area = "Identity", ReturnUrl = returnUrl });
					return Json(new { loginUrl });
				}

				var savedCommentDto = await commentService.AddAsync(
					postId,
					content,
					userId,
					parentId);

				savedCommentDto.AuthorName = User.Identity.Name;

				if (parentId.HasValue)
				{
					var parentComment = await commentService.GetByIdAsync(parentId.Value);

					await notificationService.CreateNotification(
						parentComment.AuthorId,
						$"{User.Identity.Name} replied to your comment.",
						$"/Post/Details/{postId}");
				}

				return PartialView("~/Views/Post/_CommentPartial.cshtml", savedCommentDto);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error creating comment via JsonElement");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, [FromBody] JsonElement request)
		{
			try
			{
				if (!request.TryGetProperty("postId", out var postElement) ||
					!Guid.TryParse(postElement.GetString(), out Guid postId))
				{
					return BadRequest("Invalid Post ID.");
				}

				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					var returnUrl = Url.Action("Details", "Post", new { id = postId });
					var loginUrl = Url.Page("/Account/Login", null, new { area = "Identity", ReturnUrl = returnUrl });
					return Json(new { loginUrl });
				}

				CommentDto comment = await commentService.GetByIdAsync(id);

				if (comment.AuthorId != User.FindFirstValue(ClaimTypes.NameIdentifier))
					return Forbid();

				if (!request.TryGetProperty("content", out var contentElement) ||
					string.IsNullOrWhiteSpace(contentElement.GetString()))
				{
					return BadRequest("Comment content is required.");
				}

				string content = contentElement.GetString()!;

				await commentService.EditAsync(postId, id, content);
			}
			catch (NoEntityException ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error editing comment via JsonElement");
				return StatusCode(500, "Internal server error");
			}

			return Ok();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid commentId, Guid postId)
		{
			try
			{
				CommentDto comment = await commentService.GetByIdAsync(commentId);

				if (comment.AuthorId != User.FindFirstValue(ClaimTypes.NameIdentifier))
					return Forbid();

				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					var returnUrl = Url.Action("Details", "Post", new { id = postId });
					var loginUrl = Url.Page("/Account/Login", null, new { area = "Identity", ReturnUrl = returnUrl });
					return Json(new { loginUrl });
				}

				await commentService.DeleteAsync(postId, commentId);

			}
			catch (NoEntityException ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, ex.Message);
				return StatusCode(500, "Internal server error");
			}

			return Ok();
		}

		private string GetUserId()
		{
			return User.FindFirstValue(ClaimTypes.NameIdentifier);
		}
	}
}
