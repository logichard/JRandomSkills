using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using TimerFlags = CounterStrikeSharp.API.Modules.Timers.TimerFlags;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Illusionist : ISkill
    {
        private static jRandomSkills Instance => jRandomSkills.Instance;

        private static readonly Skills skillName = Skills.Illusionist;
        private static readonly float timerCooldown = 12.0f; // Fixed 12-second cooldown

        private const int IllusionDuration = 4;
        private static readonly string defaultCTModel = "characters/models/ctm_sas/ctm_sas.vmdl";
        private static readonly string defaultTModel = "characters/models/tm_phoenix_heavy/tm_phoenix_heavy.vmdl";

        private static readonly Dictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = new();
        private static readonly Dictionary<ulong, IllusionData> activeIllusions = new();
        private static readonly Dictionary<ulong, CSTimer> cooldownTimers = new();

        private class IllusionData
        {
            public CBaseModelEntity? Entity { get; set; }
            public Vector StartPosition { get; set; }
            public Vector Direction { get; set; }
            public float Speed { get; set; } = 250.0f;
            public int TeamNum { get; set; }
            public CCSPlayerController Owner { get; set; }
            public DateTime CreationTime { get; set; }
            public CSTimer? MoveTimer { get; set; }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
            public DateTime LastClick { get; set; }
        }

        public static void LoadSkill()
        {
            if (Config.config?.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#8A2BE2");

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.1f, () =>
                {
                    foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive))
                    {
                        var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill == skillName)
                        {
                            EnableSkill(player);
                        }
                    }
                });
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                CleanupAllIllusions();
                SkillPlayerInfo.Clear();
                cooldownTimers.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var player = @event.Userid;
                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    CleanupIllusion(player.SteamID);
                    SkillPlayerInfo.Remove(player.SteamID);
                    if (cooldownTimers.TryGetValue(player.SteamID, out var timer))
                    {
                        timer.Kill();
                        cooldownTimers.Remove(player.SteamID);
                    }
                }
                return HookResult.Continue;
            });

            Instance.AddCommand("css_useSkill", "Use Skill", (player, _) =>
            {
                if (player == null || !player.IsValid) return;
                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    UseSkill(player);
                }
            });
        }

        private static void UseSkill(CCSPlayerController player)
        {
            if (!player.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null) return;

            if (!SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                skillInfo = new PlayerSkillInfo { SteamID = player.SteamID, CanUse = true, Cooldown = DateTime.MinValue, LastClick = DateTime.MinValue };
                SkillPlayerInfo[player.SteamID] = skillInfo;
            }

            if (skillInfo.CanUse)
            {
                skillInfo.CanUse = false;
                skillInfo.Cooldown = DateTime.Now;
                CreateIllusion(player);

                // One-shot timer to reset CanUse after 12 seconds
                if (cooldownTimers.ContainsKey(player.SteamID))
                {
                    cooldownTimers[player.SteamID].Kill();
                    cooldownTimers.Remove(player.SteamID);
                }
                cooldownTimers[player.SteamID] = Instance.AddTimer(timerCooldown, () =>
                {
                    if (SkillPlayerInfo.TryGetValue(player.SteamID, out var info))
                    {
                        info.CanUse = true;
                        if (player.IsValid)
                            player.PrintToChat($" \x04[jRandomSkills]\x01 Umiejętność \x02Iluzjonista\x01 gotowa!");
                    }
                    cooldownTimers.Remove(player.SteamID);
                });
            }
            else
            {
                skillInfo.LastClick = DateTime.Now;
                float remaining = (float)(skillInfo.Cooldown.AddSeconds(timerCooldown) - DateTime.Now).TotalSeconds;
                player.PrintToChat($" \x04[jRandomSkills]\x01 Umiejętność na \x02odnowieniu\x01! Pozostało: \x02{Math.Max(remaining, 0):F1}s\x01");
            }
        }

        private static void CreateIllusion(CCSPlayerController player)
        {
            var steamId = player.SteamID;
            CleanupIllusion(steamId);

            var pawn = player.PlayerPawn.Value;
            if (pawn == null) return;

            var startPosition = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 10.0f);
            var playerAngle = pawn.EyeAngles;

            float yawRadians = playerAngle.Y * (float)Math.PI / 180.0f;
            var direction = new Vector((float)Math.Cos(yawRadians), (float)Math.Sin(yawRadians), 0);
            float length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
            if (length > 0)
            {
                direction.X /= length;
                direction.Y /= length;
            }

            var illusionEntity = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic_override");
            if (illusionEntity == null)
            {
                return;
            }

            string modelName = "";
            try
            {
                if (pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState != null)
                {
                    modelName = pawn.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;
                }
            }
            catch (Exception ex)
            {
                return;
            }

            if (string.IsNullOrEmpty(modelName))
            {
                modelName = player.Team == CsTeam.CounterTerrorist ? defaultCTModel : defaultTModel;
            }

            Server.NextFrame(() =>
            {
                if (!illusionEntity.IsValid) return;

                illusionEntity.SetModel(modelName);
                illusionEntity.TeamNum = player.TeamNum;
                illusionEntity.Teleport(startPosition, new QAngle(0, playerAngle.Y, 0), new Vector(0, 0, 0));
                illusionEntity.DispatchSpawn();
            });

            var illusion = new IllusionData
            {
                Entity = illusionEntity,
                StartPosition = startPosition,
                Direction = direction,
                TeamNum = player.TeamNum,
                Owner = player,
                CreationTime = DateTime.Now
            };

            activeIllusions[steamId] = illusion;

            // Use one-shot timer for illusion duration
            Instance.AddTimer(IllusionDuration, () => CleanupIllusion(steamId));

            illusion.MoveTimer = Instance.AddTimer(0.1f, () =>
            {
                if (!activeIllusions.ContainsKey(steamId)) return;

                var elapsed = (DateTime.Now - illusion.CreationTime).TotalSeconds;
                var newPos = new Vector(
                    illusion.StartPosition.X + illusion.Direction.X * (float)(illusion.Speed * elapsed),
                    illusion.StartPosition.Y + illusion.Direction.Y * (float)(illusion.Speed * elapsed),
                    illusion.StartPosition.Z
                );

                if (illusion.Entity?.IsValid == true)
                {
                    illusion.Entity.Teleport(newPos, new QAngle(0, playerAngle.Y, 0), new Vector(0, 0, 0));
                }
            }, TimerFlags.REPEAT);
        }

        private static void CleanupIllusion(ulong steamId)
        {
            if (!activeIllusions.TryGetValue(steamId, out var illusion)) return;

            if (illusion.MoveTimer != null)
            {
                illusion.MoveTimer.Kill();
                illusion.MoveTimer = null;
            }

            if (illusion.Entity?.IsValid == true)
            {
                try
                {
                    illusion.Entity.Remove();
                }
                catch (Exception ex)
                {
                    return;
                }
            }

            activeIllusions.Remove(steamId);
        }

        private static void CleanupAllIllusions()
        {
            foreach (var steamId in activeIllusions.Keys.ToList())
            {
                CleanupIllusion(steamId);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (!player.IsValid) return;

            if (SkillPlayerInfo.ContainsKey(player.SteamID))
            {
                DisableSkill(player);
            }

            SkillPlayerInfo[player.SteamID] = new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
                LastClick = DateTime.MinValue
            };
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (!player.IsValid) return;

            CleanupIllusion(player.SteamID);

            if (cooldownTimers.TryGetValue(player.SteamID, out var timer))
            {
                timer.Kill();
                cooldownTimers.Remove(player.SteamID);
            }
        }
    }
}