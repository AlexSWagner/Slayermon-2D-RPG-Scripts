using UnityEngine;

/// <summary>
/// Makes the camera follow the player character
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target; // The player's transform
    public float smoothSpeed = 0.125f; // How smoothly the camera follows the player
    public Vector3 offset = new Vector3(0, 0, -10); // Offset from the player (z is back for 2D)
    
    private Vector3 velocity = Vector3.zero;
    
    void Start()
    {
        // If no target is assigned, try to find the player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("No player found for camera to follow. Assign a target in the inspector.");
            }
        }
    }
    
    void LateUpdate()
    {
        // Only follow if we have a target
        if (target == null)
            return;
            
        // Calculate the desired position (target position + offset)
        Vector3 desiredPosition = target.position + offset;
        
        // Keep the z position from the offset (for 2D games)
        desiredPosition.z = offset.z;
        
        // Smoothly move the camera towards the desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
    }
} 