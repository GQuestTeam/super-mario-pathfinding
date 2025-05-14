using UnityEngine; // Import Unity
using System.Collections; // IMport arraylist
using System;
using System.Text;
using System.Collections.Generic;
using Clrain.Collections;
using Utils;
using System.Timers;


public class Coordinates: MonoBehaviour
{      

    // Player Coordinates
    public Transform player; 
    // List of Obstacle Coordinates
    public Transform[] obstacles;

    // Frame controls, limits stress on computer
    private int frameCounter = 0;
    private int printInterval = 1; // Print every 1 frame

    // Camera data
    private Camera mainCamera; 
    private Vector2 cameraPos;
    public Vector4 viewPort;
    private Rect cameraRect;
    private float viewPortHeight, viewPortWidth;
    private float leftX, rightX, bottomY, topY;

    //  Bounds data
    private ArrayList playerBounds = new ArrayList();
    private ArrayList obstacleBounds = new ArrayList();

    // Path information
    public List<Vector2Int> pathMovements = null;
    public List<Vector2Int> pathNodes = new List<Vector2Int>();

    // Grid/Map
    public Node[,] grid;
        

    /// Start() method 
    /// runs when GameObject is activated
    void Start() 
    {
        mainCamera = Camera.main;
        // Check if we have the camera
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
        }
        GetGroundBounds();
    }

    /// Update() method
    /// runs on every frame
    void Update()
    {    
        // Increase frame count per frame
        frameCounter++;

        // Only run when frame count is above set print interval
        if (frameCounter >= printInterval)
        {
            // Reset bounds on new frame
            playerBounds.Clear();
            obstacleBounds.Clear();
            // Track new coordinates
            TrackCoordinates();

            //runAStar();
            runLazyTheta();
            frameCounter = 0;
        }
    }


    /// Creates a grid and runs the Lazy Theta* pathfinding algorithm
    void runLazyTheta()
    {    
        // Update camera positions and bounds
        TrackCameraPosition();

        // Set grid width and Height based on viewport
        int gridWidth = Mathf.CeilToInt(viewPortWidth);
        int gridHeight = Mathf.CeilToInt(viewPortHeight);

        // Create grid of nodes
        grid = new Node[gridWidth, gridHeight];
        
        // Calculate origin nodes (bottom-left of camera)
        float originX = viewPort.x; // left X
        float originY = viewPort.z; // bottom Y
        
        // Initialize grid with walkable nodes (all set to walkable)
        for (int x = 0; x < gridWidth; x++) 
        {
            for (int y = 0; y < gridHeight; y++) 
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                grid[x, y] = new Node(gridPos, 0); // Default to walkable
            }
        }   

        // Set player position on grid (status -2)
        int playerX = Mathf.RoundToInt(player.position.x - originX);
        int playerY = Mathf.RoundToInt(player.position.y - originY);

        // Ensure player position is within grid bounds
        playerX = Mathf.Clamp(playerX, 0, gridWidth - 1);
        playerY = Mathf.Clamp(playerY, 0, gridHeight - 1);
        
        // Place player on the grid
        if (playerX >= 0 && playerX < gridWidth && playerY >= 0 && playerY < gridHeight) {
            int gridY = gridHeight - 1 - playerY;
            grid[playerX, gridY].status = -2;
        }
    
        // Process obstacles and make them not walkable (status -1)
        foreach (Rect item in obstacleBounds)
        {   

            // Get the object dimensions in cells            
            float widthInCells = item.width;
            float heightInCells = item.height;
            
            // Special handling for pipes (identified by width = 0.5)
            bool isPipe = (item.width == .5);
            
            if (isPipe) {
                // Handle pipe case 
                int pipeX = Mathf.RoundToInt(item.x - originX);
                int pipeTopY = Mathf.RoundToInt(item.y - originY);
                
                // Make pipe 2 cells wide by ground cell tall
                for (int x = pipeX; x < pipeX + 2; x++) {
                    for (int y = 0; y <= pipeTopY; y++) {
                        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight) {
                            int gridY = gridHeight - 1 - y;
                            grid[x, gridY].status = -1; // Status -1 = obstacle
                        }
                    }
                }
            }
            
            

            // Handle standard 1x1 blocks
            else if (widthInCells <= 1.1f && heightInCells <= 1.1f) {
                int centerX = Mathf.RoundToInt(item.x - originX);
                int centerY = Mathf.RoundToInt(item.y - originY);
                if (centerX >= 0 && centerX < gridWidth && centerY >= 0 && centerY < gridHeight) {
                    int gridY = gridHeight - 1 - centerY;
                    grid[centerX, gridY].status = -1; // Status -1 = obstacle
                }
            }
            // Handle larger objects by filling their area
            else {
                int minX = Math.Max(0, Mathf.CeilToInt(item.xMin - originX));
                int maxX = Math.Min(gridWidth - 1, Mathf.FloorToInt(item.xMax - originX));
                int minY = Math.Max(0, Mathf.CeilToInt(item.yMin - originY));
                int maxY = Math.Min(gridHeight - 1, Mathf.FloorToInt(item.yMax - originY));
                
                // Fill the grid cells covered by this object
                for (int x = minX; x <= maxX; x++) {
                    for (int y = minY; y <= maxY; y++) {
                        int gridY = gridHeight - 1 - y;
                        grid[x, gridY].status = -1; // status -1 = obstacle
                    }
                }
            }
        }


        // Set target position on grid (status -3)
        int targetX = playerX+10; // rightmost on viewport
        int targetY = gridHeight - 3; // Fixed height near bottom of screen
        if (targetX >= 0 && targetX < gridWidth && targetY >= 0 && targetY < gridHeight) {
            grid[targetX, targetY].status = -3; // Status -3 = target
        }
            
        // Run Lazy Theta * pathfinding
        LazyThetaStar ltheta = new LazyThetaStar(grid);
        pathMovements = ltheta.main();

        // Process pathfinding results

        // Find source node (player position)
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
        

        // Clear previous path nodes and add source node
        pathNodes.Clear();
        pathNodes.Add(currentPos);

        // Build list of path nodes from movement directions
        for (int i = 0; i < pathMovements.Count; i++)
        {
            currentPos.x += pathMovements[i].x;
            currentPos.y += pathMovements[i].y;
            pathNodes.Add(new Vector2Int(currentPos.x, currentPos.y));
            
            // Mark path on grid (status -4), but don't overwrite player or obstacles
            if (grid[currentPos.x, currentPos.y].status != -2 && 
                grid[currentPos.x, currentPos.y].status != -1)
            {
                grid[currentPos.x, currentPos.y].status = -4; // Status -4 = path
            }
        }
        
        // Visualize grid in console using ASCII 
        DebugVisualizePath(grid);

        // Print visual representation of path in the game world
        VisualizePath();

        // Apply path to Mario's movement
        MarioJumpTheta();
    }

    // Creates a grid and runs A* pathfinding algorithm
    void runAStar()
    {    
        // Update camera positions and bounds
        TrackCameraPosition();
        // Set grid width and Height based on viewport
        int gridWidth = Mathf.CeilToInt(viewPortWidth);
        int gridHeight = Mathf.CeilToInt(viewPortHeight);
        
        // Create grid of nodes
        grid = new Node[gridWidth, gridHeight];
        
        // Calculate origin nodes (bottom-left of camera)
        float originX = viewPort.x; // left X
        float originY = viewPort.z; // bottom Y
        
        // Initialize grid with walkable nodes (all set to walkable)
        for (int x = 0; x < gridWidth; x++) 
        {
            for (int y = 0; y < gridHeight; y++) 
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                grid[x, y] = new Node(gridPos, 0); // Default to walkable, player is false
            }
        }   

        // Set player position on grid (status -2)
        int playerX = Mathf.RoundToInt(player.position.x - originX);
        int playerY = Mathf.RoundToInt(player.position.y - originY);

        // Ensure player position is within grid bounds
        playerX = Mathf.Clamp(playerX, 0, gridWidth - 1); // Makes sure number doesn't go out of range
        playerY = Mathf.Clamp(playerY, 0, gridHeight - 1);
        
        // Place player on the grid
        if (playerX >= 0 && playerX < gridWidth && playerY >= 0 && playerY < gridHeight) {
            int gridY = gridHeight - 1 - playerY;
            grid[playerX, gridY].status = -2;
        }
    
        
        // Process obstacles and make them not walkable (status -1)
        foreach (Rect item in obstacleBounds)
        {   
            // Get the object dimensions in cells            
            float widthInCells = item.width;
            float heightInCells = item.height;
            
            // Special handling for pipes (identified by width = 0.5)
            bool isPipe = (item.width == .5);
            
            if (isPipe) {
                // Handle pipe case 
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

            // Handle standard 1x1 blocks
            else if (widthInCells <= 1.1f && heightInCells <= 1.1f) {
                // For every 1x1 object, mark only that cell
                int centerX = Mathf.RoundToInt(item.x - originX);
                int centerY = Mathf.RoundToInt(item.y - originY);
                if (centerX >= 0 && centerX < gridWidth && centerY >= 0 && centerY < gridHeight) {
                    int gridY = gridHeight - 1 - centerY;
                    grid[centerX, gridY].status = -1;
                }
            }
            // Handle larger objects by filling their area
            else {
                int minX = Math.Max(0, Mathf.CeilToInt(item.xMin - originX));
                int maxX = Math.Min(gridWidth - 1, Mathf.FloorToInt(item.xMax - originX));
                int minY = Math.Max(0, Mathf.CeilToInt(item.yMin - originY));
                int maxY = Math.Min(gridHeight - 1, Mathf.FloorToInt(item.yMax - originY));
                
                // Fill the grid cells covered by this object
                for (int x = minX; x <= maxX; x++) {
                    for (int y = minY; y <= maxY; y++) {
                        int gridY = gridHeight - 1 - y;
                        grid[x, gridY].status = -1; 
                    }
                }
            }
        }

        // Set target position on grid (status -3)
        int targetX = playerX+10; // right X
        int targetY = gridHeight - 3; 
        if (targetX >= 0 && targetX < gridWidth && targetY >= 0 && targetY < gridHeight) {
            grid[targetX, targetY].status = -3;
        }
        
        // Run A * pathfinding
        AStar astar = new AStar(grid);
        pathMovements = astar.main();

        // Find source node (player position)
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
        

        // Clear previous path nodes and add source node
        pathNodes.Clear();
        pathNodes.Add(currentPos);
        // Build list of path nodes from movement directions
        for (int i = 1; i < pathMovements.Count; i++)
        {
            currentPos.x += pathMovements[i].x;
            currentPos.y += pathMovements[i].y;
            pathNodes.Add(new Vector2Int(currentPos.x, currentPos.y));
            
            // Mark path on grid (status -4), but don't overwrite player or obstacles
            if (grid[currentPos.x, currentPos.y].status != -2 && 
                grid[currentPos.x, currentPos.y].status != -3 &&
                grid[currentPos.x, currentPos.y].status != -1)
            {
                grid[currentPos.x, currentPos.y].status = -4;
            }
        }
        
        // Visualize grid in console using ASCII 
        DebugVisualizePath(grid);

        // Print visual representation of path in the game world
        VisualizePath();

        // Apply path to Mario's movement
        MarioJump();
        
    }

    /// Visualize path in debug mode
    // https://www.reddit.com/r/Unity3D/comments/dc3ttd/how_to_print_a_2d_array_to_the_unity_console_as_a/
    void DebugVisualizePath(Node [,] grid){
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
    }

    /// Creates visual spheres in game world to represent path
    void VisualizePath()
    {
        // Clear old visualization
        GameObject container = GameObject.Find("PathVisualization");
        if (container != null)
        {
            Destroy(container);
        }
        
        // Create a new visualization
        container = new GameObject("PathVisualization");
        
        // Make sure we have a valid path
        if (pathNodes == null || pathNodes.Count < 2) return;
        
        // Visualize each node in the path with a circle
        for (int i = 0; i < pathNodes.Count; i++)
        {
            Vector2Int node = pathNodes[i];
            
            // Calculate world position from grid position (viewport)
            Vector3 worldPos = new Vector3(
                viewPort.x + node.x,
                viewPort.z + (grid.GetLength(1) - 1 - node.y),
                -0.5f // In front of other objects
            );
            
            // Create sphere primitive
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = worldPos;
            marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            marker.transform.parent = container.transform;
        }
    }

    /// Updates position and bounds data for obstacles
    void TrackCoordinates()
    {
        // Update camera bounds
        if (mainCamera != null)
        {
            cameraRect = GetCameraBounds(mainCamera);
        }

        // Track player bounds if in visible part of viewport
        if (player != null && IsVisible(player))
        {   
            // Get object bounds
            Rect playerRect = GetObjectBounds(player.gameObject);
            playerBounds.Add(playerRect);

        }

        // Track obstacle bounds if in visible part of viewport
        foreach (Transform obstacle in obstacles)
        {
            if (obstacle != null && IsVisible(obstacle))
            {   
                // Get object bounds
                Rect obstacleRect = GetObjectBounds(obstacle.gameObject);
                obstacleBounds.Add(obstacleRect);
                
            }
        }
    }


    /// Get bounds for all ground objects in scene
    void GetGroundBounds()
    {   
        // Find all objects tagged as "ground"
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
       
        // Add bounds to obstacles
        foreach (GameObject groundObject in groundObjects)
        {
            obstacleBounds.Add(GetObjectBounds(groundObject));
        }
    }

    /// Gets bounds rect for a gameObject based on its built in collider
    Rect GetObjectBounds(GameObject obj)
    {   
        // Get collider
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


    /// Updates camera position and viewport data
    void TrackCameraPosition()
    {
        // Store camera position
        cameraPos = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.y);
        
        // Valculate viewport dimensions
        viewPortHeight = 2f * mainCamera.orthographicSize; //represents half the height of the camera's viewport in world units
        viewPortWidth = viewPortHeight * mainCamera.aspect; // width-to-height ratio of the camera 

        // Calculate viewport bounds
        viewPort = new Vector4(
        cameraPos.x - viewPortWidth/2,  // x = leftX
        cameraPos.x + viewPortWidth/2,  // y = rightX
        cameraPos.y - viewPortHeight/2, // z = bottomY
        cameraPos.y + viewPortHeight/2  // w = topY
        );
    


    }

    /// Gets a Rect representing the camera's bounds in viewport space
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


    /// Checks if an object is visible within the camera's view
    bool IsVisible(Transform obj)
    {
        Rect objectBounds = GetObjectBounds(obj.gameObject);
        Rect viewportBounds = GetCameraBounds(mainCamera);

        // Check if object is outside the viewport bounds
        bool visible = !(
            objectBounds.xMax < viewportBounds.xMin ||
            objectBounds.xMin > viewportBounds.xMax ||
            objectBounds.yMax < viewportBounds.yMin ||
            objectBounds.yMin > viewportBounds.yMax
        );
        
        return visible;
    }


    /// Controls Mario's jumping for A* path following
    void MarioJump()
    {
        // Ensure we have a valid path
        if (pathNodes == null || pathNodes.Count < 2) return;

        // Find Mario
        GameObject mario = GameObject.FindGameObjectWithTag("Player");
        if (mario == null) return;

        // Get Mario's components
        PlayerMovement movement = mario.GetComponent<PlayerMovement>();
        Rigidbody2D rb = mario.GetComponent<Rigidbody2D>();

        // Get first two nodes in path to determine next movement
        Vector2Int currentPos = new Vector2Int(0,0);
        Vector2Int nextPos = new Vector2Int(0,0);
        int blockJump = 0;

        // Get next 5 nodes to determine jump height of Mario
        for (int i = 0; i < 5; i++) {
            currentPos = pathNodes[i];
            nextPos = pathNodes[i+1];

            if (nextPos.y < currentPos.y){
                // Measure jump height
                blockJump += 1;
            }
        }
   
        // Always move horizontally
        movement.HorizontalMovement();
        
        // Jump if we need to go up, or if we are grounded
        if (blockJump != 0 && pathNodes[0].y > pathNodes[1].y){
            // Test if grounded
            bool grounded = rb.Raycast(Vector2.down);

            // Choose jump height
            if (blockJump <= 2 && grounded) 
            {   
                StartCoroutine(movement.GroundedMovement(90f));
            }   
            else if (blockJump <= 4 && grounded)
            {   
                StartCoroutine(movement.GroundedMovement(135f));
            }
            else if (blockJump <= 6 && grounded)
            {
                StartCoroutine(movement.GroundedMovement(150f));
            }
        
        }

        // Apply gravity, make Mario fall        
        movement.ApplyGravity();
    }

    /// Controls Mario's jumping for Lazy Theta* path following
    void MarioJumpTheta()
    {
        // Ensure valid path
        if (pathNodes == null || pathNodes.Count < 2) return;

        // Find Mario
        GameObject mario = GameObject.FindGameObjectWithTag("Player");
        if (mario == null) return;

        // Get Mario's components
        PlayerMovement movement = mario.GetComponent<PlayerMovement>();
        Rigidbody2D rb = mario.GetComponent<Rigidbody2D>();
        
        // Get first two nodes to determine next movemet
        Vector2Int currentPos = pathNodes[0];
        Vector2Int nextPos = pathNodes[1];
        
        // Calculate the differences of height and horizontal
        int heightDifference = nextPos.y - currentPos.y;
        int xDifference = nextPos.x - currentPos.x;

        // Apply horizontal movement 
        movement.HorizontalMovement();
        
        // Only jump if we need to go up (negative height difference)
        if (heightDifference < 0 && rb.Raycast(Vector2.down)) // Make sure we're grounded
        {

            // Absolute value of height difference to get positive number
            int blocksToJump = Mathf.Abs(heightDifference);
            xDifference = Mathf.Abs(xDifference);

            // Only jump when within 5 x value
            if (xDifference < 5)
            {
                // Scale jump force based on height 
                if (blocksToJump <= 2)
                {
                    // Small jump for 1 block
                    StartCoroutine(movement.GroundedMovement(90f));
                }
                else if (blocksToJump <= 6)
                {
                    Debug.Log("JUMPING FOUR BLOCKS");
                    // Medium jump for 2 blocks
                    StartCoroutine(movement.GroundedMovement(135f));
                }
                else
                {
                    // Maximum jump for 6+ blocks
                    StartCoroutine(movement.GroundedMovement(150f));
                }
            } 
            
        }
        
        // Apply gravity after jump
        movement.ApplyGravity();
    }

    




}


