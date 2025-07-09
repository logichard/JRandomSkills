using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FrozenDecoy : ISkill
    {
        private static Skills skillName = Skills.FrozenDecoy;
        private static Dictionary<uint, float> gravities = new Dictionary<uint, float>();
        private static List<CCSPlayerPawn> players = new List<CCSPlayerPawn>();
        private static Dictionary<int, CTriggerMultiple> triggers = new Dictionary<int, CTriggerMultiple>();

        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#00eaff");
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
            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                gravities.Clear();
                players.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventDecoyStarted>((@event, @info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;
                var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");
                if (trigger == null) return HookResult.Continue;

                trigger.Collision.SolidType = SolidType_t.SOLID_CAPSULE;
                trigger.Collision.SolidFlags = 0;
                trigger.Spawnflags = 1;
                trigger.Globalname = $"frozen_decoy_{trigger.Index}";
                trigger.Collision.SolidFlags = 1;

                trigger.AbsOrigin.X = @event.X;
                trigger.AbsOrigin.Y = @event.Y;
                trigger.AbsOrigin.Z = @event.Z;

                trigger.Collision.CapsuleRadius = 150;
                trigger.Collision.BoundingRadius = 150;

                trigger.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_TRIGGER;
                trigger.Collision.EnablePhysics = 1;
                trigger.Collision.TriggerBloat = 0;

                trigger.Collision.SurroundType = SurroundingBoundsType_t.USE_OBB_COLLISION_BOUNDS;
                trigger.Collision.CollisionAttribute.CollisionFunctionMask = 39;
                trigger.Collision.CollisionAttribute.CollisionGroup = 2;

                trigger.DispatchSpawn();
                triggers.Add(@event.Entityid, trigger);
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventDecoyDetonate>((@event, @info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;

                if (triggers.TryGetValue(@event.Entityid, out var existingTrigger))
                {
                    existingTrigger.AcceptInput("Kill");
                    triggers.Remove(@event.Entityid);
                }

                return HookResult.Continue;
            });

            Instance.RegisterListener<OnTick>(() =>
            {
                foreach (CCSPlayerPawn player in players)
                {
                    player.VelocityModifier = 0;
                    Utilities.SetStateChanged(player, "CCSPlayerPawn", "m_flVelocityModifier");

                    if ((player.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0)
                        player.GravityScale = float.MaxValue;
                }
            });

            VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(StartTouchFun, HookMode.Post);
            VirtualFunctions.CBaseTrigger_EndTouchFunc.Hook(EndTouchFunc, HookMode.Post);
        }
        public static void EnableSkill(CCSPlayerController player)
        {
            if (!player.IsValid || player.PlayerPawn?.Value == null) return;
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null) return;

            bool hasDecoy = pawn.WeaponServices!.MyWeapons
                .Where(h => h.IsValid)
                .Select(h => h.Value)
                .Any(w => w != null && w.DesignerName == "weapon_decoy");
            if (!hasDecoy)
            {
                player.GiveNamedItem("weapon_decoy");
            }
        }

        private static HookResult StartTouchFun(DynamicHook h)
        {
            CBaseTrigger trigger = h.GetParam<CBaseTrigger>(0);
            CBaseEntity entity = h.GetParam<CBaseEntity>(1);

            if (trigger == null || entity == null)
                return HookResult.Continue;
;
            CCSPlayerPawn player = new CCSPlayerPawn(entity.Handle);
            if (player == null) return HookResult.Continue;

            if (string.IsNullOrEmpty(trigger?.Globalname) || trigger?.Globalname?.StartsWith("frozen_decoy_") == false)
                return HookResult.Continue;

            if (!players.Contains(player))
                players.Add(player);

            gravities.TryAdd(player.Index, player.GravityScale);
            return HookResult.Continue;
        }

        private static HookResult EndTouchFunc(DynamicHook h)
        {
            var trigger = h.GetParam<CBaseTrigger>(0);
            var entity = h.GetParam<CBaseEntity>(1);

            if (trigger == null || entity == null) return HookResult.Continue;

            CCSPlayerPawn player = new CCSPlayerPawn(entity.Handle);
            if (player == null) return HookResult.Continue;

            if (string.IsNullOrEmpty(trigger?.Globalname) || trigger?.Globalname?.StartsWith("frozen_decoy_") == false)
                return HookResult.Continue;

            if (players.Contains(player))
                players.Remove(player);

            float grav = gravities.GetValueOrDefault(player.Index);
            player.GravityScale = Math.Min(grav, 1);
            return HookResult.Continue;
        }
    }
    
}