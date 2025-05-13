using UnityEngine; // Import Unity
using System.Collections; // IMport arraylist
using System;
using System.Text;
using System.Collections.Generic;
using Clrain.Collections;
using System.Timers;


public class Coordinates: MonoBehaviour // Declares a new class, inherits "MonoBehaviour", which is needed for any script that is being attached to GameObject
{
    public Transform player; // Holds coordinates, Transform
    public Transform[] obstacles;
    private Camera mainCamera; // Camera
    private int frameCounter = 0;
    private int printInterval = 1; // Print every 200 frames
    // Map data
    //private ArrayList cameraPositions = new ArrayList();
    private Vector2 cameraPos;
    public Vector4 viewPort;
    private Rect cameraRect;
    private float viewPortHeight, viewPortWidth;
    private float leftX, rightX, bottomY, topY;
    private ArrayList playerPositions = new ArrayList(); // MAKE THESE NOT ARRAYLISTS
    private ArrayList obstaclePositions = new ArrayList();
    private ArrayList playerBounds = new ArrayList();
    private ArrayList obstacleBounds = new ArrayList();

    // Store path information
    public List<Vector2Int> pathMovements = null;
    public List<Vector2Int> pathNodes = new List<Vector2Int>();


    public Node[,] grid;

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
            /*
            if  (keypress = a)
                A* = true

            if (keypress = x)
                A* = false
                theta* = false

            elif (keypress = b)
                theta* = true

            if a* == true
                makeArrayA*();

            if theta* == true
                makeArrayTheta*
            */

