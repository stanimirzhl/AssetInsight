// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using AssetInsight.Data.Models;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace AssetInsight.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<User> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {

			[Required(ErrorMessageResourceName = "UserNameEmail_Required", ErrorMessageResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			[StringLength(UserNameMaxLength, MinimumLength = UserNameMinLength, ErrorMessageResourceName = "UserNameEmail_StringLength", ErrorMessageResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			[Display(Name = "UserNameEmail",
                ResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			public string UserNameEmail { get; set; }

			[Required(ErrorMessageResourceName = "Password_Required", ErrorMessageResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			[DataType(DataType.Password)]
			[Display(Name = "Password",
				ResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			public string Password { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            //ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            User user = await _signInManager.UserManager.FindByNameAsync(Input.UserNameEmail)
                            ?? await _signInManager.UserManager.FindByEmailAsync(Input.UserNameEmail);

            if(user is null)
            {
				ModelState.AddModelError(string.Empty, Resources.Models.LoginModel.InputModel.InvalidLoginAttempt);
				return Page();
			}

			if (ModelState.IsValid)
            {
				var result = await _signInManager.PasswordSignInAsync(user, Input.Password, false, false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, Resources.Models.LoginModel.InputModel.InvalidLoginAttempt);
                    return Page();
                }
            }

            return Page();
        }
    }
}
