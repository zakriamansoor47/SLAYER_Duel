using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json;

namespace SLAYER_Duel;

public partial class SLAYER_Duel : BasePlugin, IPluginConfig<SLAYER_DuelConfig>
{
    private string GetMapTeleportPositionConfigPath()
    {
        string path = Path.GetDirectoryName(ModuleDirectory)!;
        if (Directory.Exists(path + $"/{ModuleName}"))
        {
            return Path.Combine(path, $"../configs/plugins/{ModuleName}/Duel_TeleportPositions.json");
        }
        return $"{ModuleDirectory}/Duel_TeleportPositions.json";
    }
    private Vector? GetPositionFromFile(int TeamNum)
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
}