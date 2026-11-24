using TMPro;
using UnityEngine;

// World-space sign at the safe zone. Words it for how many rescues the current level has.
public class SafeZoneSign : MonoBehaviour
{
    public TMP_Text label;

    void Start()
    {
        if (label == null) return;
        int count = GameManager.Instance != null ? GameManager.Instance.totalRescues : 1;
        if (GameSession.Level != null && GameSession.Level.rescueCount > 0) count = GameSession.Level.rescueCount;
        label.text = count <= 1
            ? "RETURN THE SURVIVOR TO THIS SAFE ZONE"
            : $"RETURN ALL {count} SURVIVORS TO THIS SAFE ZONE";
    }
}
