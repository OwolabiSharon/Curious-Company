using System;
using UnityEngine;

public enum CampaignDifficulty { Easy, Medium, Hard }

// A seed identifies a run. Retries keep it; New Run changes it.
public static class CampaignSession
{
    public static int Seed { get; private set; }
    public static CampaignDifficulty Difficulty = CampaignDifficulty.Medium;
    public static bool Started;
    public const int MissionCount = 10;
    public static readonly string[] Titles =
    {
        "Hospital • Admissions", "Office • Reception",
        "Hospital • General Ward", "Office • Open Plan",
        "Hospital • Intensive Care", "Office • Archives",
        "Hospital • Isolation", "Office • Executive Floor",
        "Hospital • Last Evacuation", "Office • Rooftop Approach"
    };
    public static readonly string[] Briefings =
    {
        "Search admissions and bring the stranded patients back to the green extraction zone.",
        "Sweep reception and the meeting rooms. Keep your escort close on the return trip.",
        "The ward is overrun. Check both wings before committing to the long corridor.",
        "Clear a route through the workspaces. Gunfire will draw attention from nearby rooms.",
        "Recover the intensive-care survivors. More hostiles are waiting beyond the doors.",
        "Search the records wing. Use the cross-passages to avoid leading enemies into your escort.",
        "Isolation has fallen. Expect larger groups and a longer route to extraction.",
        "Reach the executive offices and bring everyone down safely.",
        "Complete the final hospital evacuation. Every survivor must make it out alive.",
        "Extract the last office workers. This is the final mission of the operation."
    };
    public static string SceneName(int mission) => $"Campaign_{mission + 1:00}";
    public static float IncomingDamage => Difficulty == CampaignDifficulty.Easy ? 0.65f : Difficulty == CampaignDifficulty.Hard ? 1.35f : 1f;
    public static int SurvivorCount(int mission) => 2 + mission / 3;
    public static int EnemyCount(int mission) => 5 + mission * 2 + (int)Difficulty * 2;
    public static float EnemySpeed(int mission) => 1.55f + mission * 0.10f + (int)Difficulty * 0.22f;
    public static int MissionSeed(int mission) => unchecked(Seed * 397 ^ mission * 7919);
    public static void NewRun()
    {
        Seed = Guid.NewGuid().GetHashCode() & int.MaxValue;
        Started = false;
    }
    public static void EnsureRun()
    {
        if (Seed == 0) NewRun();
    }
}
