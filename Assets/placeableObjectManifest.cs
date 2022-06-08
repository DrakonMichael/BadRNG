using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum placeableObjectType { 
    fullTile,
    wallTile,
    cornerTile,
    freePlacement
}
public class placeableObjectManifest : MonoBehaviour
{
    public string tileDisplayName;
    public placeableObjectType objectType;
    public bool rotationAgnostic;
}
