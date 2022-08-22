using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionBoxVisual : MonoBehaviour
{
    public Color dimColor;
    public Color brightColor;
    public float speed;

    public GameObject plumbob;
    public float plumbobPosition;
    public float plumbobDistance;

    private void Update()
    {
        plumbob.transform.localPosition = new Vector3(0, plumbobPosition, 0) + new Vector3(0, Mathf.Sin(Time.realtimeSinceStartup * speed) * plumbobDistance, 0);


        Color curColor = Color.Lerp(dimColor, brightColor, (Mathf.Sin(Time.realtimeSinceStartup * speed) + 1) / 2);
        foreach(Renderer r in transform.GetComponentsInChildren<Renderer>())
        {
            r.material.SetColor(Shader.PropertyToID("_Color"), curColor);
        }
    }
}
