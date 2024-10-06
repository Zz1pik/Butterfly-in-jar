using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGrid : MonoBehaviour
{
    // Tilemaps for different layers
    public Tilemap groundTilemap;
    public Tilemap blockTilemap;
    public Tilemap fireTilemap;

    // Префаб бабочки
    public GameObject butterflyPrefab; // Префаб бабочки

    // Dictionary to store tile references
    public TileBase groundTile;     // Tile for ground ('+')
    public TileBase fireTile;       // Tile for fire ('f')
    public TileBase treeTile;       // Tile for tree ('t')
    public TileBase plantTile;      // Tile for stone ('p')
    public TileBase jarTile;        // Tile for plant ('p')
    
    private int gridWidth;
    private int gridHeight;

    private Main main;

    public void Start()
    {
        main = FindObjectOfType<Main>();
    }
    
    public void GenerateWorld(string[] worldMap)
    {
        ClearWorld(); // Очищаем предыдущий уровень

        gridHeight = worldMap.Length;
        gridWidth = worldMap[0].Length;

        // Находим координаты центра карты
        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                char tileChar = worldMap[y][x];

                // Смещаем координаты, чтобы (0, 0) был центром карты
                Vector3Int position = new Vector3Int(x - centerX, centerY - y - 1, 0);

                // Размещаем тайлы
                groundTilemap.SetTile(position, groundTile);

                switch (tileChar)
                {
                    case 'f':
                        fireTilemap.SetTile(position, fireTile);
                        break;
                    case 't':
                        blockTilemap.SetTile(position, treeTile);
                        break;
                    case 'p':
                        blockTilemap.SetTile(position, plantTile);
                        break;
                    case 'b':
                        if (main.currentLevelIndex == 10 || main.currentLevelIndex == 11 || main.currentLevelIndex == 12 || main.currentLevelIndex == 13)
                        {
                            Butterfly.CreateButterfly(butterflyPrefab, position, true, blockTilemap, fireTilemap, groundTilemap);
                        }
                        else
                        {
                            Butterfly.CreateButterfly(butterflyPrefab, position, false, blockTilemap, fireTilemap, groundTilemap);
                        }
                        break;
                    case 'j':
                        blockTilemap.SetTile(position, jarTile);
                        break;
                    case '+':
                        // Тайл земли уже размещен, ничего не делаем для '+'.
                        break;
                }
            }
        }
        
        main.AddNewFiresToList();
    
        main.butterfly = FindObjectOfType<Butterfly>();
    }

    public void ClearWorld()
    {
        groundTilemap.ClearAllTiles();
        blockTilemap.ClearAllTiles();
        fireTilemap.ClearAllTiles();

        Butterfly butterfly = FindObjectOfType<Butterfly>();

        if (butterfly != null)
        {
            Destroy(butterfly.gameObject);
        }
    }

}
