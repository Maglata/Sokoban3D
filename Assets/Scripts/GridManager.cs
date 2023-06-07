using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int Levelnumber;
    [SerializeField] private Transform Camera;

    [SerializeField] private GameObject playerTile;
    [SerializeField] private GameObject backgroundTile;
    [SerializeField] private GameObject wallTile;
    [SerializeField] private GameObject crateTile;
    [SerializeField] private GameObject targetTile;

    public Cell[,] grid;
    private int gridHeight;
    private int gridWidth;

    private PlayerController playerController;
    private float moveDelay = 0.5f;
    private float currentDelay = 0f;
    private bool isMovingPlayer = false;
    private List<Cell> playerMovementPath;

    private MenuManager menuManager;

    private string levelsDirectory;
    private GameObject level;
    private string filePath;

    private bool playerDetected;
    private bool isPaused = false;
    private int targetCount;
    private int crateCount;
    private List<GameObject> crates;

    // Start is called before the first frame update
    void Start()
    {
        levelsDirectory = Path.Combine(Application.dataPath, "Levels");
        filePath = Path.Combine(levelsDirectory, "Level" + Levelnumber + ".txt");
        GenerateGridFromFile(filePath);
        menuManager = GetComponent<MenuManager>();
    }

    // Update for moving the Player without a Coroutine with BFS
    void Update()
    {
        if (isMovingPlayer)
        {
            if (playerMovementPath.Count > 0)
            {
                currentDelay += Time.deltaTime;
                if (currentDelay >= moveDelay)
                {
                    MovePlayerToCell(playerMovementPath[0]);
                    playerMovementPath.RemoveAt(0);
                    currentDelay = 0f;
                }
            }
            else
            {
                isMovingPlayer = false;
                playerController.isMoving = false;
                Debug.LogError("Finished Path");

            }
        }
    }

    void GenerateGridFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Level file does not exist.");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        // Parse the width and height from the first line
        string[] sizeValues = lines[0].Split(' ');

        // Check for common invalid inputs and out the width and height at the same time
        if (sizeValues.Length != 2 || !int.TryParse(sizeValues[0], out int width) || !int.TryParse(sizeValues[1], out int height))
        {
            Debug.LogError("Invalid level file. Invalid width and/or height values.");
            return;
        }

        // Check if the number of lines are correct for the height
        if (lines.Length - 1 != height)
        {
            Debug.LogError("Invalid level file. Height does not match the number of lines.");
            return;
        }

        grid = new Cell[width, height]; // Create the grid as a 2D array

        level = new GameObject("Level"); // Create the level GameObject
        playerDetected = false;
        targetCount = 0;
        crateCount = 0;
        crates = new List<GameObject>();

        Debug.Log($"Selected Width:{width} Height:{height}");
        gridHeight = height;
        gridWidth = width;

        for (int y = 0; y < height; y++)
        {
            var line = lines[y + 1].Split(' ');

            // Check if the number of chars are correct for the width
            if (line.Length != width)
            {
                Debug.LogError("Invalid level file. Line length does not match the specified width.");
                return;
            }

            for (int x = 0; x < width; x++)
            {
                char symbol = line[x].ToCharArray()[0];

                Vector3 position = new(x, height - y - 1, 0); // Reverse the y-axis for correct positioning

                GameObject spawnedObject;

                Cell cell = new()
                {
                    x = x,
                    y = y
                };

                // Background placement
                spawnedObject = Instantiate(backgroundTile, new Vector3(x, height - y - 1, 1), Quaternion.identity);
                spawnedObject.name = $"Empty {x} {height - y - 1}";

                spawnedObject.transform.parent = level.transform;

                if (symbol == 'E')
                {
                    cell.tile = null;
                    grid[x, height - y - 1] = cell;
                    continue; // Skip to the next iteration                      
                }
                else if (symbol == 'X')
                {
                    // Wall
                    spawnedObject = Instantiate(wallTile, position, Quaternion.identity);
                    spawnedObject.name = $"Wall {x} {height - y - 1}";
                    cell.tile = spawnedObject;
                    cell.isPassable = false;
                    grid[x, height - y - 1] = cell; // Update the grid with the wall object
                }
                else if (symbol == 'P')
                {
                    if (playerDetected)
                    {
                        Debug.LogError("More than one player detected");
                        return;
                    }
                    // Player
                    spawnedObject = Instantiate(playerTile, position, Quaternion.identity);
                    spawnedObject.name = $"Player {x} {height - y - 1}";
                    playerController = spawnedObject.GetComponent<PlayerController>();
                    cell.tile = spawnedObject;
                    grid[x, height-y-1] = cell; // Update the grid with the player object
                    playerDetected = true;
                }
                else if (symbol == 'B')
                {
                    // Crate
                    spawnedObject = Instantiate(crateTile, position, Quaternion.identity);
                    spawnedObject.name = $"Crate {x} {height - y - 1}";
                    cell.tile = spawnedObject;

                    // cell.isPassable = false;

                    grid[x, height - y - 1] = cell; // Update the grid with the crate object

                    crates.Add(spawnedObject);

                    crateCount++;
                }
                else if (symbol == 'T')
                {
                    // Target
                    spawnedObject = Instantiate(targetTile, position, Quaternion.identity);
                    spawnedObject.name = $"Target {x} {height - y - 1}";
                    cell.isTarget = true;
                    cell.tile = null;
                    grid[x, height - y - 1] = cell; // Update the grid with the target object
                    targetCount++;
                }
                else if (symbol == 'b')
                {
                    // Crate on a Target
                    spawnedObject = Instantiate(crateTile, position, Quaternion.identity);
                    spawnedObject.name = $"Crate {x} {height - y - 1}";
                    // spawnedObject.GetComponent<CrateHandlerTest>().onTarget = true;

                    cell.tile = spawnedObject;
                    // cell.isPassable = false;
                    cell.isTarget = true;
                    grid[x, height - y - 1] = cell; // Update the grid with the crate object

                    crates.Add(spawnedObject);

                    spawnedObject.transform.parent = level.transform;

                    spawnedObject = Instantiate(targetTile, position, Quaternion.identity);
                    spawnedObject.name = $"Target {x} {height - y - 1}";

                    targetCount++;
                    crateCount++;
                }
                else if (symbol == 'p')
                {
                    if (playerDetected)
                    {
                        Debug.LogError("More than one player detected");
                        return;
                    }
                    // Player on a Target
                    spawnedObject = Instantiate(playerTile, position, Quaternion.identity);
                    spawnedObject.name = $"Player {x} {height - y - 1}";
                    spawnedObject.transform.parent = level.transform;

                    playerController = spawnedObject.GetComponent<PlayerController>();

                    cell.tile = spawnedObject;
                    cell.isTarget = true;

                    spawnedObject = Instantiate(targetTile, position, Quaternion.identity);
                    spawnedObject.name = $"Target {x} {height - y - 1}";

                    playerDetected = true;
                    targetCount++;
                }
                else
                {
                    Debug.LogWarning($"Invalid symbol '{symbol}' at position {x},{height - y - 1}");
                    continue; // Skip to the next iteration
                }

                spawnedObject.transform.parent = level.transform; // Set the level as the parent of the spawned object           
            }
        }
        // Final Check for level requirements
        if (!playerDetected || targetCount < crateCount || targetCount <= 0 || crateCount <= 0)
        {
            Debug.LogError("Level requirements not met. Please check ImportantInfo.txt");
            return;
        }     
        
        Camera.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f, -height - 1.5f);

        DisplayGrid();
    }

    public void ResetLevel()
    {
        Destroy(level);
        GenerateGridFromFile(filePath);
        menuManager.resumeGame();
        Debug.ClearDeveloperConsole();
        Debug.Log("Level Reset");
        isPaused = false;
    }
    public void NextLevel()
    {
        Levelnumber++;

        filePath = Path.Combine(levelsDirectory, "Level" + Levelnumber + ".txt");

        if (File.Exists(filePath))
        {         
            Destroy(level);
            GenerateGridFromFile(filePath);
            menuManager.resumeGame();
            isPaused = false;
        }
        else
        {
            Debug.LogError("No Next Level found");
            Levelnumber--;
            filePath = Path.Combine(levelsDirectory, "Level" + Levelnumber + ".txt");
        }
            

    }

    public void CheckWinCondition()
    {
        int cratesOnTargets = 0;

        foreach (GameObject crate in crates)
        {
            Vector3 cratePosition = crate.transform.position;

            int x = Mathf.RoundToInt(cratePosition.x);
            int y = Mathf.RoundToInt(cratePosition.y);

            Cell cell = grid[x, y];

            if (cell.isTarget)
            {
                cratesOnTargets++;
            }
        }

        if (cratesOnTargets == crateCount)
        {
            menuManager.winGame();
            isPaused = true;
        }
    }
    public Cell GetCellAtPosition(Vector3 position)
    {

        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);

        // Debug.Log($"Getting Cell At {position}, X: {x}, Y: {y}");

        if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
        {
            //Debug.Log("Found Cell:" + grid[x, y] + $"At X:{x} Y:{y}");
            return grid[x, y];
        }

        return null;
    }
    public void MoveCelltoPosition(GameObject obj, Vector3 targetPos)
    {
        int currentX = Mathf.RoundToInt(obj.transform.position.x);
        int currentY = Mathf.RoundToInt(obj.transform.position.y);

        int targetX = Mathf.RoundToInt(targetPos.x);
        int targetY = Mathf.RoundToInt(targetPos.y);

        // Debug.LogError($"Moving from {currentX},{currentY} to {targetX},{targetY}");

        GameObject targetTile = grid[targetX, targetY].tile;

        grid[targetX, targetY].tile = grid[currentX, currentY].tile;

        grid[currentX, currentY].tile = targetTile;
              
        // Update Position
        obj.transform.position = targetPos;

        // DisplayGrid();

    }
    public class Cell
    {
        public GameObject tile;
        public bool isTarget = false;
        public bool isPassable = true;
        public int x;
        public int y;

        public override string ToString()
        {
            string cellString = "";
            if (isTarget)
                cellString += "Target:";
            else
                cellString += "Not Target:";

            cellString += tile + x.ToString() + y.ToString();
            return cellString;
        }
    }
    public void DisplayGrid()
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        Debug.Log("Grid Contents:");

        for (int y = height-1; y >= 0; y--)
        {
            string row = "";

            for (int x = 0; x < width; x++)
            {

                Cell cell = grid[x, y];
                char symbol = ' ';

                if (cell.tile == null)
                {
                    if (cell.isTarget)
                    {
                        symbol = 'T';
                    }
                    else
                    {
                        symbol = 'E';
                    }
                }
                else
                {
                    switch(cell.tile.tag)
                    {
                        case "Wall":
                            symbol = 'X';
                            break;
                        case "Player":
                            if (cell.isTarget)
                            {
                                symbol = 'p';
                            }
                            else
                            {
                                symbol = 'P';
                            }
                            break;
                        case "Crate":
                            if (cell.isTarget)
                            {
                                symbol = 'b';
                            }
                            else
                            {
                                symbol = 'B';
                            }
                            break;
                    }
                }              
                row += symbol + " ";
            }

            Debug.Log(row);
        }
    }

    public void HandleMouseClick(Vector3 targetPosition)
    {
        if(!isPaused)
        {
            Debug.Log($"Start Position:{playerController.PlayerPos()}, Final Position:{targetPosition}");

            Cell startCell = GetCellAtPosition(playerController.PlayerPos());
            Cell targetCell = GetCellAtPosition(targetPosition);

            if (startCell != null && targetCell != null)
            {
                playerMovementPath = FindShortestPath(startCell, targetCell);

                if (playerMovementPath != null)
                {
                    Debug.Log("Moving Player...");

                    playerController.isMoving = true;

                    isMovingPlayer = true;
                }
                else
                {
                    Debug.LogWarning("No path found.");
                }
            }
        }      
    }
    private List<Cell> FindShortestPath(Cell startCell, Cell targetCell)
    {
        Debug.Log("Checking For Path...");

        // Perform BFS to find the shortest path
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(startCell);

        Dictionary<Cell, Cell> parentMap = new Dictionary<Cell, Cell>();
        parentMap[startCell] = null;

        bool pathFound = false;

        while (queue.Count > 0)
        {
            Cell currentCell = queue.Dequeue();
            // Debug.Log($"Checking...{currentCell}");
            if (currentCell == targetCell)
            {
                Debug.Log("Path Found!");
                pathFound = true;
                break;
            }

            foreach (Cell neighbor in GetNeighbors(currentCell))
            {
                if (!parentMap.ContainsKey(neighbor))
                {
                    queue.Enqueue(neighbor);
                    parentMap[neighbor] = currentCell;
                }
            }
        }

        if (pathFound)
        {
            // Reconstruct the path from start to target
            List<Cell> path = new List<Cell>();
            Cell cell = targetCell;

            while (cell != null)
            {
                path.Add(cell);
                cell = parentMap[cell];
            }

            path.Reverse();

            return path;
        }

        return null;
    }
    private List<Cell> GetNeighbors(Cell cell)
    {
        List<Cell> neighbors = new List<Cell>();

        // Define the possible neighbor offsets
        int[] xOffset = { -1, 1, 0, 0 };
        int[] yOffset = { 0, 0, -1, 1 };

        // Iterate over the offsets to find the neighbors
        for (int i = 0; i < xOffset.Length; i++)
        {
            int neighborX = cell.x + xOffset[i];
            int neighborY = gridHeight - 1 - cell.y + yOffset[i];

            // Check if the neighbor is within the grid boundaries
            if (neighborX >= 0 && neighborX < grid.GetLength(0) &&
                neighborY >= 0 && neighborY < grid.GetLength(1))
            {
                Cell neighborCell = grid[neighborX, neighborY];
                if (neighborCell.isPassable && neighborCell.tile == null)
                {
                    neighbors.Add(neighborCell);
                }
            }
        }

        return neighbors;
    }

    private void MovePlayerToCell(Cell cell)
    {       
        Vector3 targetPosition = new Vector3(cell.x, gridHeight - cell.y - 1, 0);
        //Debug.Log($"Moving to {targetPosition} cell {pathCell}");
        MoveCelltoPosition(playerController.gameObject, targetPosition);
        // DisplayGrid();
    }
}
