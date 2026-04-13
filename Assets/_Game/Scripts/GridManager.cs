using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 5;
    public int rows = 5;
    public float cellSize = 1.2f;

    [Header("Prefabs")]
    public GameObject arrowPrefab;

    // Двумерный массив — хранит все стрелки на доске
    private ArrowCell[,] grid;

    void Awake()
    {
        grid = new ArrowCell[columns, rows];
        CenterGrid();
    }

    // Центрирует сетку на экране
    void CenterGrid()
    {
        float offsetX = (columns - 1) * cellSize / 2f;
        float offsetY = (rows - 1) * cellSize / 2f;
        transform.position = new Vector3(-offsetX, -offsetY, 0);
    }

    // Возвращает мировую позицию ячейки по координатам сетки
    public Vector3 GetWorldPosition(int col, int row)
    {
        return transform.position + new Vector3(col * cellSize, row * cellSize, 0);
    }

    // Размещает стрелку на сетке
    public void PlaceArrow(int col, int row, ArrowDirection direction)
    {
        if (!IsInBounds(col, row)) return;
        if (grid[col, row] != null) return; // ячейка уже занята

        Vector3 pos = GetWorldPosition(col, row);
        GameObject obj = Instantiate(arrowPrefab, pos, Quaternion.identity, transform);
        
        ArrowCell cell = obj.GetComponent<ArrowCell>();
        cell.Init(col, row, direction, this);
        grid[col, row] = cell;
    }

    // Убирает стрелку с сетки (вызывается когда стрелка уходит)
    public void ClearCell(int col, int row)
    {
        if (!IsInBounds(col, row)) return;
        grid[col, row] = null;

        if (IsGridEmpty())
        {
            Debug.Log("YOU WIN!");
            // TODO: показать экран победы
        }
    }

    // Проверяет — свободен ли путь для стрелки в её направлении
    public bool IsPathClear(int col, int row, ArrowDirection direction)
    {
        int checkCol = col;
        int checkRow = row;

        while (true)
        {
            // Двигаемся на один шаг в направлении стрелки
            switch (direction)
            {
                case ArrowDirection.Up:    checkRow++; break;
                case ArrowDirection.Down:  checkRow--; break;
                case ArrowDirection.Right: checkCol++; break;
                case ArrowDirection.Left:  checkCol--; break;
            }

            // Вышли за пределы — путь свободен до края
            if (!IsInBounds(checkCol, checkRow)) return true;

            // На пути стоит другая стрелка — коллизия
            if (grid[checkCol, checkRow] != null) return false;
        }
    }

    // Проверяет все ячейки — если все null, доска пуста
    bool IsGridEmpty()
    {
        foreach (var cell in grid)
            if (cell != null) return false;
        return true;
    }

    bool IsInBounds(int col, int row)
    {
        return col >= 0 && col < columns && row >= 0 && row < rows;
    }

    // Рисует сетку в редакторе для наглядности
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        float offsetX = (columns - 1) * cellSize / 2f;
        float offsetY = (rows - 1) * cellSize / 2f;
        Vector3 origin = transform.position - new Vector3(offsetX, offsetY, 0);

        for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows; r++)
                Gizmos.DrawWireCube(origin + new Vector3(c * cellSize, r * cellSize, 0),
                                    Vector3.one * (cellSize - 0.1f));
    }
}
