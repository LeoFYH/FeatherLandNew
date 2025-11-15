using UnityEngine;
using TMPro;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    
    public Camera mainCamera;
    public float targetAspect = 256f/144f;
    public float baseOrthographicSize = 7f; // Your base ortho size for target aspect
    public TMP_Text debugTest;
    public string debugMessage;
    
    // Background image dimensions
    private const float BACKGROUND_WIDTH = 2560f;
    private const float BACKGROUND_HEIGHT = 1440f;
    
    
    void Update()
    {
        float currentAspect = (float)Screen.width / Screen.height;
        float calculatedSize = baseOrthographicSize;
        
        // Calculate size based on aspect ratio
        if (currentAspect < targetAspect)
        {
            calculatedSize = baseOrthographicSize * (targetAspect / currentAspect);
        }
        else if (currentAspect > targetAspect)
        {
            calculatedSize = baseOrthographicSize * (currentAspect / targetAspect);
        }
        
        // Calculate maximum size based on background dimensions
        float maxSizeBasedOnWidth = (BACKGROUND_WIDTH / 2f) / (currentAspect * 100f); // Convert to Unity units
        float maxSizeBasedOnHeight = (BACKGROUND_HEIGHT / 2f) / 100f; // Convert to Unity units
        float maxAllowedSize = Mathf.Min(maxSizeBasedOnWidth, maxSizeBasedOnHeight);
        
        // Use the smaller of the two sizes
        mainCamera.orthographicSize = Mathf.Min(calculatedSize, maxAllowedSize);
    }
}
