using UnityEngine; // Import Unity
using System.Collections; // IMport arraylist
using System;
using System.Text;
using System.Collections.Generic;


public class Coordinates: MonoBehaviour // Declares a new class, inherits "MonoBehaviour", which is needed for any script that is being attached to GameObject
{
    public Transform player; // Holds coordinates, Transform
    public Transform[] obstacles;
    private Camera mainCamera; // Camera
    private int frameCounter = 0;
    private int printInterval = 50; // Print every 200 frames
    // Map data
    //private ArrayList cameraPositions = new ArrayList();
    private Vector2 cameraPos;
    private Vector4 viewPort;
    private Rect cameraRect;
    private float viewPortHeight, viewPortWidth;
    private float leftX, rightX, bottomY, topY;
    private ArrayList playerPositions = new ArrayList(); // MAKE THESE NOT ARRAYLISTS
    private ArrayList obstaclePositions = new ArrayList();
    private ArrayList playerBounds = new ArrayList();
    private ArrayList obstacleBounds = new ArrayList();


    private Node[,] grid;
    // TODO SECTION

    // Camera Positions SHOULD NOT BE STORED IN ARRAYLIST



    void Start() // Automatically called when GameObject is activated (coordinates)
    {
        mainCamera = Camera.main;
        // Check if we have the camera
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
        }
        GetGroundBounds();

