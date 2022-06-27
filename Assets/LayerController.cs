using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LayerController : MonoBehaviour
{
    public GameObject listElement;
    public worldManager layerListSource;
    public GameObject buttonPrefab;
    public placementHandler placementHandler;

    public void clearUI()
    {
        foreach (Transform listEntry in listElement.transform)
        {
            listEntry.gameObject.SetActive(false);
            Destroy(listEntry.gameObject);
        }
    }

    public void populateUI()
    {
        clearUI();
        int placedObjects = 0;
        foreach (LayerHandler layer in layerListSource.GetComponentsInChildren<LayerHandler>()) 
        {
            if(layer.gameObject.activeInHierarchy == false) { continue; }
            GameObject newLayerButton = GameObject.Instantiate(buttonPrefab);
            newLayerButton.transform.SetParent(listElement.transform);
            newLayerButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -30 * placedObjects, 0);
            newLayerButton.transform.Find("LayerNameTag").GetComponent<TextMeshProUGUI>().text = layer.layerName;
            newLayerButton.GetComponentInChildren<Button>().onClick.AddListener(delegate { placementHandler.changeLayerSelection(layer); });
            placedObjects++;
        }
    }

    private void Start()
    {
        populateUI();
    }
}
