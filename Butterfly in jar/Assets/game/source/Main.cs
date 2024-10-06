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

    public GameObject fireBackground;
    
    public int stepsLeft;
    public bool butterFlyStep;
    public bool wictory = false;
    public bool canPlace;
    public bool hasGuide = false;
    
    private Level[] levels;
    private int currentLevelIndex = 0;
    
    public AudioSource audioSource;
    public AudioSource audioSourceMusic;
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
        tileCursor.OnTilePlaced += HandleTilePlaced;
        
        guideText.gameObject.SetActive(false);
        
        audioSourceMusic.clip = Resources.Load<AudioClip>("Audio/backgroundMusic"); 
        audioSourceMusic.loop = true;
        audioSourceMusic.Play(); 

        // Инициализация уровней
        levels = new Level[]
        {
            new Level(new string[]
            {
                "+bj"
            }, 1, new Tile[] {worldGrid.treeTile as Tile}),

            new Level(new string[]
            {
                "+bpjt"
            }, 2, new Tile[] {worldGrid.treeTile as Tile}),
            
            new Level(new string[]
            {
                "ttttt",
                "tb+jt",
                "ttttt"
            }, 2, new Tile[] {worldGrid.treeTile as Tile, worldGrid.plantTile as Tile}),
            
            new Level(new string[]
            {
                "++++p",
                "b++pj",
                "++++p"
            }, 10, new Tile[] {worldGrid.treeTile as Tile}),
            
            new Level(new string[]
            {
                "++j",
                "p++",
                "bp+"
            }, 4, new Tile[] {worldGrid.treeTile as Tile, worldGrid.plantTile as Tile}),
            
            new Level(new string[]
            {
                "b++++",
                "tttt+",
                "+++++",
                "+tttt",
                "++++j"
            }, 16, new Tile[] {worldGrid.treeTile as Tile, worldGrid.plantTile as Tile}),

            new Level(new string[]
            {
                "ttttt",
                "b+tjt",
                "ttttt"
            }, 3, new Tile[] {worldGrid.fireTile as Tile}),

            new Level(new string[]
            {
                "ppppp",
                "bpppj",
                "ppppp"
            }, 8, new Tile[] {worldGrid.fireTile as Tile}),
            
            new Level(new string[]
            {
                "+bt+t",
                "++t+t",
                "++ttj"
            }, 7, new Tile[] {worldGrid.fireTile as Tile}),
            
            new Level(new string[]
            {
                "+ttt+",
                "tpppt",
                "tpbpt",
                "tpppt",
                "+tjt+"
            }, 8, new Tile[] {worldGrid.fireTile as Tile}),
            
        };

        worldGrid.GenerateWorld(levels[currentLevelIndex].map);
        stepsLeft = levels[currentLevelIndex].steps;

        fireBackground.SetActive(false);

        StartTurn();
        
        tileCursor.UpdateCurrentTileImage(); 
        UiStart();
    }

    public void StartTurn()
    {
        butterFlyStep = false;
        canPlace = true;

        tileCursor.tiles = null;
        tileCursor.tiles = levels[currentLevelIndex].tiles;
        tileCursor.UpdateCurrentTileImage();

        AddNewFiresToList();
        CheckBurningBlock();
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
        else if (currentLevelIndex == 3 && !hasGuide)
        {
            canPlace = true;
            guideText.gameObject.SetActive(false);
        }
        else if (currentLevelIndex == 4 && !hasGuide)
        {
            canPlace = true;
            guideText.gameObject.SetActive(false);
        }
        else if (currentLevelIndex == 5 && !hasGuide)
        {
            canPlace = true;
            guideText.gameObject.SetActive(false);
        }
        else if (currentLevelIndex == 6 && !hasGuide)
        {
            hasGuide = true;
            guideText.gameObject.SetActive(true);
            StartCoroutine(LevelGuide6()); 
        }
        else if (currentLevelIndex == 7 && !hasGuide)
        {
            hasGuide = true;
            guideText.gameObject.SetActive(true);
            StartCoroutine(LevelGuide7()); 
        }
        else if (currentLevelIndex == 8 && !hasGuide)
        {
            canPlace = true;
            guideText.gameObject.SetActive(false);
        }
        else if (currentLevelIndex == 9 && !hasGuide)
        {
            canPlace = true;
            guideText.gameObject.SetActive(false);
        }
        else if (currentLevelIndex == 10 && !hasGuide)
        {
            canPlace = true;
            guideText.gameObject.SetActive(false);
        }
    }

    private IEnumerator LevelGuide1()
    {
        canPlace = false;
        ShowGuideText("<wave amp=1>This butterfly is beautiful, isn't it?");
        yield return new WaitForSeconds(4f);
        ShowGuideText("<wave amp=1>Click the <jump>LMB</jump> to the left of it to put the current tile.");
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
        ShowGuideText("<wave amp=1>As you can see, she can fly over flowers. Remember, I think this information will be useful to you further.");
        yield return new WaitForSeconds(6f);
        canPlace = true;
    }
    
    private IEnumerator LevelGuide3()
    {
        canPlace = false;
        ShowGuideText("<wave amp=1>It looks like we're at an impasse!");
        yield return new WaitForSeconds(3f); 
        ShowGuideText("<wave amp=1>It's good that I've saved an extra tile especially for such cases! Click the <jump>RMB</jump> to change the tile and place he!</wave>");
        yield return new WaitForSeconds(7f); 
        canPlace = true;
        yield return new WaitUntil(() => butterFlyStep);
        canPlace = false;
        ShowGuideText("<wave amp=1>Now the chances of passing have increased! Just put it in a jar!");
        yield return new WaitForSeconds(3.5f);
        canPlace = true;
    }
    
    private IEnumerator LevelGuide6()
    {
        canPlace = false;
        ShowGuideText("<wave amp=1>It looks like the tree has blocked our way...");
        yield return new WaitForSeconds(3.5f); 
        ShowGuideText("<wave amp=1>But it's good that we have a fire that burns plants and trees :)</wave>");
        yield return new WaitForSeconds(4f);
        canPlace = true;
        yield return new WaitUntil(() => butterFlyStep);
        ShowGuideText("<wave amp=1>It's so good that all living beings are afraid of fire and try to escape from it.");
        canPlace = true;
    }
    
    private IEnumerator LevelGuide7()
    {
        fireBackground.SetActive(true);
        
        canPlace = false;
        ShowGuideText("<shake><color=red>BURN EVERYTHING HERE!");
        yield return new WaitForSeconds(2f);
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
            canPlace = false;
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

            audioSourceMusic.Play();
            
            wictory = false;
            
            fireBackground.SetActive(false);

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

        audioSourceMusic.Play();
        
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
        
        audioSourceMusic.Pause();
        
        guideText.gameObject.SetActive(false);
        stepsLeftText.gameObject.SetActive(false);
        winScreen.gameObject.SetActive(true);
    }
    
    public void Lose()
    {
        hasGuide = false;
        
        audioSourceMusic.Pause();
        
        guideText.gameObject.SetActive(false);
        stepsLeftText.gameObject.SetActive(false);
        loseScreen.gameObject.SetActive(true);
    }

    private void UpdateText()
    {
        stepsLeftText.text = "Steps left: " + stepsLeft; // Обновление текста
    }
    
    public void CheckBurningBlock()
    {
        if (stepsLeft > 0)
        {
            BoundsInt bounds = worldGrid.blockTilemap.cellBounds;
            TileBase[] allTiles = worldGrid.blockTilemap.GetTilesBlock(bounds);

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int tilePosition = new Vector3Int(x, y, 0);
                    
                    TileBase blockTile = worldGrid.blockTilemap.GetTile(tilePosition);
                    TileBase fireTile = worldGrid.fireTilemap.GetTile(tilePosition);
                    
                    if ((blockTile == worldGrid.treeTile || blockTile == worldGrid.plantTile) && 
                        fireTile == worldGrid.fireTile)
                    {
                        audioSource.PlayOneShot(Resources.Load<AudioClip>("Audio/acidBurn"));

                        ParticleSystem particle;

                        // Используем Resources.Load для загрузки префаба
                        Vector3 worldPosition = worldGrid.blockTilemap.CellToWorld(tilePosition);
                        particle = Instantiate(Resources.Load<ParticleSystem>("Particle/acidBurnEffect"), worldPosition + new Vector3(0.5f, 0.5f, 0f), Quaternion.identity);

                        particle.Play();

                        // Очищаем клетку от дерева, растения и огня
                        worldGrid.blockTilemap.SetTile(tilePosition, null);
                        worldGrid.fireTilemap.SetTile(tilePosition, null);
                    }
                }
            }
        }
    }
}