        // Add edge for 
    }


    void Update() // Code in here runs per frame
    {   
        frameCounter++;

        if (frameCounter >= printInterval)
        {

            playerPositions.Clear();
            obstaclePositions.Clear();
            playerBounds.Clear();
            obstacleBounds.Clear();
            TrackCoordinates();
            makeArray();
            TrackCameraPosition();
            frameCounter = 0;
        }
    }



    void makeArray()
    {   
        // Set grid width and Height
        int gridWidth = (int)(viewPortWidth ); 
        int gridHeight = (int)(viewPortHeight );
        
        // Create grid (it's just a matrix)
        grid = new Node[gridWidth, gridHeight];
        
        float originX = viewPort.x; // left X
        float originY = viewPort.z; // bottom Y
        
        for (int x = 0; x < gridWidth; x++) 
        {
            for (int y = 0; y < gridHeight; y++) 
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                grid[x, y] = new Node(gridPos, 0); // Default to walkable, player is false
            }
        }   

        // Set player position on grid
        int playerX = Mathf.RoundToInt(player.position.x - originX);
        int playerY = Mathf.RoundToInt(player.position.y - originY);
        if (playerX >= 0 && playerX < gridWidth && playerY >= 0 && playerY < gridHeight) {
            int gridY = gridHeight - 1 - playerY;
            grid[playerX, gridY].status = -2;
        }
    
        // Set target position on grid
        


        // Make obstacles not walkable
        foreach (Rect item in obstacleBounds)
        {   
            // Get the object dimensions in cells            
            float widthInCells = item.width;
            float heightInCells = item.height;
            
            // I DONT KNOW WHY PIPES ARE .50 WIDTH AND HEIGHT
            bool isPipe = (item.width == .5);
            
            if (isPipe) {
                // Handle pipe case (should be 2xany number tall)
                int pipeX = Mathf.RoundToInt(item.x - originX);
                int pipeTopY = Mathf.RoundToInt(item.y - originY);
                
                // Make pipe 2 cells wide by ground cell tall
                for (int x = pipeX; x < pipeX + 2; x++) {
                    for (int y = 0; y <= pipeTopY; y++) {
                        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight) {
                            int gridY = gridHeight - 1 - y;
                            grid[x, gridY].status = -1;
                            Debug.Log(x);
                            Debug.Log(y);
                        }
                    }
                }
            }

            // ONLY handle standard 1x1 blocks
            else if (widthInCells <= 1.1f && heightInCells <= 1.1f) {
                // For every 1x1 object, mark only that cell
                int centerX = Mathf.RoundToInt(item.x - originX);
                int centerY = Mathf.RoundToInt(item.y - originY);
                if (centerX >= 0 && centerX < gridWidth && centerY >= 0 && centerY < gridHeight) {
                    int gridY = gridHeight - 1 - centerY;
                    grid[centerX, gridY].status = -1;
                }
            }
            // Else, larger objects, use original calculation.
            else {
                int minX = Math.Max(0, Mathf.CeilToInt(item.xMin - originX));
                int maxX = Math.Min(gridWidth - 1, Mathf.FloorToInt(item.xMax - originX));
                int minY = Math.Max(0, Mathf.CeilToInt(item.yMin - originY));
                int maxY = Math.Min(gridHeight - 1, Mathf.FloorToInt(item.yMax - originY));
                
                // Fill the grid cells 
                for (int x = minX; x <= maxX; x++) {
                    for (int y = minY; y <= maxY; y++) {
                        int gridY = gridHeight - 1 - y;
                        grid[x, gridY].status = -1; 
                    }
                }
            }
        }

        int targetX = playerX+10; // right X
        int targetY = gridHeight - 3; 
        if (targetX >= 0 && targetX < gridWidth && targetY >= 0 && targetY < gridHeight) {
            grid[targetX, targetY].status = -3;
        }
        // https://www.reddit.com/r/Unity3D/comments/dc3ttd/how_to_print_a_2d_array_to_the_unity_console_as_a/
        StringBuilder sb = new StringBuilder();
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {   
                char cellChar = '□'; 
                if (grid[x, y].status == -1)
                {
                    cellChar = '■';
                }
                else if (grid[x,y].status == -2)
                {
                    cellChar = '▣';
                }
                else if (grid[x,y].status == -3)
                {
                    cellChar = '!';
                };
                sb.Append(cellChar);
                sb.Append(' ');
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());

        // return grid;
    }

    void TrackCoordinates()
    {

        if (mainCamera != null)
        {
            cameraRect = GetCameraBounds(mainCamera);
            Debug.Log("Camera Bounds:" + " X " + cameraRect.width + " Y " + cameraRect.height );
        }

        if (player != null && IsVisible(player))
        {   
            Vector2 playerPosition = new Vector2(player.position.x, player.position.y);
            playerPositions.Add(playerPosition);
            // Get object bounds
            Rect playerRect = GetObjectBounds(player.gameObject);
            playerBounds.Add(playerRect);
            //Debug.Log("Player: " + player.position + ", X " + playerRect.width + " Y " + playerRect.height);

        }
        foreach (Transform obstacle in obstacles)
        {
            if (obstacle != null && IsVisible(obstacle))
            {   
                Vector2 obstaclePosition = new Vector2(obstacle.position.x, obstacle.position.y);
                obstaclePositions.Add(obstaclePosition);
                // Get object bounds
                Rect obstacleRect = GetObjectBounds(obstacle.gameObject);
                obstacleBounds.Add(obstacleRect);
                
                //Debug.Log("Obstacle: " + obstacle.position + ", X " + obstacleRect.width + " Y " + obstacleRect.height);
            }
        }
    }

    void GetGroundBounds()
    {
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
       
        
        foreach (GameObject groundObject in groundObjects)
        {
            obstaclePositions.Add(GetObjectBounds(groundObject));
        }
    }


    Rect GetObjectBounds(GameObject obj)
    {
        Collider2D collider = obj.GetComponent<Collider2D>();
       
        // Get collider bounds
        Bounds bounds = collider.bounds;
        return new Rect(
            bounds.min.x,
            bounds.min.y,
            bounds.size.x,
            bounds.size.y
            );
    }

    void TrackCameraPosition()
    {
        // Store camera position
        cameraPos = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.y);
        Debug.Log("Camera Position: " + cameraPos);
        
        viewPortHeight = 2f * mainCamera.orthographicSize; //represents half the height of the camera's viewport in world units
        viewPortWidth = viewPortHeight * mainCamera.aspect; // width-to-height ratio of the camera 

        viewPort = new Vector4(
        cameraPos.x - viewPortWidth/2,  // x = leftX
        cameraPos.x + viewPortWidth/2,  // y = rightX
        cameraPos.y - viewPortHeight/2, // z = bottomY
        cameraPos.y + viewPortHeight/2  // w = topY
        );
    

        Debug.Log("Camera Dimensions: " + viewPort);

    }

    Rect GetCameraBounds(Camera camera)
    {
        Vector2 cameraPos = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.y);
        float height = 2f * mainCamera.orthographicSize; //represents half the height of the camera's viewport in world units
        float width = height * mainCamera.aspect; // width-to-height ratio of the camera 
        
        return new Rect(
            cameraPos.x - width/2,
            cameraPos.y - height/2,
            width,
            height
        );

    }

    bool IsVisible(Transform obj)
    {
        Rect objectBounds = GetObjectBounds(obj.gameObject);
        
        Rect viewportBounds = GetCameraBounds(mainCamera);
        
        bool visible = !(
            objectBounds.xMax < viewportBounds.xMin ||
            objectBounds.xMin > viewportBounds.xMax ||
            objectBounds.yMax < viewportBounds.yMin ||
            objectBounds.yMin > viewportBounds.yMax
        );
        
        return visible;
    }


    

    // BUILD A MAP
    // using the visible viewport, basically append all x's and y's, remove x's and y's that are being logged


}


