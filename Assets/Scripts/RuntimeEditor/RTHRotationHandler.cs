using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTHRotationHandler : RuntimeAxisHandle
{

    public RuntimeHandle handleCallback;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private Camera camera;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

    private Vector3 normalAxis;
    private bool dragging;
    private Vector3 startPointWorldSpace;

    private float lastPosition;

    private Quaternion startingRotation;

    private float timeHeld = 0f;

    private float RADIANS_TO_DEGREES = 57.25f;
    private Renderer circleRenderer;

    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) //&& !EventSystem.current.IsPointerOverGameObject()
        {
            // Create ray from camera to mouse
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            int onlyHit = LayerMask.GetMask("RuntimeHandle");
            handleCallback.interactionService.PhysicsRaycast(2, ray, Mathf.Infinity, onlyHit, (hit) => {
                foreach (Collider childCollider in XAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        normalAxis = new Vector3(1, 0, 0);
                        handleCallback.darkenHandles();
                        foreach (Renderer r in XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(1f, 0.6f, 0.6f); }
                    }
                }
                foreach (Collider childCollider in YAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        normalAxis = new Vector3(0, 1, 0);
                        handleCallback.darkenHandles();
                        foreach (Renderer r in YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.6f, 1f, 0.6f); }
                    }
                }
                foreach (Collider childCollider in ZAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        normalAxis = new Vector3(0, 0, 1);
                        handleCallback.darkenHandles();
                        foreach (Renderer r in ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.6f, 0.6f, 1f); }
                    }
                }
                if (normalAxis != new Vector3(0, 0, 0))
                {
                    circleRenderer = hit.transform.GetComponent<Renderer>();
                    handleCallback.setStartingRotation(handleCallback.transform.rotation);
                    normalAxis = handleCallback.transform.TransformDirection(normalAxis);

                    startPointWorldSpace = hit.point;
                    dragging = true;
                    lastPosition = 0;
                }
            });
        }

        if (dragging)
        {
            // "palmAxis" refers to the right hand rule, as if "normalAxis" is the index finger axis.
            Vector3 palmAxis = startPointWorldSpace - transform.position;
            Vector3 tangentAxis = Vector3.Cross(normalAxis.normalized, palmAxis.normalized).normalized;

            Vector3 startPointCamSpace = camera.WorldToScreenPoint(startPointWorldSpace);
            Vector3 referencePointCamSpace = camera.WorldToScreenPoint(startPointWorldSpace + tangentAxis);
            Vector3 mouseVector = Input.mousePosition - startPointCamSpace;

            Vector3 tangentVector = (referencePointCamSpace - startPointCamSpace).normalized;
            float scaleDifference = 1 / (referencePointCamSpace - startPointCamSpace).magnitude;

            float dot = Vector3.Dot(mouseVector, tangentVector);

            float movement = dot * scaleDifference * RADIANS_TO_DEGREES;
            float delta = movement - lastPosition; // delta in world space

            handleCallback.getRotationData(normalAxis, movement, delta);

            lastPosition = movement;
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            normalAxis = new Vector3(0, 0, 0);
            handleCallback.normalizeHandles();
        }
    }
}
