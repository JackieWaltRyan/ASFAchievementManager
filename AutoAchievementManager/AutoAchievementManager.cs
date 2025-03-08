using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Localization;
using SteamKit2;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Globalization;
using ArchiSteamFarm.Web.GitHub.Data;
using ArchiSteamFarm.Web.GitHub;

namespace AutoAchievementManager;

[Export(typeof(IPlugin))]
internal sealed class AutoAchievementManager : IBotSteamClient, IBotCommand2, IASF, IGitHubPluginUpdates, IBotModules {
	private static readonly ConcurrentDictionary<Bot, AchievementHandler> AchievementHandlers = new();
	public string Name => nameof(AutoAchievementManager);
	public Version Version => typeof(AutoAchievementManager).Assembly.GetName().Version ?? new Version("0");
	public bool isEnabledAutoFarm;

	public static CultureInfo? AchievementsCulture { get; private set; }

	public string RepositoryName => "JackieWaltRyan/AutoAchievementManager";

	public async Task<Uri?> GetTargetReleaseURL(Version asfVersion, string asfVariant, bool asfUpdate, bool stable, bool forced) {
		ArgumentNullException.ThrowIfNull(asfVersion);
		ArgumentException.ThrowIfNullOrEmpty(asfVariant);

		if (string.IsNullOrEmpty(RepositoryName)) {
			ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, nameof(RepositoryName)));

