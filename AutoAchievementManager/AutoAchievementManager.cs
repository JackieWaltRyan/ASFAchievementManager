using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam.Cards;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Exchange;
using ArchiSteamFarm.Steam.Integration.Callbacks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Storage;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Localization;
using JetBrains.Annotations;
using SteamKit2;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Composition;
using System.Linq;
using System.Collections.Concurrent;
using System.Globalization;

namespace AutoAchievementManager;

[Export(typeof(IPlugin))]
internal sealed class AutoAchievementManager: IBotConnection, IBotSteamClient {
	public string Name => nameof(AutoAchievementManager);
	public Version Version => typeof(AutoAchievementManager).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnLoaded() {
		return Task.CompletedTask;
	}

	public Task OnBotLoggedOn(Bot bot) {
		Timer refreshTimer = new(e => OnAccountInfo(bot, "OnBotLoggedOn"), null, 1000, 1000);

		return Task.CompletedTask;
	}

	public Task OnBotDisconnected(Bot bot, EResult reason) {
		return Task.CompletedTask;
	}

	public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) {
		callbackManager.Subscribe<SteamUser.AccountInfoCallback>(callback => OnAccountInfo(bot, "AccountInfoCallback"));

		callbackManager.Subscribe<SteamApps.LicenseListCallback>(callback => OnAccountInfo(bot, "LicenseListCallback"));

		return Task.CompletedTask;
	}

	public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot) {
		return Task.FromResult((IReadOnlyCollection<ClientMsgHandler>?) null);
	}

	private static void OnAccountInfo(Bot bot, string message) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: " + message + ": " + bot.OwnedPackages.Count);
	}
}
