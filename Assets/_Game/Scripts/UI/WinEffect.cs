using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Win celebration that plays directly on the game canvas — no overlay panel.
/// Attach to the GridManager GameObject. GridManager.winEffect must point to this.
/// </summary>
public class WinEffect : MonoBehaviour
{
    // ── Pastel confetti palette ───────────────────────────────────────────────

    static readonly Color[] Pastels =
    {
        Color.white,
        new Color(0.68f, 0.85f, 0.90f), // light blue
        new Color(0.72f, 0.93f, 0.72f), // light green
        new Color(1.00f, 0.95f, 0.60f), // yellow
        new Color(1.00f, 0.75f, 0.85f), // pink
        new Color(0.80f, 0.70f, 0.95f), // purple
    };

    static readonly Color DarkInk = new Color(0.10f, 0.10f, 0.18f);

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Text")]
    public Color  textColor        = new Color(0.10f, 0.10f, 0.18f);
    public float  levelFontSize    = 52f;
    public float  solvedFontSize   = 68f;
    public string solvedText       = "Solved!";
    public string nextButtonText   = "Next  →";

    [Header("Button")]
    public Color  buttonColor      = new Color(0.10f, 0.10f, 0.18f);
    public Color  buttonTextColor  = Color.white;
    public float  buttonFontSize   = 26f;

    [Header("Timings (seconds)")]
    [Min(0f)] public float delayLevelText  = 0.4f;
    [Min(0f)] public float delaySolved     = 0.7f;
    [Min(0f)] public float delayNextButton = 0.7f;

    [Header("Confetti")]
    public int   confettiCount   = 150;
    [Min(0f)] public float confettiMinSpeed = 1.5f;
    [Min(0f)] public float confettiMaxSpeed = 9f;
    [Min(0f)] public float confettiMinLife  = 1.4f;
    [Min(0f)] public float confettiMaxLife  = 2.8f;
    [Min(0f)] public float confettiGravity  = 5.5f;

    // ── State ─────────────────────────────────────────────────────────────────

    GridManager  gm;
    Mesh         quadMesh;
    GameObject   confettiRoot;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        gm       = GetComponent<GridManager>();
        quadMesh = BuildQuadMesh();
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public void Show(int levelNumber)
    {
        StartCoroutine(Sequence(levelNumber));
    }

    // ── Main sequence ─────────────────────────────────────────────────────────

    IEnumerator Sequence(int levelNumber)
    {
        // 1. Confetti burst from world centre (grid is centred on 0,0)
        confettiRoot = new GameObject("ConfettiRoot");
        StartCoroutine(BurstConfetti(Vector3.zero, confettiCount));

        // Build all UI elements (initially invisible via scale = zero)
        var winCanvas = BuildCanvas();
        var levelTxt  = BuildLabel(winCanvas, $"Level {levelNumber}", levelFontSize, new Vector2(0f,  48f));
        var solvedTxt = BuildLabel(winCanvas, solvedText,             solvedFontSize, new Vector2(0f,   0f));
        var nextBtn   = BuildNextButton(winCanvas, new Vector2(0f, -58f), winCanvas);

        levelTxt .transform.localScale = Vector3.zero;
        solvedTxt.transform.localScale = Vector3.zero;
        nextBtn  .transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(delayLevelText);
        StartCoroutine(FadeScaleIn(levelTxt, 0.5f));

        yield return new WaitForSeconds(delaySolved);
        StartCoroutine(PopIn(solvedTxt, 0.45f));

        yield return new WaitForSeconds(delayNextButton);
        StartCoroutine(SlideUp(nextBtn, 0.35f));

        // No auto-advance — player must tap "Next →"
    }

    // ── Advance to next level ─────────────────────────────────────────────────

