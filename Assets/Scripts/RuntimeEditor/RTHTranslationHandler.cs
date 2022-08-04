using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RTHTranslationHandler : RuntimeAxisHandle
{


    public RuntimeHandle handleCallback;



#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private Camera camera;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    private Vector3 manipulationAxis;
    private bool dragging;

    private Vector3 startPointWorldSpace;
    private Vector3 lastPosition;


    private void Start()
    {
        camera = Camera.main;
        manipulationAxis = new Vector3(0, 0, 0);
        dragging = false;
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0)) //&& !EventSystem.current.IsPointerOverGameObject()
        {
            // Create ray from camera to mouse
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            int onlyHit = LayerMask.GetMask("RuntimeHandle");
            handleCallback.interactionService.PhysicsRaycast(2, ray, Mathf.Infinity, onlyHit, (hit) =>
            {
                foreach (Collider childCollider in XAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        manipulationAxis = new Vector3(1, 0, 0);
                        handleCallback.darkenHandles();
                        foreach (Renderer r in XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(1f, 0.6f, 0.6f); }
                    }
                }
                foreach (Collider childCollider in YAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        manipulationAxis = new Vector3(0, 1, 0);
                        handleCallback.darkenHandles();
                        foreach (Renderer r in YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.6f, 1f, 0.6f); }
                    }
                }
                foreach (Collider childCollider in ZAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        manipulationAxis = new Vector3(0, 0, 1);
                        handleCallback.darkenHandles();
                        foreach (Renderer r in ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.6f, 0.6f, 1f); }
                    }
                }
                if (manipulationAxis != new Vector3(0, 0, 0))
                {
                    manipulationAxis = handleCallback.transform.TransformDirection(manipulationAxis);
                    handleCallback.setStartingTranslation(handleCallback.transform.position);
                    startPointWorldSpace = hit.point;
                    dragging = true;
                    lastPosition = new Vector3(0, 0, 0);
                }
            });
        }

        if(dragging)
        {
            Vector3 referenceWorldSpace = startPointWorldSpace + manipulationAxis;
            Vector3 cameraStartPoint = camera.WorldToScreenPoint(startPointWorldSpace);
            Vector3 cameraReferencePoint = camera.WorldToScreenPoint(referenceWorldSpace);
            Vector3 mousePos = Input.mousePosition;

            Vector3 mouseVector = mousePos - cameraStartPoint;
            Vector3 relativeVector = (cameraReferencePoint - cameraStartPoint).normalized;

            float scaleDifference = 1 / (cameraReferencePoint - cameraStartPoint).magnitude;

            float dot = Vector3.Dot(mouseVector, relativeVector);
            Vector3 movement = dot * scaleDifference * manipulationAxis;
            Vector3 delta = movement - lastPosition; // delta in world space
            handleCallback.getTranslationData(movement, delta);

            
            lastPosition = movement;
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            manipulationAxis = new Vector3(0, 0, 0);
            handleCallback.normalizeHandles();
        }

    }
}
