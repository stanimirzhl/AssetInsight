using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Azure.Identity;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace AssetInsight.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration config)
		{
			using var serviceProvider = services.BuildServiceProvider();
			var log = serviceProvider.GetRequiredService<ILogger<IServiceCollection>>();

			services.AddScoped<IPostService, PostService>();

			services.AddMvc(options =>
				options
				.Filters
				.Add(new AutoValidateAntiforgeryTokenAttribute()));

			services.AddResponseCompression();


			if (string.IsNullOrEmpty(config["Cloudinary:CloudName"])
				|| string.IsNullOrEmpty(config["Cloudinary:ApiKey"]) || string.IsNullOrEmpty(config["Cloudinary:ApiSecret"]))
			{
				return services;
			}

			services.AddSingleton<Cloudinary>(sp =>
			{
				CloudinaryDotNet.Account account = new CloudinaryDotNet.Account(config["Cloudinary:CloudName"],
					config["Cloudinary:ApiKey"], config["Cloudinary:ApiSecret"]);
				return new Cloudinary(account);
			});

			return services;
		}

		public static IServiceCollection AddDbServices(this IServiceCollection services, IConfiguration config)
		{
			string connectionString = config.GetConnectionString("AssetInsightContextConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
			services.AddDbContext<AssetInsightDbContext>(options =>
				options.UseSqlServer(connectionString));

			services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

			return services;
		}

		public static IServiceCollection AddAzureKeyVaultSecrets(this IServiceCollection services, IConfigurationBuilder configBuilder)
		{
			const string keyVaultUrl = "https://external-logins.vault.azure.net/";

			configBuilder.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

			return services;
		}

		public static void MapCloudinarySecret(this IConfiguration configuration)
		{
			var secretJson = configuration["Cloudinary:Credentials"];
			if (string.IsNullOrWhiteSpace(secretJson))
				throw new InvalidOperationException("Cloudinary secret not found in Key Vault.");

			if (secretJson is null)
			{
				return;
			}

			var secretObj = JObject.Parse(secretJson);

			configuration["Cloudinary:CloudName"] = secretObj["CloudName"]?.ToString().Trim();
			configuration["Cloudinary:ApiKey"] = secretObj["ApiKey"]?.ToString().Trim();
			configuration["Cloudinary:ApiSecret"] = secretObj["ApiSecret"]?.ToString().Trim();
		}

		public static void MapGoogleOAuthSecret(this IConfiguration configuration)
		{
			var secretJson = configuration["GoogleOAuth:Credentials"];
			if (string.IsNullOrWhiteSpace(secretJson))
				throw new InvalidOperationException("GoogleOAuth secret not found in Key Vault.");

			if (secretJson is null)
			{
				return;
			}

			var secretObj = JObject.Parse(secretJson);

			configuration["Authentication:Google:ClientId"] = secretObj["ClientId"]?.ToString().Trim();
			configuration["Authentication:Google:ClientSecret"] = secretObj["ClientSecret"]?.ToString().Trim();
		}

		public static void MapFacebookOAuthSecret(this IConfiguration configuration)
		{
			var secretJson = configuration["FacebookOAuth:Credentials"];
			if (string.IsNullOrWhiteSpace(secretJson))
				throw new InvalidOperationException("FacebookOAuth secret not found in Key Vault.");

			if(secretJson is null)
			{
				return;
			}

			var secretObj = JObject.Parse(secretJson);

			configuration["Authentication:Facebook:ClientId"] = secretObj["ClientId"]?.ToString().Trim();
			configuration["Authentication:Facebook:ClientSecret"] = secretObj["ClientSecret"]?.ToString().Trim();
		}

		public static void MapMicrosoftOAuthSecret(this IConfiguration configuration)
		{
			var secretJson = configuration["MicrosoftOAuth:Credentials"];
			if (string.IsNullOrWhiteSpace(secretJson))
				 new InvalidOperationException("MicrosoftOAuth secret not found in Key Vault.");

			if (secretJson is null)
			{
				return;
			}

			var secretObj = JObject.Parse(secretJson);

			configuration["Authentication:Microsoft:ClientId"] = secretObj["ClientId"]?.ToString().Trim();
			configuration["Authentication:Microsoft:ClientSecret"] = secretObj["ClientSecret"]?.ToString().Trim();
		}

		public static IServiceCollection Authentication(this IServiceCollection services,
			IConfiguration configuration)
		{
			using var serviceProvider = services.BuildServiceProvider();
			var log = serviceProvider.GetRequiredService<ILogger<IServiceCollection>>();

			var authBuilder = services.AddAuthentication()
								.AddCookie();

			if (string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]) ||
				string.IsNullOrEmpty(configuration["Authentication:Google:ClientSecret"]))
			{
				log.LogError("Google authentication configuration is missing.");
			}
			else
			{
				authBuilder
					.AddGoogle(options =>
					{
						options.ClientId = configuration["Authentication:Google:ClientId"];
						options.ClientSecret = configuration["Authentication:Google:ClientSecret"];

						options.Events.OnRemoteFailure = HandleRemoteFailure;
					});
			}

			if (string.IsNullOrEmpty(configuration["Authentication:Facebook:ClientId"]) ||
				string.IsNullOrEmpty(configuration["Authentication:Facebook:ClientSecret"]))
			{
				log.LogError("Facebook authentication configuration is missing.");
			}
			else
			{
				authBuilder
					.AddFacebook(options =>
					{
						options.AppId = configuration["Authentication:Facebook:ClientId"];
						options.AppSecret = configuration["Authentication:Facebook:ClientSecret"];

						options.Events.OnRemoteFailure = HandleRemoteFailure;
					});
			}

			if (string.IsNullOrEmpty(configuration["Authentication:Microsoft:ClientId"]) ||
				string.IsNullOrEmpty(configuration["Authentication:Microsoft:ClientSecret"]))
			{
				log.LogError("Microsoft authentication configuration is missing.");
			}
			else
			{
				authBuilder
					.AddMicrosoftAccount(options =>
					{
						options.ClientId = configuration["Authentication:Microsoft:ClientId"];
						options.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"];

						options.Events.OnRemoteFailure = HandleRemoteFailure;
					});
			}


			return services;
		}

		private static Task HandleRemoteFailure(RemoteFailureContext context)
		{
			var logger = context.HttpContext.RequestServices
				.GetRequiredService<ILoggerFactory>()
				.CreateLogger("ExternalAuthentication");

			logger.LogWarning(context.Failure,
				"External authentication failed for provider {Provider}",
				context.Scheme.Name);

			context.Response.Redirect("/auth-failed");
			context.HandleResponse();
			return Task.CompletedTask;
		}

		public static IServiceCollection AddIdentityServices(this IServiceCollection services)
		{
			services.AddDefaultIdentity<User>(options =>
			{
				options.SignIn.RequireConfirmedAccount = false;
				options.Password.RequireDigit = true;
				options.Password.RequireNonAlphanumeric = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireUppercase = true;
				options.Password.RequiredLength = 5;
				options.User.RequireUniqueEmail = true;
			})
			.AddRoles<IdentityRole>()
			.AddEntityFrameworkStores<AssetInsightDbContext>();

			return services;
		}

		public static IServiceCollection AddAccountOptions(this IServiceCollection services)
		{
			services.ConfigureApplicationCookie(options =>
			{
				options.LoginPath = "/Account/Login";
				options.LogoutPath = "/Account/Logout";
				options.AccessDeniedPath = "/Account/AccessDenied";
				options.ReturnUrlParameter = "ReturnUrl";
			});

			return services;
		}

		public static IServiceCollection AddRouteOptions(this IServiceCollection services)
		{
			services.Configure<RouteOptions>(options =>
			 {
				 options.LowercaseUrls = true;
			 });

			return services;
		}
		public static IServiceCollection Localization(this IServiceCollection services)
		{
			services.AddLocalization(options =>
			   options.ResourcesPath = "Resources");

			services.AddControllersWithViews()
				.AddViewLocalization()
				.AddDataAnnotationsLocalization(options =>
				{
					options.DataAnnotationLocalizerProvider = (type, factory) =>
					{
						var typeName = type.DeclaringType != null
							? $"{type.DeclaringType.Name}.{type.Name}"
							: type.Name;

						return factory.Create(typeName, type.Assembly.GetName().Name);
					};
				});

			return services;
		}

		public static IApplicationBuilder UseAppLocalization(this IApplicationBuilder app)
		{
			var supportedCultures = new[]
			{
				new CultureInfo("en"),
				new CultureInfo("bg"),
				new CultureInfo("de"),
				new CultureInfo("fr"),
				new CultureInfo("es")
			};

			var localizationOptions = new RequestLocalizationOptions
			{
				DefaultRequestCulture = new RequestCulture("en"),
				SupportedCultures = supportedCultures,
				SupportedUICultures = supportedCultures,

				RequestCultureProviders = new List<IRequestCultureProvider>
				{
					 new CookieRequestCultureProvider(),
					 new AcceptLanguageHeaderRequestCultureProvider()
				}
			};

			return app.UseRequestLocalization(localizationOptions);
		}


		public static async Task ApplyDatabaseMigrations(this IHost app)
		{
			using IServiceScope scope = app.Services.CreateScope();
			AssetInsightDbContext db = scope.ServiceProvider.GetRequiredService<AssetInsightDbContext>();
			RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

			await db.Database.MigrateAsync();
		}


	}
}
