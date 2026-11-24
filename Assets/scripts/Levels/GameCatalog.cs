using UnityEngine;

// The list of every location in the game, in menu order. Lives at Resources/GameCatalog.
[CreateAssetMenu(fileName = "GameCatalog", menuName = "Curious Company/Game Catalog")]
public class GameCatalog : ScriptableObject
{
    public LocationData[] locations;

    [Tooltip("Opens every level for testing. Turn off to make players finish levels in order.")]
    public bool unlockAllLevels = true;

    static GameCatalog cached;

    public static GameCatalog Load()
    {
        if (cached == null) cached = Resources.Load<GameCatalog>("GameCatalog");
        return cached;
    }
}
