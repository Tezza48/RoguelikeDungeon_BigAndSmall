﻿using System;
using System.Collections.Generic;
using UnityEngine;

// work on it and design it on paper

public enum Tile
{
    Big_Filler,
    Big_Floor,
    Med_Filler,
    Med_Floor,
    Med_Wall,
    Small_Cap,
    Small_Corner,
    Small_Filler,
    Small_Floor,
    Small_Pillar,
    Small_TJunc,
    Small_Wall
}

/*
just set the big tiles a state, set the medium and small tiles around the big ones
*/

public class Generator : MonoBehaviour
{
    #region Public Fields
    [Header("Helper")]
    public Tile availableTiles;

    [Header("Tile Prefabs")]
    public List<GameObject> tilePrefab = new List<GameObject>();

    [Header("Spawning Settings")]
    [Tooltip("Recommended greater or equal to 10 by 10.")]
    public int width = 10;
    [Tooltip("Recommended greater or equal to 10 by 10.")]
    public int height = 10;
    [Range(1, 20)]
    public int maxRooms = 10;
    [Range(3, 10)]
    public int minRoomSize = 3;
    [Range(3, 20)]
    public int maxRoomSize = 5;
    [Tooltip("Number of times the generator will try\nto place a new room. the higher the number,\nthe more likely the max number of rooms will spawn.")]
    public int maxRoomTries = 100;
    public int maxEnemiesInRoom = 3;

    [Header("Tile Settings")]
    public const int TileUnits = 10;
    public const int BIG_TILE_WIDTH = 40;
    public const int SMALL_TILE_WIDTH = 10;

    [Header("Gameplay Assets")]
    public GameObject PlayerPrefab;
    public GameObject ExitPrefab;
    public GameObject EnemyPrefab;

    public static int SPAWN_INTERVAL;
    #endregion
    #region Private Fields
    private int level;

    private int[,] cells;//is this cell a wall?
    private List<Rect> rooms = new List<Rect>();
    private List<Line> coridors = new List<Line>();
    private List<GameObject> spawnedTiles = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private GameObject DungeonHolder;

    private GameObject player;
    private GameObject exit;

    #endregion

    public int Level { get { return level; } }

    /*
    button to press that makes a big room.
    delete button
    */

    void Start()
    {
        DungeonHolder = new GameObject("Dungeon");
        SPAWN_INTERVAL = BIG_TILE_WIDTH / 2 + SMALL_TILE_WIDTH / 2;
        level = 1;
        GenerateSimpleDungeon();
        spawnedEnemies = new List<GameObject>();
    }

    public void SpawnNextDungeon()
    {
        //destroy the preveous dungeon
        DeleteGrid();
        GenerateSimpleDungeon();
        level++;
    }

    private void GenerateSimpleDungeon()
    {
        FullSpawn();
        AddPlayer();
        AddExit();
        AddEnemies();
    }

    private void AddExit()
    {
        Vector3 exitSpawn = new Vector3((Mathf.Floor(rooms[rooms.Count - 1].center.x) * 2 + 1) * SPAWN_INTERVAL, 0, (Mathf.Floor(rooms[rooms.Count - 1].center.y) * 2 + 1) * SPAWN_INTERVAL);
        if (exit == null)
        {
            //place the exit
            exit = (GameObject)Instantiate(ExitPrefab, exitSpawn, Quaternion.Euler(-90f, 0, 0));
            exit.name = "Exit";
        }
        else
        {
            //place the exit again
            exit.transform.position = exitSpawn;
        }
    }

    private void AddPlayer()
    {
        Vector3 playerSpawn = new Vector3((Mathf.Floor(rooms[0].center.x) * 2 + 1) * SPAWN_INTERVAL, 0, (Mathf.Floor(rooms[0].center.y) * 2 + 1) * SPAWN_INTERVAL);
        if (player == null)
        {
            //place the player
            player = (GameObject)Instantiate(PlayerPrefab, playerSpawn + (Vector3.up * 10), Quaternion.identity);
            player.name = "Player";
            player.GetComponent<Player>().Generator = this;
            player.GetComponent<Player>().InitNewPlayerCharacter();
        }
        else
        {
            //place the player again
            player.transform.position = playerSpawn + (Vector3.up * 10);
        }
    }

