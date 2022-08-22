using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[System.Serializable]
public enum placeableObjectType { 
    fullTile,
    wallTile,
    cornerTile,
    prop
}

[System.Serializable]
public enum SourceType
{
    baseAsset,
    premiumAsset,
    workshopAsset
}

public class placeableObjectManifest : NetworkBehaviour
{
    public string tileDisplayName;
    public placeableObjectType objectType;
    public bool rotationAgnostic;
    public string assetID;
    public SourceType sourceLocation;

    /**
     * For 'baseAsset' - Not used
     * For 'premiumAsset' - Premium subfolder used
     * For 'workshopAsset' - the workshop ID.
     */
    public string sourceData;

}
