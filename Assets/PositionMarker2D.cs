using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[SerializeField]
public class PositionMarker2D : BRNGScript
{

    private void generateUUID()
    {
        GenerateUniqueID();
    }

    private void Awake()
    {
        generateUUID();
    }
}