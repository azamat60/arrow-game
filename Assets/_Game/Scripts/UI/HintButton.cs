using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pill-shaped HINT button at the bottom of the screen.
/// [ExecuteAlways] — visible in Editor without Play.
/// Assign lampSprite in Inspector.
/// </summary>
[ExecuteAlways]
public class HintButton : MonoBehaviour
{
    [Header("Sprite")]
    [Tooltip("Leave empty — loaded from Resources/lamp automatically")]
    public Sprite lampSprite;

    [Header("Appearance")]
    public Color buttonColor  = new Color(0x2E / 255f, 0x18 / 255f, 0x4A / 255f, 1f);
    public Color borderColor  = new Color(0x5A / 255f, 0x3A / 255f, 0x8A / 255f, 1f);
    public Color contentColor = new Color(0.78f, 0.70f, 0.92f, 1f);
    public float buttonWidth  = 280f;
    public float buttonHeight = 56f;
    [Tooltip("Distance from bottom edge in reference-resolution pixels")]
    public float bottomOffset = 90f;

    [Header("Glow")]
    [Range(1f, 6f)]
    public float glowIntensity = 1.5f;

    GameObject canvasRoot;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()  => Rebuild();
    void OnDisable() => DestroyUI();
    void OnDestroy() => DestroyUI();

#if UNITY_EDITOR
    void OnValidate() =>
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
#endif

    // ── Construction ──────────────────────────────────────────────────────────

    void Rebuild()
    {
        DestroyUI();

        canvasRoot = new GameObject("HintCanvas") { hideFlags = HideFlags.DontSave };
        canvasRoot.transform.SetParent(transform);

        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode    = Application.isPlaying ? RenderMode.ScreenSpaceCamera
                                                     : RenderMode.ScreenSpaceOverlay;
        if (Application.isPlaying) canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingOrder  = 10;

        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasRoot.AddComponent<GraphicRaycaster>();

        // ── Border pill (rendered behind, slightly larger) ────────────────────
        int   pillH  = Mathf.RoundToInt(buttonHeight);
        float border = 2f;

        var borderGO = new GameObject("HintBorder") { hideFlags = HideFlags.DontSave };
        borderGO.transform.SetParent(canvasRoot.transform, false);

        var borderRT = borderGO.AddComponent<RectTransform>();
        borderRT.anchorMin        = new Vector2(0.5f, 0f);
        borderRT.anchorMax        = new Vector2(0.5f, 0f);
        borderRT.pivot            = new Vector2(0.5f, 0f);
        borderRT.anchoredPosition = new Vector2(0f, bottomOffset - border);
        borderRT.sizeDelta        = new Vector2(buttonWidth + border * 2f, buttonHeight + border * 2f);

        var borderImg   = borderGO.AddComponent<Image>();
        borderImg.color  = borderColor;
        borderImg.sprite = MakePillSprite(pillH + Mathf.RoundToInt(border * 2f));
        borderImg.type   = Image.Type.Sliced;

        // ── Pill ─────────────────────────────────────────────────────────────
        var pill   = new GameObject("HintPill") { hideFlags = HideFlags.DontSave };
        pill.transform.SetParent(canvasRoot.transform, false);

        var pillRT = pill.AddComponent<RectTransform>();
        pillRT.anchorMin        = new Vector2(0.5f, 0f);
        pillRT.anchorMax        = new Vector2(0.5f, 0f);
        pillRT.pivot            = new Vector2(0.5f, 0f);
        pillRT.anchoredPosition = new Vector2(0f, bottomOffset);
        pillRT.sizeDelta        = new Vector2(buttonWidth, buttonHeight);

        var bg  = pill.AddComponent<Image>();
        bg.color  = buttonColor;
        bg.sprite = MakePillSprite(pillH);
        bg.type   = Image.Type.Sliced;

        var btn    = pill.AddComponent<Button>();
        var cols   = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cols.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cols;
        btn.onClick.AddListener(OnHintClicked);
        btn.targetGraphic = bg;

        // ── Centred content row (icon + label) ───────────────────────────────
        float iconSize = buttonHeight * 0.44f;

        var row = new GameObject("ContentRow") { hideFlags = HideFlags.DontSave };
        row.transform.SetParent(pill.transform, false);

        var rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot     = new Vector2(0.5f, 0.5f);
        rowRT.sizeDelta = new Vector2(0f, buttonHeight);  // width set by ContentSizeFitter

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 10f;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(0, 0, 0, 0);

        var csf = row.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

        // Icon
        var sprite = lampSprite != null ? lampSprite : Resources.Load<Sprite>("lamp");

        var iconGO = new GameObject("LampIcon") { hideFlags = HideFlags.DontSave };
        iconGO.transform.SetParent(row.transform, false);

        var iconLE        = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = iconSize;
        iconLE.preferredHeight = iconSize;

        var iconImg            = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite         = sprite;
        iconImg.color          = contentColor;

        // Label
        var labelGO = new GameObject("HintLabel") { hideFlags = HideFlags.DontSave };
        labelGO.transform.SetParent(row.transform, false);

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text               = "HINT";
        tmp.fontSize           = 20f;
        tmp.fontStyle          = FontStyles.Bold;
        tmp.alignment          = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.color              = new Color(
            contentColor.r * glowIntensity,
            contentColor.g * glowIntensity,
            contentColor.b * glowIntensity,
            1f);
    }

    void DestroyUI()
    {
        if (canvasRoot == null) return;
        if (Application.isPlaying) Destroy(canvasRoot);
        else                       DestroyImmediate(canvasRoot);
        canvasRoot = null;
    }

    // ── Gameplay ──────────────────────────────────────────────────────────────

    void OnHintClicked()
    {
        if (!Application.isPlaying) return;
        Debug.Log("Hint requested");
        // TODO: implement hint logic
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Color HDR(Color c) => new Color(
        c.r * glowIntensity, c.g * glowIntensity, c.b * glowIntensity, c.a);

    // Fully-round pill sprite (height = diameter of end caps)
    static Sprite MakePillSprite(int h)
    {
        int w = h * 2; // wide enough texture; 9-slice makes it stretch
        var tex    = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[w * h];
        float r    = h / 2f - 0.5f;

        for (int i = 0; i < pixels.Length; i++)
        {
            int   px = i % w, py = i / w;
            float cx = Mathf.Clamp(px, r, w - r - 1f);
            float cy = r;
            float dx = px - cx, dy = py - cy;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            float a  = Mathf.Clamp01(r - d + 1f);
            pixels[i] = new Color(1, 1, 1, a);
        }
        tex.SetPixels(pixels);
        tex.Apply();

        int border = h / 2;
        return Sprite.Create(tex, new Rect(0, 0, w, h),
                             new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect,
                             new Vector4(border, border, border, border));
    }
}