public class AStar
{   

    // Grid map containing nodes
    private Node[,] grid;
    // Grid dimensions
    private int rows;
    private int cols;
    
    /// Constructor initializes with grid and determines dimensions
    public AStar(Node[,] grid)
    {
        this.grid = grid;
        this.rows = grid.GetLength(0);
        this.cols = grid.GetLength(1);
    }

    /// Calculate Manhattan distance
     private int heuristic(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        return dx + dy; 
    }

    /// Get valid neighboring nodes, platform-specific behavior included
    private List<Vector2Int> get_neighbors(Node[,] grid, Node node)
    {   
        List<Vector2Int> neighbors = new List<Vector2Int>();
        
        int nodeX = node.gridPos.x;
        int nodeY = node.gridPos.y;
        
        // Check if we're standing on ground (has obstacle below)
        bool isOnGround = false;
        if (nodeY + 1 < grid.GetLength(1)) {
            isOnGround = grid[nodeX, nodeY + 1].status == -1; // Status -1 is ground/obstacle
        }
        
        // Check if we're at an edge (no ground to the right)
        bool isAtEdge = false;
        if (isOnGround && nodeX + 1 < grid.GetLength(0) && nodeY + 1 < grid.GetLength(1)) {
            isAtEdge = grid[nodeX + 1, nodeY + 1].status != -1; // No ground to the right
        }
        
        // Handling for edges, checking deepness of drop
        if (isAtEdge) {
            // Count how many blocks down until we hit ground
            int fallDepth = 0;
            bool foundGround = false;
            
            for (int checkY = nodeY + 1; checkY < grid.GetLength(1); checkY++) {
                fallDepth++;
                
                if (nodeX + 1 < grid.GetLength(0) && checkY < grid.GetLength(1)) {
                    if (grid[nodeX + 1, checkY].status == -1) {
                        // Found ground
                        foundGround = true;
                        break;
                    }
                }
                // Limit search depth to 3
                if (fallDepth >= 10) {
                    break;
                }
            }
            
            // Handle no ground found, or reached 3
            if (fallDepth >= 10 || !foundGround) {
                
                // Check if Mario can jump over gap diagonally
                if (nodeY - 2 >= 0 && nodeX + 1 < grid.GetLength(0) && 
                    (grid[nodeX + 1, nodeY - 2].status == 0 || grid[nodeX + 1, nodeY - 2].status == -3)) {
                    
                    neighbors.Add(new Vector2Int(nodeX + 1, nodeY - 2)); // Jump diagonally up 2 blocks
                }
                
                // Return the jumps only if they are valid
                if (neighbors.Count > 0) {
                    return neighbors; // Return only jump options
                }
            }
            // For shallow drops, don't jump
            else if (isAtEdge) {
                if (nodeY + 1 < grid.GetLength(1) &&
                    (grid[nodeX, nodeY + 1].status == 0 || grid[nodeX, nodeY + 1].status == -3)) {
                    neighbors.Add(new Vector2Int(nodeX, nodeY + 1));
                    return neighbors;
                }
            }
        }
        
        // Check for obstacles ahead
        bool obstacleAhead = false;
        if (nodeX + 1 < grid.GetLength(0)) {
            obstacleAhead = grid[nodeX + 1, nodeY].status == -1;
            
            if (obstacleAhead && isOnGround) {
                // Try to jump over the obstacle, up right
                if (nodeY - 1 >= 0 && nodeX + 1 < grid.GetLength(0) &&
                    (grid[nodeX + 1, nodeY - 1].status == 0 || grid[nodeX + 1, nodeY - 1].status == -3)) {
                    
                    neighbors.Add(new Vector2Int(nodeX + 1, nodeY - 1)); // Jump diagonally
                    return neighbors;
                }
            }
        }
        
        // If no special cases, add all directions
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(0, 1),   // Down
            new Vector2Int(1, 0),   // Right
            new Vector2Int(0, -1),  // Up
            new Vector2Int(-1, 0)   // Left
        };
        
