using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Mirror;


public class worldManager : NetworkBehaviour
{
    [System.Serializable]
    private class SerializedTile {
        public Vector3 position;
        public Quaternion orientation;
        public Vector3 localScale;

        public string assetID;
        public SourceType sourceLocation;
        public string sourceData;

        public SerializedTile(placeableObjectManifest m, Transform t) {
            sourceLocation = m.sourceLocation;
            sourceData = m.sourceData;
            assetID = m.assetID;
            position = t.position;
            orientation = t.rotation;
            localScale = t.localScale;
        }
    }

    [System.Serializable]
    private class SerializedWorld
    {
        public List<SerializedTile> tiles;
        public string name = "My world";
    }

    public string serializeWorld()
    {
        SerializedWorld world = new SerializedWorld();
        world.tiles = new List<SerializedTile>();
        foreach(placeableObjectManifest worldObject in transform.GetComponentsInChildren<placeableObjectManifest>())
        {
            SerializedTile tile = new SerializedTile(worldObject, worldObject.transform);
            world.tiles.Add(tile);
        }
        return JsonUtility.ToJson(world);
    }

    private SerializedWorld deserealizeWorld(string serealizedString)
    {
        return JsonUtility.FromJson<SerializedWorld>(serealizedString);
    }

    private void placeTileOnGrid(SerializedTile tile)
    {
        if (isServer)
        {
            foreach (placeableObjectManifest manifest in Resources.LoadAll<placeableObjectManifest>("CreatorAssets"))
            {
                Debug.Log(manifest.assetID);
                if(manifest.assetID == tile.assetID)
                {
                    
                    placeTile(manifest.gameObject, tile.position, tile.orientation, tile.localScale);
                }
            }
        }
    }

    private void placeTile(GameObject tileToPlace, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject instantiatedTile = GameObject.Instantiate(tileToPlace, position, rotation);
        instantiatedTile.transform.localScale = scale;
        instantiatedTile.transform.SetParent(transform);
        instantiatedTile.transform.name = instantiatedTile.transform.name.Replace("(Clone)", "").Trim();
        NetworkServer.Spawn(instantiatedTile);
    }

    private void Update()
    {
        if (!isServer) { return; }
        if (Input.GetKeyDown(KeyCode.C))
        {
            string world = serializeWorld();

            string path = "Assets/Resources/SavedWorlds/myworld.json";
            FileStream stream = new FileStream(path, FileMode.Truncate);
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(world);
            writer.Close();
        }

        if(Input.GetKeyDown(KeyCode.V))
        {
            string path = "Assets/Resources/SavedWorlds/myworld.json";
            StreamReader reader = new StreamReader(path);
            SerializedWorld world = deserealizeWorld(reader.ReadToEnd());
            reader.Close();

            foreach (SerializedTile t in world.tiles)
            {
                placeTileOnGrid(t);
            }
        }
    }
}
