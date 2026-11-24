using UnityEngine;

// One place in the game (Office, Hospital, ...). Each location is one scene, and its levels
// are different setups of that same scene.
[CreateAssetMenu(fileName = "Location", menuName = "Curious Company/Location")]
public class LocationData : ScriptableObject
{
    public string displayName = "Office";
    [Tooltip("Scene file name. The scene must be in File > Build Profiles > Scene List.")]
    public string sceneName = "Office";
    [Tooltip("Off = shown in the menu as Coming Soon and can't be picked.")]
    public bool available = true;

    [Header("Search & Rescue")]
    [Tooltip("In order: 1-1, 1-2, 1-3 ...")]
    public LevelData[] levels;

    [Header("Survival")]
    public WaveConfig survivalWaves;

    public int IndexOf(LevelData level) => System.Array.IndexOf(levels, level);

    public LevelData NextLevel(LevelData level)
    {
        int next = IndexOf(level) + 1;
        return next > 0 && next < levels.Length ? levels[next] : null;
    }
}
