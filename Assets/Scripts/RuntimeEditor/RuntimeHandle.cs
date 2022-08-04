using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeHandle : MonoBehaviour
{

    public RTHTranslationHandler translationHandler;
    public RTHRotationHandler rotationHandler;
    public RTHScaleHandler scaleHandler;

    public Transform target;
    public float scaleFactor;

    private Vector3 startingTranslation;
    private Quaternion startingRotation;
    private Vector3 startingScale;

    private Camera camera;

    private void darkenTranslationHandles()
    {
        foreach (Renderer r in translationHandler.XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.7f, 0, 0); }
        foreach (Renderer r in translationHandler.YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, .7f, 0); }
        foreach (Renderer r in translationHandler.ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, .7f); }
    }

    private void darkenRotationHandles()
    {
        foreach (Renderer r in rotationHandler.XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.7f, 0, 0); }
        foreach (Renderer r in rotationHandler.YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, .7f, 0); }
        foreach (Renderer r in rotationHandler.ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, .7f); }
    }

    private void darkenScaleHandles()
    {
        foreach (Renderer r in scaleHandler.XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.7f, 0, 0); }
        foreach (Renderer r in scaleHandler.YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, .7f, 0); }
        foreach (Renderer r in scaleHandler.ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, .7f); }
        foreach (Renderer r in scaleHandler.XIndicator.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0.7f, 0, 0); }
        foreach (Renderer r in scaleHandler.YIndicator.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, .7f, 0); }
        foreach (Renderer r in scaleHandler.ZIndicator.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, .7f); }
    }

    public void darkenHandles()
    {
        darkenTranslationHandles();
        darkenRotationHandles();
        darkenScaleHandles();
    }

    public void normalizeHandles()
    {
        foreach (Renderer r in translationHandler.XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(1f, 0, 0); }
        foreach (Renderer r in translationHandler.YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 1f, 0); }
        foreach (Renderer r in translationHandler.ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, 1f); }
        foreach (Renderer r in rotationHandler.XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(1f, 0, 0); }
        foreach (Renderer r in rotationHandler.YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 1f, 0); }
        foreach (Renderer r in rotationHandler.ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, 1f); }
        foreach (Renderer r in scaleHandler.XAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(1f, 0, 0); }
        foreach (Renderer r in scaleHandler.YAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 1f, 0); }
        foreach (Renderer r in scaleHandler.ZAxisHandle.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, 1f); }
        foreach (Renderer r in scaleHandler.XIndicator.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(1f, 0, 0); }
        foreach (Renderer r in scaleHandler.YIndicator.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 1f, 0); }
        foreach (Renderer r in scaleHandler.ZIndicator.GetComponentsInChildren<Renderer>()) { r.material.color = new Color(0, 0, 1f); }
    }

    public void setStartingTranslation(Vector3 t)
    {
        startingTranslation = t;
    }

    public void setStartingRotation(Quaternion t)
    {
        startingRotation = t;
    }

    public void setStartingScale(Vector3 t)
    {
        startingScale = t;
    }

    public void getTranslationData(Vector3 movement, Vector3 delta)
    {
        target.position = startingTranslation;
        target.Translate(movement, Space.World);
    }

    public void getRotationData(Vector3 axis, float movement, float delta)
    {
        target.rotation = startingRotation;
        target.RotateAround(transform.position, axis, movement);
    }

    public void getScaleData(Vector3 movement)
    {
        target.localScale = Vector3.Scale(startingScale, movement);
        
    }


    private void Start()
    {
        camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if(!target) { return; }
        if(!camera) { camera = Camera.main; }
        transform.position = target.position;
        transform.rotation = target.rotation;

        float size = (camera.transform.position - transform.position).magnitude * scaleFactor;

        transform.localScale = new Vector3(size, size, size);

    }
}
