using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json;
using T3MenuSharedApi;

namespace SLAYER_Duel;

public partial class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    private void PlayerDuelSettingsMenu(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !Config.PluginEnabled) return;

         var manager = GetMenuManager();
        if (manager == null) return;

        // Create menu
        var settingsMenu = manager.CreateMenu($"<font color='lime'>{Localizer["MenuSettings.Title"]}</font>", false, true, true, false);
        
        // Player Duel Rank
        settingsMenu.AddOption($"<font color='lime'>{Localizer["PlayerMenu.Rank"]}</font>", (p, option) =>
        {
            PlayerRankMenu(p);
        });

        // Change player settings
        settingsMenu.AddOption($"<font color='gold'>{Localizer["PlayerMenu.Settings"]}</font>", (p, option) =>
        {
            PlayerSettingsMenu(p);
        });

        manager!.OpenMainMenu(player, settingsMenu);
    }
    private void PlayerRankMenu(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !Config.PluginEnabled) return;

        var manager = GetMenuManager();
        if (manager == null) return;

        var stats = GetPlayerStats(player);
        int played = (stats?.Wins ?? 0) + (stats?.Losses ?? 0);

        // Create menu
        var settingsMenu = manager.CreateMenu($"<font color='lime'>{Localizer["PlayerMenu.RankTitle"]}</font>", false, true, true, true);

        settingsMenu.AddOption($"<font color='gold'>{Localizer["PlayerMenu.Played"]}</font> <font color='lime'>{played}</font>", (p, option) =>{}, true);
        settingsMenu.AddOption($"<font color='gold'>{Localizer["PlayerMenu.Wins"]}</font> <font color='lime'>{stats?.Wins ?? 0}</font>", (p, option) =>{}, true);
        settingsMenu.AddOption($"<font color='gold'>{Localizer["PlayerMenu.Losses"]}</font> <font color='lime'>{stats?.Losses ?? 0}</font>", (p, option) =>{}, true);

        settingsMenu.AddOption($"<font color='lime'>{Localizer["PlayerMenu.TopPlayers"]}</font>", (p, option) =>
        {
            TopPlayersMenu(p);
        });
        manager!.OpenSubMenu(player, settingsMenu);
    }

    private void TopPlayersMenu(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !Config.PluginEnabled) return;

        var manager = GetMenuManager();
        if (manager == null) return;

        var settingsMenu = manager.CreateMenu($"<font color='lime'>{Localizer["PlayerMenu.TopPlayers"]}</font>", false, true, true, true);

        GetTopPlayersSettings(Config.Duel_TopPlayersCount, (topPlayers) =>
        {
            int rank = 1;
            foreach (var playerSettings in topPlayers)
            {
                // Player rank and name
                settingsMenu.AddOption($"<font color='yellow'>#{rank}</font> <font color='white'>{playerSettings.PlayerName}</font>", (p, option) =>
                {
                    // Show player stats for the selected player (using playerSettings from the loop)
                    int totalPlayed = playerSettings.Wins + playerSettings.Losses;
                    double winRate = totalPlayed > 0 ? (double)playerSettings.Wins / totalPlayed * 100 : 0;
                    p.PrintToChat($" {ChatColors.DarkRed}---------------{Localizer["Chat.DuelStats"]}{ChatColors.DarkRed}---------------");
                    p.PrintToChat($" {ChatColors.Gold}Name: {ChatColors.Lime}{playerSettings.PlayerName}");
                    p.PrintToChat($" {ChatColors.Gold}Played: {ChatColors.Lime}{totalPlayed}");
                    p.PrintToChat($" {ChatColors.Gold}Wins: {ChatColors.Lime}{playerSettings.Wins}");
                    p.PrintToChat($" {ChatColors.Gold}Losses: {ChatColors.Lime}{playerSettings.Losses}");
                    p.PrintToChat($" {ChatColors.Gold}WinRate: {ChatColors.Lime}{winRate:F2}%");
                    p.PrintToChat($" {ChatColors.DarkRed}---------------{Localizer["Chat.DuelStats"]}{ChatColors.DarkRed}---------------");
                });

                rank++;
            }
            manager!.OpenSubMenu(player, settingsMenu);
        });

        
    }
    private void PlayerSettingsMenu(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !Config.PluginEnabled) return;

         var manager = GetMenuManager();
        if (manager == null) return;

        string SelectedOption = "";
        if(PlayerOption?.ContainsKey(player) == true && PlayerOption[player].Option == 1){SelectedOption = $"<font color='lime'>{Localizer["PlayerMenu.Accept"]}</font>";}
        if(PlayerOption?.ContainsKey(player) == true && PlayerOption[player].Option == 0){SelectedOption = $"<font color='red'>{Localizer["PlayerMenu.Decline"]}</font>";}
        if(PlayerOption?.ContainsKey(player) == true && PlayerOption[player].Option == -1){SelectedOption = $"<font color='gold'>{Localizer["PlayerMenu.Vote"]}</font>";}

        // Create menu
        var settingsMenu = manager.CreateMenu($"<font color='lime'>{Localizer["PlayerMenu.Title"]}</font>", false, true, true, true);

        // Show current vote option
        var currentOption = settingsMenu.AddOption($"<font color='yellow'>{Localizer["PlayerMenu.Selected"]}</font>: {SelectedOption}", (p, option) =>{}, true);
        // Ask for vote
        settingsMenu.AddOption($"<font color='yellow'>{Localizer["PlayerMenu.Vote"]}</font>", (p, option) =>
        {
            if(PlayerOption?.ContainsKey(player) == true) PlayerOption[player].Option = -1;
            SetPlayerDuelOption(player, -1);
            player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.Vote"]}");
            currentOption.Value.OptionDisplay = $"<font color='yellow'>{Localizer["PlayerMenu.Selected"]}</font> <font color='gold'>{Localizer["PlayerMenu.Vote"]}</font>";
            manager!.Refresh();
        });
        // always Accept
        settingsMenu.AddOption($"<font color='lime'>{Localizer["PlayerMenu.Accept"]}</font>", (p, option) =>
        {
            if(PlayerOption?.ContainsKey(player) == true) PlayerOption[player].Option = 1;
            SetPlayerDuelOption(player, 1);
            player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.Accept"]}");
            currentOption.Value.OptionDisplay = $"<font color='yellow'>{Localizer["PlayerMenu.Selected"]}</font> <font color='lime'>{Localizer["PlayerMenu.Accept"]}</font>";
            manager!.Refresh();
        });

        // always Reject
        settingsMenu.AddOption($"<font color='red'>{Localizer["PlayerMenu.Decline"]}</font>", (p, option) =>
        {
            if(PlayerOption?.ContainsKey(player) == true) PlayerOption[player].Option = 0;
            SetPlayerDuelOption(player, 0);
            player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.Decline"]}");
            currentOption.Value.OptionDisplay = $"<font color='yellow'>{Localizer["PlayerMenu.Selected"]}</font> <font color='red'>{Localizer["PlayerMenu.Decline"]}</font>";
            manager!.Refresh();
        });
        manager!.OpenSubMenu(player, settingsMenu);
    }
    private void DuelSettingsMenu(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !Config.PluginEnabled) return;

         var manager = GetMenuManager();
        if (manager == null) return;

        // Create menu
        var settingsMenu = manager.CreateMenu($"<font color='lime'>{Localizer["MenuSettings.Title"]}</font>", false, false, true, false);
        // Ask for vote
        settingsMenu.AddOption($"<font color='orange'>{Localizer["MenuSettings.TeleportSetT"]}</font>", (p, option) =>
        {
            if (Duel_Positions.ContainsKey(Server.MapName)) // Check If Map already exist in JSON file
            {
                Dictionary<string, string> MapData = Duel_Positions[Server.MapName]; // Get Map Settings
                MapData["T_Pos"] = $"{player.PlayerPawn.Value!.AbsOrigin}"; // Edit t_pos value
                File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions));
            }
            else // If Map not found in JSON file
            {
                // Save/add this in Global Variable
                Duel_Positions.Add(Server.MapName, new Dictionary<string, string>{{"T_Pos", $"{player.PlayerPawn.Value!.AbsOrigin}"}, {"CT_Pos", ""}});
                File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions)); // Saving Position in File
            }
            player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.TeleportSetT", player.PlayerPawn.Value!.AbsOrigin!]}");
        });
        // always Accept
        settingsMenu.AddOption($"<font color='blue'>{Localizer["MenuSettings.TeleportSetCT"]}</font>", (p, option) =>
        {
            if (Duel_Positions.ContainsKey(Server.MapName)) // Check If Map already exist in JSON file
        {
            Dictionary<string, string> MapData = Duel_Positions[Server.MapName]; // Get Map Settings
            MapData["CT_Pos"] = $"{player.PlayerPawn.Value!.AbsOrigin}"; // Edit ct_pos value
            File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions));
        }
        else // If Map not found in JSON file
        {
            // Save/add this in Global Veriable
            Duel_Positions.Add(Server.MapName, new Dictionary<string, string>{{"T_Pos", ""}, {"CT_Pos", $"{player.PlayerPawn.Value!.AbsOrigin}"}});
            File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions));
        }
		player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.DuelSettings.TeleportSetCT", player.PlayerPawn.Value!.AbsOrigin!]}");
        });

        // always Reject
        settingsMenu.AddOption($"<font color='red'>{Localizer["MenuSettings.TeleportDelete"]}</font>", (p, option) =>
        {
            if (Duel_Positions != null && Duel_Positions.ContainsKey(Server.MapName)) // Check If Map exist in JSON file
            {
                Duel_Positions.Remove(Server.MapName); // Delete Map Settings
                File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions)); // Update File
            }
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.DuelSettings.TeleportDelete"]}");
        });
        manager.OpenMainMenu(player, settingsMenu);
    }
}