        // Add all valid movement options
        foreach (Vector2Int direction in directions)
        {
            int newX = nodeX + direction.x;
            int newY = nodeY + direction.y;


            // Ensure new position is within bounds
            if (newX >= 0 && newX < grid.GetLength(0) && 
                newY >= 0 && newY < grid.GetLength(1))
            {   
                // Only add to walkable or target
                if (grid[newX, newY].status == 0 || grid[newX, newY].status == -3)
                {
                    neighbors.Add(new Vector2Int(newX, newY));
                }
            }
        }
        
        return neighbors;
    }


    /// Main A* pathfinding algorithm
    /// returns list of mvoement vectors to follow
    public List<Vector2Int> main() 
    {      
        // Find source and target nodes
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


        // Validate we found both source and target
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

        // Initialize source node
        source.gCost = 0;
        source.hCost = heuristic(source.gridPos, target.gridPos);

        // Initialize Open List (priority queue)
        PriorityQueue<Node, int> open_lst = new PriorityQueue<Node, int>();
        open_lst.Enqueue(source, source.fCost);

        // Initialize Close List to track visited nodes
        Dictionary<Vector2Int, Node> closed_lst = new Dictionary<Vector2Int, Node>();
        
        // Main A* loop
        while (open_lst.Count > 0)
        {
            // look for the lowest F cost node
            Node current = open_lst.Dequeue();

            // Skip if already in closed set
            if (closed_lst.ContainsKey(current.gridPos))
            {
                continue;
            }
            closed_lst[current.gridPos] = current;

            // If current node's grid position == target node's grid position
            if (current.gridPos.x == target.gridPos.x && current.gridPos.y == target.gridPos.y)
            {   
                // Reconstruct path by following parent nodes
                List<Vector2Int> path = new List<Vector2Int>();
                Node pathNode = current;

                while (current != null)
                {
                  path.Add(current.gridPos);
                  current = current.parent;  
                }

                // Return path from start to finish
                path.Reverse(); 

                // Convert positions to movement vectors
                List<Vector2Int> moveset = new List<Vector2Int>();  

                for (int i = 1; i < path.Count; i++)
                {
                    int x = path[i].x - path[i-1].x;
                    int y = path[i].y - path[i-1].y;
                    moveset.Add(new Vector2Int(x,y));
                }

                return moveset;
            }
            

            // Process al neighbors of current node
            foreach (Vector2Int neighborPos in get_neighbors(this.grid, current))
            {   
                // Skip if closed
                if (closed_lst.ContainsKey(neighborPos))
                {
                    continue;
                }
                
                // Get neighbor node and calculate new g-cost
                Node neighbor = grid[neighborPos.x, neighborPos.y]; 
                int newGCost = current.gCost + 1;

                // Set parent and costs
                neighbor.parent = current;
                neighbor.gCost = newGCost;
                neighbor.hCost = heuristic(neighborPos, target.gridPos);

                // Add to open list if not already there, or update if a better path was found
                if (!closed_lst.ContainsKey(neighborPos) || closed_lst[neighbor.gridPos].fCost > neighbor.fCost)
                {
                    open_lst.Enqueue(neighbor, neighbor.fCost);
                }
            }

        }
        
        // No path found
        Debug.Log("No path found from source to target");
        return null;
    }



}