            //PrintGrid();
            frameCounter = 0;
        }
    }



    void makeArray()
    {    
        TrackCameraPosition();
        // Set grid width and Height
        int gridWidth = Mathf.CeilToInt(viewPortWidth);
        int gridHeight = Mathf.CeilToInt(viewPortHeight);


        
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

        playerX = Mathf.Clamp(playerX, 0, gridWidth - 1);
        playerY = Mathf.Clamp(playerY, 0, gridHeight - 1);
        
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
        
        
        AStar astar = new AStar(grid);
        pathMovements = astar.main();

        // Mark path nodes in grid with -4 status
        // Find source node
        Vector2Int currentPos = Vector2Int.zero;
        bool foundSource = false;

        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                if (grid[x, y].status == -2) // Player position
                {
                    currentPos = new Vector2Int(x, y);
                    foundSource = true;
                    break;
                }
            }
            if (foundSource) break;
        }
        

        // Clear previous path nodes
        pathNodes.Clear();
        pathNodes.Add(currentPos);

        for (int i = 1; i < pathMovements.Count; i++)
        {
            currentPos.x += pathMovements[i].x;
            currentPos.y += pathMovements[i].y;
            pathNodes.Add(new Vector2Int(currentPos.x, currentPos.y));
            
            // Only mark as path if it's not already the player or target
            if (grid[currentPos.x, currentPos.y].status != -2 && 
                grid[currentPos.x, currentPos.y].status != -3 &&
                grid[currentPos.x, currentPos.y].status != -1)
            {
                grid[currentPos.x, currentPos.y].status = -4;
            }
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
                }
                else if (grid[x,y].status == -4)
                {
                    cellChar = '*';
                }
                sb.Append(cellChar);
                sb.Append(' ');
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
        VisualizePath();
        MarioJump();
        
        
        
    }
    void VisualizePath()
    {
        // Clear old container
        GameObject container = GameObject.Find("PathVisualization");
        if (container != null)
        {
            Destroy(container);
        }
        
        // Create a new container
        container = new GameObject("PathVisualization");
        
        if (pathNodes == null || pathNodes.Count < 2) return;
        
        // Visualize each node in the path
        for (int i = 0; i < pathNodes.Count; i++)
        {
            Vector2Int node = pathNodes[i];
            
            // Calculate world position
            Vector3 worldPos = new Vector3(
                viewPort.x + node.x,
                viewPort.z + (grid.GetLength(1) - 1 - node.y),
                -0.5f // In front of other objects
            );
            
            // Create sphere
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = worldPos;
            marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            marker.transform.parent = container.transform;
        }
    }


    void PrintGrid()
    {
        if (grid == null) return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Grid with A* Path:");
        
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {   
                char cellChar;
                
                switch (grid[x, y].status)
                {
                    case -1: // Wall
                        cellChar = '■';
                        break;
                    case -2: // Player
                        cellChar = '▣';
                        break;
                    case -3: // Target
                        cellChar = '!';
                        break;
                    case -4: // Path
                        cellChar = '*';
                        break;
                    default: // Empty space
                        cellChar = '□';
                        break;
                }
                
                sb.Append(cellChar);
                sb.Append(' ');
            }
            sb.AppendLine();
        }
        
        // Print path movement info
        if (pathMovements != null && pathMovements.Count > 0)
        {
            sb.AppendLine($"Path found with {pathMovements.Count} movements");
            sb.Append("Movements: ");
            foreach (Vector2Int move in pathMovements)
            {
                sb.Append($"({move.x},{move.y}) ");
            }
            sb.AppendLine();
            
            sb.Append("Path coordinates: ");
            foreach (Vector2Int node in pathNodes)
            {
                sb.Append($"({node.x},{node.y}) ");
            }
        }
        else
        {
            sb.AppendLine("No path found!");
        }
        
        Debug.Log(sb.ToString());
    }

    void TrackCoordinates()
    {

        if (mainCamera != null)
        {
            cameraRect = GetCameraBounds(mainCamera);
        }

        if (player != null && IsVisible(player))
        {   
            Vector2 playerPosition = new Vector2(player.position.x, player.position.y);
            playerPositions.Add(playerPosition);
            // Get object bounds
            Rect playerRect = GetObjectBounds(player.gameObject);
            playerBounds.Add(playerRect);

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
        
        viewPortHeight = 2f * mainCamera.orthographicSize; //represents half the height of the camera's viewport in world units
        viewPortWidth = viewPortHeight * mainCamera.aspect; // width-to-height ratio of the camera 

        viewPort = new Vector4(
        cameraPos.x - viewPortWidth/2,  // x = leftX
        cameraPos.x + viewPortWidth/2,  // y = rightX
        cameraPos.y - viewPortHeight/2, // z = bottomY
        cameraPos.y + viewPortHeight/2  // w = topY
        );
    


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
    
    void MarioJump()
    {
        if (pathNodes == null || pathNodes.Count < 2) return;

        // Find player
        GameObject mario = GameObject.FindGameObjectWithTag("Player");
        if (mario == null) return;

        PlayerMovement movement = mario.GetComponent<PlayerMovement>();
        Rigidbody2D rb = mario.GetComponent<Rigidbody2D>();

        // Get current pos and next pos
        Vector2Int currentPos = new Vector2Int(0,0);
        Vector2Int nextPos = new Vector2Int(0,0);
        int blockJump = 0;
        // Get next 5 path nodes
        for (int i = 0; i < 5; i++) {
            currentPos = pathNodes[i];
            nextPos = pathNodes[i+1];

            if (nextPos.y < currentPos.y){
                blockJump += 1;
            }
        }
        // For every
        Debug.Log(blockJump);
    
        movement.HorizontalMovement();
        
        if (blockJump != 0 && pathNodes[0].y > pathNodes[1].y){
            bool grounded = rb.Raycast(Vector2.down);
            Debug.Log("true");
            if (blockJump < 2 && grounded) 
            {   
                Debug.Log("jumping short");
                StartCoroutine(movement.GroundedMovement(1f));
            }   
            else if (blockJump < 4 && grounded)
            {   
                Debug.Log("Jumping medium");
                StartCoroutine(movement.GroundedMovement(30f));
            }

            else if (blockJump < 6 && grounded)
            {
                Debug.Log("Jumping long");
                StartCoroutine(movement.GroundedMovement(50f));
            }
        
        }

        

            
            // For seconds make input true
            

            
        
        movement.ApplyGravity();
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
     private int heuristic(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        return dx + dy; 
    }

    private List<Vector2Int> get_neighbors(Node[,] grid, Node node)
    {   
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(1, 0),   // Right
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0)   // Left
        };
        
        foreach (Vector2Int direction in directions)
        {   
            int newX = node.gridPos.x + direction.x;
            int newY = node.gridPos.y + direction.y;

            if (newX >= 0 && newX < grid.GetLength(0) && 
                newY >= 0 && newY < grid.GetLength(1))
            {
                // Allow nodes that are either walkable (0) OR the target (-3)
                if (grid[newX, newY].status == 0 || grid[newX, newY].status == -3)
                {
                    neighbors.Add(new Vector2Int(newX, newY));
                }
            }
        }

        neighbors.AddRange(GetJumpTargets(node));
        return neighbors;
    }

   
    private List<Vector2Int> GetJumpTargets(Node node)
    { 
        // This list will keep track of possible nodes in which we can land.  
        List<Vector2Int> jumpTargets = new List<Vector2Int>();
        // Physics variables. 
        float initalVelocityY = 5f;
        float gravity = -9.8f;
        // This probably needs to be frames. so maybe 1/30?
        float timeStep = 0.1f;
        // Change this
        float maxTime = 1f;
        // Done for calculations. 
        Vector2 start = new Vector2(node.gridPos.x, node.gridPos.y);

        // Temp to allow code to run: DELETE LATER
        float moveSpeed = 0;
       
       // Check landing areas for differents times of jump
        for (float t = 0; t < maxTime; t += timeStep)
        {
            // Formula for horizontal and vertical displacements. 
            float dx  = moveSpeed * t;
            float dy  = initalVelocityY * t + 0.5f* gravity * t * t;

            // New position. 
            Vector2 simulatedPos = start + new Vector2(dx, dy);
            Vector2Int gridPos = Vector2Int.RoundToInt(simulatedPos);
        }

        // Commented out to make coed run.
        //jumpTargets.Add(gridPos);
            
        // return list of possible targets. 
        return jumpTargets;


    }



    public List<Vector2Int> main() 
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
                }
            }
        }

        if (source == null || target == null)
        {
            Debug.LogError("Source or target not found in grid");
            return null;
        }

        // Reset nodes for pathfinding
        for (int y = 0; y < grid.GetLength(1); y++) {
            for (int x = 0; x < grid.GetLength(0); x++) {
                grid[x, y].Reset();
            }
        }

        source.gCost = 0;
        source.hCost = heuristic(source.gridPos, target.gridPos);

        // Initialize Open List (priority queue)
        PriorityQueue<Node, int> open_lst = new PriorityQueue<Node, int>();
        
        // ADD START_NODE TO DICTIONARY
        open_lst.Enqueue(source, source.fCost);
        // Initialize Close List
        Dictionary<Vector2Int, bool> closed_lst = new Dictionary<Vector2Int, bool>();
        
        // Repeat until nothing left in open list
        while (open_lst.Count > 0)
        {
            // look for the lowest F cost node
            Node current = open_lst.Dequeue();

            // Skip if already in closed set
            if (closed_lst.ContainsKey(current.gridPos))
            {
                continue;
            }
            closed_lst[current.gridPos] = true;

            // If current node's grid position == target node's grid position
            if (current.gridPos.x == target.gridPos.x && current.gridPos.y == target.gridPos.y)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                Node pathNode = current;

                while (current != null)
                {
                  path.Add(current.gridPos);
                  current = current.parent;  
                }
                // Return list of tuples
                path.Reverse(); 
                List<Vector2Int> moveset = new List<Vector2Int>();  

                for (int i = 1; i < path.Count; i++)
                {
                    int x = path[i].x - path[i-1].x;
                    int y = path[i].y - path[i-1].y;
                    moveset.Add(new Vector2Int(x,y));
                }

                return moveset;
            }
            

            foreach (Vector2Int neighborPos in get_neighbors(this.grid, current))
            {
                if (closed_lst.ContainsKey(neighborPos))
                {
                    continue;
                }
                
                Node neighbor = grid[neighborPos.x, neighborPos.y];
                int newGCost = current.gCost + 1;
                

                
                if (newGCost < neighbor.gCost)
                {
                    // Update node
                    neighbor.parent = current;
                    neighbor.gCost = newGCost;
                    
                    // Only calculate heuristic if not already done
                    if (neighbor.hCost == 0)
                    {
                        neighbor.hCost = heuristic(neighborPos, target.gridPos);
                    }
                    
                    // Add to open list to be processed
                    open_lst.Enqueue(neighbor, neighbor.fCost);
                }
            }
        }
        
        // No path found
        Debug.LogWarning("No path found from source to target");
        return null;
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
    
    public Node(Vector2Int pos, int status = 0, Node parent = null)
    {
        gridPos = pos; // Position on viewport
        this.status = status; // Coordinate type
        
        // Initialize costs
        gCost = int.MaxValue;
        hCost = 0;
        this.parent = parent;
    }
    
    public void Reset()
    {
        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }
}


