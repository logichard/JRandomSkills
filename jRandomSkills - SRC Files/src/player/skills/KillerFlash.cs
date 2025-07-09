using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class KillerFlash : ISkill
    {
        private static Skills skillName = Skills.KillerFlash;

        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#57bcff");
            
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
            Instance.RegisterEventHandler<EventPlayerBlind>((@event, info) =>
            {
                var player = @event.Userid;
                var attacker = @event.Attacker;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                var attackerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);

                if (attackerInfo?.Skill == skillName && playerInfo?.Skill != Skills.AntyFlash && player?.PlayerPawn.Value.FlashDuration >= 1)
                   player?.PlayerPawn?.Value?.CommitSuicide(false, true);

                return HookResult.Continue;
            });
        }
        public static void EnableSkill(CCSPlayerController player)
        {
            if (!player.IsValid || player.PlayerPawn?.Value == null) return;
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null) return;

            bool hasFlashBang = pawn.WeaponServices!.MyWeapons
                .Where(h => h.IsValid)
                .Select(h => h.Value)
                .Any(w => w != null && w.DesignerName == "weapon_flashbang");
            if (!hasFlashBang)
            {
                player.GiveNamedItem("weapon_flashbang");
            }
        }

    }
}