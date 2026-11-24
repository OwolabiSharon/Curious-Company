using UnityEngine;

// One Search & Rescue level, e.g. Office 1-2. It doesn't have its own scene: it tells the
// location scene how to set itself up.
[CreateAssetMenu(fileName = "Level", menuName = "Curious Company/Level")]
public class LevelData : ScriptableObject
{
    [Tooltip("Shown in the level select, e.g. 1-1")]
    public string levelCode = "1-1";
    public string displayName = "Level";
    [TextArea] public string description;

    [Tooltip("LevelVariant ids to switch on for this level. Every other LevelVariant in the scene is switched off, " +
             "so an empty list means none of them. Objects without a LevelVariant are always kept.")]
    public string[] activeVariants = new string[0];

    [Header("Search & Rescue")]
    [Tooltip("Rescues that must reach the safe zone to win. 0 = keep the GameManager's own value.")]
    public int rescueCount = 0;
    [Tooltip("Added to every zombie's NavMeshAgent speed in this level.")]
    public float zombieSpeedBonus = 0f;
}
