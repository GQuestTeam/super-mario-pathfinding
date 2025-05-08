using UnityEngine;

[ExecuteInEditMode] // Makes it work in the editor without playing
public class CameraLock : MonoBehaviour
{
    public float targetWidth = 28f;
    public float targetHeight = 14f;
    
    private Camera cam;
    
    void Awake()
    {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }
    
    void Update()
    {
        UpdateCameraSize();
    }
    
    void UpdateCameraSize()
    {
        cam.orthographicSize = targetHeight / 2f;
    }
}