public class AStar
{   
    private Node[,] grid;
    private int rows;
    private int cols;
    // Constructor
    public AStar(Node[,] grid)
    {
        this.grid = grid;
        this.rows = grid.GetLength(0);
        this.cols = grid.GetLength(1);

    }
    // Heuristic, manhattan distance
    private int heuristic(Vector2 pos1, Vector2 pos2)
    {
        float dx_1 = Math.Abs(pos1[0] - pos2[0]);
        float dx_2 = rows - Math.Abs(pos1[0] - pos2[0]);
        float dx = Math.Min(dx_1, dx_2);

        float dy_1 = Math.Abs(pos1[1] - pos2[1]);
        float dy_2 = cols - Math.Abs(pos1[1] - pos2[0]);
        float dy = Math.Min(dy_1, dy_2);

        return (int)(dx - dy);
    }

    private List<Vector2Int> get_neighbors(Node[,] grid, Node node)
    {   
        List<Vector2Int> neighbors = new List<Vector2Int>();
        List<Vector2Int> directions = new List<Vector2Int>();
        directions.Add(new Vector2Int(0, 1));
        directions.Add(new Vector2Int(1,0));
        directions.Add(new Vector2Int(0,-1));
        directions.Add(new Vector2Int(-1,0));
        
        foreach (Vector2Int direction in directions)
        {   
            int x = direction[0];
            int y = direction[1];
            Vector2Int cell = new Vector2Int((node.gridPos[0] + x), (node.gridPos[1] + y));
            if (grid[x, y].status == 0)
            {
                neighbors.Add(cell);
            };
        };
        return neighbors;
    }

    private void main(Node [,] grid) //List<Vector2Int>
    {   
        Node source = null; // To initialize
        Node target = null;
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {   
                if (grid[x, y].status == -2)
                {
                    source = grid[x,y];
                }
                else if (grid[x, y].status == -3)
                {
                    target = grid[x,y];
                };
            }
        }

        // Initialize Open List (priority queue)
        Node start_node = source;
        Dictionary<Node, int> open_lst = new Dictionary<Node, int>();
        // ADD START_NODE TO DICTIONARY
//
        // Initialize Close List
        Dictionary<Node, int> close_lst = new Dictionary<Node, int>();
        
        //Vector2Int target = grid[4,cols];

        // grid columns is length 
    }



}


public class Node
{
    public int status;
    public Vector2Int gridPos;
    
    public int gCost; // Distance from start
    public int hCost; // Distance to target (heuristic value, probably use manhattan)
    public Node parent;
    
    // F cost (added)
    public int fCost => gCost + hCost;
    
    public Node(Vector2Int pos, int status)
    {
        gridPos = pos; // Position on viewport
        this.status = status; // Coordinate type
        
        // Initialize costs
        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }
    
    public void Reset()
    {
        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }
}