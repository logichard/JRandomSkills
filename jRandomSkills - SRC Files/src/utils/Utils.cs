using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public static class Utils
    {
        public static void PrintToChat(CCSPlayerController player, string msg, bool isError)
        {
            string checkIcon = isError ? $"{ChatColors.DarkRed}✖{ChatColors.LightRed}" : $"{ChatColors.Green}✔{ChatColors.Lime}";
            player.PrintToChat($" {ChatColors.DarkRed}► {ChatColors.Green}[{ChatColors.DarkRed} jRadnomSkills {ChatColors.Green}] {checkIcon} {msg}");
        }

        public static void RegisterSkill(Skills skill, string color, bool display = true)
        {
            if (!SkillData.Skills.Any(s => s.Skill == skill))
            {
                SkillData.Skills.Add(new dSkill_SkillInfo(skill, color, display));
            }
        }

        public static bool IsDamageValid(CEntityInstance instance, CTakeDamageInfo damageInfo, bool requirePlayer = true)
        {
            if (instance == null || damageInfo == null)
            {
                return false;
            }

            // Victim checks
           // if (!instance.IsValid || instance.DesignerName == null || !instance.DesignerName.Equals("player") || instance.Entity == null || instance.CScriptComponent == null)
           // Idk what is instance.CScriptComponent and why it shoudnt be null :wtf: 
            if (!instance.IsValid || instance.DesignerName == null || !instance.DesignerName.Equals("player") || instance.Entity == null)
            {
                return false;
            }

            if (damageInfo == null || damageInfo.Attacker == null || damageInfo.Attacker.Value == null|| !damageInfo.Attacker.IsValid)
            {
                return false;
            }

            // Check if Attacker is also a player
            if (requirePlayer && !damageInfo.Attacker.Value.DesignerName.Equals("player"))
            {
                return false;
            }

            return true;
        }
    }
}