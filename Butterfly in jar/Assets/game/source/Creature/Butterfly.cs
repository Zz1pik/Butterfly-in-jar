using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Butterfly : MonoBehaviour
{
    public Tilemap blockTilemap;    // Тайлмэп объектов (деревья, растения)
    public Tilemap fireTilemap;     // Тайлмэп огня
    public Tilemap groundTilemap;   // Тайлмэп земли

    private Vector3Int currentGridPosition;
    private Vector3Int lastDirection; // Переменная для хранения последнего направления

    public SpriteRenderer shadow;

    private Main main;
    
    void Start()
    {
        shadow.transform.gameObject.SetActive(true);
        // Устанавливаем начальную позицию бабочки в позицию тайла на groundTilemap
        currentGridPosition = groundTilemap.WorldToCell(transform.position);
        lastDirection = Vector3Int.zero; // Инициализируем как вектор нуля
        main = FindObjectOfType<Main>();
    }
    
    private List<Vector3Int> visitedPlantTiles = new List<Vector3Int>(); // Список посещённых тайлов с растениями

    public void MoveButterfly()
    {
        Vector3Int[] directions = {
            new Vector3Int(0, 1, 0),   // вверх
            new Vector3Int(0, -1, 0),  // вниз
            new Vector3Int(-1, 0, 0),  // влево
            new Vector3Int(1, 0, 0)    // вправо
        };

        List<Vector3Int> plantMoves = new List<Vector3Int>(); // Тайлы с растениями
        List<Vector3Int> normalMoves = new List<Vector3Int>(); // Обычные доступные тайлы
        List<Vector3Int> jarMoves = new List<Vector3Int>();    // Тайлы с банками

        Vector3Int oppositeDirection = Vector3Int.zero;
        bool isFireNearby = false;

        foreach (var direction in directions)
        {
            Vector3Int newPosition = currentGridPosition + direction;

            if (fireTilemap.HasTile(newPosition))
            {
                oppositeDirection = -direction;
                isFireNearby = true;
            }
            else if (IsTileAvailable(newPosition, direction))
            {
                if (IsPlantTile(newPosition) && !visitedPlantTiles.Contains(newPosition))
                {
                    plantMoves.Add(newPosition); // Добавляем новые растения
                }
                else if (IsJar(newPosition))
                {
                    jarMoves.Add(newPosition); // Добавляем банки
                }
                else
                {
                    normalMoves.Add(newPosition); // Добавляем обычные тайлы
                }
            }
        }

        // Если огонь рядом, бабочка должна уйти в противоположное направление
        if (isFireNearby)
        {
            Vector3Int oppositePosition = currentGridPosition + oppositeDirection;
            if (IsTileAvailable(oppositePosition, oppositeDirection) && !IsJar(oppositePosition))
            {
                MoveToNewPosition(oppositePosition);
                return;
            }
        }

        // Если есть новые растения, бабочка идёт на новое растение
        if (plantMoves.Count > 0)
        {
            Vector3Int moveDirection = GetNewestDirection(plantMoves);
            MoveToNewPosition(moveDirection);
            return;
        }

        // Если нет новых растений, двигаемся на обычные тайлы
        if (normalMoves.Count > 0)
        {
            Vector3Int moveDirection = GetRandomDirection(normalMoves);
            MoveToNewPosition(moveDirection);
            return;
        }

        // Если других вариантов нет, бабочка идёт на тайл с банкой
        if (jarMoves.Count > 0)
        {
            Vector3Int moveDirection = GetRandomDirection(jarMoves);
            MoveToNewPosition(moveDirection);
        }
        else
        {
            ButterflyAway();
        }
    }

    // Метод для получения самого нового растения
    private Vector3Int GetNewestDirection(List<Vector3Int> plantMoves)
    {
        // Если необходимо, можете использовать логику, чтобы выбрать самое новое растение
        return plantMoves[0]; // Вернуть первое найденное новое растение
    }


    // Метод для получения всех тайлов с растениями на groundTilemap
    private List<Vector3Int> GetAllPlantTiles()
    {
        List<Vector3Int> plantTiles = new List<Vector3Int>();

        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                if (IsPlantTile(tilePosition))
                {
                    plantTiles.Add(tilePosition);
                }
            }
        }

        return plantTiles;
    }

    // Метод для нахождения ближайшего растения
    private Vector3Int GetClosestDirection(List<Vector3Int> plantMoves)
    {
        Vector3Int closestMove = plantMoves[0];
        float closestDistance = Vector3.Distance(groundTilemap.GetCellCenterWorld(currentGridPosition), groundTilemap.GetCellCenterWorld(closestMove));

        foreach (var move in plantMoves)
        {
            float distance = Vector3.Distance(groundTilemap.GetCellCenterWorld(currentGridPosition), groundTilemap.GetCellCenterWorld(move));
            if (distance < closestDistance)
            {
                closestMove = move;
                closestDistance = distance;
            }
        }

        return closestMove;
    }

    private void MoveToNewPosition(Vector3Int newPosition)
    {
        Vector3 previousPosition = groundTilemap.GetCellCenterWorld(currentGridPosition);
        currentGridPosition = newPosition;

        // Если это тайл с растением, добавляем его в список посещённых
        if (IsPlantTile(newPosition))
        {
            visitedPlantTiles.Add(newPosition);
        }

        lastDirection = newPosition - currentGridPosition; 
        
        Vector3 directionToTarget = groundTilemap.GetCellCenterWorld(newPosition) - transform.position;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;

        transform.DORotateQuaternion(Quaternion.Euler(0, 0, angle), 0.5f);
        transform.DOMove(groundTilemap.GetCellCenterWorld(currentGridPosition), 0.5f).OnComplete(ButterflyTurnEnd);
    }

    private bool IsPlantTile(Vector3Int position)
    {
        TileBase tile = blockTilemap.GetTile(position);

        if (tile != null)
        {
            return tile.name == "PlantTile"; // Замените на имя вашего тайла с растением
        }

        return false; // Тайл отсутствует или это не растение
    }

    // Метод для проверки доступности тайла
    private bool IsTileAvailable(Vector3Int position, Vector3Int direction)
    {
        // Проверяем наличие тайла на groundTilemap
        if (groundTilemap.HasTile(position))
        {
            // Проверяем наличие деревьев (недоступный тайл)
            if (blockTilemap.HasTile(position))
            {
                if (blockTilemap.GetTile(position).name == "PlantTile")
                    return true;

                if (IsJar(position))
                    return true; // Можно пройти через банки

                return false;
            }

            if (fireTilemap.HasTile(position))
                return false;

            // Проверяем наличие огня
            if (fireTilemap.HasTile(position))
            {
                // Если огонь сверху, пойдём вниз
                if (direction == new Vector3Int(0, 1, 0)) return IsTileAvailable(position + new Vector3Int(0, -1, 0), new Vector3Int(0, -1, 0));
                // Если огонь снизу, пойдём вверх
                if (direction == new Vector3Int(0, -1, 0)) return IsTileAvailable(position + new Vector3Int(0, 1, 0), new Vector3Int(0, 1, 0));
                // Если огонь слева, пойдём вправо
                if (direction == new Vector3Int(-1, 0, 0)) return IsTileAvailable(position + new Vector3Int(1, 0, 0), new Vector3Int(1, 0, 0));
                // Если огонь справа, пойдём влево
                if (direction == new Vector3Int(1, 0, 0)) return IsTileAvailable(position + new Vector3Int(-1, 0, 0), new Vector3Int(-1, 0, 0));
            }

            return true; // Тайл доступен для движения
        }
        return false; // Тайл отсутствует
    }

    // Метод для проверки, является ли тайл банкой
    private bool IsJar(Vector3Int position)
    {
        // Получаем тайл из blockTilemap
        TileBase tile = blockTilemap.GetTile(position);

        // Проверяем, существует ли тайл
        if (tile != null)
        {
            return tile.name == "JarOpenTile"; // Замените на фактическое имя вашего тайла
        }

        return false; // Тайл отсутствует или не банка
    }

    // Метод для выбора направления движения
    private Vector3Int GetRandomDirection(List<Vector3Int> possibleMoves)
    {
        Vector3Int selectedMove = possibleMoves[Random.Range(0, possibleMoves.Count)];

        // Проверка, чтобы не двигаться в противоположном направлении
        if (lastDirection != Vector3Int.zero && selectedMove == currentGridPosition + -lastDirection)
        {
            // Если выбранное направление противоположно последнему, выбираем другое
            possibleMoves.Remove(selectedMove);
            selectedMove = possibleMoves[Random.Range(0, possibleMoves.Count)];
        }

        return selectedMove;
    }

    private void ButterflyTurnEnd()
    {
        CheckVictory();

        if (main.wictory)
            return;

        if (main.stepsLeft <= 0)
        {
            ButterflyAway();
        }
        else
        {
            
            main.StartTurn();
        }
    }

    public void ButterflyAway()
    {
        shadow.transform.gameObject.SetActive(false);
        Camera mainCamera = Camera.main;
        Vector3 screenBottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 screenTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        
        // Определяем размеры экрана
        float screenWidth = screenTopRight.x - screenBottomLeft.x;
        float screenHeight = screenTopRight.y - screenBottomLeft.y;
        
        // Задаём минимальные и максимальные координаты для случайной точки за экраном
        Vector3 randomTarget = Vector3.zero;

        // Случайно выбираем одну из четырёх сторон за пределами экрана
        int side = Random.Range(0, 4); // 0 - слева, 1 - справа, 2 - сверху, 3 - снизу

        switch (side)
        {
            case 0: // Слева
                randomTarget = new Vector3(screenBottomLeft.x - Random.Range(2f, 5f), Random.Range(screenBottomLeft.y, screenTopRight.y), 0);
                break;
            case 1: // Справа
                randomTarget = new Vector3(screenTopRight.x + Random.Range(2f, 5f), Random.Range(screenBottomLeft.y, screenTopRight.y), 0);
                break;
            case 2: // Сверху
                randomTarget = new Vector3(Random.Range(screenBottomLeft.x, screenTopRight.x), screenTopRight.y + Random.Range(2f, 5f), 0);
                break;
            case 3: // Снизу
                randomTarget = new Vector3(Random.Range(screenBottomLeft.x, screenTopRight.x), screenBottomLeft.y - Random.Range(2f, 5f), 0);
                break;
        }

        // Рассчитываем направление и угол поворота к выбранной точке
        Vector3 directionToTarget = randomTarget - transform.position;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;

        // Корректируем угол, чтобы бабочка с головой наверху правильно поворачивалась
        angle -= 90f; // Добавляем корректировку на 90 градусов

        // Рассчитываем расстояние до выбранной точки
        float distance = Vector3.Distance(transform.position, randomTarget);
        float speed = 6f; // Скорость движения бабочки, которую можно регулировать
        float animationDuration = distance / speed; // Время анимации исходя из скорости

        float scaleMultiplier = 3f; // Во сколько раз увеличить бабочку
        
        // Анимация поворота, движения и увеличения масштаба
        transform.DORotateQuaternion(Quaternion.Euler(0, 0, angle), 0.5f).OnComplete(() =>
        {
            transform.DOScale(transform.localScale * scaleMultiplier, animationDuration);
            transform.DOMove(randomTarget, animationDuration).SetEase(Ease.Linear).OnComplete(() =>
            {
                DOTween.Kill(transform);
                main.Lose(); // Сообщение в консоль после завершения анимации
            });
        });
    }

    
    public void CheckVictory()
    {
        if (IsJar(currentGridPosition))
        {
            DOTween.Kill(transform);
            main.Victory();
        }
    }
}
