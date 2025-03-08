using SteamKit2.Internal;

namespace AutoAchievementManager;

public class StoredResponse {
	public bool Success { get; set; }
	public CMsgClientGetUserStatsResponse? Response { get; set; }
}
