using UnityEngine; // Import Unity
using System.Collections; // IMport arraylist

public class Coordinates: MonoBehaviour // Declares a new class, inherits "MonoBehaviour", which is needed for any script that is being attached to GameObject
{
    public Transform player; // Holds coordinates, Transform
    public Transform[] enemies;
    public Transform[] obstacles;
    private Camera mainCamera; // Camera
    private int frameCounter = 0;
    private int printInterval = 50; // Print every 200 frames
    // Map data
    private ArrayList cameraPositions = new ArrayList();
    private ArrayList playerPositions = new ArrayList();
    private ArrayList enemyPositions = new ArrayList();
    private ArrayList obstaclePositions = new ArrayList();

    private ArrayList playerBounds = new ArrayList();
    private ArrayList enemyBounds = new ArrayList();
    private ArrayList obstacleBounds = new ArrayList();


    void Start() // Automatically called when GameObject is activated (coordinates)
    {
        Debug.Log("Enemies array length: " + (enemies != null ? enemies.Length : 0));
        mainCamera = Camera.main;
        // Check if we have the camera
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
        }
        GetGroundBounds();



        // ADD ALL COMPONENTS

        //enemies.AddComponent<>();
    }


    void Update() // Code in here runs per frame
    {   
        frameCounter++;

        if (frameCounter >= printInterval)
        {
            TrackCoordinates();
            TrackCameraPosition();
            frameCounter = 0;
        }
    }
    void TrackCoordinates()
    {

        if (mainCamera != null)
        {
            Rect cameraRect = GetCameraBounds(mainCamera);
            Debug.Log("Space:" + " X " + cameraRect.width + " Y " + cameraRect.height );
        }

        if (player != null && IsVisible(player))
        {   
            Vector2 playerPosition = new Vector2(player.position.x, player.position.y);
            playerPositions.Add(playerPosition);
            // Get object bounds
            Rect playerRect = GetObjectBounds(player.gameObject);
            playerBounds.Add(playerRect);
            Debug.Log("Player: " + player.position + ", X " + playerRect.width + " Y " + playerRect.height);

        }

        foreach (Transform enemy in enemies)
        {
            if (enemy != null && IsVisible(enemy))
            {   
                Vector2 enemyPosition = new Vector2(enemy.position.x, enemy.position.y);
                enemyPositions.Add(enemyPosition);
                // Get object bounds
                Rect enemyRect = GetObjectBounds(enemy.gameObject);
                enemyBounds.Add(enemyRect);
                Debug.Log("Enemy: " + enemy.position + ", X " + enemyRect.width + " Y " + enemyRect.height);
            }
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
                Debug.Log("Obstacle: " + obstacle.position + ", X " + obstacleRect.width + " Y " + obstacleRect.height);
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
        Vector2 cameraPos = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.y);
        cameraPositions.Add(cameraPos);
        Debug.Log("Camera Position: " + cameraPos);
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
