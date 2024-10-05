using System;
using System.Collections;
using System.Collections.Generic;
using TMPEffects.Components;
using UnityEngine;
using TMPro;
using UnityEngine.Tilemaps;

[System.Serializable]
public class Level
{
    public string[] map;
    public int steps;
    public Tile[] tiles; // Массив тайлов для уровня

    public Level(string[] map, int steps, Tile[] tiles)
    {
        this.map = map;
        this.steps = steps;
        this.tiles = tiles;
    }
}


public class Main : MonoBehaviour
{
    public WorldGrid worldGrid;
    public TileCursor tileCursor; 
    public GameObject winScreen;
    public GameObject loseScreen;
    public Butterfly butterfly;
    
    public Tilemap fireTilemap;

    public TextMeshProUGUI stepsLeftText;
    public TextMeshProUGUI guideText;

    public TMPWriter guideTextWriter;
    
    public int stepsLeft;
    public bool butterFlyStep;
    public bool wictory = false;
    public bool canPlace;
    public bool hasGuide = false;
    
    private Level[] levels;
    private int currentLevelIndex = 2;
    
    public AudioSource audioSource;
    private AudioClip acidBurnSound;
    
    public List<FireInstance> activeFires = new List<FireInstance>();

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    void Start()
    {
        // Получите компонент AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        guideTextWriter = guideText.GetComponent<TMPWriter>();

        tileCursor.OnTilePlaced += HandleTilePlaced;
        
        guideText.gameObject.SetActive(false);

        // Инициализация уровней
        levels = new Level[]
        {
            new Level(new string[]
            {
                "+bj"
            }, 1, new Tile[] {worldGrid.treeTile as Tile}),

            new Level(new string[]
            {
                "+bsjt"
            }, 2, new Tile[] {worldGrid.treeTile as Tile}),
            
            new Level(new string[]
            {
            "ttttt",
            "tb+jt",
            "ttttt"
            }, 2, new Tile[] {worldGrid.treeTile as Tile, worldGrid.stoneTile as Tile}),
            
            new Level(new string[]
            {
                "+++++",
                "+++++",
                "+b+j+",
                "+++++",
                "+++++"
            }, 10, new Tile[] {worldGrid.stoneTile as Tile, worldGrid.treeTile as Tile, worldGrid.fireTile as Tile}) 
        };

        worldGrid.GenerateWorld(levels[currentLevelIndex].map);
        stepsLeft = levels[currentLevelIndex].steps;

        StartTurn();
        UiStart();
    }

    public void StartTurn()
    {
        butterFlyStep = false;

        tileCursor.tiles = null;
        tileCursor.tiles = levels[currentLevelIndex].tiles;
        tileCursor.UpdateCurrentTileImage();

        AddNewFiresToList();
        CheckAndClearFireOnTrees();
        CheckFireStates();

        if (currentLevelIndex == 0 && !hasGuide)
        {
            hasGuide = true;
            guideText.gameObject.SetActive(true);
            StartCoroutine(LevelGuide1()); 
        } 
        else if (currentLevelIndex == 1 && !hasGuide)
        {
            hasGuide = true;
            guideText.gameObject.SetActive(true);
            StartCoroutine(LevelGuide2());
        }
        else if (currentLevelIndex == 2 && !hasGuide)
        {
            hasGuide = true;
            guideText.gameObject.SetActive(true);
            StartCoroutine(LevelGuide3()); 
        }
    }

    private IEnumerator LevelGuide1()
    {
        canPlace = false;
        ShowGuideText("<wave amp=1>This butterfly is beautiful, isn't it?");
        yield return new WaitForSeconds(4f);
        ShowGuideText("<wave amp=1>Put a tree to her left so that she gets into the jar.");
        yield return new WaitForSeconds(2.5f);
        canPlace = true;
    }
    
    private IEnumerator LevelGuide2()
    {
        canPlace = false;
        ShowGuideText("<wave amp=1>So, it looks like she needs to be caught again...");
        yield return new WaitForSeconds(4f); 
        canPlace = true;
        yield return new WaitUntil(() => butterFlyStep);
        canPlace = false;
        ShowGuideText("<wave amp=1>As you can see, she can fly over rocks. Remember, I think this information will be useful to you further.");
        yield return new WaitForSeconds(6f);
        canPlace = true;
    }
    
    private IEnumerator LevelGuide3()
    {
        canPlace = false;
        ShowGuideText("<wave amp=1>It looks like we're at an impasse!");
        yield return new WaitForSeconds(3f); 
        ShowGuideText("<wave amp=1>It's good that I've saved an extra tile especially for such cases! Click the <jump>LMB</jump> to change the tile and place he!</wave>");
        yield return new WaitForSeconds(7f); 
        canPlace = true;
        yield return new WaitUntil(() => butterFlyStep);
        canPlace = false;
        ShowGuideText("<wave amp=1>Now the chances of passing have increased! Just put it in a jar!");
        yield return new WaitForSeconds(3.5f);
        canPlace = true;
    }

