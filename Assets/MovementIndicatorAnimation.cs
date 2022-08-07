using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementIndicatorAnimation : MonoBehaviour
{
    public Transform bobbingArrow;
    public Renderer glow;

    [SerializeField] private float bobDistance;
    [SerializeField] private float bobSpeed;
    [SerializeField] private Vector3 arrowPosition;

    [SerializeField] private float baseGlow;
    [SerializeField] private float glowIncrease;


    private void Update()
    {
        bobbingArrow.transform.position = transform.position + arrowPosition + (Vector3.up * bobDistance * Mathf.Sin(Time.realtimeSinceStartup*bobSpeed));
        glow.sharedMaterial.SetFloat("_ColorFactor", baseGlow + (glowIncrease * (Mathf.Sin(Time.realtimeSinceStartup * bobSpeed)+1))/2);
    }

}
