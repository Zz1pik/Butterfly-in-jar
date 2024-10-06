using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ButterflyFire : MonoBehaviour
{
    public Tilemap blockTilemap;    // Тайлмэп объектов (деревья, растения)
    public Tilemap fireTilemap;     // Тайлмэп огня
    public Tilemap groundTilemap;   // Тайлмэп земли
    public WorldGrid worldGrid;

    private Vector3Int currentGridPosition;
    private Vector3Int lastDirection; // Переменная для хранения последнего направления

    public SpriteRenderer shadow;

    private Main main;

    void Start()
    {
        shadow.transform.gameObject.SetActive(true);
        currentGridPosition = groundTilemap.WorldToCell(transform.position);
        lastDirection = Vector3Int.zero; // Инициализируем как вектор нуля
        main = FindObjectOfType<Main>();
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
        List<Vector3Int> fireMoves = new List<Vector3Int>();  // Тайлы с огнем
        List<Vector3Int> normalMoves = new List<Vector3Int>(); // Обычные доступные тайлы
        List<Vector3Int> jarMoves = new List<Vector3Int>();    // Тайлы с банками

        foreach (var direction in directions)
        {
            Vector3Int newPosition = currentGridPosition + direction;

            if (IsPlantTile(newPosition) && !visitedPlantTiles.Contains(newPosition))
            {
                plantMoves.Add(newPosition); // Добавляем новые растения
            }
            else if (IsJar(newPosition))
            {
                jarMoves.Add(newPosition); // Добавляем банки
            }
            else if (IsTileAvailable(newPosition))
            {
                normalMoves.Add(newPosition); // Добавляем обычные тайлы
            }

            // Если на тайле нет огня и он доступен, добавляем его как потенциальный
            if (fireTilemap.HasTile(newPosition))
            {
                fireMoves.Add(newPosition);
            }
        }

        // Если бабочка на тайле огня, она может покинуть его
        if (fireMoves.Count > 0)
        {
            Vector3Int fireMove = GetRandomDirection(fireMoves);
            MoveToNewPosition(fireMove);
            return;
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

    private Vector3Int GetNewestDirection(List<Vector3Int> plantMoves)
    {
        return plantMoves[0]; // Вернуть первое найденное новое растение
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
        
        // Если это пустой тайл, оставляем огонь
        if (!fireTilemap.HasTile(newPosition))
        {
            fireTilemap.SetTile(newPosition, worldGrid.fireTile);
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
        return tile != null && tile.name == "PlantTile"; // Замените на имя вашего тайла с растением
    }

    private bool IsTileAvailable(Vector3Int position)
    {
        if (groundTilemap.HasTile(position))
        {
            if (blockTilemap.HasTile(position) && blockTilemap.GetTile(position).name != "PlantTile")
            {
                return false; // Не можем пройти через деревья
            }
            return true; // Тайл доступен для движения
        }
        return false; // Тайл отсутствует
    }

    private bool IsJar(Vector3Int position)
    {
        TileBase tile = blockTilemap.GetTile(position);
        return tile != null && tile.name == "JarOpenTile"; // Замените на фактическое имя вашего тайла
    }

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
        
        float screenWidth = screenTopRight.x - screenBottomLeft.x;
        float screenHeight = screenTopRight.y - screenBottomLeft.y;
        
        Vector3 randomTarget = Vector3.zero;
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

        Vector3 directionToTarget = randomTarget - transform.position;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;
        float distance = Vector3.Distance(transform.position, randomTarget);
        float speed = 6f; // Скорость движения бабочки
        float animationDuration = distance / speed; // Время анимации

        float scaleMultiplier = 3f; // Во сколько раз увеличить бабочку
        
        transform.DORotateQuaternion(Quaternion.Euler(0, 0, angle), 0.5f);
        transform.DOMove(randomTarget, animationDuration).OnComplete(() =>
        {
            shadow.transform.gameObject.SetActive(true);
            Destroy(gameObject);
        });
        transform.DOScale(new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier), animationDuration).OnComplete(() =>
        {
            Destroy(gameObject);
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
