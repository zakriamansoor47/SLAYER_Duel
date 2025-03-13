using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
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
// kitsune Menu
using Menu;
using Menu.Enums;
using CounterStrikeSharp.API.Modules.Entities;

namespace SLAYER_Duel;
// Used these to remove compile warnings
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8619

public class SLAYER_DuelConfig : BasePluginConfig
{
    [JsonPropertyName("PluginEnabled")] public bool PluginEnabled { get; set; } = true;
    [JsonPropertyName("Duel_ForceStart")] public bool Duel_ForceStart { get; set; } = false;
    [JsonPropertyName("Duel_ShowMenuAt")] public int Duel_ShowMenuAt { get; set; } = 3;
    [JsonPropertyName("Duel_ShowDuelCounterIn")] public int Duel_ShowDuelCounterIn { get; set; } = 1;
    [JsonPropertyName("Duel_FreezePlayerOnMenuShown")] public bool Duel_FreezePlayerOnMenuShown { get; set; } = true;
    [JsonPropertyName("Duel_DrawLaserBeam")] public bool Duel_DrawLaserBeam { get; set; } = true;
    [JsonPropertyName("Duel_BotAcceptDuel")] public bool Duel_BotAcceptDuel { get; set; } = true;
    [JsonPropertyName("Duel_BotsDoDuel")] public bool Duel_BotsDoDuel { get; set; } = true;
    [JsonPropertyName("Duel_WinnerExtraHealth")] public int Duel_WinnerExtraHealth { get; set; } = 10;
    [JsonPropertyName("Duel_WinnerExtraSpeed")] public float Duel_WinnerExtraSpeed { get; set; } = 0.2f;
    [JsonPropertyName("Duel_WinnerExtraMoney")] public int Duel_WinnerExtraMoney { get; set; } = 1000;
    [JsonPropertyName("Duel_Time")] public int Duel_Time { get; set; } = 30;
    [JsonPropertyName("Duel_PrepTime")] public int Duel_PrepTime { get; set; } = 3;
    [JsonPropertyName("Duel_MinPlayers")] public int Duel_MinPlayers { get; set; } = 3;
    [JsonPropertyName("Duel_DrawPunish")] public int Duel_DrawPunish { get; set; } = 3;
    [JsonPropertyName("Duel_Beacon")] public bool Duel_Beacon { get; set; } = true;
    [JsonPropertyName("Duel_Teleport")] public bool Duel_Teleport { get; set; } = true;
    [JsonPropertyName("Duel_FreezePlayers")] public bool Duel_FreezePlayers { get; set; } = false;
    [JsonPropertyName("Duel_DuelSoundPath")] public string Duel_DuelSoundPath { get; set; } = "";
    [JsonPropertyName("Duel_Modes")] public List<DuelModeSettings> Duel_Modes { get; set; } = new List<DuelModeSettings>();
}
public class DuelModeSettings
{
    [JsonPropertyName("BulletTracers")] public bool BulletTracers { get; set; } = true;
    [JsonPropertyName("Name")] public string Name { get; set; } = "";
    [JsonPropertyName("Weapons")] public string Weapons { get; set; } = "weapon_knife";
    [JsonPropertyName("CMD")] public string CMD { get; set; } = "";
    [JsonPropertyName("CMD_End")] public string CMD_End { get; set; } = "";
    [JsonPropertyName("Health")] public int Health { get; set; } = 100;
    [JsonPropertyName("Armor")] public int Armor { get; set; } = 0;
    [JsonPropertyName("Helmet")] public int Helmet { get; set; } = 0;
    [JsonPropertyName("Speed")] public float Speed { get; set; } = 1.0f;
    [JsonPropertyName("Gravity")] public float Gravity { get; set; } = 1.0f;
    [JsonPropertyName("InfiniteAmmo")] public int InfiniteAmmo { get; set; } = 2;
    [JsonPropertyName("NoZoom")] public bool NoZoom { get; set; } = false;
    [JsonPropertyName("OnlyHeadshot")] public bool Only_headshot { get; set; } = false;
    [JsonPropertyName("DisableKnife")] public bool DisableKnife { get; set; } = false;
}
public class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    public override string ModuleName => "SLAYER_Duel";
    public override string ModuleVersion => "1.8.6";
    public override string ModuleAuthor => "SLAYER";
    public override string ModuleDescription => "1vs1 Duel at the end of the round with different weapons";
    public required SLAYER_DuelConfig Config {get; set;}
    public void OnConfigParsed(SLAYER_DuelConfig config)
    {
        Config = config;
    }
    public List<DuelModeSettings> GetDuelModes()
    {
        return Config.Duel_Modes;
    }
    
    public DuelModeSettings GetDuelModeByName(string modeName, StringComparer comparer)
    {
        return Config.Duel_Modes.FirstOrDefault(mode => comparer.Equals(mode.Name, modeName));
    }
    private SqliteConnection _connection = null!;

    Dictionary<CCSPlayerController, int> PlayerOption = new Dictionary<CCSPlayerController, int>();
    Dictionary<CCSPlayerController, bool> PlayerRescuingHostage = new Dictionary<CCSPlayerController, bool>();
    Dictionary<string, List<string>> playerSavedWeapons = new Dictionary<string, List<string>>();
    List<int> LastDuelNums = new List<int>();
    Dictionary<string, Dictionary<string, string>> Duel_Positions = new Dictionary<string, Dictionary<string, string>>();

    public bool[] g_Zoom = new bool[64];
    public bool g_BombPlanted = false;
    public bool g_DuelStarted = false;
    public bool g_PrepDuel = false;
    public bool g_DuelNoscope = false;
    public bool g_DuelHSOnly = false;
    public bool g_DuelDisableKnife = false;
    public bool g_DuelBullettracers = false;
    public bool g_IsDuelPossible = true;
    public bool g_IsVoteStarted = false;
    public bool[] PlayersDuelVoteOption = new bool[2];
    public float g_PrepTime;
    public float g_DuelTime;
    public int SelectedMode;
    public CCSPlayerController[] Duelist = new CCSPlayerController[2];
    public string SelectedDuelModeName = "";
    public string DuelWinner = "";
    public ConVar? mp_death_drop_gun = ConVar.Find("mp_death_drop_gun");
    public ConVar? mp_buytime = ConVar.Find("mp_buytime");
    public int mp_death_drop_gun_value;
    public float mp_buytime_value;
    // Timers
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_PrepDuel;
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_DuelTimer;
    Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer?> PlayerBeaconTimer = new Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer>();
    public KitsuneMenu kitsuneMenu { get; private set; } = null!;
    public override void Load(bool hotReload)
    {
        PlayerBeaconTimer?.Clear();
        PlayerRescuingHostage?.Clear();
        _connection = new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "Database/SLAYER_Duel.db")}");
        _connection.Open();
        if(hotReload)
        {
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                var steamId = player.AuthorizedSteamID?.SteamId64;
                if(PlayerOption == null)PlayerOption = new Dictionary<CCSPlayerController, int>(); // Initialize if null
                if(!PlayerOption?.ContainsKey(player) == true)PlayerOption[player] = -1; // Add the key if not present
                // Run in a separate thread to avoid blocking the main thread
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await _connection.QueryFirstOrDefaultAsync(@"SELECT `option` FROM `SLAYER_Duel` WHERE `steamid` = @SteamId;",
                        new
                        {
                            SteamId = steamId
                        });

                        // So we use `Server.NextFrame` to run it on the next game tick.
                        Server.NextFrame(() => 
                        {
                            PlayerOption[player] = Convert.ToInt32($"{result?.option ?? -1}");
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SLAYER_Duel] Error on PlayerConnectFull while retrieving player 'option': {ex.Message}");
                        Logger.LogError($"[SLAYER_Duel] Error on PlayerConnectFull while retrieving player 'option': {ex.Message}");
                    }
                });
            }
        }
        Task.Run(async () =>
        {
            await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `SLAYER_Duel` (
	                `steamid` UNSIGNED BIG INT NOT NULL,
	                `option` INT NOT NULL DEFAULT -1,
	                PRIMARY KEY (`steamid`));");
        });
        LoadPositionsFromFile();
        RegisterListener<Listeners.OnMapStart>((mapname) =>
        {
            LoadPositionsFromFile();
            if(t_PrepDuel != null)t_PrepDuel.Kill();
            if(t_DuelTimer != null)t_DuelTimer.Kill();
            PlayerBeaconTimer?.Clear();
            PlayerRescuingHostage?.Clear();
        });
        RegisterListener<Listeners.OnTick>(() =>
        {
            if(!Config.PluginEnabled)return;
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                if(g_PrepDuel)
                {
                    if(Config.Duel_ShowDuelCounterIn == 1)
                    {
                        player.PrintToCenterHtml
                        (
                            $"{Localizer["CenterHtml.DuelPrep"]}" +
                            $"{Localizer["CenterHtml.DuelPrepTime", g_PrepTime]}"
                        );
                    }
                    else
                    {
                        player.PrintToCenterAlert
                        (
                            $"{Localizer["CenterAlert.DuelPrep"]}" +
                            $"{Localizer["CenterAlert.DuelPrepTime", g_PrepTime]}"
                        );
                    }
                }
                if(g_DuelStarted)
                {
                    if(Config.Duel_ShowDuelCounterIn == 1)
                    {
                        player.PrintToCenterHtml
                        (
                            $"{Localizer["CenterHtml.DuelEnd"]}" +
                            $"{Localizer["CenterHtml.DuelEndTime", g_DuelTime]}"
                        );
                    }
                    else
                    {
                        player.PrintToCenterAlert
                        (
                            $"{Localizer["CenterAlert.DuelEnd"]}" +
                            $"{Localizer["CenterAlert.DuelEndTime", g_DuelTime]}"
                        );
                    }
                    if(g_DuelNoscope && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)OnTick(player);
                }
                
            }
        });
        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;
            if(!Config.PluginEnabled || player == null || !player.IsValid)return HookResult.Continue;
            
            var steamId = player.AuthorizedSteamID?.SteamId64;
            
            if(PlayerOption == null)PlayerOption = new Dictionary<CCSPlayerController, int>(); // Initialize if null
            if(!PlayerOption?.ContainsKey(player) == true)PlayerOption[player] = -1; // Add the key if not present
            // Run in a separate thread to avoid blocking the main thread
            Task.Run(async () =>
            {
                try
                {
                    var result = await _connection.QueryFirstOrDefaultAsync(@"SELECT `option` FROM `SLAYER_Duel` WHERE `steamid` = @SteamId;",
                    new
                    {
                        SteamId = steamId
                    });

                    // So we use `Server.NextFrame` to run it on the next game tick.
                    Server.NextFrame(() => 
                    {
                        PlayerOption[player] = Convert.ToInt32($"{result?.option ?? -1}");
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SLAYER_Duel] Error on PlayerConnectFull while retrieving player 'option': {ex.Message}");
                    Logger.LogError($"[SLAYER_Duel] Error on PlayerConnectFull while retrieving player 'option': {ex.Message}");
                }
            });
            return HookResult.Continue;
        });
        AddCommandListener("jointeam", (player, commandInfo) =>     // Ban Team Switch during duel for duelist
        {
            if(Config.PluginEnabled && player != null && player.IsValid && player == Duelist[0] || player == Duelist[1])
            {
                return HookResult.Handled;
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            Duelist = new CCSPlayerController[2];
            g_PrepDuel = false;
            g_DuelStarted = false;
            g_IsDuelPossible = true;
            g_IsVoteStarted = false;
            g_BombPlanted = false;
            if(t_PrepDuel != null)t_PrepDuel.Kill();
            if(t_DuelTimer != null)t_DuelTimer.Kill();
            foreach(var timer in PlayerBeaconTimer?.Values)
            {
                if(timer != null)timer.Kill();
            }
            PlayerBeaconTimer?.Clear();
            PlayerRescuingHostage?.Clear();
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            var player = @event.Userid;
            if(!Config.PluginEnabled || player == null || !player.IsValid || player.PlayerPawn.Value == null)return HookResult.Continue;

            // Kill player if he spawn during duel
            //if(g_PrepDuel || g_DuelStarted)player.PlayerPawn.Value.CommitSuicide(false, true);

            if(DuelWinner != "" && $"{player.AuthorizedSteamID?.SteamId64}" == DuelWinner) // Duel Winner
            {
                DuelWinner = ""; // Reset Duel Winner
                Server.NextFrame(() =>
                {
                    player.PlayerPawn.Value!.Health += Config.Duel_WinnerExtraHealth; // give extra Health to winner
                    player.InGameMoneyServices!.Account += Config.Duel_WinnerExtraMoney; // Give extra money to winner
                    Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
                    player.PlayerPawn.Value!.VelocityModifier += Config.Duel_WinnerExtraSpeed; // Give extra speed to winner
                });
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundEnd>((@event, info) =>
        {
            if(g_IsVoteStarted) 
            {
                foreach (var player in Duelist.Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected))
                {
                    MenuManager.CloseActiveMenu(player);
                    kitsuneMenu.ClearMenus(player);
                }
            }
            g_PrepDuel = false;
            g_DuelStarted = false;
            g_IsDuelPossible = false;
            g_IsVoteStarted = false;
            if(t_PrepDuel != null)t_PrepDuel.Kill();
            Duelist = new CCSPlayerController[2];
            Server.ExecuteCommand("mp_default_team_winner_no_objective -1"); // Set to default after duel

            return HookResult.Continue;
        });
        RegisterEventHandler<EventWeaponFire>((@event, info) =>
        {
            var player = @event.Userid;
            if(!Config.PluginEnabled || !g_DuelStarted || player == null || !player.IsValid)return HookResult.Continue;

            // Unlimited Reserve Ammo
            if(GetDuelItem(SelectedDuelModeName).InfiniteAmmo == 1)
            {
                ApplyInfiniteClip(player);
            }
            else if(GetDuelItem(SelectedDuelModeName).InfiniteAmmo == 2)
            {
                ApplyInfiniteReserve(player);
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventGrenadeThrown>((@event, info) =>
        {
            var player = @event.Userid;
            if(!Config.PluginEnabled || !g_DuelStarted || player == null || !player.IsValid)return HookResult.Continue;
            if(GetDuelItem(SelectedDuelModeName).InfiniteAmmo < 1)return HookResult.Continue;

            // Unlimited Grenade
            string weaponname = @event.Weapon;
            Server.NextFrame(() =>
            {
                player?.PlayerPawn.Value?.ItemServices?.As<CCSPlayer_ItemServices>().GiveNamedItem<CEntityInstance>($"weapon_{weaponname}");

            });
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerHurt>((@event, info) => 
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if(!Config.PluginEnabled || !g_DuelStarted || !g_DuelHSOnly && !g_DuelDisableKnife)return HookResult.Continue;
            if(player == null || attacker == null || !player.IsValid || !attacker.IsValid)return HookResult.Continue;
            // Some Checks to validate Attacker
            if (player.TeamNum == attacker.TeamNum && !(@event.DmgHealth > 0 || @event.DmgArmor > 0))return HookResult.Continue;

            if(g_DuelHSOnly && @event.Hitgroup != 1) // if headshot is enabled and bullet not hitting on Head
            {
                player.PlayerPawn.Value.Health += @event.DmgHealth; // add the dmg health to Normal health
                player.PlayerPawn.Value.ArmorValue += @event.DmgArmor; // Update the Armor as well
            }

            if(g_DuelDisableKnife && @event.Weapon.Contains("knife") || @event.Weapon.Contains("bayonet")) // if DisableKnife is Enabled then Disable Knife Damage
            {
                player.PlayerPawn.Value.Health += @event.DmgHealth; // add the dmg health to Normal health
                player.PlayerPawn.Value.ArmorValue += @event.DmgArmor; // Update the Armor as well
                attacker.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Duel.Knife"]}"); // Send Message to attacker
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventBombPlanted>((@event, info) =>
        {
            g_BombPlanted = true;
            if(!g_DuelStarted)
            {
                g_IsDuelPossible = false;
            }
            if(g_IsVoteStarted) 
            {
                foreach (var player in Duelist.Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected))
                {
                    MenuManager.CloseActiveMenu(player);
                    kitsuneMenu.ClearMenus(player);
                }
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventBombExploded>((@event, info) =>
        {
            g_BombPlanted = true;
            if(!g_DuelStarted)
            {
                g_IsDuelPossible = false;
            }
            if(g_IsVoteStarted) 
            {
                foreach (var player in Duelist.Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected))
                {
                    MenuManager.CloseActiveMenu(player);
                    kitsuneMenu.ClearMenus(player);
                }
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventHostageFollows>((@event, info) =>
        {
            var player = @event.Userid;
            if(player == null || !player.IsValid)return HookResult.Continue;

            PlayerRescuingHostage[player] = true; // Set Player is Rescuing Hostage

            return HookResult.Continue;
        });
        RegisterEventHandler<EventHostageRescued>((@event, info) =>
        {
            var player = @event.Userid;
            if(player == null || !player.IsValid)return HookResult.Continue;

            PlayerRescuingHostage[player] = false; // Set Player is not Rescuing Hostage

            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if(!Config.PluginEnabled || g_BombPlanted)return HookResult.Continue; // Plugin should be Enable
            int ctplayer = 0, tplayer = 0, totalplayers = 0;
            // Count Players in Both Team on Any Player Death
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !player.ControllingBot && (!PlayerRescuingHostage.ContainsKey(player) || PlayerRescuingHostage[player] == false)))
            {
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 2)tplayer++;
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 3)ctplayer++;
                //if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && g_DuelStarted)player.RemoveWeapons();
                totalplayers++;
            }
            CCSGameRules gamerules = GetGameRules();
            if(!g_IsDuelPossible)return HookResult.Continue;
            if(!gamerules.WarmupPeriod && Config.Duel_MinPlayers <= totalplayers && ctplayer == 1 && tplayer == 1) // 1vs1 Situation and its not warmup
            {
                if(Config.Duel_ForceStart) // If Force Start Duel is true
                {
                    PrepDuel();
                }
                else // if force start duel is false
                {
                    PlayersDuelVoteOption[0] = false; PlayersDuelVoteOption[1] = false;
                    foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && !player.ControllingBot))
                    {
                        // keep track of duelist
                        if(player.TeamNum == 2){Duelist[0] = player;}
                        if(player.TeamNum == 3){Duelist[1] = player;}
                        if(player.IsBot && Config.Duel_BotAcceptDuel) // Check Player is BOT and Bot allowed to Duel
                        {
                            if(player.TeamNum == 2)PlayersDuelVoteOption[0] = true;
                            else if(player.TeamNum == 3)PlayersDuelVoteOption[1] = true;
                            if(PlayersDuelVoteOption[0] && PlayersDuelVoteOption[1] && Config.Duel_BotsDoDuel)PrepDuel(); // Start Duel Between Bots if Duel_BotsDoDuel is Enabled in Config file
                        }
                        else if(player.IsBot && !Config.Duel_BotAcceptDuel) // Check Player is BOT and Bot is not allowed to Duel
                        {
                            if(player.TeamNum == 2)PlayersDuelVoteOption[0] = false;
                            else if(player.TeamNum == 3)PlayersDuelVoteOption[1] = false;
                        }
                        else // Voting
                        {
                            if(PlayerOption?.ContainsKey(player) == true && PlayerOption?[player] == 1) // if `1` is set in Database, then always accept duel without vote 
                            {
                                if(player.TeamNum == 2){PlayersDuelVoteOption[0] = true;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.T", player.PlayerName]}");}
                                if(player.TeamNum == 3){PlayersDuelVoteOption[1] = true;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.CT", player.PlayerName]}");}
                            }
                            else if(PlayerOption?.ContainsKey(player) == true && PlayerOption?[player] == 0) // if `0` is set in Database, then always decline duel without vote 
                            {
                                if(player.TeamNum == 2){PlayersDuelVoteOption[0] = false;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.T", player.PlayerName]}");}
                                if(player.TeamNum == 3){PlayersDuelVoteOption[1] = false;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.CT", player.PlayerName]}");}
                            }
                            else  // if `-1` is set in Database, then start vote 
                            {
                                g_IsVoteStarted = true;
                                if(Config.Duel_ShowMenuAt <= 1) // if Chat menu select in JSON file then show vote in Chat menu
                                {
                                    if(Config.Duel_FreezePlayerOnMenuShown)FreezePlayer(player);
                                    var DuelVote_Chat = new ChatMenu($"{Localizer["Menu.Title"]}");
                                    DuelVote_Chat.AddMenuOption($"{Localizer["Menu.Accept"]}", AcceptDuelVoteOption);
                                    DuelVote_Chat.AddMenuOption($"{Localizer["Menu.Decline"]}", DeclineDuelVoteOption);
                                    DuelVote_Chat.PostSelectAction = PostSelectAction.Close;
                                    MenuManager.OpenChatMenu(player, DuelVote_Chat);
                                }
                                else if(Config.Duel_ShowMenuAt == 2) // show vote in Center HTML menu
                                {
                                    if(Config.Duel_FreezePlayerOnMenuShown)FreezePlayer(player);
                                    var DuelVote_Center = new CenterHtmlMenu($"{Localizer["Menu.Title"]}", this);
                                    DuelVote_Center.AddMenuOption($"{Localizer["Menu.Accept"]}", AcceptDuelVoteOption);
                                    DuelVote_Center.AddMenuOption($"{Localizer["Menu.Decline"]}", DeclineDuelVoteOption);
                                    DuelVote_Center.PostSelectAction = PostSelectAction.Close;
                                    MenuManager.OpenCenterHtmlMenu(this, player, DuelVote_Center);
                                }
                                else  // otherwise show WASD vote menu in Center
                                {
                                    kitsuneMenu = new KitsuneMenu(this);
                                    if(kitsuneMenu == null)return HookResult.Continue;

                                    kitsuneMenu.ShowScrollableMenu(player, Localizer["Menu.Title"],
                                    [
                                        new MenuItem(MenuItemType.Button, [new MenuValue(Localizer["Menu.Accept"])]),
                                        new MenuItem(MenuItemType.Button, [new MenuValue(Localizer["Menu.Decline"])]),
                                    ], (buttons, menu, selected) =>
                                    {
                                        if (selected == null) return;
                                        if(buttons == MenuButtons.Select)
                                        {
                                            if(menu.Option == 0) // Accept
                                            {
                                                AcceptDuelVoteOption(player, null);
                                                kitsuneMenu.ClearMenus(player);
                                            }
                                            else if(menu.Option == 1) // Decline
                                            {
                                                DeclineDuelVoteOption(player, null);
                                                kitsuneMenu.ClearMenus(player);
                                            }
                                        }
                                    }, false, Config.Duel_FreezePlayerOnMenuShown, 5, disableDeveloper: true);
                                }
                            }
                            if(!g_IsVoteStarted) // if both players Duel Vote option is saved in Database then it means vote was not started for any player
                            {
                                if(PlayersDuelVoteOption[0] && PlayersDuelVoteOption[1]) // Both accepted duel
                                {
                                    Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.Both"]}");
                                    PrepDuel(); // Start Duel
                                }
                                else if(!PlayersDuelVoteOption[0] && !PlayersDuelVoteOption[1]) // Both rejected Duel
                                {
                                    Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.Both"]}");
                                }
                            }
                        }
                    }
                    
                }
            }
            else if(ctplayer == 0 || tplayer == 0)
            {
                g_DuelStarted = false;
                g_IsDuelPossible = false;
            }
            return HookResult.Continue;
        }, HookMode.Post);
    }
    
    private void AcceptDuelVoteOption(CCSPlayerController player, ChatMenuOption? option)
    {
        if (player == null || !player.IsValid || player.IsBot || !g_IsDuelPossible || g_BombPlanted || !g_IsVoteStarted || !Duelist.Contains(player))return;
        
        if(player.TeamNum == 2)
        {
            if(Config.Duel_FreezePlayerOnMenuShown)UnFreezePlayer(player);
            PlayersDuelVoteOption[0] = true;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.T", player.PlayerName]}");
        }
        else if(player.TeamNum == 3)
        {
            if(Config.Duel_FreezePlayerOnMenuShown)UnFreezePlayer(player);
            PlayersDuelVoteOption[1] = true;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.CT", player.PlayerName]}");
        }

        if(PlayersDuelVoteOption[0] && PlayersDuelVoteOption[1])
        {
            g_IsVoteStarted = false; // Both Accepted the Duel Vote, So no need to Exit Menu at Round End
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.Both"]}");
            PrepDuel();
        }
    }
    private void DeclineDuelVoteOption(CCSPlayerController player, ChatMenuOption? option)
    {
        if (player == null || player.IsValid == false || player.IsBot == true || !g_IsDuelPossible || g_BombPlanted || !g_IsVoteStarted || !Duelist.Contains(player))return;
        
        if(player.TeamNum == 2)
        {
            if(Config.Duel_FreezePlayerOnMenuShown)UnFreezePlayer(player);
            PlayersDuelVoteOption[0] = false;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.T", player.PlayerName]}");
        }
        else if(player.TeamNum == 3)
        {
            if(Config.Duel_FreezePlayerOnMenuShown)UnFreezePlayer(player);
            PlayersDuelVoteOption[1] = false;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.CT", player.PlayerName]}");
        }
        if(!PlayersDuelVoteOption[0] && !PlayersDuelVoteOption[1])
        {
            g_IsVoteStarted = false; // Both Rejected the Duel Vote, So no need to Exit Menu at Round End
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.Both"]}");
        }
    }
    public void RemoveAllWeaponsFromMap()
    {
        try
        {
            foreach (var weapon in Utilities.GetAllEntities().Where(weapon => weapon != null && weapon.IsValid && weapon.DesignerName.StartsWith("weapon_") || weapon.DesignerName.StartsWith("hostage_entity")))
            {
                weapon.Remove();
            }
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[SLAYER Duel] Error on Removing Weapon: {ex.Message}");
        }
    }
    
    public void PrepDuel()
    {
        if(g_PrepDuel)return;
        g_PrepDuel = true;
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 0))
        {
            
            if(!player.IsBot && Config.Duel_DuelSoundPath != "")PlaySoundOnPlayer(player, Config.Duel_DuelSoundPath); // Play Duel Sound to all players except Bots if any Given
            if(player.TeamNum > 1 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE) // Check Players who are in any Team and alive (only two duelist will be alive)
            {
                if(Config.Duel_Teleport)TeleportPlayer(player);
                if(Config.Duel_FreezePlayers)FreezePlayer(player);   // Freeze Player
                SavePlayerWeapons(player); // first save player weapons
                player.RemoveWeapons(); // then remove weapons from player
                if(Config.Duel_Beacon) // If Beacon Enabled
                {
                    // Initialize the dictionary entry if not present
                    if(PlayerBeaconTimer == null)PlayerBeaconTimer = new Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer?>(); // Initialize if null
                    if(!PlayerBeaconTimer?.ContainsKey(player) == true)PlayerBeaconTimer[player] = null; // Add the key if not present
                    if(PlayerBeaconTimer?[player] != null)PlayerBeaconTimer?[player]?.Kill(); // Kill Timer if running
                    // Start Beacon
                    PlayerBeaconTimer[player] = AddTimer(1.0f, ()=>
                    {
                        if(player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE || !g_DuelStarted && !g_PrepDuel)
                        {
                            if(PlayerBeaconTimer != null && PlayerBeaconTimer.ContainsKey(player) &&  PlayerBeaconTimer?[player] != null)PlayerBeaconTimer?[player]?.Kill(); // Kill Timer if player dies or leaves
                        } 
                        else DrawBeaconOnPlayer(player);
                    }, TimerFlags.REPEAT);
                }
            }
        }

        RemoveAllWeaponsFromMap(); // then remove all weapons from Map (this can also remove weapon from players but animation glitch)
       
        g_PrepTime = Config.Duel_PrepTime;
        g_DuelTime = Config.Duel_Time;
        
        Random DuelMode = new Random();
        do
        {
            SelectedMode = DuelMode.Next(0, Config.Duel_Modes.Count);
        } while(LastDuelNums.Count != 0 && LastDuelNums.Contains(SelectedMode) && LastDuelNums.Count < Config.Duel_Modes.Count - 1);
        LastDuelNums.Add(SelectedMode);
        if(LastDuelNums.Count == Config.Duel_Modes.Count-1)LastDuelNums.Clear();

        mp_buytime_value = mp_buytime.GetPrimitiveValue<float>();
        Server.ExecuteCommand("mp_buytime 0"); // Disable BuyZone during Duel

        t_PrepDuel = AddTimer(0.2f, PrepDuelTimer, TimerFlags.REPEAT); // start Duel Prepration Timer
    }
    public void PrepDuelTimer()
    {
        if (g_PrepTime <= 0.0f)
        {
            if(g_IsDuelPossible)
            {
                var SelectedModeName = Config.Duel_Modes.ElementAt(SelectedMode);
                SelectedDuelModeName = SelectedModeName.Name;
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.DarkRed}-----------------------------------------------------------------------");
                Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Duel.Started", SelectedModeName.Name]}");
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.DarkRed}-----------------------------------------------------------------------");
                StartDuel(SelectedModeName.Name);
                g_PrepDuel = false;
                g_DuelStarted = true;
                t_DuelTimer = AddTimer(0.2f, DuelStartedTimer, TimerFlags.REPEAT); 
            }
            t_PrepDuel?.Kill();
            return;
        }
        CreateLaserBeamBetweenPlayers(0.2f); // Create Laser Beam
        g_PrepTime = g_PrepTime - 0.2f;
    }
    public void StartDuel(string DuelModeName)
    {
        if(!g_IsDuelPossible || !Config.Duel_Modes.Contains(GetDuelItem(DuelModeName)))return;
        string[] weapons = GetDuelItem(DuelModeName)?.Weapons.Split(",");
        string[] Commands = GetDuelItem(DuelModeName)?.CMD.Split(",");
        
        g_DuelNoscope = GetDuelItem(DuelModeName).NoZoom;
        g_DuelHSOnly = GetDuelItem(DuelModeName).Only_headshot;
        g_DuelBullettracers = GetDuelItem(DuelModeName).BulletTracers;
        g_DuelDisableKnife = GetDuelItem(DuelModeName).DisableKnife;
        
        mp_death_drop_gun_value = mp_death_drop_gun.GetPrimitiveValue<int>(); // Get Convar Int value
        if(mp_death_drop_gun_value != 0)Server.ExecuteCommand("mp_death_drop_gun 0");
        Server.ExecuteCommand("mp_ignore_round_win_conditions 0");
        
        foreach(var cmd in Commands.Where(commands => commands != "")) // Execute Duel Start Commands
        {
            Server.ExecuteCommand(cmd);
        }
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            player.PlayerPawn.Value!.Health = GetDuelItem(DuelModeName).Health;
            player.PlayerPawn.Value!.VelocityModifier = GetDuelItem(DuelModeName).Speed;
            player.PlayerPawn.Value!.GravityScale *= GetDuelItem(DuelModeName).Gravity;
            if(GetDuelItem(DuelModeName).Helmet < 1)player.PlayerPawn.Value!.ArmorValue = GetDuelItem(DuelModeName).Armor;
            else player.GiveNamedItem("item_assaultsuit");
            foreach(var weapon in weapons) // Give Each Weapon
            {
                player.GiveNamedItem(weapon);
            }
            if(Config.Duel_FreezePlayers)UnFreezePlayer(player); // Unfreeze Player
        }
    }
    public void DuelStartedTimer()
    {
        float roundtimeleft = (GetGameRules().RoundStartTime + GetGameRules().RoundTime) - Server.CurrentTime;
        if(roundtimeleft <= 0.4f && roundtimeleft >= 0f)
        {
            Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
        }
        if(g_DuelTime <= 0f || !g_DuelStarted || !g_IsDuelPossible)
        {
            EndDuel();
            t_DuelTimer?.Kill();
            return;
        }
        CreateLaserBeamBetweenPlayers(0.2f); // Create Laser Beam
        g_DuelTime = g_DuelTime - 0.2f;
    }
    public void EndDuel()
    {
        g_IsDuelPossible = false;
        string Winner = "";
        bool IsCTWon = false;
        g_PrepDuel = false;
        g_DuelStarted = false;
        if(t_DuelTimer != null)t_DuelTimer?.Kill();
        CCSPlayerController? CT = null, T = null;
        int CTHealth = 0, THealth = 0;
        Random randomplayer = new Random();
        int killplayer = randomplayer.Next(0,2);
        
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 0 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && g_DuelTime <= 0f))
        {
            if(Config.Duel_DrawPunish == 1) // Kill Both if timer ends
            {
                Server.NextFrame(() => player.PlayerPawn.Value.CommitSuicide(false, true));
            }
            else if(Config.Duel_DrawPunish == 2) // Kill Random if timer ends
            {
                if(killplayer == 0 && player.TeamNum == 2){Server.NextFrame(() => player.PlayerPawn.Value.CommitSuicide(false, true));}
                else if(killplayer == 1 && player.TeamNum == 3){Server.NextFrame(() => player.PlayerPawn.Value.CommitSuicide(false, true));}
            }
            else if(Config.Duel_DrawPunish == 3) // Kill who has minimum HP if timer ends
            {
                if(player.TeamNum == 2){THealth = player.PlayerPawn.Value.Health;T = player;} // save T player
                if(player.TeamNum == 3){CTHealth = player.PlayerPawn.Value.Health;CT = player;} // save CT player
                if(CT != null && T != null && CT.IsValid && T.IsValid && CT.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && T.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    if(CTHealth < THealth){Server.NextFrame(() => CT.PlayerPawn.Value.CommitSuicide(false, true));} 
                    else if(CTHealth > THealth){Server.NextFrame(() => T.PlayerPawn.Value.CommitSuicide(false, true));}
                    else if(CTHealth == THealth) // if no damage given then kill both
                    {
                        Server.NextFrame(() => CT.PlayerPawn.Value.CommitSuicide(false, true));
                        Server.NextFrame(() => T.PlayerPawn.Value.CommitSuicide(false, true));
                    }
                }
            }
        }
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 0 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if(Winner == "")
            {
                Winner = player.PlayerName;  // Save the name of the alive Player
                DuelWinner = $"{player.AuthorizedSteamID?.SteamId64}";
                IsCTWon = player.TeamNum == 3 ? true : false;
            }
            else Winner = ""; // If Winner is already saved its mean 2 players are alived after the Duel. Then remove the Winner.
            GiveBackSavedWeaponsToPlayers(player);
        }
        Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.DarkRed}-----------------------------------------------------------------------");
        if(Winner != ""){Server.PrintToChatAll($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.Duel.EndWins", Winner]}");}
        else Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Duel.EndDraw"]}");
        Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.DarkRed}-----------------------------------------------------------------------");

        string[] Commands = GetDuelItem(SelectedDuelModeName)?.CMD_End.Split(",");
        if(mp_death_drop_gun_value != 0)Server.ExecuteCommand($"mp_death_drop_gun {mp_death_drop_gun_value}");
        if(mp_buytime_value != 0)Server.ExecuteCommand($"mp_buytime {mp_buytime_value}");

        foreach(var cmd in Commands.Where(commands => commands != "")) // Execute Duel End Commands
        {
            Server.ExecuteCommand(cmd);
        }
        
        // A new clever way to end round (when roundtime is already 0) and add team scores, instead of using TerminateRound
        if(Winner == "")Server.ExecuteCommand("mp_default_team_winner_no_objective 0"); // 0 = Round Draw
        else if(!IsCTWon)Server.ExecuteCommand("mp_default_team_winner_no_objective 2"); // 2 = Terrorist Wins
        else if(IsCTWon)Server.ExecuteCommand("mp_default_team_winner_no_objective 3"); // 3 = Counter Terroirst Wins 
        Server.NextFrame(() => Server.ExecuteCommand("mp_ignore_round_win_conditions 0"));
    }
    private DuelModeSettings GetDuelItem(string DuelModeName)
    {
        DuelModeSettings duelMode = GetDuelModeByName(DuelModeName, StringComparer.OrdinalIgnoreCase);
        return duelMode;
    }
    private void SavePlayerWeapons(CCSPlayerController? player)
    {
        if(player == null || !player.IsValid)return;
        if(player.PlayerPawn == null || !player.PlayerPawn.IsValid)return;
        if(!player.PawnIsAlive)return;
        if(player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)return;
        if(player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.WeaponServices.MyWeapons == null)return;
        // Initialize the list for the current player
        playerSavedWeapons[player.UserId.ToString()] = new List<string>();
        // Get Player Weapons
        foreach (var weapon in player.PlayerPawn.Value.WeaponServices?.MyWeapons.Where(weapons => weapons != null && weapons.IsValid && weapons.Value != null && weapons.Value.DesignerName != null))
        {
            playerSavedWeapons?[player.UserId.ToString()]?.Add($"{weapon.Value.DesignerName}");
        }
    }
    private void GiveBackSavedWeaponsToPlayers(CCSPlayerController? player)
    {
        if(player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)return;
        player.RemoveWeapons(); // Remove Weapons from player
        if (playerSavedWeapons?.TryGetValue(player.UserId.ToString(), out var savedWeapons) == true)
        {
            foreach(var weapon in savedWeapons)
            {
                player.GiveNamedItem($"{weapon}");
            }
        }
    }
    private void CreateLaserBeamBetweenPlayers(float time)
    {
        if(!Config.Duel_DrawLaserBeam || !g_PrepDuel && !g_DuelStarted)return;
        Vector CTPlayerPosition = null, TPlayerPosition = null;
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if(Config.Duel_DrawLaserBeam && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE) // if Draw laser beam is true in Config then Get alive players
            {
                if(player.TeamNum == 3)CTPlayerPosition = player.Pawn.Value.AbsOrigin;
                if(player.TeamNum == 2)TPlayerPosition = player.Pawn.Value.AbsOrigin;
            }
        }
        CTPlayerPosition = new Vector(CTPlayerPosition.X, CTPlayerPosition.Y, CTPlayerPosition.Z+50);
        TPlayerPosition = new Vector(TPlayerPosition.X, TPlayerPosition.Y, TPlayerPosition.Z+50);
        float TotalDistance = CalculateDistance(CTPlayerPosition, TPlayerPosition);
        if(TotalDistance > 700.0f) // Create Beam if Distance is Greater then this
        {
            // Create Beam if it was not already Created
            DrawLaserBetween(CTPlayerPosition, TPlayerPosition, Color.Green, time, 2.0f);
        }
    }
    private void OnTick(CCSPlayerController? player)
    {
        if (player.Pawn == null || !player.Pawn.IsValid || !Config.PluginEnabled || !g_DuelStarted || !g_DuelNoscope || player?.PlayerPawn?.Value?.WeaponServices?.MyWeapons == null)
            return;
        if(player.PlayerPawn.Value.WeaponServices!.MyWeapons.Count != 0)
        {
            var ActiveWeaponName = player.PlayerPawn.Value.WeaponServices!.ActiveWeapon.Value.DesignerName;
            if(ActiveWeaponName.Contains("weapon_ssg08") || ActiveWeaponName.Contains("weapon_awp")
            || ActiveWeaponName.Contains("weapon_scar20") || ActiveWeaponName.Contains("weapon_g3sg1"))
            {
                player.PlayerPawn.Value.WeaponServices!.ActiveWeapon.Value.NextSecondaryAttackTick = Server.TickCount + 500;
                var buttons = player.Buttons;
                if(!g_Zoom[player.Slot] && (buttons & PlayerButtons.Attack2) != 0)
                {
                    g_Zoom[player.Slot] = true;
                }
                else if(g_Zoom[player.Slot] && (buttons & PlayerButtons.Attack2) == 0)
                {
                    g_Zoom[player.Slot] = false;
                }
                
            }
        }
    }
    [GameEventHandler(HookMode.Pre)]
    public HookResult BulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        CCSPlayerController player = @event.Userid;
        if (!Config.PluginEnabled || !g_DuelStarted || !g_DuelBullettracers || player.Pawn == null || !player.Pawn.IsValid)
            return HookResult.Continue;

        Vector PlayerPosition = player.Pawn.Value.AbsOrigin;
        Vector BulletOrigin = new Vector(PlayerPosition.X, PlayerPosition.Y, PlayerPosition.Z+64);//bulletOrigin.X += 50.0f;
        Vector bulletDestination = new Vector(@event.X, @event.Y, @event.Z);
        if(player.TeamNum == 3)DrawLaserBetween(BulletOrigin, bulletDestination, Color.Blue, 0.5f, 2.0f);
        else if(player.TeamNum == 2)DrawLaserBetween(BulletOrigin, bulletDestination, Color.Red, 0.5f, 2.0f);
        
        return HookResult.Continue;
    }
    public void DrawBeaconOnPlayer(CCSPlayerController? player)
    {
        if(player == null || !player.IsValid || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)return;
        
        Vector mid =  new Vector(player?.PlayerPawn.Value.AbsOrigin.X,player?.PlayerPawn.Value.AbsOrigin.Y,player?.PlayerPawn.Value.AbsOrigin.Z);

        int lines = 20;
        int[] ent = new int[lines];
        CBeam[] beam_ent = new CBeam[lines];

        // draw piecewise approx by stepping angle
        // and joining points with a dot to dot
        float step = (float)(2.0f * Math.PI) / (float)lines;
        float radius = 20.0f;

        float angle_old = 0.0f;
        float angle_cur = step;

        float BeaconTimerSecond = 0.0f;

        
        for(int i = 0; i < lines; i++) // Drawing Beacon Circle
        {
            Vector start = angle_on_circle(angle_old, radius, mid);
            Vector end = angle_on_circle(angle_cur, radius, mid);

            if(player.TeamNum == 2)
            {
                var result = DrawLaserBetween(start, end, Color.Red, 1.0f, 2.0f);
                ent[i] = result.Item1;
                beam_ent[i] = result.Item2;
            } 
            if(player.TeamNum == 3)
            {
                var result = DrawLaserBetween(start, end, Color.Blue, 1.0f, 2.0f);
                ent[i] = result.Item1;
                beam_ent[i] = result.Item2;
            }

            angle_old = angle_cur;
            angle_cur += step;
        }
        
        AddTimer(0.1f, ()=>
        {
            if (BeaconTimerSecond >= 0.9f)
            {
                return;
            }
            for(int i = 0; i < lines; i++) // Moving Beacon Circle
            {
                Vector start = angle_on_circle(angle_old, radius, mid);
                Vector end = angle_on_circle(angle_cur, radius, mid);

                TeleportLaser(beam_ent[i], start, end);

                angle_old = angle_cur;
                angle_cur += step;
            }
            radius += 10;
            BeaconTimerSecond += 0.1f;
        }, TimerFlags.REPEAT);
        PlaySoundOnPlayer(player, "sounds/tools/sfm/beep.vsnd_c");
        return;
    }
    private void PlaySoundOnPlayer(CCSPlayerController? player, String sound)
    {
        if(player == null || !player.IsValid)return;
        player.ExecuteClientCommand($"play {sound}");
    }
    private static readonly Vector VectorZero = new Vector(0, 0, 0);
    private static readonly QAngle RotationZero = new QAngle(0, 0, 0);
    public (int, CBeam) DrawLaserBetween(Vector startPos, Vector endPos, Color color, float life, float width)
    {
        if (startPos == null || endPos == null)
            return (-1, null);

        CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");

        if (beam == null)
        {
            Logger.LogError($"Failed to create beam...");
            return (-1, null);
        }

        beam.Render = color;
        beam.Width = width;

        beam.Teleport(startPos, RotationZero, VectorZero);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();

        AddTimer(life, () => {if(beam != null && beam.IsValid) beam.Remove(); }); // destroy beam after specific time

        return ((int)beam.Index, beam);
    }
    public void TeleportLaser(CBeam? laser,Vector start, Vector end)
    {
        if(laser == null || !laser.IsValid)return;
        // set pos
        laser.Teleport(start, RotationZero, VectorZero);
        // end pos
        // NOTE: we cant just move the whole vec
        laser.EndPos.X = end.X;
        laser.EndPos.Y = end.Y;
        laser.EndPos.Z = end.Z;
        Utilities.SetStateChanged(laser,"CBeam", "m_vecEndPos");
    }
    private float CalculateDistance(Vector point1, Vector point2)
    {
        float dx = point2.X - point1.X;
        float dy = point2.Y - point1.Y;
        float dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    private Vector angle_on_circle(float angle, float radius, Vector mid)
    {
        // {r * cos(x),r * sin(x)} + mid
        // NOTE: we offset Z so it doesn't clip into the ground
        return new Vector((float)(mid.X + (radius * Math.Cos(angle))),(float)(mid.Y + (radius * Math.Sin(angle))), mid.Z + 6.0f);
    }
    private void TeleportPlayer(CCSPlayerController? player)
	{
		if (Duel_Positions?.ContainsKey(Server.MapName) == true) // If Map Exist in File
		{
            if(player == null || !player.IsValid || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)return; // If player is not Valid then return
            Vector TeleportPosition = GetPositionFromFile(player.TeamNum); // Get Teleport Position From JSON file
            if(TeleportPosition != null)player.PlayerPawn.Value.Teleport(TeleportPosition, player.PlayerPawn.Value.AngVelocity, new Vector(0f, 0f, 0f)); // Teleport Player to That position
        }
        else return; // If Map not Exist in File then do nothing
    }

    // Commands
    [ConsoleCommand("duel", "Open Chat Menu of Player Duel Settings")]
	public void PlayerDuelSettings(CCSPlayerController? player, CommandInfo command)
	{
        if (!Config.PluginEnabled || player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
		{
			return;
		}
        string SelectedOption = "";
        if(PlayerOption?.ContainsKey(player) == true && PlayerOption[player] == 1){SelectedOption = $"{Localizer["PlayerMenu.Accept"]}";}
        if(PlayerOption?.ContainsKey(player) == true && PlayerOption[player] == 0){SelectedOption = $"{Localizer["PlayerMenu.Decline"]}";}
        if(PlayerOption?.ContainsKey(player) == true && PlayerOption[player] == -1){SelectedOption = $"{Localizer["PlayerMenu.Vote"]}";}
        var PlayerDuelSettings = new ChatMenu($"{Localizer["PlayerMenu.Title"]} {Localizer["PlayerMenu.Selected", SelectedOption]}");
        //PlayerDuelSettings.AddMenuOption($"{Localizer["PlayerMenu.Selected", SelectedOption]}",SetShowVote,true);
        PlayerDuelSettings.AddMenuOption($"{Localizer["PlayerMenu.Accept"]}", SetAlwaysAccept);
        PlayerDuelSettings.AddMenuOption($"{Localizer["PlayerMenu.Decline"]}", SetAlwaysDecline);
        PlayerDuelSettings.AddMenuOption($"{Localizer["PlayerMenu.Vote"]}", SetShowVote);
        PlayerDuelSettings.ExitButton = true;
        PlayerDuelSettings.PostSelectAction = PostSelectAction.Close;
        MenuManager.OpenChatMenu(player, PlayerDuelSettings);
    }
    private void SetAlwaysAccept(CCSPlayerController? player, ChatMenuOption option)
    {
        if(player == null || player.IsValid == false)return;

        PlayerOption[player] = 1;
        SetPlayerDuelOption(player, 1);
        
        player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.Accept", player.PlayerPawn.Value!.AbsOrigin]}");
    }
    private void SetAlwaysDecline(CCSPlayerController? player, ChatMenuOption option)
    {
        if(player == null || player.IsValid == false)return;
        
        PlayerOption[player] = 0;
        SetPlayerDuelOption(player, 0);

        player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.Decline", player.PlayerPawn.Value!.AbsOrigin]}");
    }
    private void SetShowVote(CCSPlayerController? player, ChatMenuOption option)
    {
        if(player == null || player.IsValid == false)return;
        
        PlayerOption[player] = -1;
        SetPlayerDuelOption(player, -1);

        player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.Vote", player.PlayerPawn.Value!.AbsOrigin]}");
    }

    [ConsoleCommand("duel_settings", "Open Chat Menu of Duel Settings")]
	[RequiresPermissions("@css/root")]
	public void DuelSettings(CCSPlayerController? player, CommandInfo command)
	{
        if (!Config.PluginEnabled || player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
		{
			return;
		}
        var DuelSettings = new ChatMenu($"{Localizer["MenuSettings.Title"]}");
        DuelSettings.AddMenuOption($"{Localizer["MenuSettings.TeleportSetT"]}", SetTerroristTeleportPosition);
        DuelSettings.AddMenuOption($"{Localizer["MenuSettings.TeleportSetCT"]}", SetCTerroristTeleportPosition);
        DuelSettings.AddMenuOption($"{Localizer["MenuSettings.TeleportDelete"]}", DeleteTeleportPositions);
        DuelSettings.ExitButton = true;
        DuelSettings.PostSelectAction = PostSelectAction.Close;
        MenuManager.OpenChatMenu(player, DuelSettings);
    }
    private void SetTerroristTeleportPosition(CCSPlayerController? player, ChatMenuOption option)
    {
        if(player == null || player.IsValid == false)return;
        
        if (Duel_Positions.ContainsKey(Server.MapName)) // Check If Map already exist in JSON file
        {
            Dictionary<string, string> MapData = Duel_Positions[Server.MapName]; // Get Map Settings
            MapData["T_Pos"] = $"{player.PlayerPawn.Value!.AbsOrigin}"; // Edit t_pos value
            File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions));
        }
        else // If Map not found in JSON file
        {
            // Save/add this in Global Veriable
            Duel_Positions.Add(Server.MapName, new Dictionary<string, string>{{"T_Pos", $"{player.PlayerPawn.Value!.AbsOrigin}"}, {"CT_Pos", ""}});
            File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions)); // Saving Position in File
        }
        player.PrintToChat($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.DuelSettings.TeleportSetT", player.PlayerPawn.Value!.AbsOrigin]}");
    }
    private void SetCTerroristTeleportPosition(CCSPlayerController? player, ChatMenuOption option)
    {
        if(player == null || player.IsValid == false)return;

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
		player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.DuelSettings.TeleportSetCT", player.PlayerPawn.Value!.AbsOrigin]}");
	}
    private void DeleteTeleportPositions(CCSPlayerController? player, ChatMenuOption option)
    {
        if(player == null || player.IsValid == false)return;
        
        if (Duel_Positions != null && Duel_Positions.ContainsKey(Server.MapName)) // Check If Map exist in JSON file
        {
            Duel_Positions.Remove(Server.MapName); // Delete Map Settings
            File.WriteAllText(GetMapTeleportPositionConfigPath(), JsonSerializer.Serialize(Duel_Positions)); // Update File
        }
        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.DuelSettings.TeleportDelete"]}");
    }
    private string GetMapTeleportPositionConfigPath()
    {
        string? path = Path.GetDirectoryName(ModuleDirectory);
        if (Directory.Exists(path + $"/{ModuleName}"))
        {
            return Path.Combine(path, $"../configs/plugins/{ModuleName}/Duel_TeleportPositions.json");
        }
        return $"{ModuleDirectory}/Duel_TeleportPositions.json";
    }
    private Vector GetPositionFromFile(int TeamNum)
    {
        var mapData = Duel_Positions[Server.MapName]; // Get Current Map Teleport Positions
        if (TeamNum == 2 && mapData.ContainsKey("T_Pos") && mapData["T_Pos"] != "") // If player team is Terrorist then get the T_Pos from File
        {
            string[] Positions = mapData["T_Pos"].Split(" "); // Split Coordinates with space " "
            return new Vector(float.Parse(Positions[0]), float.Parse(Positions[1]), float.Parse(Positions[2])); // Return Coordinates in Vector
        }
        else if(TeamNum == 3 && mapData.ContainsKey("CT_Pos") && mapData["CT_Pos"] != "") // If player team is C-Terrorist then get the CT_Pos from File
        {
            string[] Positions = mapData["CT_Pos"].Split(" "); // Split Coordinates with space " "
            return new Vector(float.Parse(Positions[0]), float.Parse(Positions[1]), float.Parse(Positions[2])); // Return Coordinates in Vector
        }
        return null;
    }
    private void LoadPositionsFromFile()
    {
        if (!File.Exists(GetMapTeleportPositionConfigPath()))
		{
			return;
		}
		
		var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(GetMapTeleportPositionConfigPath()));
		
		if(data != null)
		{
			Duel_Positions = data;
		}
    }
    private void SetPlayerDuelOption(CCSPlayerController? player, int choice)
    {
        if(player == null || player.IsValid == false)return;

        var steamId = player.AuthorizedSteamID?.SteamId64; // Get Player Steam ID
        if (steamId == null) return; // Steam ID shouldn't be Null
        
        Task.Run(async () => // Run in a separate thread to avoid blocking the main thread
        {
            try
            {
                // insert or update the player's fov
                await _connection.ExecuteAsync(@"
                    INSERT INTO `SLAYER_Duel` (`steamid`, `option`) VALUES (@SteamId, @Option)
                    ON CONFLICT(`steamid`) DO UPDATE SET `option` = @Option;",
                    new
                    {
                        SteamId = steamId,
                        Option = choice
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLAYER_Duel] Error while saving player 'Option': {ex.Message}");
                Logger.LogError($"[SLAYER_Duel] Error while saving player 'Option': {ex.Message}");
            }
        });
    }
    public static CCSGameRules GetGameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
    private void FreezePlayer(CCSPlayerController? player)
    {
        if(player == null || !player.IsValid)return;
        player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
        Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 1); // freeze
        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
        player.PlayerPawn.Value.TakesDamage = false;
    }
    private void UnFreezePlayer(CCSPlayerController? player)
    {
        if(player == null || !player.IsValid)return;
        player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
        player.PlayerPawn.Value.TakesDamage = true;
    }
    private void ApplyInfiniteClip(CCSPlayerController player)
    {
        var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
        if (activeWeaponHandle?.Value != null)
        {
            activeWeaponHandle.Value.Clip1 = 100;
        }
    }

    private void ApplyInfiniteReserve(CCSPlayerController player)
    {
        var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
        if (activeWeaponHandle?.Value != null)
        {
            activeWeaponHandle.Value.ReserveAmmo[0] = 100;
        }
    }
}
