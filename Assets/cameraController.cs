using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class cameraController : NetworkBehaviour
{
    public float baseSpeed;
    public float maxSpeed;
    public float speedIncreaseOverTime;
    public float cameraSensitivity = 1.0f;

    public float orthographicScrollSpeed = 0.5f;

    private float timeSprintHeld;
    private float curSpeed = 0.0f;
    private Vector3 lastMouseLocation;

    private bool allowInput = true;
    private float transitionTime = 0.5f;
    private float FOV = 60f;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    public Camera camera;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

    private void Start()
    {
        if (!isLocalPlayer) { return; }
        camera = Camera.main;
        camera.transform.SetParent(this.transform);
        camera.transform.localPosition = Vector3.zero;
        camera.fieldOfView = FOV;
    }

    private void Update()
    {
        if(!isLocalPlayer) { return; }
        // Translational movement
        curSpeed = baseSpeed;
        if(Input.GetKey(KeyCode.LeftShift))
        {
            timeSprintHeld += Time.deltaTime;
        } else {
            timeSprintHeld = 0;
        }
        curSpeed = Mathf.Clamp(curSpeed + (speedIncreaseOverTime * timeSprintHeld), 0, maxSpeed);

        if(camera.orthographic)
        {
            curSpeed *= 1.5f;
        }

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
            if (allowInput && !camera.orthographic) {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x - delta.y, transform.eulerAngles.y + delta.x, 0);
            }
            lastMouseLocation = Input.mousePosition;
        }


        // Apply movement transformations
        if (allowInput)
        {
            transform.Translate(translation);
        }

        // If camera is orthographic:
        if(camera.orthographic)
        {
            camera.orthographicSize += -Input.mouseScrollDelta.y * orthographicScrollSpeed;
            if(camera.orthographicSize < 0.5f)
            {
                camera.orthographicSize = 0.5f;
            }
        }


        // Other controls
        if(Input.GetKeyDown(KeyCode.J))
        {
            if(camera.orthographic)
            {
                StartCoroutine(transitionToPerspective());
            } else
            {
                StartCoroutine(transitionToOrthographic());
            }
        }
    }

    IEnumerator transitionToOrthographic()
    {
        camera.orthographicSize = 5;
        allowInput = false;
        float updates = 60;
        float timePerUpdate = transitionTime / updates;
        Vector3 eulerAngles = transform.eulerAngles;
        Vector3 position = transform.position;
        for(int i = 0; i < updates; i++)
        {
            float t = i / updates;
            camera.fieldOfView = Mathf.Lerp(FOV, 1, t);
            transform.eulerAngles = new Vector3(Mathf.LerpAngle(eulerAngles.x, 90, t), Mathf.LerpAngle(eulerAngles.y, 0, t), Mathf.LerpAngle(eulerAngles.z, 0, t));
            transform.position = new Vector3(position.x, Mathf.Lerp(position.y, 125, t), position.z);
            yield return new WaitForSeconds(timePerUpdate);
        }


        camera.orthographic = true;
        transform.rotation = Quaternion.Euler(90, 0, 0);
        allowInput = true;
    }

    IEnumerator transitionToPerspective()
    {
        camera.orthographicSize = 5;
        allowInput = false;
        float updates = 60;
        float timePerUpdate = transitionTime / updates;
        Vector3 position = transform.position;
        camera.orthographic = false;
        for (int i = 0; i < updates; i++)
        {
            float t = i / updates;
            camera.fieldOfView = Mathf.Lerp(1, FOV, t);
            transform.position = new Vector3(position.x, Mathf.Lerp(position.y, 10, t), position.z);
            yield return new WaitForSeconds(timePerUpdate);
        }

        allowInput = true;
    }


    private Vector3 getKeyMovement()
    {
        Vector3 movement = new Vector3(0, 0, 0);
        if (!camera.orthographic)
        {
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
        } else
        {
            if (Input.GetKey(KeyCode.W))
            {
                movement += new Vector3(0, 1, 0);
            }
            if (Input.GetKey(KeyCode.S))
            {
                movement += new Vector3(0, -1, 0);
            }
            if (Input.GetKey(KeyCode.A))
            {
                movement += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey(KeyCode.D))
            {
                movement += new Vector3(1, 0, 0);
            }
        }
        return movement;
    }

}
