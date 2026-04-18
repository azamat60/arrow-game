using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ArrowDirection { Up, Down, Left, Right }

[System.Serializable]
public class ArrowData
{
    public List<Vector2Int> cells;
    public ArrowDirection   exitDirection;

    public ArrowData(List<Vector2Int> cells, ArrowDirection exitDirection)
    {
        this.cells         = new List<Vector2Int>(cells);
        this.exitDirection = exitDirection;
    }
}

[RequireComponent(typeof(LineRenderer))]
public class ArrowCell : MonoBehaviour
{
    public ArrowData data { get; private set; }

    private GridManager  gridManager;
    private bool         isMoving;
    private LineRenderer line;
    private Transform    arrowHeadTransform;
    private MeshRenderer headMeshRenderer;
    private Color        arrowColor;

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Init(ArrowData arrowData, GridManager manager, Sprite headSprite, Color color)
    {
        data        = arrowData;
        gridManager = manager;
        arrowColor  = color;

        transform.position = Vector3.zero;

        BuildLine(color);
        BuildArrowHead(headSprite, color);
        BuildBodyCollider();
    }

    // ── Visual setup ──────────────────────────────────────────────────────────

    void BuildLine(Color color)
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace     = false;
        line.positionCount     = data.cells.Count;
        float lw               = gridManager.cellSize * gridManager.arrowBodyWidth;
        line.startWidth        = lw;
        line.endWidth          = lw;
        line.numCornerVertices = 16;
        line.numCapVertices    = 8;
        line.sortingOrder      = 1;
        // HDR material: color values > 1 trigger URP Bloom
        line.material          = MakeHDRMaterial(color, gridManager.glowIntensity);
        line.startColor        = Color.white; // color baked into material for HDR
        line.endColor          = Color.white;

