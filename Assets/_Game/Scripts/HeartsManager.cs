using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central game UI manager: radial-gradient background, top header bar
/// (hearts left, "Level X" centre), lives system.
/// [ExecuteAlways] — visible in Editor without pressing Play.
/// Rename the component and rename BackgroundSetup off the scene — this handles both.
/// </summary>
[ExecuteAlways]
public class HeartsManager : MonoBehaviour
{
    public static HeartsManager Instance { get; private set; }

    const int MaxHearts = 3;

    [Header("Heart Sprite")]
    public Sprite heartSprite;

    [Header("Colors")]
    public Color heartFullColor  = new Color(0.90f, 0.20f, 0.20f, 1f);
    public Color heartEmptyColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Glow")]
    [Tooltip("HDR brightness multiplier — >1 triggers URP Bloom")]
    [Range(1f, 6f)]
    public float heartGlowIntensity = 1.5f;

    [Header("Header Bar")]
    [Tooltip("Height of the top header strip in reference-resolution pixels")]
    public float headerHeight = 72f;

    int        currentHearts = MaxHearts;
    Image[]    heartImages;
    TMP_Text   levelLabel;
    GameObject canvasRoot;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (Application.isPlaying) Instance = this;
        Rebuild();
    }

    void OnDisable() => DestroyUI();
    void OnDestroy() => DestroyUI();

#if UNITY_EDITOR
    void OnValidate() =>
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
#endif

    // ── Called by TestLevel at runtime ────────────────────────────────────────

    public void RuntimeInit()
    {
        Instance = this;
        Rebuild();
        RefreshLevelLabel();
    }

    void RefreshLevelLabel()
    {
        if (levelLabel != null)
            levelLabel.text = $"Level {GridManager.currentLevel}";
    }

    // ── Construction ──────────────────────────────────────────────────────────

    void Rebuild()
    {
        DestroyUI();
        RebuildCanvas();
    }

    void RebuildCanvas()
    {
        var sprite = heartSprite != null ? heartSprite : MakeCircleSprite();

        canvasRoot = new GameObject("GameCanvas") { hideFlags = HideFlags.DontSave };
        canvasRoot.transform.SetParent(transform);

        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode  = Application.isPlaying ? RenderMode.ScreenSpaceCamera
                                                    : RenderMode.ScreenSpaceOverlay;
        if (Application.isPlaying) canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingOrder  = 10;

        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasRoot.AddComponent<GraphicRaycaster>();

        // ── Header strip ──────────────────────────────────────────────────────
        var header = new GameObject("Header") { hideFlags = HideFlags.DontSave };
        header.transform.SetParent(canvasRoot.transform, false);

        var headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin        = new Vector2(0f, 1f);
        headerRT.anchorMax        = new Vector2(1f, 1f);
        headerRT.pivot            = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta        = new Vector2(0f, headerHeight);

        var bg = header.AddComponent<Image>();
        bg.color = new Color(0x23 / 255f, 0x0D / 255f, 0x39 / 255f, 1f);

        // ── Hearts row ────────────────────────────────────────────────────────
        float heartSize    = 29f;
        float heartSpacing = 7f;
        float rowWidth     = MaxHearts * heartSize + (MaxHearts - 1) * heartSpacing;
        float sidePad      = 16f;

        var row = new GameObject("HeartsRow") { hideFlags = HideFlags.DontSave };
        row.transform.SetParent(header.transform, false);

        var rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 0.5f);
        rowRT.anchorMax        = new Vector2(0f, 0.5f);
        rowRT.pivot            = new Vector2(0f, 0.5f);
        rowRT.anchoredPosition = new Vector2(sidePad, 0f);
        rowRT.sizeDelta        = new Vector2(rowWidth, heartSize);

        var hg = row.AddComponent<HorizontalLayoutGroup>();
        hg.spacing                = heartSpacing;
        hg.childAlignment         = TextAnchor.MiddleLeft;
        hg.childForceExpandWidth  = false;
        hg.childForceExpandHeight = false;

        heartImages = new Image[MaxHearts];
        for (int i = 0; i < MaxHearts; i++)
        {
            var heartGO = new GameObject($"Heart_{i}") { hideFlags = HideFlags.DontSave };
            heartGO.transform.SetParent(row.transform, false);

            var rt = heartGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(heartSize, heartSize);

            var img = heartGO.AddComponent<Image>();
            img.sprite         = sprite;
            img.preserveAspect = true;
            img.color          = Color.white;
            img.material       = MakeHDRMaterial(HDR(heartFullColor));

            heartImages[i] = img;
        }

        // ── Level label ───────────────────────────────────────────────────────
        var labelGO = new GameObject("LevelLabel") { hideFlags = HideFlags.DontSave };
        labelGO.transform.SetParent(header.transform, false);

        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        levelLabel                  = labelGO.AddComponent<TextMeshProUGUI>();
        levelLabel.text             = $"Level {GridManager.currentLevel}";
        levelLabel.alignment        = TextAlignmentOptions.Center;
        levelLabel.fontSize         = 22f;
        levelLabel.fontStyle        = FontStyles.Bold;
        levelLabel.color            = new Color(heartGlowIntensity, heartGlowIntensity, heartGlowIntensity, 1f);
        levelLabel.enableAutoSizing = false;

        currentHearts = MaxHearts;
    }

    void DestroyUI()
    {
        if (canvasRoot == null) return;
        if (Application.isPlaying) Destroy(canvasRoot);
        else                       DestroyImmediate(canvasRoot);
        canvasRoot  = null;
        heartImages = null;
        levelLabel  = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void LoseHeart()
    {
        if (!Application.isPlaying || currentHearts <= 0) return;
        currentHearts--;
        SetHeartState(heartImages[currentHearts], false);
        if (currentHearts <= 0)
            StartCoroutine(RestartAfterDelay(0.6f));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    void SetHeartState(Image img, bool full)
    {
        img.material.color = full ? HDR(heartFullColor) : heartEmptyColor;
    }

    Color HDR(Color c) => new Color(
        c.r * heartGlowIntensity,
        c.g * heartGlowIntensity,
        c.b * heartGlowIntensity,
        c.a);

    static Material MakeHDRMaterial(Color hdrColor)
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = hdrColor;
        return mat;
    }

    IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentHearts = MaxHearts;
        for (int i = 0; i < MaxHearts; i++) SetHeartState(heartImages[i], true);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    static Sprite MakeCircleSprite()
    {
        var tex    = new Texture2D(64, 64);
        var pixels = new Color[64 * 64];
        var center = new Vector2(32, 32);
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % 64, y = i / 64;
            pixels[i] = Vector2.Distance(new Vector2(x, y), center) < 30f
                        ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
}
