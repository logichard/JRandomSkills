using CounterStrikeSharp.API.Core;

namespace jRandomSkills.src.player;

public interface ISkill
{
    public static void LoadSkill() { }
    public static void EnableSkill(CCSPlayerController player)
    {

    }

    public static void DisableSkill(CCSPlayerController player)
    {

    }
}

public enum Skills
{
    None,
    Dwarf,
    SwapPosition,
    FrozenDecoy,
    Soldier,
    Armored,
    Aimbot,
    Retreat,
    EnemySpawn,
    Zeus,
    RadarHack,
    QuickShot,
    Planter,
    Silent,
    KillerFlash,
    TimeManipulator,
    GodMode,
    RandomWeapon,
    WeaponsSwap,
    Wallhack,
    Avenger,
    Flash,
    PawelJumper,
    BunnyHop,
    Impostor,
    OneShot,
    Muhammed,
    RichBoy,
    Rambo,
    Medic,
    Ghost,
    Chicken,
    Astronaut,
    Disarmament,
    AntyFlash,
    Behind,
    InfiniteAmmo,
    Catapult,
    Dracula,
    Teleporter,
    Saper,
    Phoenix,
    Pilot,
    Shade,
    AntyHead,
    Illusionist,
    Chemic,
}