/// Node represents single cell in grid
public class Node
{   
    // Status identifiers:
    // -1 = obstacle/ground
    // -2 = player
    // -3 = target position
    // -4 = path
    // 0 = walkable
    public int status;

    // Grid position
    public Vector2Int gridPos;
    
    // A* algorithm data
    public int gCost; // Distance from start
    public int hCost; // Distance to target (heuristic value, probably use manhattan)
    public Node parent; // Previous node in optimal path
    
    // Toal estimated cost (f = g + h)
    public int fCost => gCost + hCost;
    
    // Constructur creates new node
    public Node(Vector2Int pos, int status = 0, Node parent = null)
    {
        gridPos = pos; // Position on viewport
        this.status = status; // Coordinate type
        
        // Initialize costs
        gCost = int.MaxValue;
        hCost = 0;
        this.parent = parent;
    }
    
    // Resets node to initial state
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
    public LazyThetaStar(Node[,] grid)
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
        
        int nodeX = node.gridPos.x;
        int nodeY = node.gridPos.y;
        
        // Check if we're standing on ground
        bool isOnGround = false;
        if (nodeY + 1 < grid.GetLength(1)) {
            isOnGround = grid[nodeX, nodeY + 1].status == -1; // Status -1 is ground/obstacle
        }
        
