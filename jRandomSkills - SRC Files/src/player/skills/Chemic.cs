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
    public class Chemik : ISkill
    {
        private static jRandomSkills Instance => jRandomSkills.Instance;

        private static readonly Skills skillName = Skills.Chemic;
        private const float SmokeRadius = 144.0f;
        private const float DamagePerSecond = 10.0f;
        private const float SmokeDuration = 18.0f;

        private static readonly Dictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = new();
        private static readonly Dictionary<uint, SmokeData> activeSmokes = new();
        private static readonly Dictionary<uint, CCSPlayerController> smokeOwners = new();

        private class SmokeData
        {
            public Vector Position { get; set; }
            public CCSPlayerController Owner { get; set; }
            public DateTime CreationTime { get; set; }
            public CSTimer DamageTimer { get; set; }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool IsActive { get; set; }
        }

        public static void LoadSkill()
        {
            if (Config.config?.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#00FF00");

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
                CleanupAllSmokes();
                SkillPlayerInfo.Clear();
                smokeOwners.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var player = @event.Userid;
                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    CleanupPlayerSmokes(player.SteamID);
                    SkillPlayerInfo.Remove(player.SteamID);
                }
                return HookResult.Continue;
            });

            Instance.RegisterListener<Listeners.OnEntitySpawned>((entity) =>
            {
                if (entity.DesignerName != "smokegrenade_projectile") return;

                var smokeEntity = entity as CSmokeGrenadeProjectile;
                if (smokeEntity == null || smokeEntity.Thrower.Value == null) return;

                var owner = smokeEntity.Thrower.Value.Controller.Value as CCSPlayerController;
                if (owner == null || !owner.IsValid || !SkillPlayerInfo.ContainsKey(owner.SteamID)) return;

                smokeOwners[entity.Index] = owner;

                Server.NextFrame(() =>
                {
                    if (!smokeEntity.IsValid) return;
                    
                    Instance.AddTimer(1.5f, () => HandleSmokeDetonation(smokeEntity, owner));
                });
            });
        }

        private static void HandleSmokeDetonation(CSmokeGrenadeProjectile entity, CCSPlayerController owner)
        {
            if (!entity.IsValid || !owner.IsValid) return;

            var position = new Vector(entity.AbsOrigin.X, entity.AbsOrigin.Y, entity.AbsOrigin.Z);
            var smoke = new SmokeData
            {
                Position = position,
                Owner = owner,
                CreationTime = DateTime.Now
            };

            activeSmokes[entity.Index] = smoke;
            
            smoke.DamageTimer = Instance.AddTimer(1.0f, () =>
            {
                if (!activeSmokes.ContainsKey(entity.Index)) return;

                var elapsed = (DateTime.Now - smoke.CreationTime).TotalSeconds;
                if (elapsed >= SmokeDuration)
                {
                    CleanupSmoke(entity.Index);
                    return;
                }

                foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive))
                {
                    if (player.SteamID == owner.SteamID || player.TeamNum == owner.TeamNum) continue; // Immune to own/team smoke

                    var playerPos = player.PlayerPawn.Value?.AbsOrigin;
                    if (playerPos == null) continue;

                    var distance = (float)Math.Sqrt(
                        Math.Pow(playerPos.X - smoke.Position.X, 2) +
                        Math.Pow(playerPos.Y - smoke.Position.Y, 2) +
                        Math.Pow(playerPos.Z - smoke.Position.Z, 2)
                    );

                    if (distance <= SmokeRadius)
                    {
                        var pawn = player.PlayerPawn.Value;
                        if (pawn.Health > 0)
                        {
                            pawn.Health = Math.Max(0, pawn.Health - (int)DamagePerSecond);
                            if (pawn.Health <= 0)
                            {
                                pawn.CommitSuicide(false, true);
                            }
                        }
                    }
                }
            }, TimerFlags.REPEAT);
            
            Instance.AddTimer(SmokeDuration, () => CleanupSmoke(entity.Index));
        }

        private static void CleanupSmoke(uint entityIndex)
        {
            if (!activeSmokes.TryGetValue(entityIndex, out var smoke)) return;

            if (smoke.DamageTimer != null)
            {
                smoke.DamageTimer.Kill();
                smoke.DamageTimer = null;
            }

            activeSmokes.Remove(entityIndex);
            smokeOwners.Remove(entityIndex);
        }

        private static void CleanupPlayerSmokes(ulong steamId)
        {
            var smokesToRemove = activeSmokes.Where(s => s.Value.Owner.SteamID == steamId).Select(s => s.Key).ToList();
            foreach (var entityIndex in smokesToRemove)
            {
                CleanupSmoke(entityIndex);
            }
        }

        private static void CleanupAllSmokes()
        {
            foreach (var entityIndex in activeSmokes.Keys.ToList())
            {
                CleanupSmoke(entityIndex);
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
                IsActive = true
            };

            player.PrintToChat($" \x04[jRandomSkills]\x01 Otrzymano \x02Chemika\x01! Twoje granaty dymne są teraz trujące!");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (!player.IsValid) return;

            CleanupPlayerSmokes(player.SteamID);
            
        }
    }
}