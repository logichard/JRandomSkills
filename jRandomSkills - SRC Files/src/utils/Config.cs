using jRandomSkills.src.player;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static class Config
    {
        private static readonly string configPath = Path.Combine(Instance.ModuleDirectory, "Config.json");
        public static ConfigModel config;
        private static FileSystemWatcher fileWatcher;

        public static ConfigModel LoadedConfig => config;

        public static void Initialize()
        {
            config = LoadConfig();
            SetupFileWatcher();
        }

        private static ConfigModel LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                Instance.Logger.LogInformation("Config file does not exist. Create a new config file...");
                var defaultConfig = new ConfigModel();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<ConfigModel>(json) ?? new ConfigModel();
            }
            catch (Exception ex)
            {
                Instance.Logger.LogError($"Error when loading the config file.");
                return new ConfigModel();
            }
        }

        public static void SaveConfig(ConfigModel config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Instance.Logger.LogError($"Error when saving the config file: {ex.Message}");
            }
        }

        private static void SetupFileWatcher()
        {
            fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(configPath))
            {
                Filter = Path.GetFileName(configPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            fileWatcher.Changed += (sender, e) => config = LoadConfig();
            fileWatcher.EnableRaisingEvents = true;
        }

        public class ConfigModel
        {
            public Settings Settings { get; set; } = new Settings();
            public SkillInfo[] SkillsInfo = {
                new SkillInfo(Skills.None),
                new SkillInfo(Skills.Dwarf, chanceFrom : .6f, chanceTo : .95f),
                new SkillInfo(Skills.SwapPosition, cooldown: 30),
                new SkillInfo(Skills.FrozenDecoy),
                new SkillInfo(Skills.Soldier, chanceFrom: 1.15f, chanceTo: 1.35f),
                new SkillInfo(Skills.Armored, chanceFrom : .65f, chanceTo : .85f),
                new SkillInfo(Skills.Aimbot, only1v1: true),
                new SkillInfo(Skills.Retreat, cooldown: 15),
                new SkillInfo(Skills.EnemySpawn, cooldown: 15),
                new SkillInfo(Skills.Zeus),
                new SkillInfo(Skills.RadarHack),
                new SkillInfo(Skills.QuickShot),
                new SkillInfo(Skills.Planter, team: 2),
                new SkillInfo(Skills.Silent),
                new SkillInfo(Skills.KillerFlash),
                new SkillInfo(Skills.TimeManipulator, cooldown: 30),
                new SkillInfo(Skills.GodMode, cooldown: 30),
                new SkillInfo(Skills.RandomWeapon, cooldown: 15),
                new SkillInfo(Skills.WeaponsSwap, cooldown: 30),
                new SkillInfo(Skills.Wallhack),
                new SkillInfo(Skills.Flash, chanceFrom : 1.2f, chanceTo : 3.0f),
                new SkillInfo(Skills.PawelJumper),
                new SkillInfo(Skills.BunnyHop),
                new SkillInfo(Skills.Impostor),
                new SkillInfo(Skills.OneShot),
                new SkillInfo(Skills.Muhammed),
                new SkillInfo(Skills.RichBoy),
                new SkillInfo(Skills.Rambo),
                new SkillInfo(Skills.Medic),
                new SkillInfo(Skills.Ghost),
                new SkillInfo(Skills.Chicken),
                new SkillInfo(Skills.Astronaut, chanceFrom : .1f, chanceTo : .7f),
                new SkillInfo(Skills.Disarmament, chanceFrom: .2f, chanceTo: .4f),
                new SkillInfo(Skills.AntyFlash),
                new SkillInfo(Skills.Behind, chanceFrom: .2f, chanceTo: .4f),
                new SkillInfo(Skills.InfiniteAmmo),
                new SkillInfo(Skills.Catapult, chanceFrom : .2f, chanceTo : .4f),
                new SkillInfo(Skills.Dracula),
                new SkillInfo(Skills.Teleporter),
                new SkillInfo(Skills.Saper),
                new SkillInfo(Skills.Phoenix, chanceFrom: .2f, chanceTo: .4f),
                new SkillInfo(Skills.Pilot),
                new SkillInfo(Skills.Shade),
                new SkillInfo(Skills.AntyHead),
                new SkillInfo(Skills.Chemic),
            };
        }

        public class Settings
        {
            public string LangCode { get; set; } = "en";
            public string Set_Skill { get; set; } = "ustawskill, setskill";
            public string SkillsList_Menu { get; set; } = "supermoc, skille, listamocy, supermoce, losowemoce";
            public bool KillerSkillInfo { get; set; } = true;
            public bool TeamMateSkillInfo { get; set; } = true;
            public bool SummaryAfterTheRound { get; set; } = true;
        }

        public class SkillInfo
        {
            public string Name { get; set; }
            public bool Active { get; set; }
            public float Cooldown { get; set; }
            public float ChanceFrom { get; set; }
            public float ChanceTo { get; set; }
            public int Team { get; set; }
            public bool Only1v1 { get; set; }

            public SkillInfo(Skills skill, bool active = true, float cooldown = 15f, float chanceFrom = 1, float chanceTo = 1, int team = 1, bool only1v1 = false)
            {
                Name = skill.ToString();
                Active = active;
                Cooldown = cooldown;
                ChanceFrom = chanceFrom;
                ChanceTo = chanceTo;
                Team = team;
                Only1v1 = only1v1;
            }
        }
    }
}