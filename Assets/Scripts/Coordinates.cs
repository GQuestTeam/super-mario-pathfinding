using UnityEngine; // Import Unity

public class Coordinates: MonoBehaviour // Declares a new class, inherits "MonoBehaviour", which is needed for any script that is being attached to GameObject
{
    public Transform player; // Holds coordinates, Transform
    public Transform[] enemies;
    public Transform[] obstacles;
    private Camera mainCamera; // Camera
    private int frameCounter = 0;
    private int printInterval = 200; // Print every 200 frames

    void Start() // Automatically called when GameObject is activated (coordinates)
    {
        Debug.Log("Enemies array length: " + (enemies != null ? enemies.Length : 0));
        mainCamera = Camera.main;
        // Check if we have the camera
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
        }
    }


    void Update() // Code in here runs per frame
    {   
        frameCounter++;

        if (frameCounter >= printInterval)
        {
            PrintCoordinates();
            frameCounter = 0;
        }
    }
    void PrintCoordinates()
    {
        if (player != null && IsVisible(player))
        {
            Debug.Log("Player: " + player.position);

        }

        foreach (Transform enemy in enemies)
        {
            if (enemy != null && IsVisible(enemy))
            {
                Debug.Log("EnemyL" + enemy.position);
            }
        }

        foreach (Transform obstacle in obstacles)
        {
            if (obstacle != null && IsVisible(obstacle))
            {
                Debug.Log("Obstacle: " + obstacle.position);
            }
        }
    }

    bool IsVisible(Transform obj)
    {
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(obj.position); // Convert gameObject position to viewport coordinates

        // Check if within camera's view
        bool visible = viewportPoint.z > 0 && 
                    viewportPoint.x > 0 && viewportPoint.x < 1 &&
                    viewportPoint.y > 0 && viewportPoint.y < 1;

        return visible;
    }


    // BUILD A MAP
    // using the visible viewport, basically append all x's and y's, remove x's and y's that are being logged


}
