using AssetInsight.Core.DTOs.Comment;
using AssetInsight.Core.Interfaces;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AssetInsight.Controllers
{
	public class CommentController : Controller
	{
		private readonly ILogger<CommentController> logger;
		private readonly ICommentService commentService;

		public CommentController(ILogger<CommentController> logger,
			ICommentService commentService)
		{
			this.logger = logger;
			this.commentService = commentService;
		}

		public async Task<IActionResult> GetMoreComments(Guid postId, int pageIndex)
		{
			var comments = await commentService.GetRootCommentsPaginated(postId, pageIndex, this.GetUserId());
			if (comments == null || !comments.Items.Any()) return NoContent();

			return PartialView("~/Views/Post/_CommentListPartial.cshtml", comments.Items);
		}

		public async Task<IActionResult> GetReplies(Guid parentId)
		{
			var replies = await commentService.GetRepliesByParentId(parentId, this.GetUserId());

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

				return PartialView("~/Views/Post/_CommentPartial.cshtml", savedCommentDto);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error creating comment via JsonElement");
				return StatusCode(500, "Internal server error");
			}
		}

		private string GetUserId()
		{
			return User.FindFirstValue(ClaimTypes.NameIdentifier);
		}
	}
}
