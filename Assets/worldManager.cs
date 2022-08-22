using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Mirror;


public class worldManager : NetworkBehaviour
{
    public SerializationService serializationService;
    public LayerController layerUIController;

    public LayerHandler LayerPrefab;

    public SerializationService.WorldData meta;

    private List<LayerHandler> layerList;

    private void Awake()
    {
        layerList = new List<LayerHandler>(this.transform.GetComponentsInChildren<LayerHandler>());
        meta = new SerializationService.WorldData();
        serializationService = this.transform.GetComponent<SerializationService>();
    }

    #region Layer control
    public List<LayerHandler> getLayers()
    {
        return layerList;
    }

    public void removeAllLayers()
    {
        layerList = new List<LayerHandler>();
    }

    public LayerHandler getLayer(int index)
    {
        foreach (LayerHandler LH in layerList)
        {
            if (LH.index == index)
            {
                return LH;
            }
        }
        return null;
    }

    public LayerHandler newLayer()
    {
        // get highest layer num
        int highestnum = -1;
        LayerHandler highest = null;
        foreach(LayerHandler LH in layerList)
        {
            if(LH.index > highestnum)
            {
                highest = LH;
                highestnum = LH.index;
            }
        }

        GameObject newLayer = GameObject.Instantiate(LayerPrefab.gameObject);
        newLayer.transform.SetParent(transform);
        LayerHandler newLayerHandler = newLayer.GetComponent<LayerHandler>();
        
        newLayerHandler.layerName = "New Layer";
        newLayerHandler.visibleToServer = true;
        this.GetComponent<worldManager>().addLayer(newLayerHandler);

        if (highest == null)
        {
            newLayerHandler.index = 0;
        } else
        {
            newLayerHandler.index = highestnum;
        }

        //addLayer(newLayerHandler);
        NetworkServer.Spawn(newLayer);
        layerUIController.populateUI();
        return newLayerHandler;
    }

    public void addLayer(LayerHandler layer)
    {
        layerList.Add(layer);
    }

    public void removeLayer(LayerHandler layer)
    {
        layerList.Remove(layer);
        Destroy(layer.gameObject);
        layerUIController.populateUI();
    }

    public void hideLayer(LayerHandler layer)
    {
        foreach(LayerHandler L in layerList)
        {
            if(L.layerName == layer.layerName)
            {
                layer.gameObject.SetActive(false);
                return;
            }
        }
    }

    public void showLayer(LayerHandler layer)
    {
        foreach (LayerHandler L in layerList)
        {
            if (L.layerName == layer.layerName)
            {
                layer.gameObject.SetActive(true);
                return;
            }
        }
    }
    #endregion

    private void SaveFile(string data, string filename)
    {
        string path = "./Assets/Resources/SavedWorlds/" + filename + ".brng";

        using (var stream = new FileStream(path, FileMode.Truncate))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
            }
        }
    }

    #region Hierarchy Persistence
    // Hierarchy persistence means that the heirarchy is preserved whenever a new client joins
    // This is because mirror usually just spews out all the objects into the scene, so we need to work to re-organize them after they are spawned.

    [System.Serializable]
    public class NetworkHeirarchyPersistenceData
    {
        public GameObject[] child;
        public GameObject[] parent;
        public SerializationService.SerializableTransform[] transforms;

        public GameObject[] layer;
        public string[] layerName;
        public int[] layerIndex;
    }

    [Server]
    public void SendNetworkHeirarchyPersistenceData(NetworkConnection client)
    {
        NetworkHeirarchyPersistenceData data = new NetworkHeirarchyPersistenceData();
        List<GameObject> children = new List<GameObject>();
        List<GameObject> parents = new List<GameObject>();
        List<SerializationService.SerializableTransform> transforms = new List<SerializationService.SerializableTransform>();

        List<GameObject> layers = new List<GameObject>();
        List<string> layerNames = new List<string>();
        List<int> layerIndices = new List<int>();

        foreach (LayerHandler lh in transform.GetComponentsInChildren<LayerHandler>())
        {
            children.Add(lh.gameObject);
            parents.Add(this.gameObject);
            transforms.Add(new SerializationService.SerializableTransform(lh.transform));

            layers.Add(lh.gameObject);
            layerNames.Add(lh.layerName);
            layerIndices.Add(lh.index);

            foreach (placeableObjectManifest obj in lh.transform.GetComponentsInChildren<placeableObjectManifest>())
            {
                children.Add(obj.gameObject);
                parents.Add(lh.gameObject);
                transforms.Add(new SerializationService.SerializableTransform(obj.transform));
            }
        }

        data.child = children.ToArray();
        data.parent = parents.ToArray();
        data.transforms = transforms.ToArray();
        data.layer = layers.ToArray();
        data.layerName = layerNames.ToArray();
        data.layerIndex = layerIndices.ToArray();

        setNetworkHeirarchyPersistenceData(client, data);
    }

    [TargetRpc]
    private void setNetworkHeirarchyPersistenceData(NetworkConnection client, NetworkHeirarchyPersistenceData data)
    {
        for(int i = 0; i < data.child.Length; i++)
        {
            data.child[i].transform.SetParent(data.parent[i].transform);
            data.transforms[i].assignTransform(data.child[i].transform);
        }

        for (int i = 0; i < data.layer.Length; i++)
        {
            data.layer[i].GetComponent<LayerHandler>().layerName = data.layerName[i];
            data.layer[i].GetComponent<LayerHandler>().index = data.layerIndex[i];
        }
    }
    #endregion

    #region UUID Search
    public T GetScriptByUUID<T>(string uuid) where T : BRNGScript
    {
        foreach(BRNGScript script in this.transform.GetComponentsInChildren<BRNGScript>(true))
        {
            if(script.GetUUID() == uuid && script.GetType() == typeof(T))
            {
                return (T)script;
            }
        }
        return null;
    }

    #endregion

    #region Layer Toggling for clients
    [Server]
    public void setLayerStateForPlayer(BRNGPlayer player, LayerHandler layer, bool state)
    {
        setLayerStateClient(player.connectionToClient, layer.gameObject, state);
    }

    [TargetRpc]
    public void setLayerStateClient(NetworkConnection client, GameObject layer, bool state)
    {
        layer.SetActive(state);
    }

    [Server]
    public void hideAllLayersBut(BRNGPlayer player, LayerHandler layer)
    {
        foreach(LayerHandler lh in getLayers())
        {
            if(layer.index != lh.index)
            {
                setLayerStateClient(player.connectionToClient, lh.gameObject, false);
            }
        }
        setLayerStateClient(player.connectionToClient, layer.gameObject, true);
    }


    #endregion

    private void Update()
    {

        if (!isServer) { return; }
        if (Input.GetKeyDown(KeyCode.C))
        {
                SaveFile(serializationService.serializeWorld(this), "testworld");
        }

        if(Input.GetKeyDown(KeyCode.V))
        {
            StreamReader inputStream = new StreamReader("./Assets/Resources/SavedWorlds/testworld.brng");

            string data = inputStream.ReadToEnd();
            layerUIController.clearUI();
            serializationService.DeserializeWorld(data, this);
            inputStream.Close();

            layerUIController.populateUI();
        }

    }

    public LayerHandler getDefaultLayer()
    {
        foreach(LayerHandler layer in transform.GetComponentsInChildren<LayerHandler>())
        {
            if(layer.index == meta.startingLayerIndex)
            {
                return layer;
            }
        }
        return null;
    }
}
