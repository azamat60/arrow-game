using UnityEngine;

// Временный скрипт для теста — расставляет стрелки на сетке
// Удалим когда подключим нормальную систему уровней
public class TestLevel : MonoBehaviour
{
    private GridManager gridManager;

    void Start()
    {
        gridManager = GetComponent<GridManager>();
        LoadTestLevel();
    }

    void LoadTestLevel()
    {
        // Простой тестовый уровень 5x5
        // Формат: PlaceArrow(колонка, ряд, направление)
        gridManager.PlaceArrow(0, 2, ArrowDirection.Left);
        gridManager.PlaceArrow(1, 2, ArrowDirection.Left);
        gridManager.PlaceArrow(2, 4, ArrowDirection.Up);
        gridManager.PlaceArrow(2, 3, ArrowDirection.Up);
        gridManager.PlaceArrow(4, 1, ArrowDirection.Right);
        gridManager.PlaceArrow(3, 1, ArrowDirection.Right);
        gridManager.PlaceArrow(2, 0, ArrowDirection.Down);
    }
}
