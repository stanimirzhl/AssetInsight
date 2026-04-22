using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Core.StrategyEngine.JSON_Options;
using AssetInsight.Core.StrategyEngine.Nodes;
using AssetInsight.Core.Trackers;
using AssetInsight.Data;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Azure.Identity;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.Json;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace AssetInsight.Extensions
{
	public static class ServiceCollectionExtensions
	{
		private static ILogger logger;

		public static void InitializeLogger(this IServiceProvider provider)
		{
			logger = provider
				.GetRequiredService<ILogger<IServiceCollection>>();
		}

		public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration config)
		{

			services.AddScoped<IPostService, PostService>();
			services.AddScoped<IImageService, ImageService>();
			services.AddScoped<ITagService, TagService>();
			services.AddScoped<IPostTagService, PostTagService>();
			services.AddScoped<IPostImageService, PostImageService>();
			services.AddScoped<ICommentService, CommentService>();
			services.AddScoped<IPostReactionService, PostReactionService>();
			services.AddScoped<ICommentReactionService, CommentReactionService>();	
			services.AddScoped<ISavedPostService, SavedPostService>();
			services.AddScoped<INotificationService, NotificationService>();
			services.AddScoped<IFollowService, FollowService>();
			services.AddScoped<IStockService, StockService>();
			services.AddScoped<IWatchListService, WatchListService>();
			services.AddScoped<IBacktestService, BacktestService>();
			services.AddScoped<IStrategyService, StrategyService>();
			services.AddSingleton<PresenceTracker>();

			services.AddMvc(options =>
				options
				.Filters
				.Add(new AutoValidateAntiforgeryTokenAttribute()));

			services.AddResponseCompression();
			services.AddHttpClient();


			if (string.IsNullOrEmpty(config["Cloudinary:CloudName"])
				|| string.IsNullOrEmpty(config["Cloudinary:ApiKey"]) || string.IsNullOrEmpty(config["Cloudinary:ApiSecret"]))
			{
				logger.LogError("Cloudinary configuration is missing. Cloudinary services will not be registered.");
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
			string connectionString = config.GetConnectionString("AssetInsightContextConnection") ??
				throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


			services.AddDbContext<AssetInsightDbContext>(options =>
				options.UseSqlServer(connectionString));

			services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

			return services;
		}

		public static IServiceCollection AddAzureKeyVaultSecrets(this IServiceCollection services, IConfigurationBuilder configBuilder)
		{
			const string keyVaultUrl = "https://external-logins.vault.azure.net/";

			try
			{
				configBuilder.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load secrets from Azure Key Vault.");
				return services;
			}

			return services;
		}

		public static void MapFinnhubSecret(this IConfiguration configuration)
		{
			var secretJson = configuration["Finnhub:Credentials"];

			if (string.IsNullOrWhiteSpace(secretJson))
			{
				logger.LogWarning("Finnhub secret not found in Key Vault.");
				return;
			}

			var secretObj = JObject.Parse(secretJson);

			configuration["Finnhub:ApiKey"] = secretObj["ApiKey"]?.ToString().Trim();
		}

		public static void MapCloudinarySecret(this IConfiguration configuration)
		{
			var secretJson = configuration["Cloudinary:Credentials"];
			if (string.IsNullOrWhiteSpace(secretJson))
				logger.LogWarning("Cloudinary secret not found in Key Vault.");

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
				logger.LogWarning("GoogleOAuth secret not found in Key Vault.");

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
				logger.LogWarning("FacebookOAuth secret not found in Key Vault.");

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
				logger.LogWarning("MicrosoftOAuth secret not found in Key Vault.");

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
			var authBuilder = services.AddAuthentication()
								.AddCookie();

			if (string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]) ||
				string.IsNullOrEmpty(configuration["Authentication:Google:ClientSecret"]))
			{
				logger.LogError("Google authentication configuration is missing.");
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
				logger.LogError("Facebook authentication configuration is missing.");
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
				logger.LogError("Microsoft authentication configuration is missing.");
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
				options.LoginPath = "/identity/account/login";
				options.LogoutPath = "/identity/account/logout";
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

			string[] roleNames = { "Admin", "Moderator", "User" };

			foreach (var roleName in roleNames)
			{
				var roleExists = await roleManager.RoleExistsAsync(roleName);
				if (!roleExists)
				{
					await roleManager.CreateAsync(new IdentityRole(roleName));
				}
			}

			var random = new Random();

			if (!db.Users.Any())
			{
				Console.WriteLine("Seeding Users... (This may take a minute due to password hashing)");

				string[] firstNames = { "Alex", "Jordan", "Taylor", "Casey", "Morgan", "Riley", "Sam", "Jamie", "Drew", "Avery", "Liam", "Emma", "Noah", "Olivia", "Ethan", "Ava" };
				string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Trade", "Bull", "Bear", "Stonks" };

				var seededUsers = new List<User>();

				var admin = new User { UserName = "admin", Email = "admin@assetinsight.com", FirstName = "System", LastName = "Admin"};
				await userManager.CreateAsync(admin, "Admin@123!");
				await userManager.AddToRoleAsync(admin, "Admin");
				seededUsers.Add(admin);

				for (int i = 1; i <= 60; i++)
				{
					var first = firstNames[random.Next(firstNames.Length)];
					var last = lastNames[random.Next(lastNames.Length)];
					var user = new User
					{
						UserName = $"{first.ToLower()}{last.ToLower()}{i}",
						Email = $"user{i}@test.com",
						FirstName = first,
						LastName = last,
						EmailConfirmed = true
					};

					var result = await userManager.CreateAsync(user, "User@123!");
					if (result.Succeeded)
					{
						await userManager.AddToRoleAsync(user, i % 15 == 0 ? "Moderator" : "User");
						seededUsers.Add(user);
					}
				}

				var tagNames = new[] { "Stocks", "Crypto", "Forex", "Options", "Tech", "Earnings", "Dividends", "DayTrading", "Investing", "Macro" };
				var tags = tagNames.Select(t => new Tag { Id = Guid.NewGuid(), Name = t }).ToList();
				db.Set<Tag>().AddRange(tags);
				await db.SaveChangesAsync();

				string[] postSubjects = { "AAPL", "TSLA", "BTC", "ETH", "The Market", "Inflation", "My Portfolio", "Options strategy", "Tech Stocks", "Gold" };
				string[] postVerbs = { "is breaking out!", "looks bearish.", "crushed earnings.", "is a strong buy.", "might crash soon.", "is showing a golden cross." };

				var posts = new List<Post>();
				for (int i = 0; i < 200; i++)
				{
					var author = seededUsers[random.Next(seededUsers.Count)];
					var post = new Post
					{
						Id = Guid.NewGuid(),
						Title = $"{postSubjects[random.Next(postSubjects.Length)]} {postVerbs[random.Next(postVerbs.Length)]}",
						Content = "This is a detailed analysis generated for the purpose of backtesting and community discussion. " +
								  "Looking at the RSI and MACD, we can clearly see convergence. What do you guys think about these levels?",
						AuthorId = author.Id,
						CreatedAt = DateTime.Now.AddDays(-random.Next(1, 300)).AddHours(-random.Next(1, 24)),
						IsLocked = i % 50 == 0
					};
					posts.Add(post);
				}
				db.Set<Post>().AddRange(posts);
				await db.SaveChangesAsync();

				var postTags = new List<PostTag>();
				foreach (var post in posts)
				{
					var numTags = random.Next(1, 4);
					var shuffledTags = tags.OrderBy(x => random.Next()).Take(numTags).ToList();
					foreach (var tag in shuffledTags)
					{
						postTags.Add(new PostTag { Id = Guid.NewGuid(), PostId = post.Id, TagId = tag.Id });
					}
				}
				db.Set<PostTag>().AddRange(postTags);

				var cloudinaryImages = new List<(string Url, string PublicId)>
				{
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776877160/photo-1579621970563-ebec7560ff3e_k5yw9i.webp", "photo-1579621970563-ebec7560ff3e_k5yw9i"),
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776877152/photo-1604594849809-dfedbc827105_mmj8sv.webp", "photo-1604594849809-dfedbc827105_mmj8sv"),
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776877097/photo-1579532537598-459ecdaf39cc_jrdpaw.webp", "photo-1579532537598-459ecdaf39cc_jrdpaw"),
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776877072/photo-1518186233392-c232efbf2373_wek45q.webp", "photo-1518186233392-c232efbf2373_wek45q"),
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776877046/photo-1590283603385-17ffb3a7f29f_y2w00c.webp", "photo-1590283603385-17ffb3a7f29f_y2w00c"),
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776877025/photo-1526304640581-d334cdbbf45e_sjcerj.webp", "photo-1526304640581-d334cdbbf45e_sjcerj"),
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776876979/photo-1460925895917-afdab827c52f_fdvcsc.webp", "photo-1460925895917-afdab827c52f_fdvcsc"),
					("https://res.cloudinary.com/dn2vdvskd/image/upload/v1776876849/photo-1611974789855-9c2a0a7236a3_iin2an.webp", "photo-1611974789855-9c2a0a7236a3_iin2an")
				};

				var postImages = new List<PostImage>();

				foreach (var post in posts.Where(p => random.Next(100) < 30))
				{
					var img = cloudinaryImages[random.Next(cloudinaryImages.Count)];

					postImages.Add(new PostImage
					{
						PostId = post.Id,
						ImgUrl = img.Url,
						PublicId = img.PublicId
					});
				}

				db.Set<PostImage>().AddRange(postImages);
				await db.SaveChangesAsync();

				var comments = new List<Comment>();
				string[] commentTexts = { "Totally agree with this.", "I'm not so sure.", "Great analysis, thanks!", "Following.", "What about the fed rates?", "Diamond hands!", "Buy the dip." };

				for (int i = 0; i < 500; i++)
				{
					var post = posts[random.Next(posts.Count)];
					var author = seededUsers[random.Next(seededUsers.Count)];

					var comment = new Comment
					{
						Id = Guid.NewGuid(),
						PostId = post.Id,
						AuthorId = author.Id,
						Content = commentTexts[random.Next(commentTexts.Length)],
						CreatedAt = post.CreatedAt.AddHours(random.Next(1, 48))
					};
					comments.Add(comment);
				}
				db.Set<Comment>().AddRange(comments);
				await db.SaveChangesAsync();

				var replies = new List<Comment>();
				for (int i = 0; i < 100; i++)
				{
					var parent = comments[random.Next(comments.Count)];
					var author = seededUsers[random.Next(seededUsers.Count)];
					replies.Add(new Comment
					{
						Id = Guid.NewGuid(),
						PostId = parent.PostId,
						ParentCommentId = parent.Id,
						AuthorId = author.Id,
						Content = "Good point!",
						CreatedAt = parent.CreatedAt.AddMinutes(random.Next(5, 120))
					});
				}
				db.Set<Comment>().AddRange(replies);

				var usedReactions = new HashSet<string>();
				for (int i = 0; i < 1000; i++)
				{
					var user = seededUsers[random.Next(seededUsers.Count)];
					var post = posts[random.Next(posts.Count)];
					var key = $"{user.Id}-{post.Id}";

					if (usedReactions.Add(key))
					{
						db.Set<PostReaction>().Add(new PostReaction { PostId = post.Id, UserId = user.Id, IsUpVote = random.Next(100) > 20 });

						if (random.Next(100) > 80) 
						{
							db.Set<SavedPost>().Add(new SavedPost { PostId = post.Id, UserId = user.Id });
						}
					}
				}

				var allComments = comments.Concat(replies).ToList();
				var usedCommentReactions = new HashSet<string>();

				for (int i = 0; i < 800; i++)
				{
					var user = seededUsers[random.Next(seededUsers.Count)];
					var comment = allComments[random.Next(allComments.Count)];
					var key = $"{user.Id}-{comment.Id}";

					if (usedCommentReactions.Add(key))
					{
						db.Set<CommentReaction>().Add(new CommentReaction
						{
							CommentId = comment.Id,
							UserId = user.Id,
							IsUpVote = random.Next(100) > 15
						});
					}
				}

				var usedFollows = new HashSet<string>();
				for (int i = 0; i < 300; i++)
				{
					var follower = seededUsers[random.Next(seededUsers.Count)];
					var followed = seededUsers[random.Next(seededUsers.Count)];
					if (follower.Id != followed.Id && usedFollows.Add($"{follower.Id}-{followed.Id}"))
					{
						db.Set<Follow>().Add(new Follow { FollowerId = follower.Id, FollowedUserId = followed.Id });
					}
				}

				var notifications = new List<Notification>();
				for (int i = 0; i < 200; i++)
				{
					var user = seededUsers[random.Next(seededUsers.Count)];
					notifications.Add(new Notification
					{
						ReceiverId = user.Id,
						Message = random.Next(2) == 0 ? "Someone liked your post!" : "New follower alert.",
						TargetUrl = "/",
						IsRead = random.Next(100) > 30,
						CreatedAt = DateTime.Now.AddHours(-random.Next(1, 100))
					});
				}
				db.Set<Notification>().AddRange(notifications);

				string[] symbols = { "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA", "BTC-USD", "ETH-USD", "SPY", "QQQ" };
				for (int i = 0; i < 150; i++)
				{
					var user = seededUsers[random.Next(seededUsers.Count)];
					var symbol = symbols[random.Next(symbols.Length)];

					if (!db.Set<WatchList>().Local.Any(w => w.UserId == user.Id && w.Symbol == symbol))
					{
						db.Set<WatchList>().Add(new WatchList { UserId = user.Id, Symbol = symbol, AddedOn = DateTime.Now.AddDays(-random.Next(1, 50)) });
					}
				}

				var globalStrategyDef = new StrategyDefinition
				{
					Buy = new ConditionNode { Indicator = "SMA", Period = 50, Operator = ">", CompareIndicator = "SMA", ComparePeriod = 200 },
					Sell = new ConditionNode { Indicator = "SMA", Period = 50, Operator = "<", CompareIndicator = "SMA", ComparePeriod = 200 }
				};

				db.Set<TradingStrategy>().Add(new TradingStrategy
				{
					Name = "Golden Cross (Global)",
					UserId = null,
					DefinitionJson = JsonSerializer.Serialize(globalStrategyDef, StrategyJsonOptions.Default),
					CreatedAt = DateTime.Now
				});

				for (int i = 0; i < 20; i++)
				{
					var user = seededUsers[random.Next(seededUsers.Count)];
					var userStrategyDef = new StrategyDefinition
					{
						Buy = new ConditionNode { Indicator = "RSI", Period = 14, Operator = "<=", Value = random.Next(20, 40) },
						Sell = new ConditionNode { Indicator = "RSI", Period = 14, Operator = ">=", Value = random.Next(60, 80) }
					};

					db.Set<TradingStrategy>().Add(new TradingStrategy
					{
						Name = $"My RSI Strat {i}",
						UserId = user.Id,
						DefinitionJson = JsonSerializer.Serialize(userStrategyDef, StrategyJsonOptions.Default),
						CreatedAt = DateTime.Now.AddDays(-random.Next(1, 100))
					});
				}

				await db.SaveChangesAsync();
			}

		}

	}

}
