using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Globalization;

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

		public IActionResult GetCurrentCulture()
		{
			var cookie = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];

			string currentCulture = CultureInfo.CurrentCulture.Name;

			if (!string.IsNullOrEmpty(cookie))
			{
				var requestCulture = CookieRequestCultureProvider.ParseCookieValue(cookie);
				if (requestCulture != null)
				{
					currentCulture = requestCulture.Cultures.FirstOrDefault().Value;
				}
			}

			return Json(new { culture = currentCulture });
		}
	}
}
