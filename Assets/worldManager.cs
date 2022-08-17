using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Mirror;


public class worldManager : NetworkBehaviour
{
    public SerializationService serializationService;
    public LayerController layerUIController;

    public SerializationService.WorldData meta;

    private void Start()
    {
        meta = new SerializationService.WorldData();
        serializationService = this.transform.GetComponent<SerializationService>();
    }

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
