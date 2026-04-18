using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // Survives scene reloads so WinEffect can increment it between levels
    public static int currentLevel = 1;

    [Header("Grid Settings")]
    public int   columns  = 5;
    public int   rows     = 5;
    public float cellSize = 1.2f;

    [Header("Visuals")]
    [Tooltip("Triangle sprite for arrowheads — assign in Inspector")]
    public Sprite  arrowHeadSprite;
    public Color[] arrowColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };


    [Header("Collision Feedback")]
    public Color collisionFlashColor    = Color.red;
    [Min(0.1f)]
    public float collisionFlashDuration = 1.0f;

    [Header("Arrow Appearance & Feel")]
    [Tooltip("How fast arrows slide off the board (units/sec)")]
    [Min(1f)]
    public float arrowSpeed         = 12f;
    [Tooltip("Line thickness as a fraction of cell size (0.1 = thin, 0.5 = fat)")]
    [Range(0.1f, 0.5f)]
    public float arrowBodyWidth     = 0.36f;
    [Tooltip("Arrowhead scale as a fraction of cell size (0.8 = small, 1.4 = big)")]
    [Range(0.5f, 2.0f)]
    public float arrowHeadScale     = 1.0f;

    [Header("Glow (requires URP Bloom — see BloomSetup)")]
    [Tooltip("HDR brightness multiplier. > 1 triggers URP Bloom. 1 = no glow, 3 = strong neon")]
    [Range(1f, 6f)]
    public float glowIntensity = 1.5f;

    // Each element is the ArrowCell occupying that cell (multiple cells → same ArrowCell reference)
    private ArrowCell[,]    grid;
    private List<ArrowCell> allArrows = new List<ArrowCell>();
    private int             colorIndex;
    private WinEffect       winEffect;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        winEffect = GetComponent<WinEffect>() ?? gameObject.AddComponent<WinEffect>();
        // Auto-add Bloom post-processing if not already in scene
        if (FindObjectOfType<BloomSetup>() == null)
            gameObject.AddComponent<BloomSetup>();
        grid = new ArrowCell[columns, rows];
        CenterGrid();
    }

    void CenterGrid()
    {
        float ox = (columns - 1) * cellSize / 2f;
        float oy = (rows    - 1) * cellSize / 2f;
        transform.position = new Vector3(-ox, -oy, 0);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Override grid dimensions at runtime (call before PlaceArrow)
    public void Reinitialize(int cols, int rows, float cs)
    {
        columns    = cols;
        this.rows  = rows;
        cellSize   = cs;
        grid       = new ArrowCell[columns, rows];
        allArrows.Clear();
        colorIndex = 0;
        CenterGrid();
    }

    // Returns the world-space centre of a grid cell
    public Vector3 GetWorldPosition(int col, int row)
        => transform.position + new Vector3(col * cellSize, row * cellSize, 0);

    // Spawns a snake arrow; silently returns if any cell is out of bounds or occupied
    public void PlaceArrow(ArrowData data)
    {
        foreach (var c in data.cells)
        {
            if (!InBounds(c.x, c.y))   { Debug.LogWarning($"PlaceArrow: {c} out of bounds");    return; }
            if (grid[c.x, c.y] != null) { Debug.LogWarning($"PlaceArrow: {c} already occupied"); return; }
        }

        // Create the snake GameObject; [RequireComponent] on ArrowCell auto-adds LineRenderer
        var go   = new GameObject($"Arrow_{data.cells[0]}");
        go.transform.SetParent(transform);
        var cell = go.AddComponent<ArrowCell>();

        Color color = arrowColors[colorIndex++ % arrowColors.Length];
        cell.Init(data, this, arrowHeadSprite, color);

        foreach (var c in data.cells)
            grid[c.x, c.y] = cell;

        allArrows.Add(cell);
    }

    // Called by ArrowCell when it starts its exit animation — frees all of its cells
    public void ClearArrow(ArrowCell arrow)
    {
        foreach (var c in arrow.data.cells)
            if (InBounds(c.x, c.y) && grid[c.x, c.y] == arrow)
                grid[c.x, c.y] = null;

        allArrows.Remove(arrow);

        if (IsGridEmpty())
        {
            if (winEffect != null)
                winEffect.Show(currentLevel);
            else
                Debug.Log($"YOU WIN! Level {currentLevel}");
        }
    }

    // Checks every cell ahead of the arrowhead (in exit direction) for blockage
    public bool IsPathClear(ArrowData data)
    {
        Vector2Int last = data.cells[data.cells.Count - 1];
        Vector2Int step = StepFor(data.exitDirection);
        int col = last.x + step.x;
        int row = last.y + step.y;

        while (InBounds(col, row))
        {
            if (grid[col, row] != null) return false;
            col += step.x;
            row += step.y;
        }
        return true; // reached grid edge without hitting anything
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    bool IsGridEmpty()
    {
        foreach (var c in grid)
            if (c != null) return false;
        return true;
    }

    bool InBounds(int col, int row)
        => col >= 0 && col < columns && row >= 0 && row < rows;

    static Vector2Int StepFor(ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Up:   return Vector2Int.up;
            case ArrowDirection.Down: return Vector2Int.down;
            case ArrowDirection.Left: return Vector2Int.left;
            default:                  return Vector2Int.right; // Right
        }
    }

    // ── Editor gizmos ─────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        float ox = (columns - 1) * cellSize / 2f;
        float oy = (rows    - 1) * cellSize / 2f;

        for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows; r++)
            {
                // Computed independently of transform so it's correct in both edit & play mode
                Vector3 center = new Vector3(c * cellSize - ox, r * cellSize - oy, 0);
                Gizmos.DrawWireCube(center, Vector3.one * (cellSize - 0.1f));
            }
    }
}