        // Check if we're at an edge (no ground to the right)
        bool isAtEdge = false;
        if (isOnGround && nodeX + 1 < grid.GetLength(0) && nodeY + 1 < grid.GetLength(1)) {
            isAtEdge = grid[nodeX + 1, nodeY + 1].status != -1; // No ground to the right
        }
        
        // If at an edge, check how deep the fall is
        if (isAtEdge) {
            // Count how many blocks down until we hit ground
            int fallDepth = 0;
            bool foundGround = false;
            
            for (int checkY = nodeY + 1; checkY < grid.GetLength(1); checkY++) {
                fallDepth++;
                
                if (nodeX + 1 < grid.GetLength(0) && checkY < grid.GetLength(1)) {
                    if (grid[nodeX + 1, checkY].status == -1) {
                        // Found ground
                        foundGround = true;
                        break;
                    }
                }
                
                // If we've checked 10 blocks down and still no ground, stop checking
                if (fallDepth >= 10) {
                    break;
                }
            }
            
            // If fall is too deep (>10 blocks) or we didn't find ground at all
            if (fallDepth >= 10 || !foundGround) {
                
                
                if (nodeY - 2 >= 0 && nodeX + 1 < grid.GetLength(0) && 
                    (grid[nodeX + 1, nodeY - 2].status == 0 || grid[nodeX + 1, nodeY - 2].status == -3)) {

                    grid[nodeX + 1, nodeY ].status = -1;
                    grid[nodeX + 4, nodeY ].status = -1;
                }
                
                if (neighbors.Count > 0) {
                    return neighbors; // Return only jump options
                }
            }
            // For regular edges (not deep falls), go straight down
            else if (isAtEdge) {
                // Add downward direction only
                if (nodeY + 1 < grid.GetLength(1) &&
                    (grid[nodeX, nodeY + 1].status == 0 || grid[nodeX, nodeY + 1].status == -3)) {
                    neighbors.Add(new Vector2Int(nodeX, nodeY + 1));
                    return neighbors;
                }
            }
        }
        
