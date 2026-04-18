using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Adds URP Post-Processing Bloom to the scene at runtime.
/// Attach to GridManager (or any scene object).
/// Requires: Camera → "Render Post Processing" must be ON (set automatically here).
/// </summary>
public class BloomSetup : MonoBehaviour
{
    [Header("Bloom")]
    [Tooltip("Minimum brightness that starts glowing. 1.05 = only true HDR colors glow")]
    [Range(0f, 2f)]
    public float threshold  = 1.05f;

    [Tooltip("Bloom spread intensity")]
    [Range(0f, 10f)]
    public float intensity  = 2f;

    [Tooltip("How far the glow spreads")]
    [Range(0f, 1f)]
    public float scatter    = 0.65f;

    void Awake()
    {
        // 1. Enable post-processing on the main camera
        var cam = Camera.main;
        if (cam != null)
        {
            var urpData = cam.GetUniversalAdditionalCameraData();
            if (urpData != null)
                urpData.renderPostProcessing = true;
        }

        // 2. Create a global Volume with Bloom
        var go     = new GameObject("BloomVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1;
        DontDestroyOnLoad(go);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom   = profile.Add<Bloom>(true);

        bloom.active                     = true;
        bloom.threshold.value            = threshold;
        bloom.intensity.value            = intensity;
        bloom.scatter.value              = scatter;
        bloom.highQualityFiltering.value = true;

        volume.profile = profile;
    }
}
