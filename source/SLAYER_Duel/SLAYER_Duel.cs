﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using System.Text.Json.Serialization;
using System.Drawing;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;

namespace SLAYER_Duel;
// Used these to remove compile warnings
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604

public class SLAYER_DuelConfig : BasePluginConfig
{
    [JsonPropertyName("PluginEnabled")] public bool PluginEnabled { get; set; } = true;
    [JsonPropertyName("Duel_ForceStart")] public bool Duel_ForceStart { get; set; } = true;
    [JsonPropertyName("Duel_DrawLaserBeam")] public bool Duel_DrawLaserBeam { get; set; } = true;
    [JsonPropertyName("Duel_Time")] public int Duel_Time { get; set; } = 30;
    [JsonPropertyName("Duel_PrepTime")] public int Duel_PrepTime { get; set; } = 3;
    [JsonPropertyName("Duel_MinPlayers")] public int Duel_MinPlayers { get; set; } = 3;
    [JsonPropertyName("Duel_DrawPunish")] public int Duel_DrawPunish { get; set; } = 3;
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

    
}
public class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    public override string ModuleName => "SLAYER_Duel";
    public override string ModuleVersion => "1.0";
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
    // Create a Dictionary to store weapons for each player by player steam id
    Dictionary<string, List<string>> playerSavedWeapons = new Dictionary<string, List<string>>();
    List<int> LastDuelNums = new List<int>();
    public bool[] g_Zoom = new bool[64];
    public bool g_DuelStarted = false;
    public bool g_PrepDuel = false;
    public bool g_DuelNoscope = false;
    public bool g_DuelHSOnly = false;
    public bool g_DuelBullettracers = false;
    public bool g_IsDuelPossible = false;
    public bool[] PlayersDuelVoteOption = new bool[2];
    public float g_PrepTime;
    public float g_DuelTime;
    public int SelectedMode;
    public string SelectedDuelModeName = "";
    public ConVar? mp_death_drop_gun = ConVar.Find("mp_death_drop_gun");
    int mp_death_drop_gun_value;
    // Timers
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_PrepDuel;
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_DuelTimer;
    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(() =>
        {
            if(!Config.PluginEnabled)return;
            
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                if(g_PrepDuel)
                {
                    player.PrintToCenterHtml
                    (
                        $"<font color='red'>.:| </font> <font class='fontSize-l' color='green'>Duel will Start in:</font><font color='red'> |:.</font><br>" +
                        $"<font color='green'>►</font> <font class='fontSize-m' color='red'>{g_PrepTime:0}s</font> <font color='green'>◄</font><br>"
                    );
                }
                if(g_DuelStarted)
                {
                    player.PrintToCenterHtml
                    (
                        $"<font color='red'>.:| </font> <font class='fontSize-l' color='green'>The Duel will End in:</font><font color='red'> |:.</font><br>" +
                        $"<font color='green'>►</font> <font class='fontSize-m' color='red'>{g_DuelTime:0}s</font> <font color='green'>◄</font><br>"
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
            if(!Config.PluginEnabled || !g_PrepDuel || !g_DuelStarted)return HookResult.Continue;
            // Kill player if he spawn during duel
            @event.Userid.PlayerPawn.Value.CommitSuicide(false, true);
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundEnd>((@event, info) =>
        {
            g_PrepDuel = false;
            g_DuelStarted = false;
            g_IsDuelPossible = false;
            return HookResult.Continue;
        });
        RegisterEventHandler<EventWeaponFire>((@event, info) =>
        {
            if(!Config.PluginEnabled || !g_DuelStarted)return HookResult.Continue;
            // Unlimited Reserve Ammo
            var weapon = @event.Userid.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;
            if(weapon.DesignerName != "weapon_knife" || weapon.DesignerName != "weapon_bayonet")weapon.ReserveAmmo[0] = 100;
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerHurt>((@event, info) => 
        {
            if(!Config.PluginEnabled || !g_DuelStarted || !g_DuelHSOnly)return HookResult.Continue;
            if (!@event.Userid.IsValid || @event.Userid == null)
                return HookResult.Continue;

            CCSPlayerController attacker = @event.Attacker;

            if (!attacker.IsValid || @event.Userid.TeamNum == attacker.TeamNum && !(@event.DmgHealth > 0 || @event.DmgArmor > 0))
                return HookResult.Continue;

            if(@event.Hitgroup != 1) // if bullet not hitting on Head
            {
                if(@event.Userid.PlayerPawn.Value.Health < 1)@event.Userid.PlayerPawn.Value.Health = 100; // If somehow player health get low from 1 then set it to 100
                else @event.Userid.PlayerPawn.Value.Health = @event.DmgHealth; // Otherwise add the dmg health to Normal health
                @event.Userid.PlayerPawn.Value.ArmorValue += @event.DmgArmor; // Update the Armor as well
            }
            
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if(!Config.PluginEnabled)return HookResult.Continue; // Plugin should be Enabled
            int ctplayer = 0, tplayer = 0, totalplayers = 0;
            // Count Players in Both Team on Any Player Death
            foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV))
            {
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 2)tplayer++;
                if(player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.TeamNum == 3)ctplayer++;
                if(g_DuelStarted)player.RemoveWeapons();
                totalplayers++;
            }
            if(Config.Duel_MinPlayers <= totalplayers && ctplayer == 1 && tplayer == 1) // 1vs1 Situation
            {
                g_IsDuelPossible = true;
                if(Config.Duel_ForceStart) // If Force Start Duel is true
                {
                    g_PrepDuel = true;
                    PrepDuel();
                }
                else // if force start duel is false
                {
                    PlayersDuelVoteOption[0] = false; PlayersDuelVoteOption[1] = false;
                    var DuelVote = new ChatMenu($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER {ChatColors.Gold}Duel {ChatColors.Purple}Vote {ChatColors.Darkred}★{ChatColors.Gold}]");
                    DuelVote.AddMenuOption($" {ChatColors.Green}Accept", AcceptDuelVoteOption);
                    DuelVote.AddMenuOption($"{ChatColors.Darkred}Decline", DeclineDuelVoteOption);
                    foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                    {
                        if(player.IsBot)
                        {
                            if(player.TeamNum == 2)PlayersDuelVoteOption[0] = true;
                            else if(player.TeamNum == 3)PlayersDuelVoteOption[1] = true;
                        }
                        else ChatMenus.OpenMenu(player, DuelVote);
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
            Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Red}{player.PlayerName} {ChatColors.Green}Accepted {ChatColors.Gold}to Duel!");
        }
        else if(player.TeamNum == 3)
        {
            PlayersDuelVoteOption[1] = true;
            Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Blue}{player.PlayerName} {ChatColors.Green}Accepted {ChatColors.Gold}to Duel!");
        }
        if(PlayersDuelVoteOption[0] && PlayersDuelVoteOption[1])
        {
            Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Purple}Both Players {ChatColors.Green}Accepted {ChatColors.Gold}to Duel!");
            g_PrepDuel = true;
            PrepDuel();
        }
    }
    private void DeclineDuelVoteOption(CCSPlayerController player, ChatMenuOption option)
    {
        if (player == null || player.IsValid == false || player.IsBot == true || !g_IsDuelPossible)return;

        if(player.TeamNum == 2)
        {
            PlayersDuelVoteOption[0] = false;
            Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Red}{player.PlayerName} {ChatColors.Purple}Rejected {ChatColors.Gold}to Duel!");
        }
        else if(player.TeamNum == 3)
        {
            PlayersDuelVoteOption[1] = false;
            Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Blue}{player.PlayerName} {ChatColors.Purple}Rejected {ChatColors.Gold}to Duel!");
        }
        if(!PlayersDuelVoteOption[0] && !PlayersDuelVoteOption[1])
        {
            Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Purple}Both Players {ChatColors.Darkred}Rejected {ChatColors.Gold}to Duel!");
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
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Darkred}-----------------------------------------------------------------------");
                Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Darkred}Duel Started: {ChatColors.Green} {SelectedModeName.Name}");
                Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Darkred}-----------------------------------------------------------------------");
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
        if(g_DuelTime <= 0.0f || g_DuelStarted == false)
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
        mp_death_drop_gun_value = mp_death_drop_gun.GetPrimitiveValue<int>();
        foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            SavePlayerWeapons(player); // first save player weapons
            player.RemoveWeapons(); // then remove weapons from player
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
        
        
        if(mp_death_drop_gun_value > 1)Server.ExecuteCommand("mp_death_drop_gun 0");

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
        if(mp_death_drop_gun_value > 1)Server.ExecuteCommand($"mp_death_drop_gun {mp_death_drop_gun_value}");
        foreach(var cmd in Commands) // Execute Duel End Commands
        {
            Server.ExecuteCommand(cmd);
        }
        Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Darkred}-----------------------------------------------------------------------");
        if(Winner != "")Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}]  {ChatColors.Darkred}Duel Ended! {ChatColors.LightPurple}And the {ChatColors.Gold}Winner {ChatColors.LightPurple}is: {ChatColors.Green}{Winner}");
        else Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.Darkred}★ {ChatColors.Green}SLAYER Duel {ChatColors.Darkred}★{ChatColors.Gold}] {ChatColors.Darkred}Duel Ended! {ChatColors.Grey}Draw");
        Server.PrintToChatAll($" {ChatColors.Green}★ {ChatColors.Darkred}-----------------------------------------------------------------------");
        g_PrepDuel = false;
        g_DuelStarted = false;
        
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
    private float CalculateDistance(Vector point1, Vector point2)
    {
        float dx = point2.X - point1.X;
        float dy = point2.Y - point1.Y;
        float dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
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
    private static readonly Vector VectorZero = new Vector(0, 0, 0);
    private static readonly QAngle RotationZero = new QAngle(0, 0, 0);
    public void DrawLaserBetween(Vector startPos, Vector endPos, Color color, float life, float width)
    {
        if(startPos == null || endPos == null)return;
        CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null)
        {
            Logger.LogError($"Failed to create beam...");
            return;
        }
        beam.Render = color;
        beam.Width = width;
        
        beam.Teleport(startPos, RotationZero, VectorZero);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();
        AddTimer(life, () => { beam.Remove(); }); // destroy beam after specific time
    }
}