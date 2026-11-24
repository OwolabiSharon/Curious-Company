using UnityEngine;

// Settings for Survival mode at one location. Nothing reads these yet; they're for the wave
// spawner once Survival gameplay is built.
[CreateAssetMenu(fileName = "Waves", menuName = "Curious Company/Survival Waves")]
public class WaveConfig : ScriptableObject
{
    [Tooltip("0 = endless")]
    public int waveCount = 0;
    public int zombiesFirstWave = 5;
    public int zombiesAddedPerWave = 3;
    public float secondsBetweenWaves = 10f;
}