    public void ShowGuideText(string messageText)
    {
        guideText.text = messageText;
    }

    public void CheckFireStates()
    {
        for (int i = activeFires.Count - 1; i >= 0; i--)
        {
            activeFires[i].UpdateFire();
            
            if (activeFires[i].turnsLeft <= 0)
            {
                activeFires.RemoveAt(i);
            }
        }
    }
    
    private void AddNewFiresToList()
    {
        // Проходим по всем тайлам на fireTilemap
        BoundsInt bounds = fireTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);

                // Проверяем, есть ли тайл огня на этой позиции
                if (fireTilemap.HasTile(tilePosition))
                {
                    // Проверяем, есть ли уже огонь на этой позиции в activeFires
                    if (!IsFireInstanceAtPosition(tilePosition))
                    {
                        // Если огня еще нет в списке, добавляем новый
                        FireInstance newFire = new FireInstance(tilePosition, fireTilemap, 3);
                        activeFires.Add(newFire);
                    }
                }
            }
        }
    }

    // Метод для проверки, есть ли огонь на данной позиции в списке
    private bool IsFireInstanceAtPosition(Vector3Int position)
    {
        foreach (FireInstance fire in activeFires)
        {
            if (fire.position == position)
            {
                return true; // Огонь уже есть на этой позиции
            }
        }
        return false; // Огоня на этой позиции нет
    }
    
    public void UiStart()
    {
        stepsLeftText.gameObject.SetActive(true);
        
        winScreen.gameObject.SetActive(false);
        loseScreen.gameObject.SetActive(false);
        
        UpdateText(); 
    }

    private void HandleTilePlaced()
    {
        if (butterfly != null)
        {
            butterfly.MoveButterfly();
            stepsLeft--;

            UpdateText();
        }
    }

    public void NextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex < levels.Length)
        {
            winScreen.gameObject.SetActive(false);
            stepsLeftText.gameObject.SetActive(true);
            
            wictory = false;

            worldGrid.GenerateWorld(levels[currentLevelIndex].map);
            stepsLeft = levels[currentLevelIndex].steps; 
            
            StartTurn();
            UpdateText();
        }
    }
    
    public void RestartLevel()
    {
        guideText.gameObject.SetActive(false);
        loseScreen.gameObject.SetActive(false);
        stepsLeftText.gameObject.SetActive(true);

        activeFires.Clear();

        worldGrid.GenerateWorld(levels[currentLevelIndex].map);
        stepsLeft = levels[currentLevelIndex].steps; 
        
        StartTurn();
        UiStart();
    }

    public void Victory()
    {
        hasGuide = false;
        wictory = true;
        
        guideText.gameObject.SetActive(false);
        stepsLeftText.gameObject.SetActive(false);
        winScreen.gameObject.SetActive(true);
    }
    
    public void Lose()
    {
        hasGuide = false;
        
        guideText.gameObject.SetActive(false);
        stepsLeftText.gameObject.SetActive(false);
        loseScreen.gameObject.SetActive(true);
    }

    private void UpdateText()
    {
        stepsLeftText.text = "Steps left: " + stepsLeft; // Обновление текста
    }
    
    public void CheckAndClearFireOnTrees()
    {
        if (stepsLeft > 0)
        {
            for (int x = 0; x < worldGrid.groundTilemap.size.x; x++)
            {
                for (int y = 0; y < worldGrid.groundTilemap.size.y; y++)
                {
                    Vector3Int tilePosition = new Vector3Int(x, -y, 0);

                    // Проверяем наличие дерева и огня на одной клетке
                    if (worldGrid.blockTilemap.GetTile(tilePosition) == worldGrid.treeTile &&
                        worldGrid.fireTilemap.GetTile(tilePosition) == worldGrid.fireTile)
                    {
                        audioSource.PlayOneShot(Resources.Load<AudioClip>("Audio/acidBurn"));
                
                        ParticleSystem particle;

                        // Используем Resources.Load для загрузки префаба
                        Vector3 worldPosition = worldGrid.blockTilemap.CellToWorld(tilePosition);
                        particle = Instantiate(Resources.Load<ParticleSystem>("Particle/acidBurnEffect"), worldPosition + new Vector3(0.5f, 0.5f, 0f), Quaternion.identity);

                        particle.Play();

                        // Очищаем клетку от дерева и огня
                        worldGrid.blockTilemap.SetTile(tilePosition, null);
                        worldGrid.fireTilemap.SetTile(tilePosition, null);
                    }
                }
            }
        }
    }
}
