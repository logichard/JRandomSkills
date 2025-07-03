using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Avenger : ISkill
    {
        private static Skills skillName = Skills.Avenger;
        private static Dictionary<ulong, bool> avengerActive = new Dictionary<ulong, bool>();
        private static readonly string grenadeType = "weapon_hegrenade";
        private static int avengerTime = 5;
        private static Dictionary<ulong, CounterStrikeSharp.API.Modules.Timers.Timer> avengerTimers = new Dictionary<ulong, CounterStrikeSharp.API.Modules.Timers.Timer>();
        private static Color invisibleColor = Color.FromArgb(0, 255, 255, 255);
        private static Color normalColor = Color.FromArgb(255, 255, 255, 255);

        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#FF4242", false);

            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var victim = @event.Userid;
                var attacker = @event.Attacker;

                if (!Instance.IsPlayerValid(victim)) return HookResult.Continue;

                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == victim.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;

                if (avengerActive.ContainsKey(victim.SteamID) && avengerActive[victim.SteamID])
                    return HookResult.Continue;

                victim.Respawn();
                Instance.AddTimer(0.2f, () => victim.Respawn());

                ActivateAvenger(victim, attacker);

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventGrenadeThrown>((@event, info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;

                if (avengerActive.TryGetValue(player.SteamID, out bool isActive) && isActive)
                {
                    DeactivateAvenger(player);
                    Utils.PrintToChat(player, Localization.GetTranslation("avenger_grenade_thrown"), false);
                }

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                ClearAllAvengers();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!Instance.IsPlayerValid(player)) continue;
                    DisableSkill(player);
                }
                ClearAllAvengers();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;

                if (avengerActive.TryGetValue(player.SteamID, out bool isActive) && isActive)
                {
                    DeactivateAvenger(player);
                }

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                var player = @event.Userid;
                if (player != null && avengerActive.ContainsKey(player.SteamID))
                {
                    DeactivateAvenger(player);
                }
                return HookResult.Continue;
            });
        }

        private static void ActivateAvenger(CCSPlayerController player, CCSPlayerController attacker = null)
        {
            ulong steamId = player.SteamID;
            avengerActive[steamId] = true;

            RemovePlayerGrenades(player);
            var grenade = player.GiveNamedItem(grenadeType);
            if (grenade == null)
            {
                DeactivateAvenger(player);
                return;
            }

            SetPlayerVisibility(player, false);

            var timer = Instance.AddTimer(avengerTime, () =>
            {
                if (Instance.IsPlayerValid(player) && avengerActive.TryGetValue(steamId, out bool isActive) && isActive)
                {
                    DeactivateAvenger(player);
                    Utils.PrintToChat(player, Localization.GetTranslation("avenger_time_expired"), false);
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
            avengerTimers[steamId] = timer;

            try
            {
                player.ExecuteClientCommand("play weapons/hegrenade/grenade_throw.wav");
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"Error playing sound: {ex.Message}");
            }
        }

        private static void DeactivateAvenger(CCSPlayerController player)
        {
            ulong steamId = player.SteamID;
            if (!avengerActive.TryGetValue(steamId, out bool isActive) || !isActive) return;

            avengerActive[steamId] = false;
            if (avengerTimers.ContainsKey(steamId))
            {
                avengerTimers.Remove(steamId);
            }

            SetPlayerVisibility(player, true);

            RemovePlayerGrenades(player);
        }

        private static void RemovePlayerGrenades(CCSPlayerController player)
        {
            if (!Instance.IsPlayerValid(player) || player.PlayerPawn?.Value == null) return;

            var grenades = new[] { "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_incgrenade" };
            foreach (var grenade in grenades)
            {
                player.RemoveItemByDesignerName(grenade);
            }
        }

        private static void ClearAllAvengers()
        {
            foreach (var steamId in avengerActive.Keys.ToList())
            {
                avengerActive[steamId] = false;
                avengerTimers.Remove(steamId);
            }
            avengerActive.Clear();
        }

        private static void SetPlayerVisibility(CCSPlayerController player, bool visible)
        {
            if (!Instance.IsPlayerValid(player) || player.PlayerPawn?.Value == null) return;

            var playerPawn = player.PlayerPawn.Value;
            var color = visible ? normalColor : invisibleColor;

            try
            {
                playerPawn.Render = color;
                Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");

                var weapons = playerPawn.WeaponServices?.MyWeapons;
                if (weapons != null)
                {
                    foreach (var weapon in weapons)
                    {
                        if (weapon.Value != null && weapon.Value.IsValid)
                        {
                            weapon.Value.Render = color;
                            Utilities.SetStateChanged(weapon.Value, "CBaseModelEntity", "m_clrRender");
                        }
                    }
                }

                if (!visible)
                {
                    Utils.PrintToChat(player, Localization.GetTranslation("avenger_invisible"), false);
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"Error setting visibility: {ex.Message}");
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            Utils.PrintToChat(player, Localization.GetTranslation("avenger_enabled", avengerTime), false);
            try
            {
                player.ExecuteClientCommand("play items/smallmedkit1.wav");
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            DeactivateAvenger(player);
        }
    }
}