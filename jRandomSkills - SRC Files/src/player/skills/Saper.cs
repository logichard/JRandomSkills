using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Saper : ISkill
    {
        private static Skills skillName = Skills.Saper;

        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#8A2BE2");
            
            Instance.RegisterEventHandler<EventBombBeginplant>((@event, info) =>
            {
                var player = @event.Userid;

                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    var bombEntities = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").ToList();

                    if (bombEntities.Any())
                    {
                        var bomb = bombEntities.FirstOrDefault();
                        if (bomb != null)
                        {
                            bomb.BombPlacedAnimation = false;
                            bomb.ArmedTime = 0.0f;
                        }
                    }
                }

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventBombBegindefuse>((@event, info) =>
            {
                var player = @event.Userid;
                
                if (Instance.IsPlayerValid(player))
                {
                    var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (playerInfo?.Skill == skillName)
                    {
                        var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                        if (plantedBomb != null)
                        {
                            Server.NextFrame(() =>
                            {
                                plantedBomb.DefuseCountDown = 0;
                            });
                        }
                    }
                }

                return HookResult.Continue;
            });
        }
    }
}