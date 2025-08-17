using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using T3MenuSharedApi;

namespace SLAYER_Duel;
// Used these to remove compile warnings
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8619

public partial class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    // Commands
    [ConsoleCommand("duel", "Open Chat Menu of Player Duel Settings")]
	public void PlayerDuelSettings(CCSPlayerController? player, CommandInfo command)
	{
        if (!Config.PluginEnabled || player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)return;

        PlayerDuelSettingsMenu(player);
    }

    [ConsoleCommand("duel_settings", "Open Chat Menu of Duel Settings")]
	[RequiresPermissions("@css/root")]
	public void DuelSettings(CCSPlayerController? player, CommandInfo command)
	{
        if (!Config.PluginEnabled || player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
		{
			return;
		}
        DuelSettingsMenu(player);
    }
}