        // Check for obstacles ahead
        bool obstacleAhead = false;
        if (nodeX + 1 < grid.GetLength(0)) {
            obstacleAhead = grid[nodeX + 1, nodeY].status == -1;
            
            if (obstacleAhead && isOnGround) {
                // Try to jump over the obstacle
                if (nodeY - 1 >= 0 && nodeX + 1 < grid.GetLength(0) &&
                    (grid[nodeX + 1, nodeY - 1].status == 0 || grid[nodeX + 1, nodeY - 1].status == -3)) {
                    
                    neighbors.Add(new Vector2Int(nodeX + 1, nodeY - 1)); // Jump diagonally
                    return neighbors;
                }
            }
        }
        
        // If no special cases apply, add all standard directions
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(0, 1),   // Down
            new Vector2Int(1, 0),   // Right
            new Vector2Int(0, -1),  // Up
            new Vector2Int(-1, 0)   // Left
        };
        
        foreach (Vector2Int direction in directions)
        {
            int newX = nodeX + direction.x;
            int newY = nodeY + direction.y;

            if (newX >= 0 && newX < grid.GetLength(0) && 
                newY >= 0 && newY < grid.GetLength(1))
            {
                if (grid[newX, newY].status == 0 || grid[newX, newY].status == -3)
                {
                    neighbors.Add(new Vector2Int(newX, newY));
                }
            }
        }
        
        return neighbors;
    }


    /// <summary>
    /// This function continues the task from in_sight
    /// Basically confirms if there is a interrupted path between two nodes.
    /// Depending on the order our line changes.
    /// </summary>
    /// <param name="a"> Granparent node</param> 
    /// <param name="b">current neighbor</param>
    /// <returns> A boolean signaling a connection</returns>
    bool drawLine(Node a, Node b)
    {   

        // Get coordinates.
        List<Vector2Int> putPixel = new List<Vector2Int>();

        int x0 = a.gridPos.x;
        int y0 = a.gridPos.y;
        int x1 = b.gridPos.x;
        int y1 = b.gridPos.y;

        // Check horizontal direction of line
        // In other words, cover the adjecent/opposite octant. 
        if (x0 > x1){
            x0 = x1; x1 = x0;
            y0 = y1; y1 = y0; 
        }

        //Calculate slopes/ rates of change. 
        int dx = x1 - x0;
        int dy = y1 - y0;


        // Figure out if line is moving up or down.
        int dir  = 0;
        if (dy < 0){
            dir = -1;
        }
        else {
            dir = 1;
        }

        // Use this to increase or decrease our Y in every iteration. 
        dy *= dir;

        int y = 0;
        int p = 0;
        if (dx != 0)
        {
            y = y0;
            p = 2*dy - dx;
            for (int i = 0; i < dx+1; i++){
                putPixel.Add(new Vector2Int(x0 + i, y));
                if (p >= 0)
                {
                    y += dir;
                    p = p-2*dx;
                }
                p = p+2*dy;
            }
            
        }

        foreach (Vector2Int pixel in putPixel){
            if (grid[pixel.x, pixel.y].status == -1)
            {
                return false;
            }
        }
        return true;
    }




    /// <summary>
    /// This method checks to see if two nodes have conection without obstacles interfering.
    /// If yes, that means that a can see b, therefore we can draw a direc line from A to B.
    /// </summary>
    /// <param name="a"></param> Starting Node
    /// <param name="b"></param> Current neighbor we are checking. 
    /// <returns></returns> A boolean
   bool in_sight(Node a, Node b)
   {

    // Getting positions / coodinates of each node. 
    int x0  = a.gridPos.x;
    int y0 = a.gridPos.y;
    int x1 = b.gridPos.x;
    int y1 = b.gridPos.y;

    // Determine magnitudes to see if line is horizontal or vertical. 
    // Depending on the result, we set out points accordingly. 
    if(Math.Abs(x1-x0) > Math.Abs(y1-y0))
    {
        return drawLine(a,b);
    }
        
    else
    {
        return drawLine(b,a);
    }

    
    
   }




    /// <summary>
    /// Literally same as A*
    /// </summary>
    /// <returns> A list of moves</returns>
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
        Dictionary<Vector2Int, Node> closed_lst = new Dictionary<Vector2Int, Node>();
        
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
            closed_lst[current.gridPos] = current;

            // If current node's grid position == target node's grid position
            if (current.gridPos.x == target.gridPos.x && current.gridPos.y == target.gridPos.y)
            {
                List<Vector2Int> path = new List<Vector2Int>();
            

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
                foreach (Vector2Int move in path){
                    Debug.Log(move);
                }
                return moveset;
            }



            foreach (Vector2Int neighborPos in get_neighbors(this.grid, current))
            {
                if (closed_lst.ContainsKey(neighborPos))
                {
                    continue;
                }
                

                // Create neighbor node
                Node neighbor = grid[neighborPos.x, neighborPos.y];
                // Instantiate parent
                Node parent;
                

                // If current has a parent or is in sight of last node
                if (current.parent != null && in_sight(current.parent, neighbor))
                {
                    parent = current.parent;
                    //Debug.Log("IN SIGHT - parent + current.parent + current + neighbor" + parent.gridPos + " " + current.parent.gridPos + " " + current.gridPos + " " + neighbor.gridPos);
                }
                // Otherwise continue normally.
                else
                {
                    parent = current;
                    
                }
                
                // Set parent as previous parent or current (line of sight or not)
                neighbor.parent = parent;
                // Apply gCost, hCost, fCost
                int newGCost = parent.gCost + 1;
                neighbor.gCost = newGCost;
                neighbor.hCost = heuristic(neighborPos, target.gridPos);
            
                // If closed list contains key, 
                if (!closed_lst.ContainsKey(neighborPos) || closed_lst[neighbor.gridPos].fCost > neighbor.fCost)
                {
                    open_lst.Enqueue(neighbor, neighbor.fCost);
                }

                
            }
        }
        
        // No path found
        Debug.LogWarning("No path found from source to target");
        return null;
    }
}