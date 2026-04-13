using UnityEngine;

// Направления стрелки
public enum ArrowDirection
{
    Up,
    Down,
    Left,
    Right
}

public class ArrowCell : MonoBehaviour
{
    public int col;
    public int row;
    public ArrowDirection direction;

    private GridManager gridManager;
    private bool isMoving = false;

    public void Init(int col, int row, ArrowDirection direction, GridManager manager)
    {
        this.col = col;
        this.row = row;
        this.direction = direction;
        this.gridManager = manager;

        // Поворачиваем спрайт в нужную сторону
        ApplyRotation();
    }

    // Игрок нажал на стрелку
    void OnMouseDown()
    {
        TryMove();
    }

    public void TryMove()
    {
        if (isMoving) return;

        if (gridManager.IsPathClear(col, row, direction))
        {
            // Путь свободен — стрелка уходит с доски
            isMoving = true;
            gridManager.ClearCell(col, row);
            StartCoroutine(MoveOffScreen());
        }
        else
        {
            // Путь заблокирован — потеря сердца
            Debug.Log("COLLISION! Heart lost.");
            // TODO: вызвать HeartsManager.LoseHeart()
            StartCoroutine(ShakeAnimation());
        }
    }

    // Стрелка улетает за пределы экрана
    System.Collections.IEnumerator MoveOffScreen()
    {
        Vector3 moveDir = DirectionToVector(direction);
        float speed = 10f;
        float maxDistance = 20f;
        float traveled = 0f;

        while (traveled < maxDistance)
        {
            float step = speed * Time.deltaTime;
            transform.position += moveDir * step;
            traveled += step;
            yield return null;
        }

        Destroy(gameObject);
    }

    // Анимация тряски при коллизии
    System.Collections.IEnumerator ShakeAnimation()
    {
        Vector3 originalPos = transform.position;
        float duration = 0.3f;
        float elapsed = 0f;
        float magnitude = 0.1f;

        while (elapsed < duration)
        {
            float x = originalPos.x + Random.Range(-magnitude, magnitude);
            float y = originalPos.y + Random.Range(-magnitude, magnitude);
            transform.position = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }

    // Поворачиваем объект в зависимости от направления
    void ApplyRotation()
    {
        switch (direction)
        {
            case ArrowDirection.Up:    transform.rotation = Quaternion.Euler(0, 0, 90);  break;
            case ArrowDirection.Down:  transform.rotation = Quaternion.Euler(0, 0, -90); break;
            case ArrowDirection.Right: transform.rotation = Quaternion.Euler(0, 0, 0);   break;
            case ArrowDirection.Left:  transform.rotation = Quaternion.Euler(0, 0, 180); break;
        }
    }

    Vector3 DirectionToVector(ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Up:    return Vector3.up;
            case ArrowDirection.Down:  return Vector3.down;
            case ArrowDirection.Right: return Vector3.right;
            case ArrowDirection.Left:  return Vector3.left;
            default:                   return Vector3.right;
        }
    }
}
