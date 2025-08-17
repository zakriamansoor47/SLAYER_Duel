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

public class SLAYER_DuelConfig : BasePluginConfig
{
    [JsonPropertyName("PluginEnabled")] public bool PluginEnabled { get; set; } = true;
    [JsonPropertyName("Duel_ForceStart")] public bool Duel_ForceStart { get; set; } = false;
    [JsonPropertyName("Duel_ShowMenuAt")] public int Duel_ShowMenuAt { get; set; } = 3;
    [JsonPropertyName("Duel_ShowDuelCounterIn")] public int Duel_ShowDuelCounterIn { get; set; } = 1;
    [JsonPropertyName("Duel_TopPlayersCount")] public int Duel_TopPlayersCount { get; set; } = 10;
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
public partial class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    public override string ModuleName => "SLAYER_Duel";
    public override string ModuleVersion => "2.0";
    public override string ModuleAuthor => "SLAYER";
    public override string ModuleDescription => "1vs1 Duel at the end of the round with different weapons";
    public required SLAYER_DuelConfig Config {get; set;}
    public void OnConfigParsed(SLAYER_DuelConfig config)
    {
        Config = config;
    }
    private SqliteConnection _connection = null!;

    Dictionary<CCSPlayerController, PlayerSettings> PlayerOption = new Dictionary<CCSPlayerController, PlayerSettings>();
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
    Dictionary<CCSPlayerController, (int, bool)> PlayerArmorBeforeDuel = new Dictionary<CCSPlayerController, (int, bool)>();
    public IT3MenuManager? T3MenuManager; // get the instance
    public IT3MenuManager? GetMenuManager()
    {
        if (T3MenuManager == null)
            T3MenuManager = new PluginCapability<IT3MenuManager>("t3menu:manager").Get();

        return T3MenuManager;
    }
    public override void Load(bool hotReload)
    {
        PlayerBeaconTimer?.Clear();
        //PlayerRescuingHostage?.Clear();
        _connection = new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "Database/SLAYER_Duel.db")}");
        _connection.Open();
        if(hotReload)
        {
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                LoadPlayerSettings(player);
            }
        }
        Task.Run(async () =>
        {
            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS `SLAYER_Duel` (`steamid` UNSIGNED BIG INT NOT NULL,`option` INT NOT NULL DEFAULT -1,`wins` INT NOT NULL DEFAULT 0,`losses` INT NOT NULL DEFAULT 0, PRIMARY KEY (`steamid`));");
        });
        LoadPositionsFromFile();
        RegisterListener<Listeners.OnMapStart>((mapname) =>
        {
            LoadPositionsFromFile();
            if(t_PrepDuel != null)t_PrepDuel.Kill();
            if(t_DuelTimer != null)t_DuelTimer.Kill();
            PlayerBeaconTimer?.Clear();
            //PlayerRescuingHostage?.Clear();
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
            
            LoadPlayerSettings(player);
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
            PlayerArmorBeforeDuel?.Clear();
            //PlayerRescuingHostage?.Clear();
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            var player = @event.Userid;
            if(!Config.PluginEnabled || player == null || !player.IsValid || player.PlayerPawn.Value == null)return HookResult.Continue;

            // Kill player if he spawn during duel
            if(g_PrepDuel || g_DuelStarted)player.PlayerPawn.Value.CommitSuicide(false, true);

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
                    T3MenuManager.CloseMenu(player);
                }
            }
            g_PrepDuel = false;
            g_DuelStarted = false;
            g_IsDuelPossible = false;
            g_IsVoteStarted = false;
            if(t_PrepDuel != null)t_PrepDuel.Kill();
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
                    T3MenuManager.CloseMenu(player);
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
                    T3MenuManager.CloseMenu(player);
                }
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventHostageFollows>((@event, info) =>
        {
            var player = @event.Userid;
            if(player == null || !player.IsValid)return HookResult.Continue;

            //PlayerRescuingHostage[player] = true; // Set Player is Rescuing Hostage
            if(!g_DuelStarted)
            {
                g_IsDuelPossible = false;
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if(!Config.PluginEnabled || g_BombPlanted)return HookResult.Continue; // Plugin should be Enable
            int ctplayer = 0, tplayer = 0, totalplayers = 0;
            // Count Players in Both Team on Any Player Death
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 2 && !player.ControllingBot)tplayer++;
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 3 && !player.ControllingBot)ctplayer++;
                //if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && g_DuelStarted)player.RemoveWeapons();
                totalplayers++;
            }
            CCSGameRules gamerules = GetGameRules();
            if(!g_IsDuelPossible)return HookResult.Continue;
            if(!gamerules.WarmupPeriod && totalplayers >= Config.Duel_MinPlayers && ctplayer == 1 && tplayer == 1) // 1vs1 Situation and its not warmup
            {
                if(Config.Duel_ForceStart) // If Force Start Duel is true
                {
                    RemoveObjectives(); // Remove Objectives from Map
                    AddTimer(0.1f, ()=> PrepDuel());
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
                            if(PlayerOption.ContainsKey(player) && PlayerOption[player].Option == 1) // if `1` is set in Database, then always accept duel without vote 
                            {
                                if(player.TeamNum == 2){PlayersDuelVoteOption[0] = true;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.T", player.PlayerName]}");}
                                if(player.TeamNum == 3){PlayersDuelVoteOption[1] = true;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.CT", player.PlayerName]}");}
                            }
                            else if(PlayerOption.ContainsKey(player) && PlayerOption[player].Option == 0) // if `0` is set in Database, then always decline duel without vote 
                            {
                                if(player.TeamNum == 2){PlayersDuelVoteOption[0] = false;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.T", player.PlayerName]}");}
                                if(player.TeamNum == 3){PlayersDuelVoteOption[1] = false;Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.CT", player.PlayerName]}");}
                            }
                            else  // if `-1` is set in Database, then start vote 
                            {
                                g_IsVoteStarted = true;
                                if (Config.Duel_ShowMenuAt <= 1) // if Chat menu select in JSON file then show vote in Chat menu
                                {
                                    if (Config.Duel_FreezePlayerOnMenuShown) FreezePlayer(player);
                                    var DuelVote_Chat = new ChatMenu($"{Localizer["Menu.Title"]}");
                                    DuelVote_Chat.AddMenuOption($"{Localizer["Menu.Accept"]}", AcceptDuelVoteOption);
                                    DuelVote_Chat.AddMenuOption($"{Localizer["Menu.Decline"]}", DeclineDuelVoteOption);
                                    DuelVote_Chat.PostSelectAction = PostSelectAction.Close;
                                    MenuManager.OpenChatMenu(player, DuelVote_Chat);
                                }
                                else if (Config.Duel_ShowMenuAt == 2) // show vote in Center HTML menu
                                {
                                    if (Config.Duel_FreezePlayerOnMenuShown) FreezePlayer(player);
                                    var DuelVote_Center = new CenterHtmlMenu($"{Localizer["Menu.Title"]}", this);
                                    DuelVote_Center.AddMenuOption($"{Localizer["Menu.Accept"]}", AcceptDuelVoteOption);
                                    DuelVote_Center.AddMenuOption($"{Localizer["Menu.Decline"]}", DeclineDuelVoteOption);
                                    DuelVote_Center.PostSelectAction = PostSelectAction.Close;
                                    MenuManager.OpenCenterHtmlMenu(this, player, DuelVote_Center);
                                }
                                else  // otherwise show WASD vote menu in Center
                                {
                                    var manager = GetMenuManager();
                                    if (manager == null) return HookResult.Continue;

                                    // Create menu
                                    var settingsMenu = manager.CreateMenu($"<font color='gold'>{Localizer["Menu.Title"]}</font>", false, Config.Duel_FreezePlayerOnMenuShown, true, false, false);
                                    // Add option to Accept
                                    settingsMenu.AddOption($"<font color='lime'>{Localizer["Menu.Accept"]}</font>", (p, option) =>
                                    {
                                        AcceptDuelVoteOption(player, null);
                                        T3MenuManager.CloseMenu(player);
                                    });

                                    // Add option to Reject
                                    settingsMenu.AddOption($"<font color='red'>{Localizer["Menu.Decline"]}</font>", (p, option) =>
                                    {
                                        DeclineDuelVoteOption(player, null);
                                        T3MenuManager.CloseMenu(player);
                                    });
                                    T3MenuManager.OpenMainMenu(player, settingsMenu);
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
            RemoveObjectives(); // Remove Objectives from Map
            AddTimer(0.1f, ()=> PrepDuel());
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
                player.PlayerPawn!.Value!?.ItemServices?.As<CCSPlayer_ItemServices>().RemoveWeapons(); // then remove weapons from player
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
        foreach (var duelist in Duelist.Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 0 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if (Winner == "")
            {
                Winner = duelist.PlayerName;  // Save the name of the alive Player
                DuelWinner = $"{duelist.AuthorizedSteamID?.SteamId64}";
                IsCTWon = duelist.TeamNum == 3 ? true : false;

                AddPlayerWin(duelist);
                // Add loss to the other duelist
                var loser = Duelist.FirstOrDefault(d => d != null && d != duelist && d.IsValid);
                if (loser != null)
                {
                    AddPlayerLoss(loser);
                }
            }
            else // If Winner is already saved its mean 2 players are alived after the Duel. Then remove the Winner.
            {
                Winner = "";
            } 
            GiveBackSavedWeaponsToPlayers(duelist);
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
        // Save Player Armor and Helmet
        if(new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices!.Handle).HasHelmet)
        {
            PlayerArmorBeforeDuel[player] = (player.PlayerPawn.Value.ArmorValue, true); // Save Player Armor + helmet before Duel
        }
        else PlayerArmorBeforeDuel[player] = (player.PlayerPawn.Value.ArmorValue, false); // Save only Player Armor before Duel
    }
    private void GiveBackSavedWeaponsToPlayers(CCSPlayerController? player)
    {
        if(player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)return;
        player.PlayerPawn!.Value!?.ItemServices?.As<CCSPlayer_ItemServices>().RemoveWeapons(); // Remove Weapons from player
        if (playerSavedWeapons?.TryGetValue(player.UserId.ToString(), out var savedWeapons) == true)
        {
            foreach(var weapon in savedWeapons)
            {
                player.GiveNamedItem($"{weapon}");
            }
        }
        CCSPlayer_ItemServices services = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices!.Handle);
        
        if(PlayerArmorBeforeDuel.ContainsKey(player) && PlayerArmorBeforeDuel[player].Item2) // if player has helmet then give back armor + helmet
        {
            player.PlayerPawn.Value.ArmorValue = PlayerArmorBeforeDuel[player].Item1;
            services.HasHelmet = true;
        }
        else if(PlayerArmorBeforeDuel.ContainsKey(player) && PlayerArmorBeforeDuel[player].Item2 == false) // if player has no helmet then give back only armor
        {
            player.PlayerPawn.Value.ArmorValue = PlayerArmorBeforeDuel[player].Item1;
            services.HasHelmet = false;
        }
        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBasePlayerPawn", "m_pItemServices");
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
        if(player.PlayerPawn.Value.WeaponServices!.MyWeapons.Count != 0) // Noscope
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
    public HookResult BulletImpact(EventBulletImpact @event, GameEventInfo info) // Bullet Tracers
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
    public List<DuelModeSettings> GetDuelModes()
    {
        return Config.Duel_Modes;
    }
    
    public DuelModeSettings GetDuelModeByName(string modeName, StringComparer comparer)
    {
        return Config.Duel_Modes.FirstOrDefault(mode => comparer.Equals(mode.Name, modeName));
    }
    

    
    
}
