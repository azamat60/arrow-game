using System.Collections.Generic;
using UnityEngine;

public class TestLevel : MonoBehaviour
{
    GridManager gm;

    void Start()
    {
        gm = GetComponent<GridManager>();

        // HeartsManager must be a scene object — just call RuntimeInit on it
        FindFirstObjectByType<HeartsManager>()?.RuntimeInit();

#if UNITY_EDITOR
        LoadQuickTestLevel();   // 2 arrows — fast to complete while testing
#else
        LoadFullLevel();        // real 9×9 level in builds
#endif
    }

    // ── Quick 2-arrow level for fast testing ──────────────────────────────────
    //
    //   Same 9×9 / 0.5f grid as the full level so Inspector settings apply.
    //
    //   A1: row 5, exits Right (immediate)
    //   A2: row 3, exits Left  (immediate)

    void LoadQuickTestLevel()
    {
        gm.Reinitialize(9, 9, 0.5f);

        gm.PlaceArrow(new ArrowData(Cells((0,5),(1,5),(2,5),(3,5),(4,5)), ArrowDirection.Right));
        gm.PlaceArrow(new ArrowData(Cells((4,3),(3,3),(2,3),(1,3),(0,3)), ArrowDirection.Left));
    }

    // ── Full 9×9 puzzle ───────────────────────────────────────────────────────
    //
    //   Solve order:
    //   Free immediately : A1(←) A3(↓) A4(↓) A5(←) A6(↓) A10(←)
    //   After A1+A3      : A2(→)
    //   After A4+A6+A3   : A7(→) A8(→) A9(→)

    void LoadFullLevel()
    {
        gm.Reinitialize(9, 9, 0.5f);

        gm.PlaceArrow(new ArrowData(Cells(
            (8,8),(7,8),(6,8),(5,8),
            (5,7),(6,7),(7,7),
            (7,6),(6,6),(5,6),(4,6),(3,6),(2,6),(1,6),(0,6)),
            ArrowDirection.Left));

        gm.PlaceArrow(new ArrowData(Cells(
            (4,8),(3,8),(2,8),(1,8),(0,8),
            (0,7),(1,7),(2,7),(3,7),(4,7)),
            ArrowDirection.Right));

        gm.PlaceArrow(new ArrowData(Cells(
            (8,7),(8,6),(8,5),(8,4),(8,3),(8,2),(8,1),(8,0)),
            ArrowDirection.Down));

        gm.PlaceArrow(new ArrowData(Cells(
            (6,5),(6,4),(6,3),(6,2),(6,1),(6,0)),
            ArrowDirection.Down));

        gm.PlaceArrow(new ArrowData(Cells(
            (5,5),(4,5),(3,5),(2,5),(1,5),(0,5)),
            ArrowDirection.Left));

        gm.PlaceArrow(new ArrowData(Cells(
            (7,5),(7,4),(7,3),(7,2),(7,1),(7,0)),
            ArrowDirection.Down));

        gm.PlaceArrow(new ArrowData(Cells(
            (0,4),(1,4),(2,4),(3,4),(4,4),(5,4)),
            ArrowDirection.Right));

        gm.PlaceArrow(new ArrowData(Cells(
            (0,3),(1,3),(2,3),(3,3),(4,3),(5,3)),
            ArrowDirection.Right));

        gm.PlaceArrow(new ArrowData(Cells(
            (0,2),(1,2),(2,2),(3,2),(4,2),(5,2)),
            ArrowDirection.Right));

        gm.PlaceArrow(new ArrowData(Cells(
            (0,1),(1,1),(2,1),(3,1),(4,1),(5,1),
            (5,0),(4,0),(3,0),(2,0),(1,0),(0,0)),
            ArrowDirection.Left));
    }

    static List<Vector2Int> Cells(params (int col, int row)[] pts)
    {
        var list = new List<Vector2Int>(pts.Length);
        foreach (var (c, r) in pts) list.Add(new Vector2Int(c, r));
        return list;
    }
}
