using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    public float baseSpeed;
    public float maxSpeed;
    public float speedIncreaseOverTime;
    public float cameraSensitivity = 1.0f;

    private float timeSprintHeld;
    private float curSpeed = 0.0f;
    private Vector3 lastMouseLocation;

    private void Update()
    {
        // Translational movement
        curSpeed = baseSpeed;
        if(Input.GetKey(KeyCode.LeftShift))
        {
            timeSprintHeld += Time.deltaTime;
        } else {
            timeSprintHeld = 0;
        }
        curSpeed = Mathf.Clamp(curSpeed + (speedIncreaseOverTime * timeSprintHeld), 0, maxSpeed);
        Vector3 translation = new Vector3(0, 0, 0);
        translation += getKeyMovement() * curSpeed * Time.deltaTime;


        // Rotational movement
        if(Input.GetMouseButtonDown(1))
        {
            lastMouseLocation = Input.mousePosition;
        }

        if(Input.GetMouseButton(1))
        {
            Vector3 delta = (Input.mousePosition - lastMouseLocation) * cameraSensitivity;
            transform.eulerAngles = new Vector3(transform.eulerAngles.x - delta.y, transform.eulerAngles.y + delta.x, 0);
            lastMouseLocation = Input.mousePosition;
        }


        // Apply movement transformations

        transform.Translate(translation);
    }

    private Vector3 getKeyMovement()
    {
        Vector3 movement = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.W))
        {
            movement += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += new Vector3(1, 0, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            movement += new Vector3(0, 1, 0);
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            movement += new Vector3(0, -1, 0);
        }
        return movement;
    }

}
