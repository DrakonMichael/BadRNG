using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class SerializationService : NetworkBehaviour
{
    public LayerHandler LayerPrefab;

    #region Helper Serialization Classes
    /* -------------------- BEGIN SERIALIZABLE HELPER CLASSES -------------------- */

    [System.Serializable]
    public class SerializedScript
    {
        public string fullScriptTypename;
        public string serializedScriptData;
    }

    [System.Serializable]
    public class SerializableTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public SerializableTransform()
        {
            position = new Vector3(0, 0, 0);
            rotation = Quaternion.Euler(0, 0, 0);
            localScale = new Vector3(0, 0, 0);
        }

        public SerializableTransform(Transform other) {
            position = other.position;
            rotation = other.rotation;
            localScale = other.localScale;
        }

        public void assignTransform(Transform other)
        {
            other.position = position;
            other.rotation = rotation;
            other.localScale = localScale;
        }
    }

    [System.Serializable]
    public class SerializedObject
    {
        public string assetID;
        public SerializableTransform transform;
        public List<SerializedScript> scripts;
        public List<SerializedObject> children;
    }

    [System.Serializable]
    public class SerializedLayer {
        public List<SerializedObject> placeableObjects;
        public string layerName;
        public int index;
        public bool visibleToServer;
        public SerializableTransform transform;
    }


    [System.Serializable]
    public class WorldData
    {
        public string name;
        public string version;
        public string createdAt;
        public string editedAt;
        public int startingLayerIndex = 0;
    }

    [System.Serializable]
    public class SerializedWorld
    {
        public List<SerializedLayer> layers;
        public WorldData metaData;
    }

    /* -------------------- END SERIALIZABLE HELPER CLASSES -------------------- */
    #endregion

    #region Serialization Code
    /* -------------------- BEGIN SERIALIZATION CODE -------------------- */
    private List<SerializedScript> SerializeObjectBRNGScripts(GameObject obj)
    {
        List<SerializedScript> scriptList = new List<SerializedScript>();
        foreach (Component comp in obj.GetComponents<Component>())
        {
            if (comp is BRNGScript)
            {
                SerializedScript serialized = new SerializedScript();
                serialized.fullScriptTypename = comp.GetType().FullName;
                serialized.serializedScriptData = JsonUtility.ToJson(comp);
                scriptList.Add(serialized);
            }
        }
        return scriptList;
    }

    private List<SerializedObject> RecursiveObjectSerialize(GameObject parentObject)
    {
        
        List<SerializedObject> children = new List<SerializedObject>();
        foreach(Transform t in parentObject.transform)
        {
            placeableObjectManifest manifest = t.GetComponent<placeableObjectManifest>();
            if (manifest)
            {
                SerializedObject serializedChild = SerializePlaceableObject(manifest);
                serializedChild.children = RecursiveObjectSerialize(manifest.gameObject);
                children.Add(serializedChild);
            }
        }
        return children;
    }

    private SerializedObject SerializePlaceableObject(placeableObjectManifest objectToSerialize)
    {
        SerializedObject serializedObject = new SerializedObject();
        serializedObject.assetID = objectToSerialize.assetID;
        serializedObject.transform = new SerializableTransform(objectToSerialize.transform);
        serializedObject.scripts = SerializeObjectBRNGScripts(objectToSerialize.gameObject);
        return serializedObject;
    }

    private SerializedLayer serializeLayer(LayerHandler layer)
    {
        SerializedLayer serializedLayer = new SerializedLayer();
        serializedLayer.index = layer.index;
        serializedLayer.layerName = layer.layerName;
        serializedLayer.visibleToServer = layer.visibleToServer;
        serializedLayer.placeableObjects = new List<SerializedObject>();
        serializedLayer.placeableObjects = RecursiveObjectSerialize(layer.gameObject);
        serializedLayer.transform = new SerializableTransform(layer.transform);
        return serializedLayer;
    }

    public string serializeWorld(worldManager world)
    {
        SerializedWorld serializedWorld = new SerializedWorld();
        serializedWorld.layers = new List<SerializedLayer>();
        foreach(LayerHandler layerhandler in world.transform.GetComponentsInChildren<LayerHandler>())
        {
            serializedWorld.layers.Add(serializeLayer(layerhandler));
        }
        WorldData worlddata = world.meta;
        worlddata.name = "NEW WORLD / PLACEHOLDER";
        worlddata.version = "PA v0.1";
        serializedWorld.metaData = worlddata;
        return JsonUtility.ToJson(serializedWorld);
    }

    /* -------------------- END SERIALIZATION CODE -------------------- */
    #endregion

    #region Deserialization Code
    /* -------------------- BEGIN DESERIALIZATION CODE -------------------- */

    [ClientRpc]
    private void RpcSetupObjectTransform(GameObject networkedObject, GameObject parentObject, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        networkedObject.transform.SetParent(parentObject.transform);
        networkedObject.transform.position = pos;
        networkedObject.transform.rotation = rot;
        networkedObject.transform.localScale = scale;
    }


    private void RecursiveDeserializeObjects(GameObject parent, List<SerializedObject> childrenToDeserialize)
    {
        if (!isServer) { return; }
        foreach (SerializedObject serializedObject in childrenToDeserialize)
        {
            foreach (placeableObjectManifest manifest in Resources.LoadAll<placeableObjectManifest>("/"))
            {
                if (manifest.assetID == serializedObject.assetID)
                {
                    GameObject newObject = Instantiate(manifest.gameObject);
                    newObject.transform.name = newObject.transform.name.Replace("(Clone)", "").Trim();

                    // script deserialization
                    foreach(SerializedScript script in serializedObject.scripts)
                    {
                        Type scriptType = Type.GetType(script.fullScriptTypename);
                        JsonUtility.FromJsonOverwrite(script.serializedScriptData, newObject.GetComponent(scriptType));
                    }

                    NetworkServer.Spawn(newObject);
                    RpcSetupObjectTransform(newObject, parent, serializedObject.transform.position, serializedObject.transform.rotation, serializedObject.transform.localScale);
                    RecursiveDeserializeObjects(newObject, serializedObject.children);
                }
            }
            
        }
    }

    [ClientRpc]
    private void RpcUpdateLayers(GameObject Layer, SerializedLayer layerInfo)
    {
        Layer.GetComponent<LayerHandler>().index = layerInfo.index;
        Layer.GetComponent<LayerHandler>().layerName = layerInfo.layerName;
        Layer.GetComponent<LayerHandler>().visibleToServer = layerInfo.visibleToServer;
    }

    [Server]
    public void DeserializeWorld(string serializedWorldData, worldManager toWorld)
    {
        // Removes all children and descendants of the existing world to clear it for deserialization.
        foreach (Transform child in this.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        SerializedWorld world = JsonUtility.FromJson<SerializedWorld>(serializedWorldData);
        this.GetComponent<worldManager>().removeAllLayers();
        foreach (SerializedLayer layer in world.layers)
        {
            GameObject newLayer = GameObject.Instantiate(LayerPrefab.gameObject);
            LayerHandler newLayerHandler = newLayer.GetComponent<LayerHandler>();
            newLayerHandler.index = layer.index;
            newLayerHandler.layerName = layer.layerName;
            newLayerHandler.visibleToServer = layer.visibleToServer;
            this.GetComponent<worldManager>().addLayer(newLayerHandler);
            NetworkServer.Spawn(newLayer);
            newLayer.transform.SetParent(toWorld.transform);

            RpcSetupObjectTransform(newLayer, toWorld.gameObject, layer.transform.position, layer.transform.rotation, layer.transform.localScale);
            RpcUpdateLayers(newLayer, layer);
            RecursiveDeserializeObjects(newLayer, layer.placeableObjects);
        }
        toWorld.meta = world.metaData;

        
        foreach(BRNGScript script in toWorld.getDefaultLayer().GetComponentsInChildren<BRNGScript>(true))
        {
            Debug.Log(script.GetType().Name);
            script.onDeserialization();
        }

    }

    /* -------------------- END DESERIALIZATION CODE -------------------- */
    #endregion
}
