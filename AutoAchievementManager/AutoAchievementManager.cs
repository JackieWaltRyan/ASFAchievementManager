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
internal sealed class AutoAchievementManager: IBot, IBotCardsFarmerInfo, IBotCommand2, IBotConnection, IBotCustomMachineInfoProvider, IBotFriendRequest, IBotIdentity, IBotMessage, IBotModules, IBotSteamClient, IBotTradeOffer2, IBotTradeOfferResults, IBotUserNotifications {
	public string Name => nameof(AutoAchievementManager);
	public string RepositoryName => "JackieWaltRyan/AutoAchievementManager";
	public Version Version => typeof(AutoAchievementManager).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnBotDestroy(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnLoaded: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotInit(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotInit: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotFarmingFinished(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotFarmingFinished: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotFarmingStarted(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotFarmingStarted: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotFarmingStopped(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotFarmingStopped: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotCommand(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotCommand: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotDisconnected(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotDisconnected: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task GetMachineInfoProvider(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: GetMachineInfoProvider: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotFriendRequest(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotFriendRequest: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnSelfPersonaState(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnSelfPersonaState: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotMessage(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotMessage: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotInitModules(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotInitModules: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotSteamCallbacksInit(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotSteamCallbacksInit: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotSteamHandlersInit(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotSteamHandlersInit: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotTradeOffer(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotTradeOffer: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotTradeOfferResults(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotTradeOfferResults: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}

	public Task OnBotUserNotifications(Bot bot) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: OnBotUserNotifications: " + bot.OwnedPackages.Count);

		return Task.CompletedTask;
	}
}
