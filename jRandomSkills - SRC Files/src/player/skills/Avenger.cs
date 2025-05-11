using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Avenger : ISkill
    {
        private static Skills skillName = Skills.Avenger;
        private static Dictionary<ulong, bool> avengerActive = new Dictionary<ulong, bool>();
        private static readonly string grenadeType = "weapon_hegrenade"; // Typ granatu: HE
        private static int avengerTime = 5; // Czas na zemstę w sekundach
        private static Dictionary<ulong, CounterStrikeSharp.API.Modules.Timers.Timer> avengerTimers = new Dictionary<ulong, CounterStrikeSharp.API.Modules.Timers.Timer>(); // Timery dla graczy
        private static Color invisibleColor = Color.FromArgb(50, 255, 66, 66); // Czerwonawy przezroczysty kolor
        private static Color normalColor = Color.FromArgb(255, 255, 255, 255); // Normalny kolor

        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            Utils.RegisterSkill(skillName, "#FF4242"); // Czerwony kolor dla Mściciela
            
            // Obsługa śmierci gracza
            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var victim = @event.Userid;
                var attacker = @event.Attacker;
                
                if (!Instance.IsPlayerValid(victim)) return HookResult.Continue;
                
                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == victim.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;
                
                // Sprawdź czy umiejętność nie jest już aktywna
                if (avengerActive.ContainsKey(victim.SteamID) && avengerActive[victim.SteamID]) 
                    return HookResult.Continue;
                
                // Zapisz informacje o osobie, która zabiła gracza (dla celów zemsty)
                if (Instance.IsPlayerValid(attacker) && attacker.TeamNum != victim.TeamNum)
                {
                    // Aktywuj umiejętność Mściciela
                    ActivateAvenger(victim, attacker);
                }
                else
                {
                    // Aktywuj umiejętność Mściciela nawet bez atakującego (np. samobójstwo)
                    ActivateAvenger(victim);
                }
                
                return HookResult.Continue;
            });
            
            // Obsługa rozrzucania granatów
            Instance.RegisterEventHandler<EventGrenadeThrown>((@event, info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;
                
                ulong steamId = player.SteamID;
                if (!avengerActive.ContainsKey(steamId) || !avengerActive[steamId]) 
                    return HookResult.Continue;
                
                // Koniec zemsty po rzuceniu granatu
                DeactivateAvenger(player);
                
                player.PrintToChat($" \x04[jRandomSkills]\x01 Granat zemsty rzucony! Umiejętność dezaktywowana.");
                
                return HookResult.Continue;
            });
            
            // Czyszczenie przy starcie rundy
            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                ClearAllAvengers();
                return HookResult.Continue;
            });
            
            // Czyszczenie na koniec rundy
            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!Instance.IsPlayerValid(player)) continue;
                    DisableSkill(player);
                }
                
                ClearAllAvengers();
                return HookResult.Continue;
            });
            
            // Obsługa respawnu gracza (na wypadek trybów z respawnem)
            Instance.RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
            {
                var player = @event.Userid;
                if (!Instance.IsPlayerValid(player)) return HookResult.Continue;
                
                ulong steamId = player.SteamID;
                if (avengerActive.ContainsKey(steamId) && avengerActive[steamId])
                {
                    DeactivateAvenger(player);
                }
                
                return HookResult.Continue;
            });
        }
        
        private static void ActivateAvenger(CCSPlayerController player, CCSPlayerController attacker = null)
        {
            ulong steamId = player.SteamID;
            
            // Usuń poprzedni timer jeśli istnieje
            if (avengerTimers.ContainsKey(steamId))
            {
                avengerTimers[steamId].Kill();
                avengerTimers.Remove(steamId);
            }
            
            // Ustaw flagę aktywności
            avengerActive[steamId] = true;
            
            // Informuj gracza
            player.PrintToChat($" \x04[jRandomSkills]\x01 Twoja umiejętność \x02Mściciel\x01 została aktywowana! Masz {avengerTime} sekund na rzucenie granatu zemsty!");
            
            if (attacker != null && Instance.IsPlayerValid(attacker))
            {
                player.PrintToChat($" \x04[jRandomSkills]\x01 Zabił Cię {attacker.PlayerName}. Zemścij się!");
            }
            
            // Daj granat (usuń stare granaty HE najpierw)
            RemovePlayerGrenades(player);
            player.GiveNamedItem(grenadeType);
            
            // Ustaw niewidzialność za pomocą metody z Ghost
            SetPlayerVisibility(player, false);
            
            // Ustaw timer na zakończenie umiejętności
            var timer = Instance.AddTimer(avengerTime, () =>
            {
                if (Instance.IsPlayerValid(player) && avengerActive.ContainsKey(steamId) && avengerActive[steamId])
                {
                    DeactivateAvenger(player);
                    player.PrintToChat($" \x04[jRandomSkills]\x01 Czas na zemstę minął!");
                }
                
                if (avengerTimers.ContainsKey(steamId))
                {
                    avengerTimers.Remove(steamId);
                }
            });
            
            // Zapisz timer
            avengerTimers[steamId] = timer;
            
            // Efekt dźwiękowy dla gracza
            try
            {
                player.ExecuteClientCommand("play weapons/hegrenade/grenade_throw.wav");
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"Error playing sound: {ex.Message}");
            }
        }
        
        private static void DeactivateAvenger(CCSPlayerController player)
        {
            ulong steamId = player.SteamID;
            
            // Resetuj flagę aktywności
            if (avengerActive.ContainsKey(steamId))
            {
                avengerActive[steamId] = false;
            }
            
            // Usuń timer jeśli istnieje
            if (avengerTimers.ContainsKey(steamId))
            {
                avengerTimers[steamId].Kill();
                avengerTimers.Remove(steamId);
            }
            
            // Przywróć normalną widoczność
            SetPlayerVisibility(player, true);
        }
        
        private static void RemovePlayerGrenades(CCSPlayerController player)
        {
            if (!player.IsValid || player.PlayerPawn?.Value == null) return;
            
            // Usuń wszystkie granaty HE
            player.RemoveItemByDesignerName(grenadeType);
        }
        
        private static void ClearAllAvengers()
        {
            // Usuń wszystkie aktywne umiejętności Mściciela
            foreach (var steamId in avengerActive.Keys.ToList())
            {
                avengerActive[steamId] = false;
                
                if (avengerTimers.ContainsKey(steamId))
                {
                    avengerTimers[steamId].Kill();
                    avengerTimers.Remove(steamId);
                }
            }
            
            avengerActive.Clear();
        }
        
        private static void SetPlayerVisibility(CCSPlayerController player, bool visible)
        {
            if (!Instance.IsPlayerValid(player)) return;
            
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn != null)
            {
                // Ustaw kolor gracza (widzialny lub niewidzialny)
                var color = visible ? normalColor : invisibleColor;
                
                try
                {
                    // Zastosuj zmiany widoczności na podstawie klasy Ghost
                    playerPawn.Render = color;
                    
                    // Używamy Utilities.SetStateChanged tylko jeśli dostępne
                    try
                    {
                        Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
                    }
                    catch
                    {
                        // Jeśli metoda nie jest dostępna, pomijamy
                        Server.PrintToConsole("Could not set state changed for player visibility");
                    }
                    
                    // Ustaw widoczność broni
                    SetWeaponVisibility(player, visible);
                    
                    // Ustaw cień (jeśli dostępny)
                    if (playerPawn.ShadowStrength != null)
                    {
                        playerPawn.ShadowStrength = visible ? 1.0f : 0.0f;
                    }
                }
                catch (Exception ex)
                {
                    Server.PrintToConsole($"Error setting visibility: {ex.Message}");
                }
                
                // Informuj gracza o stanie widoczności
                if (!visible)
                {
                    ServerUtils.AnnounceToPlayer(player, "Jesteś niewidzialny dla przeciwników!");
                }
            }
        }
        
        private static void SetWeaponVisibility(CCSPlayerController player, bool visible)
        {
            if (!Instance.IsPlayerValid(player)) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return;
            
            var color = visible ? normalColor : invisibleColor;
            
            try
            {
                // Ustaw widoczność aktywnej broni
                var activeWeapon = playerPawn.WeaponServices?.ActiveWeapon.Value;
                if (activeWeapon != null && activeWeapon.IsValid)
                {
                    activeWeapon.Render = color;
                    
                    try
                    {
                        Utilities.SetStateChanged(activeWeapon, "CBaseModelEntity", "m_clrRender");
                    }
                    catch
                    {
                        // Jeśli metoda nie jest dostępna, pomijamy
                    }
                    
                    // Ustaw cień (jeśli dostępny)
                    if (activeWeapon.ShadowStrength != null)
                    {
                        activeWeapon.ShadowStrength = visible ? 1.0f : 0.0f;
                    }
                }
                
                // Ustaw widoczność wszystkich broni gracza
                var myWeapons = playerPawn.WeaponServices?.MyWeapons;
                if (myWeapons != null)
                {
                    foreach (var gun in myWeapons)
                    {
                        var weapon = gun.Value;
                        if (weapon != null && weapon.IsValid)
                        {
                            weapon.Render = color;
                            
                            try
                            {
                                Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
                            }
                            catch
                            {
                                // Jeśli metoda nie jest dostępna, pomijamy
                            }
                            
                            // Ustaw cień (jeśli dostępny)
                            if (weapon.ShadowStrength != null)
                            {
                                weapon.ShadowStrength = visible ? 1.0f : 0.0f;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"Error setting weapon visibility: {ex.Message}");
            }
        }
        
        public static void EnableSkill(CCSPlayerController player)
        {
            player.PrintToChat($" \x04[jRandomSkills]\x01 Otrzymałeś umiejętność \x02Mściciel\x01! Po śmierci będziesz mieć {avengerTime} sekund na rzucenie granatu zemsty.");
            // Dodatkowy efekt dźwiękowy przy uzyskaniu umiejętności
            try
            {
                player.ExecuteClientCommand("play items/smallmedkit1.wav");
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"Error playing sound: {ex.Message}");
            }
        }
        
        public static void DisableSkill(CCSPlayerController player)
        {
            ulong steamId = player.SteamID;
            
            if (avengerActive.ContainsKey(steamId) && avengerActive[steamId])
            {
                DeactivateAvenger(player);
            }
        }
        
        // Pomocnicza klasa do wyświetlania komunikatów na ekranie
        private static class ServerUtils
        {
            public static void AnnounceToPlayer(CCSPlayerController player, string message)
            {
                try
                {
                    // Próba użycia domyślnych funkcji wyświetlania komunikatów na środku ekranu
                    player.PrintToCenter(message);
                    
                    // Wyświetl również na czacie dla pewności
                    player.PrintToChat($" \x04[jRandomSkills]\x01 {message}");
                }
                catch (Exception ex)
                {
                    // Jeśli nie działa, używamy zwykłego chatu
                    player.PrintToChat($" \x04[jRandomSkills]\x01 {message}");
                    Server.PrintToConsole($"Error in AnnounceToPlayer: {ex.Message}");
                }
            }
        }
    }
}