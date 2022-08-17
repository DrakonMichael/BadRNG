using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class placeableObjectListHandler : MonoBehaviour
{
    public placementHandler placementHandler;
    public GameObject buttonPrefab;
    public GameObject uiPanel;

    public void populateList()
    {
        placementHandler.reloadObjectList();
        List<GameObject> objectList = placementHandler.getObjectList();
        for(int i = 0; i < objectList.Count; i++)
        {
            GameObject placeableObject = objectList[i];
            GameObject newButton = Instantiate(buttonPrefab);
            newButton.transform.SetParent(transform);
            newButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 150 - (i*30), 0);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = placeableObject.GetComponent<placeableObjectManifest>().tileDisplayName;
            newButton.GetComponent<Button>().onClick.AddListener(delegate { placementHandler.setSelectedObject(placeableObject); placementHandler.loadNewSelector(); });
        }
    }

    private void Start()
    {
        populateList();
    }

    private void Update()
    {
        // We want to resize the content frame to accomodate scrolling for all objects
        transform.GetComponent<RectTransform>().sizeDelta = new Vector2(0, transform.GetComponent<GridLayoutGroup>().preferredHeight);
    }

}