    void Advance(GameObject winCanvas)
    {
        StopAllCoroutines();
        if (confettiRoot) Destroy(confettiRoot);
        if (winCanvas)    Destroy(winCanvas);

        GridManager.currentLevel++;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // ── Canvas / UI helpers ───────────────────────────────────────────────────

    GameObject BuildCanvas()
    {
        var go = new GameObject("WinCanvas");
        var c  = go.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 50;                              // above hearts (99) — wait, hearts are 99, use 98

        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(390f, 844f);
        cs.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    /// Creates a centred TMP label (initially at localScale zero).
    GameObject BuildLabel(GameObject canvas, string text, float fontSize, Vector2 pos)
    {
        var go = new GameObject(text);
        go.transform.SetParent(canvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(420f, 90f);

        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = textColor;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    /// Creates the "Next →" pill button. Clicking calls Advance immediately.
    GameObject BuildNextButton(GameObject canvas, Vector2 pos, GameObject winCanvas)
    {
        // Pill background
        var go = new GameObject("NextButton");
        go.transform.SetParent(canvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(170f, 54f);

        var img   = go.AddComponent<Image>();
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.highlightedColor = new Color(0.25f, 0.25f, 0.40f);
        btn.colors = cb;
        btn.onClick.AddListener(() => Advance(winCanvas));

        // Label
        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);

        var lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var tmp       = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text      = nextButtonText;
        tmp.fontSize  = buttonFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = buttonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    // ── Confetti ──────────────────────────────────────────────────────────────

    IEnumerator BurstConfetti(Vector3 origin, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Color   col    = Pastels[Random.Range(0, Pastels.Length)];
            float   size   = Random.Range(0.04f, 0.10f);
            Vector2 vel    = Random.insideUnitCircle.normalized * Random.Range(confettiMinSpeed, confettiMaxSpeed);
            float   angVel = Random.Range(-420f, 420f);
            float   life   = Random.Range(confettiMinLife, confettiMaxLife);

            var piece = new GameObject("C");
            piece.transform.SetParent(confettiRoot.transform);
            piece.transform.position   = origin;
            piece.transform.localScale = Vector3.one * size;

            var mf = piece.AddComponent<MeshFilter>();
            mf.mesh = quadMesh;

            var mr  = piece.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default")) { color = col };
            mr.material    = mat;
            mr.sortingOrder = 10;

            StartCoroutine(AnimateConfetti(piece, mat, vel, angVel, life));

            // Spawn in small batches to avoid a single-frame spike
            if (i % 30 == 29) yield return null;
        }
    }

    IEnumerator AnimateConfetti(GameObject piece, Material mat, Vector2 vel, float angVel, float life)
    {
        float   elapsed = 0f;
        Vector3 pos     = piece.transform.position;
        float   rot     = Random.Range(0f, 360f);
        Color   col     = mat.color;

        while (elapsed < life && piece != null)
        {
            elapsed += Time.deltaTime;
            vel.y   -= confettiGravity * Time.deltaTime;
            pos     += (Vector3)vel * Time.deltaTime;
            rot     += angVel * Time.deltaTime;

            // Fade out over the last 30 % of lifetime
            col.a = 1f - Mathf.Clamp01((elapsed - life * 0.70f) / (life * 0.30f));

            piece.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, rot));
            mat.color = col;

            yield return null;
        }

        if (piece != null) Destroy(piece);
    }

    // ── UI animations ─────────────────────────────────────────────────────────

    /// Scale 0 → 1 + fade in.
    IEnumerator FadeScaleIn(GameObject go, float dur)
    {
        var cg      = go.AddComponent<CanvasGroup>();
        float elapsed = 0f;

        while (elapsed < dur)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            go.transform.localScale = Vector3.one * t;
            cg.alpha = t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        go.transform.localScale = Vector3.one;
        cg.alpha = 1f;
    }

    /// Scale 0 → 1.2 → 1.0 (elastic pop).
    IEnumerator PopIn(GameObject go, float dur)
    {
        var   cg      = go.AddComponent<CanvasGroup>();
        float elapsed = 0f;

        while (elapsed < dur)
        {
            float t = elapsed / dur;
            float s = t < 0.65f
                ? Mathf.Lerp(0f,   1.2f, t / 0.65f)
                : Mathf.Lerp(1.2f, 1.0f, (t - 0.65f) / 0.35f);

            go.transform.localScale = Vector3.one * s;
            cg.alpha = Mathf.Clamp01(t * 4f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        go.transform.localScale = Vector3.one;
        cg.alpha = 1f;
    }

    /// Slide in from 60 px below + fade in.
    IEnumerator SlideUp(GameObject go, float dur)
    {
        var     cg        = go.AddComponent<CanvasGroup>();
        var     rt        = go.GetComponent<RectTransform>();
        Vector2 target    = rt.anchoredPosition;
        Vector2 start     = target - new Vector2(0f, 60f);
        float   elapsed   = 0f;

        while (elapsed < dur)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            rt.anchoredPosition     = Vector2.Lerp(start, target, t);
            go.transform.localScale = Vector3.one;
            cg.alpha = t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = target;
        cg.alpha = 1f;
    }

    // ── Mesh helper ───────────────────────────────────────────────────────────

    static Mesh BuildQuadMesh()
    {
        var m = new Mesh { name = "ConfettiQuad" };
        m.vertices  = new[] {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
        };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.uv        = new[] {
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(1,1), new Vector2(0,1)
        };
        m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        m.RecalculateNormals();
        return m;
    }
}
