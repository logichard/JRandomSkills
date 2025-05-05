using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using jRandomSkills.src.player;
using System.Runtime.InteropServices;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Aimbot : ISkill
    {
        private static Skills skillName = Skills.Aimbot;
        private static Dictionary<nint, int> hitGroups = new Dictionary<nint, int>();

        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#ff0000");
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        }

        private static HookResult OnTakeDamage(DynamicHook h)
        {
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (!Utils.IsDamageValid(param, param2))
            {
                return HookResult.Continue;
            }

            // Checked in the function above
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            CCSPlayerPawn victimPawn = new CCSPlayerPawn(param.Handle);

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            CCSPlayerController victim = victimPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo == null) return HookResult.Continue;

            if (attacker.PawnIsAlive)
            {
                IntPtr hitGroupPointer = Marshal.ReadIntPtr(param2.Handle, 0x78);
                if (hitGroupPointer != nint.Zero)
                {
                    IntPtr hitGroupOffset = Marshal.ReadIntPtr(hitGroupPointer, 16);
                    if (hitGroupOffset != nint.Zero)
                    {
                        if (playerInfo.Skill == skillName)
                        {
                            int oldValue = Marshal.ReadInt32(hitGroupOffset, 56);
                            hitGroups.TryAdd(hitGroupOffset, Marshal.ReadInt32(hitGroupOffset, 56));
                            Marshal.WriteInt32(hitGroupOffset, 56, (int)HitGroup_t.HITGROUP_HEAD);
                        } else if (hitGroups.TryGetValue(hitGroupOffset, out var hitGroup))
                                Marshal.WriteInt32(hitGroupOffset, 56, hitGroup);
                    }
                }
            }

            return HookResult.Continue;
        }
    }
}