        for (int i = 0; i < data.cells.Count; i++)
            line.SetPosition(i, gridManager.GetWorldPosition(data.cells[i].x, data.cells[i].y));
    }

    void BuildArrowHead(Sprite headSprite, Color color)
    {
        var head = new GameObject("ArrowHead");
        head.transform.SetParent(transform);

        Vector2Int last   = data.cells[data.cells.Count - 1];
        Vector3    headWP = gridManager.GetWorldPosition(last.x, last.y);
        head.transform.localPosition = new Vector3(headWP.x, headWP.y, 0f);
        head.transform.rotation      = ExitRotation(data.exitDirection);
        head.transform.localScale    = Vector3.one * (gridManager.cellSize * gridManager.arrowHeadScale);

        var mf  = head.AddComponent<MeshFilter>();
        var mr  = head.AddComponent<MeshRenderer>();
        mr.material     = MakeHDRMaterial(color, gridManager.glowIntensity);
        mr.sortingOrder = 2;

        float h = 0.42f, hw = 0.38f;
        var mesh = new Mesh { name = "ArrowHead" };
        mesh.vertices  = new[] {
            new Vector3(-hw, -h, 0f),
            new Vector3(-hw,  h, 0f),
            new Vector3( hw,  0f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.colors    = new[] { Color.white, Color.white, Color.white };
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        var poly = head.AddComponent<PolygonCollider2D>();
        poly.SetPath(0, new Vector2[] {
            new Vector2(-hw, -h), new Vector2(-hw, h), new Vector2(hw, 0f)
        });

        head.AddComponent<ArrowHeadTap>().owner = this;
        arrowHeadTransform = head.transform;
        headMeshRenderer   = mr;
    }

    void BuildBodyCollider()
    {
        var edge   = gameObject.AddComponent<EdgeCollider2D>();
        var points = new Vector2[data.cells.Count];
        for (int i = 0; i < data.cells.Count; i++)
        {
            Vector3 wp = gridManager.GetWorldPosition(data.cells[i].x, data.cells[i].y);
            points[i]  = new Vector2(wp.x, wp.y);
        }
        edge.points     = points;
        edge.edgeRadius = gridManager.cellSize * 0.18f;
    }

    // HDR material: multiplying color by intensity makes it > 1.0, which URP Bloom picks up
    static Material MakeHDRMaterial(Color color, float intensity)
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        // HDR color: values above 1 make URP Bloom glow
        mat.color = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        return mat;
    }

    // ── Tap ───────────────────────────────────────────────────────────────────

    void OnMouseDown() => TryMove();

    public void TryMove()
    {
        if (isMoving) return;

        if (gridManager.IsPathClear(data))
        {
            isMoving = true;
            gridManager.ClearArrow(this);
            StartCoroutine(MoveOffScreen());
        }
        else
        {
            StartCoroutine(ShakeAnimation());
            StartCoroutine(FlashCollision());
            HeartsManager.Instance?.LoseHeart();
        }
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator MoveOffScreen()
    {
        int     n     = data.cells.Count;
        float   cs    = gridManager.cellSize;
        Vector3 dir   = ExitVector(data.exitDirection);
        float   speed = gridManager.arrowSpeed;

        var pts = new List<Vector3>(n + 256);
        var cum = new List<float>  (n + 256);

        pts.Add(gridManager.GetWorldPosition(data.cells[0].x, data.cells[0].y));
        cum.Add(0f);
        for (int i = 1; i < n; i++)
        {
            pts.Add(gridManager.GetWorldPosition(data.cells[i].x, data.cells[i].y));
            cum.Add(cum[i - 1] + cs);
        }

        float stopDist     = (n + 4) * cs;
        float headTraveled = 0f;
        float elapsed      = 0f;
        float rampDuration = 0.12f;

        int   renderPts = n * 4;
        float snakeLen  = (n - 1) * cs;
        line.positionCount = renderPts;

        while (headTraveled < stopDist)
        {
            elapsed += Time.deltaTime;
            float ramp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / rampDuration));
            float step = speed * ramp * Time.deltaTime;
            headTraveled += step;

            Vector3 newHead = pts[pts.Count - 1] + dir * step;
            pts.Add(newHead);
            cum.Add(cum[cum.Count - 1] + step);

            float headCum = cum[cum.Count - 1];

            for (int j = 0; j < renderPts; j++)
            {
                float   distFromHead = (renderPts - 1 - j) / (float)(renderPts - 1) * snakeLen;
                Vector3 pt           = SamplePath(pts, cum, headCum - distFromHead);
                line.SetPosition(j, pt);
            }

            arrowHeadTransform.localPosition = new Vector3(newHead.x, newHead.y, 0f);
            yield return null;
        }

        Destroy(gameObject);
    }

    static Vector3 SamplePath(List<Vector3> pts, List<float> cum, float d)
    {
        if (d <= cum[0])             return pts[0];
        if (d >= cum[cum.Count - 1]) return pts[pts.Count - 1];

        int lo = 0, hi = cum.Count - 1;
        while (hi - lo > 1) { int mid = (lo + hi) / 2; if (cum[mid] <= d) lo = mid; else hi = mid; }
        return Vector3.Lerp(pts[lo], pts[hi], (d - cum[lo]) / (cum[hi] - cum[lo]));
    }

    IEnumerator ShakeAnimation()
    {
        Vector3 origin  = transform.position;
        float   elapsed = 0f;
        while (elapsed < 0.3f)
        {
            transform.position = origin + (Vector3)(Random.insideUnitCircle * 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = origin;
    }

    IEnumerator FlashCollision()
    {
        SetColor(gridManager.collisionFlashColor);
        yield return new WaitForSeconds(gridManager.collisionFlashDuration);
        if (this != null) SetColor(arrowColor);
    }

    void SetColor(Color c)
    {
        var hdrColor = new Color(c.r * gridManager.glowIntensity,
                                 c.g * gridManager.glowIntensity,
                                 c.b * gridManager.glowIntensity, 1f);
        line.material.color = hdrColor;
        if (headMeshRenderer != null) headMeshRenderer.material.color = hdrColor;
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    public static Quaternion ExitRotation(ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Up:   return Quaternion.Euler(0, 0,  90);
            case ArrowDirection.Down: return Quaternion.Euler(0, 0, -90);
            case ArrowDirection.Left: return Quaternion.Euler(0, 0, 180);
            default:                  return Quaternion.identity;
        }
    }

    public static Vector3 ExitVector(ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Up:   return Vector3.up;
            case ArrowDirection.Down: return Vector3.down;
            case ArrowDirection.Left: return Vector3.left;
            default:                  return Vector3.right;
        }
    }
}

public class ArrowHeadTap : MonoBehaviour
{
    [HideInInspector] public ArrowCell owner;
    void OnMouseDown() => owner.TryMove();
}
