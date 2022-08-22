using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class LayerController : MonoBehaviour
{
    public GameObject listElement;
    public worldManager layerListSource;
    public GameObject buttonPrefab;
    public placementHandler placementHandler;
    public ContextMenuSpawner contextMenuSpawner;
    public InteractionService interactionService;

    public Button newLayerButton;

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
        foreach (LayerHandler layer in layerListSource.getLayers()) 
        {
            
            if(layer == null) { continue; }
            if(layer.gameObject.activeInHierarchy == false) { continue; }
            GameObject newLayerButton = GameObject.Instantiate(buttonPrefab);
            newLayerButton.transform.SetParent(listElement.transform);
            newLayerButton.transform.Find("LayerNameTag").GetComponent<TextMeshProUGUI>().text = layer.layerName;
            

            Transform visButton = newLayerButton.transform.Find("VisButton");
            Transform delButton = newLayerButton.transform.Find("DelButton");

            Action toggleVisibility = () =>
            {
                if (visButton.GetComponent<Image>().color == new Color(1f, 0f, 0f, 0.4f))
                {
                    layer.visibleToServer = true;
                    layerListSource.showLayer(layer);
                    visButton.GetComponent<Image>().color = new Color(0f, 1f, 0f, 0.4f);
                }
                else
                {
                    layer.visibleToServer = false;
                    layerListSource.hideLayer(layer);
                    visButton.GetComponent<Image>().color = new Color(1f, 0f, 0f, 0.4f);
                }

            };

            Action focusLayer = () =>
            {
                placementHandler.changeLayerSelection(layer);
            };

            newLayerButton.GetComponent<Button>().onClick.AddListener(() => { focusLayer(); });

            newLayerButton.GetComponent<UIRightClickHandler>().rightClick.AddListener(() =>
            {
                Dictionary<string, Action> actions = new Dictionary<string, Action>();
                actions.Add("Move", () => { interactionService.ICRuntimeHandle(layer.transform, true); });
                actions.Add("Focus", () => { focusLayer(); });
                actions.Add("Toggle Visibility", () => { toggleVisibility(); });
                actions.Add("Delete", () => { layerListSource.removeLayer(layer); });

                contextMenuSpawner.SpawnBasicContextMenu2D("Layer", actions, Input.mousePosition);
            });


            if (!layer.visibleToServer) {
                visButton.GetComponent<Image>().color = new Color(1f, 0f, 0f, 0.4f);
            }

            delButton.GetComponent<Button>().onClick.AddListener(() => {
                layerListSource.removeLayer(layer);
            });
           

            visButton.GetComponent<Button>().onClick.AddListener(() => { toggleVisibility(); });
            placedObjects++;
        }
    }

    private void Start()
    {
        newLayerButton.onClick.AddListener(() =>
        {
            layerListSource.newLayer();
        });
        populateUI();
    }
}
