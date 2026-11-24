using UnityEngine;

// Put this on anything that only exists in some levels (a blocked hallway, a light, a level's zombies).
// It's switched on in a level if that level is in "Shows In Levels", or if the level lists its id
// in LevelData.activeVariants. In every other level it's switched off.
public class LevelVariant : MonoBehaviour
{
    [Tooltip("Levels this object appears in")]
    public LevelData[] showInLevels = new LevelData[0];
    [Tooltip("Optional: an id levels can list in Active Variants instead")]
    public string variantId;

    public bool IsInLevel(LevelData level)
    {
        if (System.Array.IndexOf(showInLevels, level) >= 0) return true;
        return !string.IsNullOrEmpty(variantId) && System.Array.IndexOf(level.activeVariants, variantId) >= 0;
    }
}