    private void AddEnemies()
    {
        int numToSpawn = 0;
        Vector3 spawnPos;
        for (int i = 1; i < rooms.Count - 1; i++)
        {
            Rect room = rooms[i];
            numToSpawn = UnityEngine.Random.Range(0, maxEnemiesInRoom);
            for (int j = 1; j <= numToSpawn; j++)
            {
                int xPos = (int)UnityEngine.Random.Range(Mathf.CeilToInt(room.xMin), Mathf.FloorToInt(room.xMax)) * 2 + 1;
                int yPos = (int)UnityEngine.Random.Range(Mathf.CeilToInt(room.yMin), Mathf.FloorToInt(room.yMax)) * 2 + 1;
                spawnPos = new Vector3(xPos, 0, yPos) * SPAWN_INTERVAL;
                var newEnemy = (GameObject)Instantiate(EnemyPrefab, spawnPos, Quaternion.identity);
                spawnedEnemies.Add(newEnemy);
            }
        }
    }

    #region Spawn_Buttons
    public void DeleteGrid()
    {
        for (int i = 0; i < DungeonHolder.transform.childCount; i++)
        {
            Destroy(DungeonHolder.transform.GetChild(i).gameObject);
        }
        spawnedTiles = new List<GameObject>();
        foreach (GameObject enemy in spawnedEnemies)
        {
            Destroy(enemy);
        }
        spawnedEnemies = new List<GameObject>();
        rooms = new List<Rect>();
        coridors = new List<Line>();
    }

    public void MakeEmptyRoom()
    {
        DeleteGrid();
        //initialize cells
        InnitCells(ref cells);
        //make one big room
        //rooms coordinates just point to big cells
        Rect room = new Rect(0, 0, width, height);//rectangle that's one less than the size of the array leaving a big tile and a small tile on the border
        rooms.Add(room);

        #region Manual_Room_Setting
        //make the cells on the borders into walls
        foreach (Rect currentRoom in rooms)
        {

            //make the cells on the borders into walls
            for (int x = (int)currentRoom.x; x < currentRoom.xMax; x++)
            {
                for (int y = (int)currentRoom.y; y < currentRoom.yMax; y++)
                {
                    cells[x * 2 + 1, y * 2 + 1] = 0;
                    //cells[x * 2 + 1, y * 2] = 0;
                    //cells[x * 2, y * 2 + 1] = 0;
                    //cells[x * 2 + 1, y * 2 + 1] = 0;
                }
            }
            cells[((int)currentRoom.xMax - 1) * 2, ((int)currentRoom.yMax - 1) * 2] = 1;//last corner
        }
        #endregion

        InitialiseTiles(cells);
    }

    public void SpawnRooms()
    {
        DeleteGrid();

        InnitCells(ref cells);

        AddRooms(ref cells, true);

        InitialiseTiles(cells);
    }

    public void SpawnCoridors()
    {
        DeleteGrid();

        InnitCells(ref cells);

        AddRooms(ref cells, false);

        MakeCoridors(ref cells);

        InitialiseTiles(cells);
    }

    public void FullSpawn()
    {
        DeleteGrid();

        InnitCells(ref cells);

        AddRooms(ref cells);

        MakeCoridors(ref cells);

        InitialiseTiles(cells);
    }
    #endregion

    #region Spawning_Methods
    private void InnitCells(ref int[,] _cells)
    {
        //initialize cells
        _cells = new int[width * 2 + 1, height * 2 + 1];
        for (int y = 0; y < height * 2 + 1; y++)
        {
            for (int x = 0; x < width * 2 + 1; x++)
            {
                _cells[x, y] = 1;
            }
        }
    }

    private void AddRooms(ref int[,] _cells, bool writeToCells = true)
    {
        #region Generate_Rooms
        for (int i = 0; i < maxRooms; i++)
        {
            int tries = maxRoomTries;
            while (tries > 0)
            {
                int xPos = UnityEngine.Random.Range(0, width - minRoomSize);
                int yPos = UnityEngine.Random.Range(0, height - minRoomSize);
                int roomWidth = UnityEngine.Random.Range(minRoomSize, maxRoomSize);
                int roomHeight = UnityEngine.Random.Range(minRoomSize, maxRoomSize);

                Rect newRoom = new Rect(xPos, yPos, roomWidth, roomHeight);

                if (xPos + roomWidth > width || yPos + roomHeight > height)
                {
                    continue;
                }

                bool isValid = true;
                foreach (Rect currentRoom in rooms)
                {
                    if (newRoom.Overlaps(currentRoom))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    rooms.Add(newRoom);
                    break;
                }

                tries--;
            }
        }
        #endregion
        if (writeToCells)
        {
            #region Write_Rooms_To_Cells
            foreach (Rect currentRoom in rooms)
            {

                //make the cells on the borders into walls
                for (int x = (int)currentRoom.x; x < currentRoom.xMax; x++)
                {
                    for (int y = (int)currentRoom.y; y < currentRoom.yMax; y++)
                    {
                        cells[x * 2+1, y * 2+1] = 0;
                        if (x > currentRoom.xMin)
                        {
                            cells[x * 2, y * 2 + 1] = 0;
                        }
                        if (y > currentRoom.yMin)
                        {
                            cells[x * 2 + 1, y * 2] = 0;
                        }
                        if (x > currentRoom.xMin && y > currentRoom.yMin)
                        {

                            cells[x * 2, y * 2] = 0;
                        }
                    }
                }
            }
            #endregion
        }
    }

