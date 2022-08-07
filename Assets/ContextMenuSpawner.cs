using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public static class RectTransformExtensions
{
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }
}

public class ContextMenuSpawner : MonoBehaviour
{
    public GameObject contextMenuPrefab;
    public GameObject methodGroupPrefab;
    public GameObject interactionButtonPrefab;

    private Camera playerCamera;
    private Vector3 worldSpaceMove;

    public void RemoveContextMenu()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SpawnContextMenu(string menuName, List<BRNGInteractionData> interactions, BRNGPlayerData playerData, Vector3 worldSpaceLocation)
    {
        worldSpaceMove = worldSpaceLocation;
        Dictionary<string, List<BRNGInteractionData>> contextMenuGrouping = new Dictionary<string, List<BRNGInteractionData>>();
        foreach(BRNGInteractionData data in interactions)
        {
            List<BRNGInteractionData> dataList;
            if(contextMenuGrouping.TryGetValue(data.InteractionGroup, out dataList))
            {
                dataList.Add(data);
            } else
            {
                dataList = new List<BRNGInteractionData>();
                dataList.Add(data);
                contextMenuGrouping.Add(data.InteractionGroup, dataList);
            }
        }

        RemoveContextMenu();


        GameObject newContextMenu = GameObject.Instantiate(contextMenuPrefab);
        newContextMenu.transform.SetParent(this.transform);
        newContextMenu.GetComponent<RectTransform>().anchoredPosition = new Vector3(50, -50, 0);
        newContextMenu.transform.Find("ObjectName").GetComponent<TextMeshProUGUI>().text = menuName;
        float trackedYSize = 15;
        foreach(string groupName in contextMenuGrouping.Keys)
        {
            List<BRNGInteractionData> dataList = contextMenuGrouping[groupName];
            GameObject newMethodGroup = GameObject.Instantiate(methodGroupPrefab);
            newMethodGroup.transform.SetParent(newContextMenu.transform);
            newMethodGroup.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -trackedYSize, 0);
            newMethodGroup.transform.Find("GroupName").GetComponent<TextMeshProUGUI>().text = groupName;
            float trackedGroupSize = 15;

            foreach(BRNGInteractionData data in dataList)
            {
                GameObject newButton = GameObject.Instantiate(interactionButtonPrefab);
                newButton.transform.SetParent(newMethodGroup.transform);
                newButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -(trackedGroupSize), 0);
                newButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = data.InteractionName;

                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    data.interactionExecutionFunction();
                    RemoveContextMenu();
                });

                trackedGroupSize += 20;
            }

            trackedYSize += trackedGroupSize;
            newMethodGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(150, trackedGroupSize);
            newMethodGroup.GetComponent<RectTransform>().SetLeft(0);
            newMethodGroup.GetComponent<RectTransform>().SetRight(0);
        }

        newContextMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(150, trackedYSize);

        transform.position = Input.mousePosition;
    }

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        transform.position = playerCamera.WorldToScreenPoint(worldSpaceMove);
    }
}
