using AssetInsight.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
			var comments = await commentService.GetRootCommentsPaginated(postId, pageIndex);
			if (!comments.Items.Any()) return NoContent();

			return PartialView("_CommentListPartial", comments.Items);
		}

		public async Task<IActionResult> GetReplies(Guid parentId)
		{
			var replies = await commentService.GetRepliesByParentId(parentId);
			return PartialView("_CommentListPartial", replies);
		}
	}
}
