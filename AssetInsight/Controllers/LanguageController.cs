using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AssetInsight.Controllers
{
	public class LanguageController : Controller
	{
		[HttpPost]
		[IgnoreAntiforgeryToken]
		public IActionResult SetLanguage(string culture, string returnUrl)
		{
			Response.Cookies.Append(
				CookieRequestCultureProvider.DefaultCookieName,
				CookieRequestCultureProvider.MakeCookieValue(
					new RequestCulture(culture)),
				new CookieOptions
				{					
					HttpOnly = true,
					Secure = true,
					IsEssential = true,
				}
			);

			return LocalRedirect(returnUrl);
		}
	}
}
