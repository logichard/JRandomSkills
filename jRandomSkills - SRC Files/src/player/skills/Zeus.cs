using System;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Zeus : ISkill
    {
        private static jRandomSkills Instance => jRandomSkills.Instance;

        private static readonly Skills skillName = Skills.Zeus;
        private const float ExtendedRange = 1000.0f;
        private const int TaserDamage = 100;
        private const float EyeHeight = 64.0f;

        public static void LoadSkill()
        {
            if (Config.config?.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#fbff00");
            
            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.1f, () => 
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        if (!Instance.IsPlayerValid(player)) continue;
                        var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill != skillName) continue;
                        EnableSkill(player);
                    }
                });
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventWeaponFire>((@event, info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;

                var activeWeapon = player.Pawn.Value?.WeaponServices?.ActiveWeapon?.Value;
                if (activeWeapon?.DesignerName != "weapon_taser") return HookResult.Continue;

                var taser = activeWeapon.As<CWeaponTaser>();
                if (taser == null) return HookResult.Continue;
                
                Instance.AddTimer(0.1f, () =>
                {
                    if (taser.IsValid)
                    {
                        taser.LastAttackTick = 0;
                        taser.FireTime = 0;
                    }
                });
                
                var pawn = player.Pawn.Value;
                if (pawn == null || pawn.AbsOrigin == null) return HookResult.Continue;
                
                var eyePosition = new Vector(
                    pawn.AbsOrigin.X,
                    pawn.AbsOrigin.Y,
                    pawn.AbsOrigin.Z + EyeHeight
                );
                
                var angles = pawn.AbsRotation ?? new QAngle(0, 0, 0);
                var direction = AngleToDirection(angles);
                var endPosition = new Vector(
                    eyePosition.X + direction.X * ExtendedRange,
                    eyePosition.Y + direction.Y * ExtendedRange,
                    eyePosition.Z + direction.Z * ExtendedRange
                );

                var trace = Utilities.FindAllEntitiesByDesignerName<CCSPlayerPawn>("player");
                CCSPlayerPawn hitPlayer = null;
                float closestDistance = ExtendedRange;

                foreach (var entity in trace)
                {
                    if (entity == null || !entity.IsValid || entity == pawn) continue;
                    var target = entity.As<CCSPlayerPawn>();
                    if (target == null || target.Health <= 0 || target.TeamNum == player.TeamNum) continue;

                    var targetPos = target.AbsOrigin;
                    if (targetPos == null) continue;

                    // Check if target is within the ray's path
                    var toTarget = new Vector(
                        targetPos.X - eyePosition.X,
                        targetPos.Y - eyePosition.Y,
                        targetPos.Z - eyePosition.Z
                    );
                    var distance = toTarget.Length();
                    if (distance > ExtendedRange) continue;

                    var dot = toTarget.X * direction.X + toTarget.Y * direction.Y + toTarget.Z * direction.Z;
                    if (dot < 0) continue;

                    var closestPoint = new Vector(
                        eyePosition.X + direction.X * dot,
                        eyePosition.Y + direction.Y * dot,
                        eyePosition.Z + direction.Z * dot
                    );
                    var distanceToRay = (float)Math.Sqrt(
                        Math.Pow(closestPoint.X - targetPos.X, 2) +
                        Math.Pow(closestPoint.Y - targetPos.Y, 2) +
                        Math.Pow(closestPoint.Z - targetPos.Z, 2)
                    );

                    if (distanceToRay < 100.0f && distance < closestDistance)
                    {
                        hitPlayer = target;
                        closestDistance = distance;
                    }
                }

                if (hitPlayer != null)
                {
                    var targetPlayer = hitPlayer.Controller.Value as CCSPlayerController;
                    if (targetPlayer != null && targetPlayer.IsValid)
                    {
                        hitPlayer.Health = Math.Max(0, hitPlayer.Health - TaserDamage);
                        if (hitPlayer.Health <= 0)
                        {
                            hitPlayer.CommitSuicide(false, true); 
                        }
                    }
                }

                return HookResult.Continue;
            });
        }
        public static void EnableSkill(CCSPlayerController player)
        {
            if (!player.IsValid || player.PlayerPawn?.Value == null) return;
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null) return;

            bool hasZeus = pawn.WeaponServices!.MyWeapons
                .Where(h => h.IsValid)
                .Select(h => h.Value)
                .Any(w => w != null && w.DesignerName == "weapon_taser");
            if (!hasZeus)
            {
                player.GiveNamedItem("weapon_taser");
            }
        }


        private static Vector AngleToDirection(QAngle angles)
        {
            float yaw = angles.Y * (float)Math.PI / 180.0f;
            float pitch = angles.X * (float)Math.PI / 180.0f;
            return new Vector(
                (float)(Math.Cos(pitch) * Math.Cos(yaw)),
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)-Math.Sin(pitch)
            );
        }
    }
}