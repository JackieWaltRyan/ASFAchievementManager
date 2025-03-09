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
using System.Timers;
using System.Threading.Tasks;
using System;
using System.Composition;
using System.Linq;
using System.Collections.Concurrent;
using System.Globalization;

namespace AutoAchievementManager;

[Export(typeof(IPlugin))]
internal sealed class AutoAchievementManager: IBotConnection {
	public string Name => nameof(AutoAchievementManager);
	public Version Version => typeof(AutoAchievementManager).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	private static Timer aTimer = new ();

	public Task OnLoaded() {
		return Task.CompletedTask;
	}

	public Task OnBotLoggedOn(Bot bot) {
		aTimer = new Timer(1000);
        aTimer.Elapsed += (sender, e) => OnAccountInfo(bot, "OnBotLoggedOn");
        aTimer.AutoReset = true;
        aTimer.Enabled = true;

		return Task.CompletedTask;
	}

	public Task OnBotDisconnected(Bot bot, EResult reason) {
		return Task.CompletedTask;
	}

	private static void OnAccountInfo(Bot bot, string message) {
		bot.ArchiLogger.LogGenericWarning("AutoAchievementManager: " + message + ": " + bot.OwnedPackages.Count);

		if (bot.OwnedPackages.Count != 0) {
			aTimer.Stop();
      		aTimer.Dispose();
		}
	}
}
