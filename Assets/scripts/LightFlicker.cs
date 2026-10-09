using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Lighting/Light Flicker")]
public class LightFlicker : MonoBehaviour
{
    [Header("Light")]
    [Tooltip("Light to flicker. Leave empty to use a Light on this GameObject.")]
    [SerializeField] private Light targetLight;

    [Header("Timing (seconds)")]
    [Tooltip("Random time between flickers. X is the minimum and Y is the maximum.")]
    [SerializeField] private Vector2 interval = new Vector2(1f, 3f);

    [Tooltip("How long each flicker stays dim. X is the minimum and Y is the maximum.")]
    [SerializeField] private Vector2 dimDuration = new Vector2(0.05f, 0.2f);

    [Tooltip("Time to fade into and out of each flicker. Set to 0 for an instant change.")]
    [Min(0f)]
    [SerializeField] private float fadeDuration;

    [Tooltip("Use real time so flickering continues while the game is paused.")]
    [SerializeField] private bool useUnscaledTime;

    [Header("Brightness")]
    [Tooltip("Flicker intensity relative to the light's starting intensity. 0 is dark, 1 is unchanged, and values above 1 are brighter.")]
    [SerializeField] private Vector2 flickerIntensityMultiplier = new Vector2(0f, 0.35f);

    [Tooltip("Chance that a flicker turns the light completely dark.")]
    [Range(0f, 1f)]
    [SerializeField] private float blackoutChance;

    private Light activeLight;
    private float startingIntensity;

    private void Reset()
    {
        targetLight = GetComponent<Light>();
    }

    private void OnValidate()
    {
        interval = ClampRange(interval);
        dimDuration = ClampRange(dimDuration);
        flickerIntensityMultiplier = ClampRange(flickerIntensityMultiplier);
        fadeDuration = Mathf.Max(0f, fadeDuration);
    }

    private void OnEnable()
    {
        activeLight = targetLight != null ? targetLight : GetComponent<Light>();
        if (activeLight == null)
        {
            Debug.LogWarning("Light Flicker needs a Light reference or a Light on the same GameObject.", this);
            return;
        }

        startingIntensity = activeLight.intensity;
        StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        if (activeLight != null)
        {
            activeLight.intensity = startingIntensity;
            activeLight = null;
        }
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            yield return WaitForDuration(Random.Range(interval.x, interval.y));

            float multiplier = Random.value < blackoutChance
                ? 0f
                : Random.Range(flickerIntensityMultiplier.x, flickerIntensityMultiplier.y);
            float dimmedIntensity = startingIntensity * multiplier;

            yield return FadeTo(dimmedIntensity);
            yield return WaitForDuration(Random.Range(dimDuration.x, dimDuration.y));
            yield return FadeTo(startingIntensity);
        }
    }

    private IEnumerator FadeTo(float targetIntensity)
    {
        if (fadeDuration <= 0f)
        {
            activeLight.intensity = targetIntensity;
            yield break;
        }

        float fromIntensity = activeLight.intensity;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            activeLight.intensity = Mathf.Lerp(fromIntensity, targetIntensity, elapsed / fadeDuration);
            yield return null;
        }

        activeLight.intensity = targetIntensity;
    }

    private IEnumerator WaitForDuration(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private static Vector2 ClampRange(Vector2 range)
    {
        range.x = Mathf.Max(0f, range.x);
        range.y = Mathf.Max(range.x, range.y);
        return range;
    }
}
