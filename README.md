# Accepting Paid Request! Discord: Slayer47#7002
# Donate: If you like my work, you can donate to me via [Steam Trade Offer](https://bit.ly/3qDpgPd)

## Description:
This Plugin Allow Players to do **1vs1 Duel**. This plugin has many features. The main feature is that you can your own **Custom Duel Mode** from **JSON** file Easily.

## Installation:
**1.** Upload files to your server.

**2.** Edit **configs/plugins/SLAYER_Duel/SLAYER_Duel.json** if you want to change the settings.

**3.** Change the Map **or** Restart the Server **or** Load the Plugin.

## Features:
**1.** Fully Customizable.

**2.** You can create your own **Duel Modes**.

**3.** You can **Force Players** to do Duel **OR** You can take **Vote from Players** to Duel.

**4.** This Plugin Draw a **Green Laser Beam** Between Players to find each other if they are Far from each other.

**5.** You can set Duel Draw Punishment

**6.** You can set players health, armor, speed, gravity.

**7.** You can also set Noscope only, and Headshot-only modes in Duel.

**8.** You can give ANY weapon to Players.

**9.** You can Execute Server Commands on Duel Start and End.

**10.** You can Enable/Disable Bullet Tracers in Duel Mode.

**11.** You can Enable/Disable **Beacon** on Players.

**12.** You can add/remove **Custom Teleport Points** through **!duel_settings** command (Only for Root Admins).

**13.** You can Disable the Knife Damage in Duel Modes. (By disabling Knife damage you can still able to swap weapons or run fast. Best for the modes like AWP+Noscope)

**14.** You can Enable/Disable Duel Between Bots.

**15.** You can Enable/Disable Duel with Player vs Bot.

**16.** Play Sound with Duel Starts.

**17.** You can Enable/Disable Infinite Ammo in Duel. Infinite Clip and Reserve Ammo

## Commands:
`!duel_settings` - To Set/Delete Custom Teleport Points for Duel. (Only for Root Admins)

## TO DO:
**1.** ~~Add the ability to Disable the Knife Damage in Duel Modes. (By disabling Knife damage you can still able to swap weapons or run fast. Best for the modes like AWP+Noscope)~~