// My Implementation of lazy theta* 
public class LazyThetaStar
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
     private int heuristic(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        return dx + dy; 
    }

    private List<Vector2Int> get_neighbors(Node[,] grid, Node node)
    {   
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(1, 0),   // Right
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0)   // Left
        };
        
        foreach (Vector2Int direction in directions)
        {   
            int newX = node.gridPos.x + direction.x;
            int newY = node.gridPos.y + direction.y;

            if (newX >= 0 && newX < grid.GetLength(0) && 
                newY >= 0 && newY < grid.GetLength(1))
            {
                // Allow nodes that are either walkable (0) OR the target (-3)
                if (grid[newX, newY].status == 0 || grid[newX, newY].status == -3)
                {
                    neighbors.Add(new Vector2Int(newX, newY));
                }
            }
        }

        neighbors.AddRange(GetJumpTargets(node));
        return neighbors;
    }

   bool in_sight(Node a, Node b)
   {
    // Need to get coordinates
    int x1 = a.gridPos.x;
    int y1 = a.gridPos.y;

    int x2 = b.gridPos.x;
    int y2 = b.gridPos.y;

    if (x2 > x1){
        x1 = 1
    }
    else if(x2 < x1)
    {
        x1 = -1
    }
    else
    {
        x1 = 0
    }
    if (y2 > y1){
        y1 = 1
    }
    else if(y2 < y1)
    {
        y1 = -1
    }
    else
    {
        y1 = 0
    }

    while (x1 != x2 && y1 != y2)
    {
        if (x1 != x2)
        {
            x1 = x1 + stepx;
        }
        if (y1 != y2)
        {
            y1 = y1 + stepy;
        }
        if (grid[x1,y1].status == -1)
        {
            return false;
        }
    }

    // If we got here. There is a clear path. 
    return true;
   }






    private List<Vector2Int> GetJumpTargets(Node node)
    { 
        // This list will keep track of possible nodes in which we can land.  
        List<Vector2Int> jumpTargets = new List<Vector2Int>();
        // Physics variables. 
        float initalVelocityY = 5f;
        float gravity = -9.8f;
        // This probably needs to be frames. so maybe 1/30?
        float timeStep = 0.1f;
        // Change this
        float maxTime = 1f;
        // Done for calculations. 
        Vector2 start = new Vector2(node.gridPos.x, node.gridPos.y);

        // Temp to allow code to run: DELETE LATER
        float moveSpeed = 0;
       
       // Check landing areas for differents times of jump
        for (float t = 0; t < maxTime; t += timeStep)
        {
            // Formula for horizontal and vertical displacements. 
            float dx  = moveSpeed * t;
            float dy  = initalVelocityY * t + 0.5f* gravity * t * t;

            // New position. 
            Vector2 simulatedPos = start + new Vector2(dx, dy);
            Vector2Int gridPos = Vector2Int.RoundToInt(simulatedPos);
        }

        // Commented out to make coed run.
        //jumpTargets.Add(gridPos);
            
        // return list of possible targets. 
        return jumpTargets;


    }



    public List<Vector2Int> main() 
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
                }
            }
        }

        if (source == null || target == null)
        {
            Debug.LogError("Source or target not found in grid");
            return null;
        }

        // Reset nodes for pathfinding
        for (int y = 0; y < grid.GetLength(1); y++) {
            for (int x = 0; x < grid.GetLength(0); x++) {
                grid[x, y].Reset();
            }
        }

        source.gCost = 0;
        source.hCost = heuristic(source.gridPos, target.gridPos);

        // Initialize Open List (priority queue)
        PriorityQueue<Node, int> open_lst = new PriorityQueue<Node, int>();
        
        // ADD START_NODE TO DICTIONARY
        open_lst.Enqueue(source, source.fCost);
        // Initialize Close List
        Dictionary<Vector2Int, bool> closed_lst = new Dictionary<Vector2Int, bool>();
        
        // Repeat until nothing left in open list
        while (open_lst.Count > 0)
        {
            // look for the lowest F cost node
            Node current = open_lst.Dequeue();

            // Skip if already in closed set
            if (closed_lst.ContainsKey(current.gridPos))
            {
                continue;
            }
            closed_lst[current.gridPos] = true;

            // If current node's grid position == target node's grid position
            if (current.gridPos.x == target.gridPos.x && current.gridPos.y == target.gridPos.y)
            {
                List<Vector2Int> path = new List<Vector2Int>();

            
                Node pathNode = current;

                Node parent;


                while (current != null)
                {
                  path.Add(current.gridPos);
                  current = current.parent;  
                }
                // Return list of tuples
                path.Reverse(); 
                List<Vector2Int> moveset = new List<Vector2Int>();  

                for (int i = 1; i < path.Count; i++)
                {
                    int x = path[i].x - path[i-1].x;
                    int y = path[i].y - path[i-1].y;
                    moveset.Add(new Vector2Int(x,y));
                }

                return moveset;
            }
            

            foreach (Vector2Int neighborPos in get_neighbors(this.grid, current))
            {
                if (closed_lst.ContainsKey(neighborPos))
                {
                    continue;
                }
                
                Node neighbor = grid[neighborPos.x, neighborPos.y];
                int newGCost = current.gCost + 1;
                

                
                Node parent;

                // Check if there is a line that connects this node 
                if (current.parent != null && LineOfSight(current.parent, neighbor))
                {
                    parent = current.parent;
                }
                // Otherwise continue normally.
                else
                {
                    parent = current;
                }
                
                int furtherst_parent  = parent.gCost + heuristic(parent.gridPos, neighbor.gridPos);

                if (furtherst_parent < neighbor.gCost)
                {
                    // Update node
                    neighbor.parent = current;
                    neighbor.gCost = furtherst_parent;
                    
                    // Only calculate heuristic if not already done
                    if (neighbor.hCost == 0)
                    {
                        neighbor.hCost = heuristic(neighborPos, target.gridPos);
                    }
                    
                    // Add to open list to be processed
                    open_lst.Enqueue(neighbor, neighbor.fCost);
                }
            }
        }
        
        // No path found
        Debug.LogWarning("No path found from source to target");
        return null;
    }



}