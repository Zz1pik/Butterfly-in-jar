using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TileCursor : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap blockTilemap;
    public Tilemap fireTilemap;

    public Tile[] tiles;
    private int currentTileIndex = 0;

    public Image currentTileImage;
    public Image nextTileImage; // Новое изображение для следующего тайла

    private GameObject ghostTile; // Призрачный тайл
    private SpriteRenderer ghostSpriteRenderer; // Рендерер призрачного тайла

    public event Action OnTilePlaced;

    public bool switchComplite = false;
    
    public Main main;

    public void Start()
    {
        // Создание нового объекта и добавление его в сцену
        ghostTile = new GameObject("GhostTile");

        // Добавление компонента SpriteRenderer
        ghostSpriteRenderer = ghostTile.AddComponent<SpriteRenderer>();
        ghostSpriteRenderer.color = new Color(1, 1, 1, 0.8f); // Установлено на 0.3 для большей прозрачности
        ghostSpriteRenderer.sortingOrder = 11; // Убедитесь, что он отображается поверх других объектов
    }

    void Update()
    {
        if (main.butterFlyStep || !main.canPlace)
        {
            ghostSpriteRenderer.enabled = false;
            transform.GetComponent<SpriteRenderer>().enabled = false;
            return;
        }
        
        ghostSpriteRenderer.enabled = true;
        transform.GetComponent<SpriteRenderer>().enabled = true;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPosition = groundTilemap.WorldToCell(mouseWorldPos);
        Vector3 tileCenterPosition = groundTilemap.GetCellCenterWorld(gridPosition);
        ghostTile.transform.position = tileCenterPosition; // Перемещаем призрачный тайл
        transform.position = tileCenterPosition;

        // Проверяем доступность места
        bool canPlaceTile = CanPlaceTile(gridPosition);
        ghostSpriteRenderer.color = canPlaceTile ? new Color(1, 1, 1, 0.8f) : new Color(1, 0, 0, 0.8f); // Красный, если недоступно

        if (Input.GetMouseButtonDown(0) && canPlaceTile && main.stepsLeft > 0)
        {
            PlaceTile(gridPosition);
        }

        if (Input.GetMouseButtonDown(1))
        {
            SwitchTile();
        }
    }

    private bool CanPlaceTile(Vector3Int position)
    {
        // Проверяем, есть ли уже тайл на этой позиции в blockTilemap
        TileBase existingBlockTile = blockTilemap.GetTile(position);
        TileBase existingFireTile = fireTilemap.GetTile(position);
        TileBase existingGroundTile = groundTilemap.GetTile(position);

        Vector3 worldPosition = blockTilemap.GetCellCenterWorld(position);
        Collider2D hitCollider = Physics2D.OverlapPoint(worldPosition);

        if (hitCollider != null)
        {
            Butterfly butterfly = hitCollider.GetComponent<Butterfly>();
            if (butterfly != null)
            {
                return false; // Запрещаем ставить на бабочку
            }
        }
        
        if (existingBlockTile != null)
        {
            if ((existingBlockTile.name == "TreeTile" && tiles[currentTileIndex].name == "FireTile" || existingBlockTile.name == "PlantTile" && tiles[currentTileIndex].name == "FireTile") && existingFireTile == null)
            {
                return true; 
            }
            return false;
        }
        
        if (existingFireTile != null)
            return false;
        
        if (existingGroundTile == null)
            return false;

        return true; // Если ни блокирующего тайла, ни бабочки нет, возвращаем true
    }

    private void PlaceTile(Vector3Int position)
    {
        main.butterFlyStep = true;
        // Получаем название текущего тайла
        string currentTileName = tiles[currentTileIndex].name;

        // Создаем временный объект для анимации
        GameObject tileObject = new GameObject("PlacedTile");
        tileObject.transform.position = currentTileName == "FireTile"
            ? blockTilemap.GetCellCenterWorld(position) // Для огня остается на позиции
            : new Vector3(blockTilemap.GetCellCenterWorld(position).x, 10f, blockTilemap.GetCellCenterWorld(position).z); // Для остальных тайлов позиция над экраном

        // Добавляем SpriteRenderer с текущим тайлом
        SpriteRenderer tileSpriteRenderer = tileObject.AddComponent<SpriteRenderer>();
        tileSpriteRenderer.sprite = tiles[currentTileIndex].sprite;
        tileSpriteRenderer.sortingOrder = 10; // Убедитесь, что он будет отображаться поверх других объектов

        // Анимация
        if (currentTileName == "FireTile")
        {
            // Анимация со скейлом для огня
            tileObject.transform.localScale = Vector3.zero; // Начальный маленький размер
            tileObject.transform.DOScale(Vector3.one, 0.5f).OnComplete(() =>
            {
                fireTilemap.SetTile(position, tiles[currentTileIndex]);
                main.audioSource.PlayOneShot(Resources.Load<AudioClip>("Audio/firePlace"));

                FireInstance newFire = new FireInstance(position, fireTilemap, 3);
                main.activeFires.Add(newFire);

                OnTilePlaced?.Invoke();
                Destroy(tileObject); // Удаляем временный объект
            });
        }
        else
        {
            // Анимация падения с эффектом уплотнения для остальных тайлов
            Sequence tileFallSequence = DOTween.Sequence();

            // Анимируем падение
            tileFallSequence.Append(tileObject.transform.DOMove(blockTilemap.GetCellCenterWorld(position), 0.5f).SetEase(Ease.InBounce));

            // Эффект уплотнения и растяжения
            tileFallSequence.Append(tileObject.transform.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.1f)); // Уплотнение
            tileFallSequence.Append(tileObject.transform.DOScale(Vector3.one, 0.1f)); // Возвращение к норме

            tileFallSequence.OnComplete(() =>
            {
                blockTilemap.SetTile(position, tiles[currentTileIndex]);

                // Воспроизводим звук в зависимости от типа тайла
                if (currentTileName == "TreeTile" || currentTileName == "PlantTile")
                {
                    string audioName = currentTileName == "TreeTile" ? "Audio/treePlace" : "Audio/plantPlace";
                    main.audioSource.PlayOneShot(Resources.Load<AudioClip>(audioName));
                }

                OnTilePlaced?.Invoke();
                Destroy(tileObject); // Удаляем временный объект
            });
        }
    }

    public void SwitchTile()
    {
        if (tiles.Length == 0)
            return; // Если нет тайлов, просто выходим из метода

        currentTileIndex = (currentTileIndex + 1) % tiles.Length; // Переход к следующему тайлу
        UpdateCurrentTileImage(); // Обновляем текущее изображение
        ghostSpriteRenderer.sprite = tiles[currentTileIndex].sprite; // Обновляем спрайт призрачного тайла

        if (main.hasGuide)
            switchComplite = true;
    }

    public void UpdateCurrentTileImage()
    {
        currentTileImage.sprite = tiles[currentTileIndex].sprite;

        // Проверяем количество тайлов для обновления следующего тайла
        if (tiles.Length > 1) // Если тайлов больше 1
        {
            int nextTileIndex = (currentTileIndex + 1) % tiles.Length; // Индекс следующего тайла
            nextTileImage.sprite = tiles[nextTileIndex].sprite; // Обновляем изображение следующего тайла
            nextTileImage.gameObject.SetActive(true); // Делаем активным объект следующего тайла
        }
        else
        {
            nextTileImage.gameObject.SetActive(false); // Делаем неактивным объект следующего тайла, если тайлов только 1
        }

        ghostSpriteRenderer.sprite = tiles[currentTileIndex].sprite; 
    }

}
