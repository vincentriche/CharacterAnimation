using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrbitCamera : MonoBehaviour
{
	public List<Transform> objsToSee;
	private int currentMaterial;

    public float distance = 3.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 90.0f;

    public float yMinLimit = -15f;
	public float yMaxLimit = 80f;

    public float distanceMin = 0.5f;
    public float distanceMax = 5f;

    private float x = 0.0f;
    private float y = 0.0f;
	
    void Start()
    {
		currentMaterial = 0;
		Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    void LateUpdate()
    {
        if (objsToSee[currentMaterial])
        {
            x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + objsToSee[currentMaterial].position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}