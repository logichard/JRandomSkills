using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Medic : ISkill
    {
        private static Skills skillName = Skills.Medic;
        private static int healAmount = 40;
        private static float healRange = 150.0f;
        private static float taserRechargeTime = 0.05f;
        
        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#42FF5F");
            
            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.1f, () => 
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        if (!Instance.IsPlayerValid(player)) continue;
                        player.RemoveItemByDesignerName("weapon_healthshot");
                        player.RemoveItemByDesignerName("weapon_taser");
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
                
                var activeWeapon = player.Pawn.Value?.WeaponServices?.ActiveWeapon.Value;
                if (activeWeapon?.DesignerName != "weapon_taser") return HookResult.Continue;
                
                HealWithTaser(player);
                
                var taser = activeWeapon.As<CWeaponTaser>();
                Instance.AddTimer(taserRechargeTime, () =>
                {
                    if (!taser.IsValid) return;
                    taser.Clip1 = 1;
                    
                    player.PrintToCenter("Zeus ponownie naładowany!");
                });
                
                return HookResult.Continue;
            });
            
            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!Instance.IsPlayerValid(player)) continue;
                    DisableSkill(player);
                }
                
                return HookResult.Continue;
            });
        }
        
        private static void HealWithTaser(CCSPlayerController medic)
        {
            try
            {
                bool healedSomeone = false;
                
                if (medic?.PlayerPawn?.Value == null || !medic.IsValid)
                    return;
                    
                var medicPosition = medic.PlayerPawn.Value.AbsOrigin;
                if (medicPosition == null) return;
                
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!Instance.IsPlayerValid(player) || player.SteamID == medic.SteamID || player?.PlayerPawn?.Value == null)
                        continue;
                        
                    var targetPosition = player.PlayerPawn.Value.AbsOrigin;
                    if (targetPosition == null) continue;

                    float distance = (medicPosition - targetPosition).Length();
                    
                    if (distance <= healRange)
                    {
                        bool isTeammate = medic.TeamNum == player.TeamNum;
                        
                        if (isTeammate)
                        {
                            int currentHealth = player.PlayerPawn.Value.Health;
                            int maxHealth = 100;
                            
                            if (currentHealth < maxHealth)
                            {
                                int newHealth = Math.Min(currentHealth + healAmount, maxHealth);
                                int actualHeal = newHealth - currentHealth;
                                
                                if (actualHeal > 0)
                                {
                                    UpdateHealthGraphically(player, newHealth);
                                    healedSomeone = true;
                                    medic.PrintToChat($" \x04[jRandomSkills]\x01 Wyleczyłeś gracza {player.PlayerName} o {actualHeal} HP!");
                                    player.PrintToChat($" \x04[jRandomSkills]\x01 Zostałeś wyleczony przez {medic.PlayerName} o {actualHeal} HP!");
                                }
                            }
                        }
                        else
                        {
                            UpdateHealthGraphically(player, 1);
                            healedSomeone = true;
                        }
                    }
                }
                
                if (!healedSomeone)
                {
                    int currentHealth = medic.PlayerPawn.Value.Health;
                    int maxHealth = 100;
                    
                    if (currentHealth < maxHealth)
                    {
                        int newHealth = Math.Min(currentHealth + (healAmount / 2), maxHealth);
                        int actualHeal = newHealth - currentHealth;
                        
                        if (actualHeal > 0)
                        {
                            UpdateHealthGraphically(medic, newHealth);
                            medic.PrintToChat($" \x04[jRandomSkills]\x01 Wyleczyłeś się o {actualHeal} HP!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"Error in HealWithTaser: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void UpdateHealthGraphically(CCSPlayerController player, int newHealth)
        {
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null) return;
            
            pawn.Health = newHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            
            Server.NextFrame(() => 
            {
                if (player.IsValid && player.PlayerPawn?.Value != null)
                {
                    try {
                        var cmd = $"r_portalsopenall 1; r_drawparticles 1";
                        player.ExecuteClientCommand(cmd);
                        
                        player.ExecuteClientCommand("play items/healthshot_success_01.wav");
                    } catch (Exception ex) {
                        Server.PrintToConsole($"Error in visual effect: {ex.Message}");
                    }
                }
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (!player.IsValid || player.PlayerPawn?.Value == null) return;
            
            player.GiveNamedItem("weapon_taser");
 
            int healthshots = Instance.Random.Next(2, 5);
            for (int i = 0; i < healthshots; i++)
                player.GiveNamedItem("weapon_healthshot");
            
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (!player.IsValid || player.PlayerPawn?.Value == null) return;
            
            player.RemoveItemByDesignerName("weapon_healthshot");
            player.RemoveItemByDesignerName("weapon_taser");
        }
    }
}