			return null;
		}

		ReleaseResponse? releaseResponse = await GitHubService.GetLatestRelease(RepositoryName, stable).ConfigureAwait(false);

		if (releaseResponse == null) {
			return null;
		}

		Version newVersion = new(releaseResponse.Tag);

		if (!(Version.Major == newVersion.Major && Version.Minor == newVersion.Minor && Version.Build == newVersion.Build) && !(asfUpdate || forced)) {
			ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, "New {0} plugin version {1} is only compatible with latest ASF version", Name, newVersion));
			return null;
		}


		if (Version >= newVersion & !forced) {
			ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateNotFound, Name, Version, newVersion));

			return null;
		}

		if (releaseResponse.Assets.Count == 0) {
			ASF.ArchiLogger.LogGenericWarning(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateNoAssetFound, Name, Version, newVersion));

			return null;
		}

		ReleaseAsset? asset = await ((IGitHubPluginUpdates) this).GetTargetReleaseAsset(asfVersion, asfVariant, newVersion, releaseResponse.Assets).ConfigureAwait(false);

		if ((asset == null) || !releaseResponse.Assets.Contains(asset)) {
			ASF.ArchiLogger.LogGenericWarning(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateNoAssetFound, Name, Version, newVersion));

			return null;
		}

		ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateFound, Name, Version, newVersion));

		return asset.DownloadURL;
	}

	private static readonly char[] Separator = [','];

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo("AutoAchievementManager Plugin Loaded.");
		return Task.CompletedTask;
	}

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		switch (args.Length) {
			case 0:
				bot.ArchiLogger.LogNullError(null, nameof(args));

				return null;
			case 1:
				return args[0].ToUpperInvariant() switch {
					_ => null,
				};
			default:
				return args[0].ToUpperInvariant() switch {
					"ALIST" when args.Length > 2 => await ResponseAchievementList(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false),
					"ALIST" => await ResponseAchievementList(access, bot, args[1]).ConfigureAwait(false),
					"ASET" when args.Length > 3 => await ResponseAchievementSet(access, steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), true).ConfigureAwait(false),
					"ASET" when args.Length > 2 => await ResponseAchievementSet(access, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), true).ConfigureAwait(false),
					"ARESET" when args.Length > 3 => await ResponseAchievementSet(access, steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), false).ConfigureAwait(false),
					"ARESET" when args.Length > 2 => await ResponseAchievementSet(access, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), false).ConfigureAwait(false),
					_ => null,
				};
		}
	}

	public Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		if (additionalConfigProperties == null) {
            ASF.ArchiLogger.LogGenericError("AutoAchievementManager: AutoFarm: additionalConfigPropertiesNotFound");
			return Task.CompletedTask;
		}

		foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
			switch (configProperty.Key) {
				case "AchievementsAutoFarm" when (configProperty.Value.ValueKind == JsonValueKind.True || configProperty.Value.ValueKind == JsonValueKind.False): {
					isEnabledAutoFarm = configProperty.Value.GetBoolean();

	                ASF.ArchiLogger.LogGenericInfo("AutoAchievementManager: AutoFarm: " + isEnabledAutoFarm.ToString());
					break;
				}
			}
		}

        return Task.CompletedTask;
	}

	public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) {
		callbackManager.Subscribe<SteamApps.LicenseListCallback>(callback => OnLicenseList(bot));

		return Task.CompletedTask;
	}

	private async void OnLicenseList(Bot bot) {
        if (bot == null) {
			ASF.ArchiLogger.LogGenericError("AutoAchievementManager: AutoFarm: BotNotFound");
			return;
		}
  
		if (isEnabledAutoFarm) {
            #pragma warning disable CA2000
	            Timer RefreshTimer;
	  
				await AchievementsAutoFarm(bot).ConfigureAwait(false);

				int CollectTimeout = 60 * 60 * 1000;

				RefreshTimer = new Timer(async e => await AchievementsAutoFarm(bot).ConfigureAwait(false), null, CollectTimeout, CollectTimeout);
            #pragma warning restore CA2000
		}

		return;
	}

	public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) => Task.CompletedTask;

	public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot) {
		AchievementHandler currentBotAchievementHandler = new();
		_ = AchievementHandlers.TryAdd(bot, currentBotAchievementHandler);
		return Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>([currentBotAchievementHandler]);
	}

	//Responses

	private static async Task AchievementsAutoFarm(Bot bot) {
		if (bot == null) {
			ASF.ArchiLogger.LogGenericError("AutoAchievementManager: AutoFarm: BotNotFound");
			return;
		}

		if (bot.OwnedPackages.Count == 0) {
			ASF.ArchiLogger.LogGenericError("AutoAchievementManager: AutoFarm: NoAppsFound");
			return;
		}

		if (ASF.GlobalDatabase == null) {
			ASF.ArchiLogger.LogGenericError("AutoAchievementManager: AutoFarm: GlobalDatabaseNotFound");
			return;
		}

		var ownedPackageIDs = bot.OwnedPackages.Keys.ToHashSet();
		var ownedAppIDs = ASF.GlobalDatabase!.PackagesDataReadOnly.Where(x => ownedPackageIDs.Contains(x.Key) && x.Value.AppIDs != null).SelectMany(x => x.Value.AppIDs!).ToHashSet().ToList();
		
		ASF.ArchiLogger.LogGenericInfo("AutoAchievementManager: AutoFarm: Найдено игр: " + ownedAppIDs.Count);

		if (!AchievementHandlers.TryGetValue(bot, out AchievementHandler? achievementHandler)) {
			ASF.ArchiLogger.LogGenericWarning(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(AchievementHandlers)));
			return;
		}

		if (achievementHandler == null) {
			ASF.ArchiLogger.LogNullError(achievementHandler);
			return;
		}

		string achievementNumbers = "*";

		foreach (uint gameID in ownedAppIDs) {
			string appid = gameID.ToString(CultureInfo.InvariantCulture);

			if (!uint.TryParse(appid, out uint appId)) {
				ASF.ArchiLogger.LogGenericWarning(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(appId)));
				return;
			}

			string results = await Task.Run(() => achievementHandler.GetAchievements(bot, appId)).ConfigureAwait(false);

			if (results.Contains("u274C", StringComparison.OrdinalIgnoreCase)) {
				HashSet<uint> achievements = [];

				string[] achievementStrings = achievementNumbers.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

				if (!achievementNumbers.Equals("*", StringComparison.Ordinal)) {
					foreach (string achievement in achievementStrings) {
						if (!uint.TryParse(achievement, out uint achievementNumber) || (achievementNumber == 0)) {
							ASF.ArchiLogger.LogGenericWarning(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, achievement));
							return;
						}

						_ = achievements.Add(achievementNumber);
					}

					if (achievements.Count == 0) {
						return;
					}
				}

				ASF.ArchiLogger.LogGenericInfo(await Task.Run(() => achievementHandler.SetAchievements(bot, appId, achievements, true)).ConfigureAwait(false));
                return;
			}
		}
	}

	private static async Task<string?> ResponseAchievementList(EAccess access, Bot bot, string appids) {
		if (access < EAccess.Master) {
			return null;
		}

		string[] gameIDs = appids.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

		if (gameIDs.Length == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(gameIDs)));
		}
		if (AchievementHandlers.TryGetValue(bot, out AchievementHandler? achievementHandler)) {
			if (achievementHandler == null) {
				bot.ArchiLogger.LogNullError(achievementHandler);
				return null;
			}

			HashSet<uint> gamesToGetAchievements = [];

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, nameof(gameID)));
				}

				_ = gamesToGetAchievements.Add(gameID);
			}


			IList<string> results = await Utilities.InParallel(gamesToGetAchievements.Select(appID => Task.Run(() => achievementHandler.GetAchievements(bot, appID)))).ConfigureAwait(false);

			List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? bot.Commands.FormatBotResponse(string.Join(Environment.NewLine, responses)) : null;

		} else {

			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(AchievementHandlers)));
		}

	}

	private static async Task<string?> ResponseAchievementList(EAccess access, ulong steamID, string botNames, string appids) {

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames));
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementList(Commands.GetProxyAccess(bot, access, steamID), bot, appids))).ConfigureAwait(false);

		List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}


	private static async Task<string?> ResponseAchievementSet(EAccess access, Bot bot, string appid, string achievementNumbers, bool set = true) {
		if (access < EAccess.Master) {
			return null;
		}

		if (string.IsNullOrEmpty(achievementNumbers)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorObjectIsNull, nameof(achievementNumbers)));
		}
		if (!uint.TryParse(appid, out uint appId)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(appId)));
		}

		if (!AchievementHandlers.TryGetValue(bot, out AchievementHandler? achievementHandler)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(AchievementHandlers)));
		}

		if (achievementHandler == null) {
			bot.ArchiLogger.LogNullError(achievementHandler);
			return null;
		}

		HashSet<uint> achievements = [];

		string[] achievementStrings = achievementNumbers.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

		if (!achievementNumbers.Equals("*", StringComparison.Ordinal)) {
			foreach (string achievement in achievementStrings) {
				if (!uint.TryParse(achievement, out uint achievementNumber) || (achievementNumber == 0)) {
					return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, achievement));
				}

				_ = achievements.Add(achievementNumber);
			}
			if (achievements.Count == 0) {
				return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, "Achievements list"));
			}
		}
		return bot.Commands.FormatBotResponse(await Task.Run(() => achievementHandler.SetAchievements(bot, appId, achievements, set)).ConfigureAwait(false));
	}

	private static async Task<string?> ResponseAchievementSet(EAccess access, ulong steamID, string botNames, string appid, string achievementNumbers, bool set = true) {

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames));
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementSet(Commands.GetProxyAccess(bot, access, steamID), bot, appid, achievementNumbers, set))).ConfigureAwait(false);

		List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		if (additionalConfigProperties != null) {
			foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "AchievementsCulture" when configProperty.Value.ValueKind == JsonValueKind.String: {
						string configCulture = configProperty.Value.ToString();
						try {
							AchievementsCulture = CultureInfo.CreateSpecificCulture(configCulture);
						} catch (Exception) {
							AchievementsCulture = null;
							ASF.ArchiLogger.LogGenericError(Strings.ErrorInvalidCurrentCulture);
						}
						ASF.ArchiLogger.LogGenericInfo("Culture for achievement names was set to " + configCulture);
						break;
					}

					default:
						break;
				}
			}
		}
		return Task.CompletedTask;
	}
};
