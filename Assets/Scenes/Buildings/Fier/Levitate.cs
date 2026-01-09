using UnityEngine;

public class Levitate : MonoBehaviour
{
    [Header("Movement Settings")]
    public float rotationSpeed = 50f;
    public float amplitude = 0.5f; // How high/low it goes
    public float frequency = 1f;    // How fast it oscillates

    // Store the starting position of the object
    private Vector3 startPosition;

    void Start()
    {
        // Store the initial position to use as a reference point
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. Rotation: Rotates around the Y axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 2. Levitation (Vertical movement)
        // Using Sine wave for a smooth up-and-down motion
        float newY = startPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;
        
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}