using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Memory;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;
using Microsoft.Extensions.Logging;


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
    [JsonPropertyName("Duel_ForceStart")] public bool Duel_ForceStart { get; set; } = true;
    [JsonPropertyName("Duel_ShowMenuInCenter")] public bool Duel_ShowMenuInCenter { get; set; } = true;
    [JsonPropertyName("Duel_DrawLaserBeam")] public bool Duel_DrawLaserBeam { get; set; } = true;
    [JsonPropertyName("Duel_BotAcceptDuel")] public bool Duel_BotAcceptDuel { get; set; } = true;
    [JsonPropertyName("Duel_BotsDoDuel")] public bool Duel_BotsDoDuel { get; set; } = true;
    [JsonPropertyName("Duel_Time")] public int Duel_Time { get; set; } = 30;
    [JsonPropertyName("Duel_PrepTime")] public int Duel_PrepTime { get; set; } = 3;
    [JsonPropertyName("Duel_MinPlayers")] public int Duel_MinPlayers { get; set; } = 3;
    [JsonPropertyName("Duel_DrawPunish")] public int Duel_DrawPunish { get; set; } = 3;
    [JsonPropertyName("Duel_Beacon")] public bool Duel_Beacon { get; set; } = true;
    [JsonPropertyName("Duel_Teleport")] public bool Duel_Teleport { get; set; } = true;
    [JsonPropertyName("Duel_FreezePlayers")] public bool Duel_FreezePlayers { get; set; } = false;
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
    [JsonPropertyName("NoZoom")] public bool NoZoom { get; set; } = false;
    [JsonPropertyName("OnlyHeadshot")] public bool Only_headshot { get; set; } = false;
     [JsonPropertyName("DisableKnife")] public bool DisableKnife { get; set; } = false;
}
public class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    public override string ModuleName => "SLAYER_Duel";
    public override string ModuleVersion => "1.4";
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
    // Create a Dictionary to store weapons for each player by player id
    Dictionary<string, List<string>> playerSavedWeapons = new Dictionary<string, List<string>>();
    List<int> LastDuelNums = new List<int>();
    Dictionary<string, Dictionary<string, string>> Duel_Positions = new Dictionary<string, Dictionary<string, string>>();
    public bool[] g_Zoom = new bool[64];
    public bool g_DuelStarted = false;
    public bool g_PrepDuel = false;
    public bool g_DuelNoscope = false;
    public bool g_DuelHSOnly = false;
    public bool g_DuelDisableKnife = false;
    public bool g_DuelBullettracers = false;
    public bool g_IsDuelPossible = false;
    public bool[] PlayersDuelVoteOption = new bool[2];
    public float g_PrepTime;
    public float g_DuelTime;
    public int SelectedMode;
    public string SelectedDuelModeName = "";
    public ConVar? mp_death_drop_gun = ConVar.Find("mp_death_drop_gun");
    public int mp_death_drop_gun_value;
    // Timers
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_PrepDuel;
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_DuelTimer;
    CounterStrikeSharp.API.Modules.Timers.Timer[]? PlayerBeaconTimer = new CounterStrikeSharp.API.Modules.Timers.Timer[64];
    public override void Load(bool hotReload)
    {
        LoadPositionsFromFile();
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnTick>(() =>
        {
            if(!Config.PluginEnabled)return;
            
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                if(g_PrepDuel)
                {
                    player.PrintToCenterHtml
                    (
                        $"{Localizer["CenterHtml.DuelPrep"]}" +
                        $"{Localizer["CenterHtml.DuelPrepTime", g_PrepTime]}"
                    );
                }
                if(g_DuelStarted)
                {
                    player.PrintToCenterHtml
                    (
                        $"{Localizer["CenterHtml.DuelEnd"]}" +
                        $"{Localizer["CenterHtml.DuelPrepTime", g_DuelTime]}"
                    );
                    if(g_DuelNoscope && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)OnTick(player);
                }
                
            }
        });
        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            g_PrepDuel = false;
            g_DuelStarted = false;
            g_IsDuelPossible = false;
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            if(!Config.PluginEnabled || @event.Userid == null || !@event.Userid.IsValid)return HookResult.Continue;
            // Kill player if he spawn during duel
            if(g_PrepDuel || g_DuelStarted)@event.Userid.PlayerPawn.Value.CommitSuicide(false, true);
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundEnd>((@event, info) =>
        {
            if(g_IsDuelPossible)
            {
                foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    MenuManager.CloseActiveMenu(player);
                }
            }
            g_PrepDuel = false;
            g_DuelStarted = false;
            g_IsDuelPossible = false;
            
            return HookResult.Continue;
        });
        RegisterEventHandler<EventWeaponFire>((@event, info) =>
        {
            if(!Config.PluginEnabled || !g_DuelStarted)return HookResult.Continue;
            if(!@event.Userid.IsValid || @event.Userid == null)return HookResult.Continue;
            // Unlimited Reserve Ammo
            var weapon = @event.Userid.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;
            if(weapon.DesignerName != "weapon_knife" || weapon.DesignerName != "weapon_bayonet")weapon.ReserveAmmo[0] = 100;
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerHurt>((@event, info) => 
        {
            if(!Config.PluginEnabled || !g_DuelStarted || !g_DuelHSOnly && !g_DuelDisableKnife)return HookResult.Continue;
            if(!@event.Userid.IsValid || @event.Userid == null || !@event.Attacker.IsValid || @event.Attacker == null)
                return HookResult.Continue;
            // Some Checks to validate Attacker
            CCSPlayerController attacker = @event.Attacker;
            CCSPlayerController player = @event.Userid;
            if (!attacker.IsValid || @event.Userid.TeamNum == attacker.TeamNum && !(@event.DmgHealth > 0 || @event.DmgArmor > 0))
                return HookResult.Continue;

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
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if(!Config.PluginEnabled)return HookResult.Continue; // Plugin should be Enable
            int ctplayer = 0, tplayer = 0, totalplayers = 0;
            // Count Players in Both Team on Any Player Death
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 2)tplayer++;
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 3)ctplayer++;
                if(g_DuelStarted)player.RemoveWeapons();
                totalplayers++;
            }
            if(Config.Duel_MinPlayers <= totalplayers && ctplayer == 1 && tplayer == 1)g_IsDuelPossible = true;
            else g_IsDuelPossible = false;
            CCSGameRules gamerules = GetGameRules();
            if(!gamerules.WarmupPeriod && g_IsDuelPossible) // 1vs1 Situation and its not warmup
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
                        else 
                        {
                            if(!Config.Duel_ShowMenuInCenter)
                            {   
                                var DuelVote_Chat = new ChatMenu($"{Localizer["Menu.Title"]}");
                                DuelVote_Chat.AddMenuOption($"{Localizer["Menu.Accept"]}", AcceptDuelVoteOption);
                                DuelVote_Chat.AddMenuOption($"{Localizer["Menu.Decline"]}", DeclineDuelVoteOption);
                                DuelVote_Chat.PostSelectAction = PostSelectAction.Close;
                                MenuManager.OpenChatMenu(player, DuelVote_Chat);
                            }
                            else 
                            {
                                var DuelVote_Center = new CenterHtmlMenu($"{Localizer["Menu.Title"]}", this);
                                DuelVote_Center.AddMenuOption($"{Localizer["Menu.Accept"]}", AcceptDuelVoteOption);
                                DuelVote_Center.AddMenuOption($"{Localizer["Menu.Decline"]}", DeclineDuelVoteOption);
                                DuelVote_Center.PostSelectAction = PostSelectAction.Close;
                                MenuManager.OpenCenterHtmlMenu(this, player, DuelVote_Center);
                                
                            }
                        }
                    }
                    
                }
            }
            else if(ctplayer == 0 || tplayer == 0)
            {
                g_DuelStarted = false;
            }
            return HookResult.Continue;
        }, HookMode.Post);
    }
    private void AcceptDuelVoteOption(CCSPlayerController player, ChatMenuOption option)
    {
        if (player == null || player.IsValid == false || player.IsBot == true || !g_IsDuelPossible)return;
        
        if(player.TeamNum == 2)
        {
            PlayersDuelVoteOption[0] = true;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.T", player.PlayerName]}");
        }
        else if(player.TeamNum == 3)
        {
            PlayersDuelVoteOption[1] = true;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.CT", player.PlayerName]}");
        }
        if(PlayersDuelVoteOption[0] && PlayersDuelVoteOption[1])
        {
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.AcceptedDuel.Both"]}");
            PrepDuel();
        }
    }
    private void DeclineDuelVoteOption(CCSPlayerController player, ChatMenuOption option)
    {
        if (player == null || player.IsValid == false || player.IsBot == true || !g_IsDuelPossible)return;
        
        if(player.TeamNum == 2)
        {
            PlayersDuelVoteOption[0] = false;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.Both", player.PlayerName]}");
        }
        else if(player.TeamNum == 3)
        {
            PlayersDuelVoteOption[1] = false;
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.RejectedDuel.Both", player.PlayerName]}");
        }
        if(!PlayersDuelVoteOption[0] && !PlayersDuelVoteOption[1])
        {
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
    
    public void DuelStartedTimer()
    {
        if(g_DuelTime <= 0.0f || g_DuelStarted == false || g_IsDuelPossible == false)
        {
            EndDuel();
            t_DuelTimer?.Kill();
            return;
        }
        CreateLaserBeamBetweenPlayers(0.2f); // Create Laser Beam
        g_DuelTime = g_DuelTime - 0.2f;
    }
    public void PrepDuel()
    {
        if(g_PrepDuel)return;
        g_PrepDuel = true;
        foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if(Config.Duel_Teleport)TeleportPlayer(player);
            if(Config.Duel_FreezePlayers)FreezePlayer(player);   // Freeze Player
            SavePlayerWeapons(player); // first save player weapons
            player.RemoveWeapons(); // then remove weapons from player
            if(Config.Duel_Beacon) // If Beacon Enabled
            {
                if(PlayerBeaconTimer[player.Slot] != null){PlayerBeaconTimer[player.Slot].Kill();} // Kill Timer if running
                // Start Beacon
                PlayerBeaconTimer[player.Slot] = AddTimer(1.0f, ()=>
                {
                    if(!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE || !g_DuelStarted && !g_PrepDuel)
                    {
                        
                        PlayerBeaconTimer[player.Slot].Kill(); // Kill Timer if player die or leave
                    } 
                    DrawBeaconOnPlayer(player);
                }, TimerFlags.REPEAT);
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
        t_PrepDuel = AddTimer(0.2f, PrepDuelTimer, TimerFlags.REPEAT); // start Duel Prepration Timer
    }
    public void StartDuel(string DuelModeName)
    {
        if(!g_IsDuelPossible)return;
        string[] weapons = GetDuelItem(DuelModeName)?.Weapons.Split(",");
        string[] Commands = GetDuelItem(DuelModeName)?.CMD.Split(",");
        
        g_DuelNoscope = GetDuelItem(DuelModeName).NoZoom;
        g_DuelHSOnly = GetDuelItem(DuelModeName).Only_headshot;
        g_DuelBullettracers = GetDuelItem(DuelModeName).BulletTracers;
        g_DuelDisableKnife = GetDuelItem(DuelModeName).DisableKnife;
        
        mp_death_drop_gun_value = mp_death_drop_gun.GetPrimitiveValue<int>();
        if(mp_death_drop_gun_value != 0)Server.ExecuteCommand("mp_death_drop_gun 0");

        foreach(var cmd in Commands) // Execute Duel Start Commands
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
    
    public void EndDuel()
    {
        g_IsDuelPossible = false;
        if(Config.Duel_DrawPunish == 1 && g_DuelStarted) // Kill Both if timer ends
        {
            foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                player.PlayerPawn.Value.CommitSuicide(false, true);
            }
        }
        else if(Config.Duel_DrawPunish == 2 && g_DuelStarted) // Kill Random if timer ends
        {
            Random randomplayer = new Random();
            int killplayer = randomplayer.Next(0,2);
            foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                if(killplayer == 0 && player.TeamNum == 2){player.PlayerPawn.Value.CommitSuicide(false, true);}
                else if(killplayer == 1 && player.TeamNum == 3){player.PlayerPawn.Value.CommitSuicide(false, true);}
            }
        }
        else if(Config.Duel_DrawPunish == 3 && g_DuelStarted) // Kill who has minimum HP if timer ends
        {
            CCSPlayerController CT = null, T = null;
            int THealth = 0, CTHealth = 0;
            foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                if(player.TeamNum == 2){THealth = player.PlayerPawn.Value.Health;T = player;}
                if(player.TeamNum == 3){CTHealth = player.PlayerPawn.Value.Health;CT = player;}
            }
            if(CTHealth < THealth){CT.PlayerPawn.Value.CommitSuicide(false, true);} // Give Back Saved Weapons to player
            else if(CTHealth > THealth){T.PlayerPawn.Value.CommitSuicide(false, true);} // Give Back Saved Weapons to player
            else if(CTHealth == THealth) // if no damage given then kill both
            {
                CT.PlayerPawn.Value.CommitSuicide(false, true);
                T.PlayerPawn.Value.CommitSuicide(false, true);
            };
        }
        string Winner = "";
        foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if(Winner == "")Winner = player.PlayerName; // Save the name of the alive Player
            else Winner = ""; // If Winner is already saved its mean 2 players are alived after the Duel. Then remove the Winner.
            GiveBackSavedWeaponsToPlayers(player);
        }
        string[] Commands = GetDuelItem(SelectedDuelModeName)?.CMD_End.Split(",");
        if(mp_death_drop_gun_value != 0)Server.ExecuteCommand($"mp_death_drop_gun {mp_death_drop_gun_value}");
        foreach(var cmd in Commands) // Execute Duel End Commands
        {
            Server.ExecuteCommand(cmd);
        }
        Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.DarkRed}-----------------------------------------------------------------------");
        if(Winner != "")Server.PrintToChatAll($"{Localizer["Chat.Prefix"]}  {Localizer["Chat.Duel.EndWins", Winner]}");
        else Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Duel.EndDraw"]}");
        Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.DarkRed}-----------------------------------------------------------------------");
        g_PrepDuel = false;
        g_DuelStarted = false;
        if(t_DuelTimer != null)t_DuelTimer?.Kill();
    }
    private DuelModeSettings GetDuelItem(string DuelModeName)
    {
        DuelModeSettings duelMode = GetDuelModeByName(DuelModeName, StringComparer.OrdinalIgnoreCase);
        return duelMode;
    }
    private void SavePlayerWeapons(CCSPlayerController player)
    {
        // Initialize the list for the current player
        playerSavedWeapons[player.UserId.ToString()] = new List<string>();
        // Get Player Weapons
        foreach (var weapon in player.PlayerPawn.Value.WeaponServices?.MyWeapons.Where(weapons => weapons != null && weapons.IsValid))
        {
            playerSavedWeapons[player.UserId.ToString()].Add($"{weapon.Value.DesignerName}");
        }
    }
    private void GiveBackSavedWeaponsToPlayers(CCSPlayerController player)
    {
        if (playerSavedWeapons.TryGetValue(player.UserId.ToString(), out var savedWeapons))
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
        foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
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
    public void OnMapStart(string mapName) // Loading Duel Map Teleport Locations
	{
		LoadPositionsFromFile();
	}
    
    private void OnTick(CCSPlayerController player)
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

        AddTimer(life, () => { beam.Remove(); }); // destroy beam after specific time

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
		if (Duel_Positions != null && Duel_Positions.ContainsKey(Server.MapName)) // If Map Exist in File
		{
            if(player == null || !player.IsValid || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)return; // If player is not Valid then return
            Vector TeleportPosition = GetPositionFromFile(player.TeamNum); // Get Teleport Position From JSON file
            if(TeleportPosition != null)player.PlayerPawn.Value.Teleport(TeleportPosition, player.PlayerPawn.Value.AngVelocity, new Vector(0f, 0f, 0f)); // Teleport Player to That position
        }
        else return; // If Map not Exist in File then do nothing
    }

    // Commands
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
        DuelSettings.PostSelectAction = PostSelectAction.Close;
        MenuManager.OpenChatMenu(player, DuelSettings);
    }
    private void SetTerroristTeleportPosition(CCSPlayerController player, ChatMenuOption option)
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
    private void SetCTerroristTeleportPosition(CCSPlayerController player, ChatMenuOption option)
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
    private void DeleteTeleportPositions(CCSPlayerController player, ChatMenuOption option)
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
    public static CCSGameRules GetGameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
    private void FreezePlayer(CCSPlayerController? player)
    {
        if(player == null || !player.IsValid)return;
        player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NONE;
        Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0); // freeze
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

}
