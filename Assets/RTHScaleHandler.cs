using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTHScaleHandler : RuntimeAxisHandle
{
    public LineRenderer XIndicator;
    public LineRenderer YIndicator;
    public LineRenderer ZIndicator;

    public RuntimeHandle handleCallback;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private Camera camera;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    private Vector3 manipulationAxis;
    private bool dragging;

    private Vector3 startPointWorldSpace;
    private Vector3 lastPosition;

    private Transform draggingHandle;
    private LineRenderer indicatorLine;

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

            handleCallback.interactionService.PhysicsRaycast(2, ray, Mathf.Infinity, onlyHit, (hit) => {
                foreach (Collider childCollider in XAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        manipulationAxis = new Vector3(1, 0, 0);
                        indicatorLine = XIndicator;
                        handleCallback.darkenHandles();
                        foreach (Renderer r in XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(1f, 0.6f, 0.6f); }
                    }
                }
                foreach (Collider childCollider in YAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        manipulationAxis = new Vector3(0, 1, 0);
                        indicatorLine = YIndicator;
                        handleCallback.darkenHandles();
                        foreach (Renderer r in YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.6f, 1f, 0.6f); }

                    }
                }
                foreach (Collider childCollider in ZAxisHandle.GetComponentsInChildren<Collider>())
                {
                    if (childCollider == hit.transform.GetComponent<Collider>())
                    {
                        manipulationAxis = new Vector3(0, 0, 1);
                        indicatorLine = ZIndicator;
                        handleCallback.darkenHandles();
                        foreach (Renderer r in ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.6f, 0.6f, 1f); }
                    }
                }
                if (manipulationAxis != new Vector3(0, 0, 0))
                {
                    manipulationAxis = handleCallback.transform.TransformDirection(manipulationAxis);
                    draggingHandle = hit.transform;
                    handleCallback.setStartingScale(handleCallback.target.localScale);

                    startPointWorldSpace = hit.point;
                    dragging = true;
                    lastPosition = new Vector3(0, 0, 0);
                }
            });
        }

        if (dragging)
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

            draggingHandle.Translate(delta, Space.World);
            indicatorLine.SetPosition(1, draggingHandle.transform.localPosition);
            lastPosition = movement;

            Vector3 scaleNum = handleCallback.transform.InverseTransformDirection(manipulationAxis);

            Vector3 scaling = (movement + manipulationAxis * 2) / 2;

            Vector3 antinormal = (scaleNum - new Vector3(1, 1, 1));
            Vector3 normalizationVector = new Vector3(Mathf.Abs(antinormal.x), Mathf.Abs(antinormal.y), Mathf.Abs(antinormal.z));

            handleCallback.getScaleData(scaleNum * scaling.magnitude + normalizationVector);


            
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            draggingHandle.transform.localPosition = handleCallback.transform.InverseTransformDirection(manipulationAxis) * 2;
            indicatorLine.SetPosition(1, draggingHandle.transform.localPosition);
            dragging = false;
            manipulationAxis = new Vector3(0, 0, 0);
            handleCallback.normalizeHandles();


        }

    }
}