## Configuration:
```
{
  "PluginEnabled": true,          // Enable/Disable Plugin
  "Duel_ForceStart": true,        // Force Start Duel? (true = Yes, false = Vote for Duel)
  "Duel_ShowMenuInCenter": true,  // Location of Duel Voting Menu? (true = Center, false = Chat)
  "Duel_DrawLaserBeam": true,     // Draw a Laser Beam Between Players to find each other if they are Far from Each other. 
  "Duel_Time": 30,                // Duel Time
  "Duel_PrepTime": 3,             // Duel Preperation Time
  "Duel_MinPlayers": 3,           // Minimum Players needed on Server to Start Duel (3 - is the minimum Value, otherwise duel won't start)
  "Duel_DrawPunish": 3,           // What to do with the players when the timer expires? 0 - Nothing, 1 - Kill both, 2 - Kill a random player, 3 - Kill the one with less health (if non of them is given any damage then kill both).
  "Duel_Beacon": true,            // Enable/Disable Beacon on Players
  "Duel_Teleport": true,          // Teleport Players to Custom Teleport Points? (true = Yes, false = No) Set Custom Teleport Points with `!duel_settings` (Only for ROOT Admins) Command. 
  "Duel_FreezePlayers": false,    // Freeze Players During Duel Preparation time? (true = Yes, false = No)
  "Duel_BotAcceptDuel": true,     // Bot do Duel with Player? (true = Yes, false = No)
  "Duel_BotsDoDuel": true,        // Bots do Duel with each other? (true = Yes, false = No)
  "Duel_DuelSoundPath": "",       // Path of the Duel Sound which will play on Duel Start? NOTE: Please Use a very short sound cause there is no way to stop the sound if Duel Ends. ("" = Disabled)
  "Duel_Modes": [
    // Example Duel Mode
    //{
    //  "Weapons": "weapon_awp,weapon_knife",                       // Weapons with Players Fight. (Separate Weapons with ',')
    //  "CMD": "sv_autobunnyhopping 0",                             // Execute Commands on Duel Start. (Separate Commands with ',')
    //  "CMD_End": "sv_autobunnyhopping 1,sv_autobunnyhopping 1",   // Execute Commands on Duel End. (Separate Commands with ',')
    //  "Health": 100,                                              // Health of the Players?
    //  "Armor": 100,                                               // Armor of the Players? No Helmet will be given
    //  "Helmet": 0,                                                // 0 = No Helmet, 1 = Helmet + 100 Armor
    //  "Speed": 2.0,                                               // Speed of the Players? (1.0 = Normal, <1.0 = Slow, >1.0 = Fast)
    //  "Gravity": 0.2,                                             // Gravity of the Players? (1.0 = Normal Gravity, <1.0 = Low Gravity, >1.0 = High Gravity)
    //	"InfiniteAmmo": 2,											// Infinite Ammo (0 = Disable | 1 = Unlimited Clip Ammo | 2 = Unlimited Reserve Ammo)
	//  "NoZoom": true,                                             // Enable Noscope Only? (true = Yes, false = No)
    //  "OnlyHeadshot": true,                                       // Enable Headshot Only? (true = Yes, false = No)
    //  "BulletTracers": true,                                      // Show Bullet Tracers? (true = Yes, false = No)
    //  "DisableKnife": true,                                       // Disable Knife Damage? (true = Yes, false = No)
    //  "Name": "Awp+Noscope"                                       // Duel Mode Name (Required)
    //},
	{
		"Weapons": "weapon_knife",
		"Health": 100,
		"Armor": 0,
		"Helmet": 0,
		"CMD": "sv_autobunnyhopping 1",
		"CMD_End": "sv_autobunnyhopping 0",
		"Name": "Knife Only + Bhop"
    },
	{
		"Weapons": "weapon_knife",
		"Health": 35,
		"Armor": 0,
		"Helmet": 0,
		"Name": "35 HP + Knife Only"
    },
    {
		"Weapons": "weapon_awp,weapon_knife",
		"Health": 100,
		"Armor": 100,
		"Helmet": 0,
		"InfiniteAmmo": 2,
		"NoZoom": true,
		"BulletTracers": true,
		"DisableKnife": true,
		"Name": "Awp+Noscope"
    },
    {
		"Weapons": "weapon_ssg08,weapon_knife",
		"Health": 100,
		"Armor": 100,
		"Helmet": 0,
		"Gravity": 0.2,
		"InfiniteAmmo": 2,
		"BulletTracers": true,
		"DisableKnife": true,
		"Name": "Scout+Gravity"
    },
    {
		"Weapons": "weapon_nova,weapon_knife",
		"Health": 200,
		"Armor": 100,
		"Helmet": 0,
		"Speed": 2,
		"InfiniteAmmo": 2,
		"OnlyHeadshot": true,
		"BulletTracers": true,
		"DisableKnife": true,
		"Name": "Shotgun+Speed"
    },
    {
		"Weapons": "weapon_ak47,weapon_knife",
		"Health": 100,
		"Armor": 100,
		"Helmet": 0,
		"InfiniteAmmo": 2,
		"OnlyHeadshot": true,
		"BulletTracers": true,
		"DisableKnife": true,
		"Name": "AK47+Headshot"
    },
    {
		"Weapons": "weapon_hegrenade,weapon_knife",
		"Health": 100,
		"Armor": 100,
		"Helmet": 0,
		"InfiniteAmmo": 1,
		"BulletTracers": true,
		"DisableKnife": true,
		"Name": "Grenade Only"
    }
  ],
  "ConfigVersion": 1
}
```

