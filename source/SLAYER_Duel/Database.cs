using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace SLAYER_Duel;

public partial class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    public class PlayerSettings
    {
        public string PlayerName { get; set; } = "";
        public int Option { get; set; } = -1;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
    }

    private void LoadPlayerSettings(CCSPlayerController player)
    {
        var steamId = player.AuthorizedSteamID?.SteamId64;
        if (PlayerOption == null) PlayerOption = new Dictionary<CCSPlayerController, PlayerSettings>();
        if (PlayerOption?.ContainsKey(player) == false) PlayerOption[player] = new PlayerSettings();
        PlayerOption![player].PlayerName = player.PlayerName;

        Task.Run(async () =>
        {
            try
            {
                var result = await _connection.QueryFirstOrDefaultAsync(@"SELECT `option`, `wins`, `losses` FROM `SLAYER_Duel` WHERE `steamid` = @SteamId;",
                new
                {
                    SteamId = steamId
                });

                Server.NextFrame(() =>
                {
                    PlayerOption![player].Option = Convert.ToInt32($"{result?.option ?? -1}");
                    PlayerOption[player].Wins = Convert.ToInt32($"{result?.wins ?? 0}");
                    PlayerOption[player].Losses = Convert.ToInt32($"{result?.losses ?? 0}");
                });

                // Update player name in database when they connect
                await _connection.ExecuteAsync(@"INSERT INTO `SLAYER_Duel` (`steamid`, `name`, `option`, `wins`, `losses`) VALUES (@SteamId, @Name, -1, 0, 0)
                    ON CONFLICT(`steamid`) DO UPDATE SET `name` = @Name;",
                    new
                    {
                        SteamId = steamId,
                        Name = PlayerOption![player].PlayerName
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLAYER_Duel] Error on PlayerConnectFull while retrieving player data: {ex.Message}");
                Logger.LogError($"[SLAYER_Duel] Error on PlayerConnectFull while retrieving player data: {ex.Message}");
            }
        });
    }

    private void SetPlayerDuelOption(CCSPlayerController? player, int choice)
    {
        if (player == null || player.IsValid == false || PlayerOption == null) return;

        var steamId = player.AuthorizedSteamID?.SteamId64;
        if (steamId == null) return;

        // Update local settings
        if (PlayerOption?.ContainsKey(player) == true)
        {
            PlayerOption[player].Option = choice;
            PlayerOption![player].PlayerName = player.PlayerName;
        }

        Task.Run(async () =>
        {
            try
            {
                await _connection.ExecuteAsync(@"
                    INSERT INTO `SLAYER_Duel` (`steamid`, `name`, `option`, `wins`, `losses`) VALUES (@SteamId, @Name, @Option, 0, 0)
                    ON CONFLICT(`steamid`) DO UPDATE SET `name` = @Name, `option` = @Option;",
                    new
                    {
                        SteamId = steamId,
                        Name = PlayerOption![player].PlayerName,
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

    private void AddPlayerWin(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || PlayerOption == null) return;

        var steamId = player.AuthorizedSteamID?.SteamId64;
        if (steamId == null) return;

        // Update local settings
        var PlayerName = "";
        if (PlayerOption?.ContainsKey(player) == true)
        {
            PlayerOption[player].Wins++;
            PlayerName = PlayerOption[player].PlayerName;
        }
        else
        {   
            PlayerName = player.PlayerName;
        }

        Task.Run(async () =>
        {
            try
            {
                await _connection.ExecuteAsync(@"
                    INSERT INTO `SLAYER_Duel` (`steamid`, `name`, `option`, `wins`, `losses`) VALUES (@SteamId, @Name, -1, 1, 0)
                    ON CONFLICT(`steamid`) DO UPDATE SET `name` = @Name, `wins` = `wins` + 1;",
                    new
                    {
                        SteamId = steamId,
                        Name = PlayerName
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLAYER_Duel] Error while updating player wins: {ex.Message}");
                Logger.LogError($"[SLAYER_Duel] Error while updating player wins: {ex.Message}");
            }
        });
    }

    private void AddPlayerLoss(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || PlayerOption == null) return;

        var steamId = player.AuthorizedSteamID?.SteamId64;
        if (steamId == null) return;

        // Update local settings
        var PlayerName = "";
        if (PlayerOption?.ContainsKey(player) == true)
        {
            PlayerOption[player].Losses++;
            PlayerName = PlayerOption[player].PlayerName;
        }
        else
        {   
            PlayerName = player.PlayerName;
        }

        Task.Run(async () =>
        {
            try
            {
                await _connection.ExecuteAsync(@"
                    INSERT INTO `SLAYER_Duel` (`steamid`, `name`, `option`, `wins`, `losses`) VALUES (@SteamId, @Name, -1, 0, 1)
                    ON CONFLICT(`steamid`) DO UPDATE SET `name` = @Name, `losses` = `losses` + 1;",
                    new
                    {
                        SteamId = steamId,
                        Name = PlayerName
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLAYER_Duel] Error while updating player losses: {ex.Message}");
                Logger.LogError($"[SLAYER_Duel] Error while updating player losses: {ex.Message}");
            }
        });
    }

    private void SetPlayerStats(CCSPlayerController? player, int wins, int losses)
    {
        if (player == null || !player.IsValid) return;

        var steamId = player.AuthorizedSteamID?.SteamId64;
        if (steamId == null) return;

        // Update local settings
        var PlayerName = "";
        if (PlayerOption?.ContainsKey(player) == true)
        {
            PlayerOption[player].Wins = wins;
            PlayerOption[player].Losses = losses;
            PlayerName = player.PlayerName;
        }
        else
        {
            PlayerName = player.PlayerName;
        }

        Task.Run(async () =>
        {
            try
            {
                await _connection.ExecuteAsync(@"
                    INSERT INTO `SLAYER_Duel` (`steamid`, `name`, `option`, `wins`, `losses`) VALUES (@SteamId, @Name, -1, @Wins, @Losses)
                    ON CONFLICT(`steamid`) DO UPDATE SET `name` = @Name, `wins` = @Wins, `losses` = @Losses;",
                    new
                    {
                        SteamId = steamId,
                        Name = PlayerName,
                        Wins = wins,
                        Losses = losses
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLAYER_Duel] Error while setting player stats: {ex.Message}");
                Logger.LogError($"[SLAYER_Duel] Error while setting player stats: {ex.Message}");
            }
        });
    }

    private PlayerSettings? GetPlayerStats(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || PlayerOption?.ContainsKey(player) != true)
            return null;

        return PlayerOption[player];
    }
    
    private void GetTopPlayersSettings(int limit, Action<List<PlayerSettings>> callback)
    {
        Task.Run(async () =>
        {
            var topPlayersSettings = new List<PlayerSettings>();

            try
            {
                var result = await _connection.QueryAsync(@"
                    SELECT `steamid`, `name`, `option`, `wins`, `losses` 
                    FROM `SLAYER_Duel` 
                    WHERE `wins` > 0 
                    ORDER BY `wins` DESC, `losses` ASC 
                    LIMIT @Limit;",
                    new { Limit = limit });

                var dbResults = new List<(ulong steamId, string name, int option, int wins, int losses)>();
                
                // Store database results first
                foreach (var row in result)
                {
                    dbResults.Add((
                        Convert.ToUInt64(row.steamid),
                        Convert.ToString(row.name) ?? "",
                        Convert.ToInt32(row.option),
                        Convert.ToInt32(row.wins),
                        Convert.ToInt32(row.losses)
                    ));
                }

                // Switch back to main thread to process results
                Server.NextFrame(() =>
                {
                    foreach (var (steamId, storedName, option, wins, losses) in dbResults)
                    {
                        // Use stored name from database, but try to get updated name from online players if available
                        string playerName = storedName;
                        var onlinePlayer = Utilities.GetPlayers().FirstOrDefault(p => 
                            p != null && p.IsValid && p.AuthorizedSteamID?.SteamId64 == steamId);

                        if (onlinePlayer != null)
                        {
                            playerName = onlinePlayer.PlayerName;
                        }
                        else if (string.IsNullOrEmpty(storedName))
                        {
                            playerName = $"[{steamId}]";
                        }

                        var playerSettings = new PlayerSettings
                        {
                            PlayerName = playerName,
                            Option = option,
                            Wins = wins,
                            Losses = losses
                        };

                        topPlayersSettings.Add(playerSettings);
                    }

                    callback(topPlayersSettings);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLAYER_Duel] Error retrieving top players settings from database: {ex.Message}");
                Logger.LogError($"[SLAYER_Duel] Error retrieving top players settings from database: {ex.Message}");
                
                // Even on error, call the callback on main thread
                Server.NextFrame(() =>
                {
                    callback(topPlayersSettings);
                });
            }
        });
    }
}