    private void MakeCoridors(ref int[,] _cells)
    {
        for (int i = 1; i < rooms.Count; i++)
        {

            //horiz first
            Vector2 start = new Vector2(Mathf.Floor(rooms[i - 1].center.x), Mathf.Floor(rooms[i - 1].center.y));
            Vector2 end = new Vector2(Mathf.Floor(rooms[i].center.x), Mathf.Floor(rooms[i].center.y));
            Vector2 corner = new Vector2(end.x, start.y);
            
            Debug.DrawLine(new Vector3(start.x*SPAWN_INTERVAL*2, 0, start.y * SPAWN_INTERVAL*2),
                new Vector3(end.x * SPAWN_INTERVAL*2, 0, end.y * SPAWN_INTERVAL*2),
                Color.black,
                5f,
                false);

            //make a horizontal coridor
            coridors.Add(new Line(start, corner, true));
            //then make the vertical component
            coridors.Add(new Line(corner, end, false));
        }
        //write to the cells.
        foreach (Line coridor in coridors)
        {
            //write here, here and here
            if (coridor.IsHoriz)
            {
                int y = (int)coridor.O1.y;

                bool isIncreasing = coridor.O1.x < coridor.O2.x;

                if (isIncreasing)
                {
                    for (int x = (int)coridor.O1.x; x < coridor.O2.x; x++)
                    {
                        cells[x * 2 + 1, y * 2 + 1] = 0;
                        cells[x * 2 + 2, y * 2 + 1] = 0;
                    }
                }
                else
                {
                    for (int x = (int)coridor.O1.x; x > coridor.O2.x; x--)
                    {
                        cells[x * 2 + 1, y * 2 + 1] = 0;
                        cells[x * 2, y * 2 + 1] = 0;
                    }
                }
            }
            else
            {
                int x = (int)coridor.O1.x;

                bool isIncreasing = coridor.O1.y < coridor.O2.y;
                if (isIncreasing)
                {
                    for (int y = (int)coridor.O1.y; y < coridor.O2.y; y++)
                    {
                        cells[x * 2 + 1, y * 2 + 1] = 0;
                        cells[x * 2 + 1, y * 2 + 2] = 0;
                    }
                }
                else
                {
                    for (int y = (int)coridor.O1.y; y > coridor.O2.y; y--)
                    {
                        cells[x * 2 + 1, y * 2 + 1] = 0;
                        cells[x * 2 + 1, y * 2] = 0;
                    }
                }
            }
        }
    }

    private void InitialiseTiles(int[,] _cells)
    {
        GameObject spawnedTile;
        for (int y = 0; y < height * 2 + 1; y++)
        {
            for (int x = 0; x < width * 2 + 1; x++)
            {
                //small cells
                if (y % 2 == 0 && x % 2 == 0)
                {
                    if (_cells[x, y] == 0)
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Small_Floor], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.identity);
                    }
                    else
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Small_Pillar], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.identity);
                    }
                }

                //med cells
                else if (x % 2 == 0)
                {
                    if (_cells[x, y] == 0)
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Med_Floor], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.Euler(0f, 90f, 0f));
                    }
                    else
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Med_Wall], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.Euler(0f, 90f, 0f));
                    }
                }
                else if (y % 2 == 0)
                {
                    if (_cells[x, y] == 0)
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Med_Floor], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.Euler(0f, 0f, 0f));
                    }
                    else
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Med_Wall], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.Euler(0f, 0f, 0f));
                    }
                }

                //big cells
                else
                {
                    if (_cells[x, y] == 0)
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Big_Floor], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.identity);
                    }
                    else
                    {
                        spawnedTile = (GameObject)Instantiate(tilePrefab[(int)Tile.Big_Filler], new Vector3(x * SPAWN_INTERVAL, 0, y * SPAWN_INTERVAL), Quaternion.identity);
                    }
                }
                spawnedTiles.Add(spawnedTile);
                spawnedTile.name = "(" + x + ", " + y + ") Exits: " + _cells[x, y];
                spawnedTile.transform.SetParent(DungeonHolder.transform);
            }
        }
    }
    #endregion
}