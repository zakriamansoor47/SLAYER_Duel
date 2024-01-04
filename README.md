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

## TO DO:
**1.** Add the ability to Create Custom Teleport Points.

**2.** Add the ability to Disable the Knife Damage in Duel Modes. (By disabling Knife damage you can still able to swap weapons or run fast. Best for the modes like AWP+Noscope)

## Configuration:
```
{
  "PluginEnabled": true,        // Enable/Disable Plugin
  "Duel_ForceStart": true,      // Force Start Duel? (true = Yes, false = Vote for Duel)
  "Duel_DrawLaserBeam": true,   // Draw Laser Beam Between Player to find each other if they are Far from Each other. 
  "Duel_Time": 30,              // Duel Time
  "Duel_PrepTime": 3,           // Duel Preperation Time
  "Duel_MinPlayers": 3,         // Minimum Players needed on Server to Start Duel (3 - is the minimum Value, otherwise duel won't start)
  "Duel_DrawPunish": 3,         // What to do with the players when the timer expires? 0 - Nothing, 1 - Kill both, 2 - Kill a random player, 3 - Kill the one with less health (if non of them given any damage then kill both).
  "Duel_Modes": [
    // Example Duel Mode
    //{
    //  "Weapons": "weapon_awp,weapon_knife",                       // Weapons with Players Fight. (Seperate Commands with ',')
    //  "CMD": "sv_autobunnyhopping 0",                             // Execute Commands on Duel Start. (Seperate Commands with ',')
    //  "CMD_End": "sv_autobunnyhopping 1,sv_autobunnyhopping 1",   // Execute Commands on Duel End. (Seperate Commands with ',')
    //  "Health": 100,                                              // Health of the Players?
    //  "Armor": 100,                                               // Armor of the Players? No Helmet will be given
    //  "Helmet": 0,                                                // 0 = No Helmet, 1 = Helmet + 100 Armor
    //  "Speed": 2.0,                                               // Speed of the Players? (1.0 = Normal, <1.0 = Slow, >1.0 = Fast)
    //  "Gravity": 0.2,                                             // Gravity of the Players? (1.0 = Normal Gravity, <1.0 = Low Gravity, >1.0 = High Gravity)
    //  "NoZoom": true,                                             // Enable Noscope Only? (true = Yes, false = No)
    //  "OnlyHeadshot": true,                                       // Enable Headshot Only? (true = Yes, false = No)
    //  "BulletTracers": true,                                      // Show Bullet Tracers? (true = Yes, false = No)
    //  "Name": "Awp+Noscope"                                       // Duel Mode Name
    //},
    {
      "Weapons": "weapon_knife",
      "CMD": "sv_autobunnyhopping 1",
      "CMD_End": "sv_autobunnyhopping 0",
      "Health": 100,
      "Armor": 100,
      "Helmet": 0,
      "Name": "Knife+Bhop"
    },
    {
      "Weapons": "weapon_knife",
      "Health": 35,
      "Armor": 0,
      "Helmet": 0,
      "Name": "35 HP+Knife"
    },
    {
      "Weapons": "weapon_awp,weapon_knife",
      "Health": 100,
      "Armor": 100,
      "Helmet": 0,
      "NoZoom": true,
      "BulletTracers": true,
      "Name": "Awp+Noscope"
    },
    {
      "Weapons": "weapon_ssg08,weapon_knife",
      "Health": 100,
      "Armor": 100,
      "Helmet": 0,
      "Gravity": 0.2,
      "Name": "Scout+Gravity"
    },
    {
      "Weapons": "weapon_nova,weapon_knife",
      "Health": 200,
      "Armor": 100,
      "Helmet": 1,
      "Speed": 2,
      "OnlyHeadshot": true,
      "Name": "Shotgun+Speed+200 HP"
    },
    {
      "Weapons": "weapon_deagle,weapon_knife",
      "Health": 100,
      "Armor": 100,
      "Helmet": 0,
      "OnlyHeadshot": true,
      "Name": "Deagle+Headshot"
    }
    // Add your Custom Duels
  ],
  "ConfigVersion": 1
}
```

