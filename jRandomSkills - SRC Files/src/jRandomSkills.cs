using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using System.Reflection;

namespace jRandomSkills
{
    public partial class jRandomSkills : BasePlugin
    {
        public static jRandomSkills Instance { get; private set; }

        public List<dSkill_PlayerInfo> skillPlayer { get; } = new List<dSkill_PlayerInfo>();
        public Random Random { get; } = new Random();

        public override string ModuleName => "[CS2] [ jRandomSkills ]";
        public override string ModuleAuthor => "D3X, Juzlus";
        public override string ModuleDescription => "Plugin adds random skills every round for CS2 by D3X. Modified by Juzlus.";
        public override string ModuleVersion => "1.0.4";

        public override void Load(bool hotReload)
        {
            Instance = this;

            Config.Initialize();
            Localization.Load();
            Event.Load();
            PlayerOnTick.Load();
            Command.Load();

            foreach (var skill in Enum.GetValues(typeof(Skills)))
                SkillAction(skill.ToString(), "LoadSkill");
        }

        internal void SkillAction(string skill, string methodName, object[] param = null)
        {
            string className = $"jRandomSkills.{skill}";
            Type? type = Type.GetType(className);

            if (type != null && typeof(ISkill).IsAssignableFrom(type))
            {
                MethodInfo? method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, param);
            }
            else
                Server.PrintToConsole($"Could not find or load {className}");
        }

        internal void RegisterListener<T>(Action onTick, HookMode pre)
        {
            throw new NotImplementedException();
        }

        internal void RegisterListener<T>(Action<object, object> value)
        {
            throw new NotImplementedException();
        }

        internal void AddCommand(string name, string description, CommandInfo.CommandCallback handler)
        {
            var definition = new CommandDefinition(name, description, handler);
            CommandDefinitions.Add(definition);
            CommandManager.RegisterCommand(definition);
        }

        internal bool IsPlayerValid(CCSPlayerController player)
        {
            return player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE;
        }

        public uint[] footstepSoundEvents = { 2026488395, 2745524735, 2684452812, 2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732, 2240518199, 2829617974, 1194677450, 1803111098, 3749333696, 29217150, 1692050905, 2207486967, 2633527058, 3342414459, 988265811, 540697918, 1763490157, 3755338324, 3161194970, 3753692454, 3166948458, 3997353267, 3161194970, 3753692454, 3166948458, 3997353267, 809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576, 737696412, 1403457606, 1770765328, 892882552, 3023174225, 4163677892, 3952104171, 4082928848, 1019414932, 1485322532, 1161855519, 1557420499, 1163426340, 809738584, 3368720745, 2708661994, 2479376962, 3295206520, 1404198078, 1194093029, 1253503839, 2189706910, 1218015996, 96240187, 1116700262, 84876002, 1598540856, 2231399653 };
        public uint[] silentSoundEvents = { 117596568, 117596568, 740474905, 1661204257, 3009312615, 1506215040, 115843229, 3299941720, 1016523349, 2684452812, 2067683805, 2067683805, 1016523349, 4160462271, 1543118744, 585390608, 3802757032, 2302139631, 2546391140, 144629619, 4152012084, 4113422219, 1627020521, 2899365092, 819435812, 3218103073, 961838155, 1535891875, 1826799645, 3460445620, 1818046345, 3666896632, 3099536373, 1440734007, 1409986305, 1939055066, 782454593, 4074593561, 1540837791, 3257325156 };
    }

    public class dSkill_PlayerInfo
    {
        public required ulong SteamID { get; set; }
        public required string PlayerName { get; set; }
        public Skills Skill { get; set; }
        public float? SkillChance { get; set; }
        public bool IsDrawing { get; set; }
    }

    public class dSkill_SkillInfo
    {
        public Skills Skill { get; }
        public string Name { get; }
        public string Description { get; }
        public string Color { get; }

        public bool Display { get; }

        public dSkill_SkillInfo(Skills skill, string color, bool display)
        {
            Skill = skill;
            Name = Localization.GetTranslation(skill.ToString().ToLower());
            Description = Localization.GetTranslation($"{skill.ToString().ToLower()}_desc");
            Color = color;
            Display = display;
        }
    }

    public static class SkillData
    {
        public static List<dSkill_SkillInfo> Skills { get; } = new List<dSkill_SkillInfo>();
    }
}