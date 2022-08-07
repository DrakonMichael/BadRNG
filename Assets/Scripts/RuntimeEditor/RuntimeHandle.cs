using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum RuntimeHandleMode
{
    None,
    X,
    Y,
    Z,
    XY,
    XZ,
    YZ,
    XYZ
}

public class RuntimeHandle : MonoBehaviour
{
    [Header("Translation Settings")]
    public RTHTranslationHandler translationHandler;
    public RuntimeHandleMode TranslationMode;
    private RuntimeHandleMode TMode_Stored = RuntimeHandleMode.XYZ;

    public float TranslationSnapping;

    [Header("Rotation Settings")]
    public RTHRotationHandler rotationHandler;
    public RuntimeHandleMode RotationMode;
    private RuntimeHandleMode RMode_Stored = RuntimeHandleMode.XYZ;

    public float RotationSnapping;

    [Header("Scale Settings")]
    public RTHScaleHandler scaleHandler;
    public RuntimeHandleMode ScaleMode;
    private RuntimeHandleMode SMode_Stored = RuntimeHandleMode.XYZ;

    public float ScaleSnapping;

    [Header("General Settings")]
    public InteractionService interactionService;

    public Transform target;
    public float scaleFactor;

    private Vector3 startingTranslation;
    private Quaternion startingRotation;
    private Vector3 startingScale;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private Camera camera;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

    #region Handle Colors
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
    #endregion

    #region Movement information
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
        float scale = movement.magnitude;
        if(TranslationSnapping != 0)
        {
            scale = Mathf.Round(scale / TranslationSnapping) * TranslationSnapping;
        }
        target.Translate(movement.normalized * scale, Space.World);
    }

    public void getRotationData(Vector3 axis, float movement, float delta)
    {
        target.rotation = startingRotation;

        float scaled = movement;
        if (RotationSnapping != 0)
        {
            scaled = Mathf.Round(scaled / RotationSnapping) * RotationSnapping;
        }

        target.RotateAround(transform.position, axis, scaled);
    }

    public void getScaleData(Vector3 movement)
    {
        float scale = movement.magnitude;
        if (ScaleSnapping != 0)
        {
            scale = Mathf.Round(scale / ScaleSnapping) * ScaleSnapping;
        }
        target.localScale = Vector3.Scale(startingScale, movement);
        
    }
    #endregion

    #region Restriction

    private void handleModeUpdate(RuntimeAxisHandle handle, bool X, bool Y, bool Z)
    {
        if(!X && !Y && !Z) { handle.gameObject.SetActive(false); } else
        {
            handle.gameObject.SetActive(true);
        }
        handle.XAxisHandle.gameObject.SetActive(X);
        handle.YAxisHandle.gameObject.SetActive(Y);
        handle.ZAxisHandle.gameObject.SetActive(Z);
    }

    private void SetHandleMode(RuntimeAxisHandle handle, RuntimeHandleMode mode)
    {
        switch(mode)
        {
            case RuntimeHandleMode.None:
                handleModeUpdate(handle, false, false, false);
                break;
            case RuntimeHandleMode.X:
                handleModeUpdate(handle, true, false, false);
                break;
            case RuntimeHandleMode.Y:
                handleModeUpdate(handle, false, true, false);
                break;
            case RuntimeHandleMode.Z:
                handleModeUpdate(handle, false, false, true);
                break;
            case RuntimeHandleMode.XY:
                handleModeUpdate(handle, true, true, false);
                break;
            case RuntimeHandleMode.XZ:
                handleModeUpdate(handle, true, false, true);
                break;
            case RuntimeHandleMode.YZ:
                handleModeUpdate(handle, false, true, true);
                break;
            case RuntimeHandleMode.XYZ:
                handleModeUpdate(handle, true, true, true);
                break;
        }
    }

    #endregion

    private void Start()
    {
        camera = Camera.main;
        interactionService = GameObject.FindGameObjectWithTag("Interaction Service").GetComponent<InteractionService>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!target || !interactionService) { return; }
        if(!camera) { camera = Camera.main; }
        transform.position = target.position;
        transform.rotation = target.rotation;

        float size = (camera.transform.position - transform.position).magnitude * scaleFactor;

        transform.localScale = new Vector3(size, size, size);


        if (TMode_Stored != TranslationMode) { TMode_Stored = TranslationMode; SetHandleMode(translationHandler, TranslationMode); }
        if (RMode_Stored != RotationMode) { RMode_Stored = RotationMode; SetHandleMode(rotationHandler, RotationMode); }
        if (SMode_Stored != ScaleMode) { SMode_Stored = ScaleMode; SetHandleMode(scaleHandler, ScaleMode); }

    }
}
