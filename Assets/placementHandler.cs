using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;

public class placementHandler : NetworkBehaviour
{
    
    public Material selectionMaterial;
    public Material placementMaterial;
    public worldManager placementDestination;

    private List<GameObject> tiles;
    private GameObject selector;
    private Plane gridPlane;
    private GameObject selectedTile;

    private GameObject focusedTile;
    private List<GameObject> focusSelectorVisual;

    private bool dragging = false;
    private float draggingThreshhold = 0.2f;
    private float draggingDebounce = 0.0f;

    private void Start()
    {
        reloadObjectList();
        gridPlane = new Plane(Vector3.up, 0);
    }

    public void reloadObjectList()
    {
        focusSelectorVisual = new List<GameObject>();
        tiles = new List<GameObject>();
        foreach (placeableObjectManifest manifest in Resources.LoadAll<placeableObjectManifest>("CreatorAssets"))
        {
            tiles.Add(manifest.gameObject);
        }
    }

    public List<GameObject> getObjectList()
    {
        return tiles;
    }

    public void setSelectedObject(GameObject selected)
    {
        selectedTile = selected;
        loadNewSelector();
    }

    public void loadNewSelector()
    {
        clearFocusTile();
        if(selector != null) { Destroy(selector); }
        if(selectedTile == null) { return; }
        
        selector = GameObject.Instantiate(selectedTile, new Vector3(0, -10, 0), Quaternion.Euler(0, 0, 0));
        foreach (Renderer r in selector.GetComponentsInChildren<Renderer>())
        {
            r.material = placementMaterial;
        }
        
    }

    private void clearFocusTile()
    {
        if (focusSelectorVisual.Count != 0)
        {
            foreach (GameObject o in focusSelectorVisual)
            {
                Destroy(o);
            }
        }
        focusSelectorVisual = new List<GameObject>();
    }

    private void focusTile(GameObject tile)
    {
        clearFocusTile();
        focusedTile = tile;
        foreach (Renderer r in focusedTile.GetComponentsInChildren<Renderer>())
        {
            GameObject newVisual = Instantiate(r.gameObject, r.transform.position, r.transform.rotation);
            focusSelectorVisual.Add(newVisual);
            newVisual.GetComponent<Renderer>().material = selectionMaterial;
        }
    }

    private void Update()
    {

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && selectedTile == null)
        {
            RaycastHit hit;
            Ray selectionRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(selectionRay, out hit))
            {
                if(hit.transform.GetComponentInChildren<placeableObjectManifest>())
                {
                    focusTile(hit.transform.gameObject);
                }
            }
        }

        if(focusedTile && Input.GetKeyDown(KeyCode.Delete))
        {
            clearFocusTile();
            Destroy(focusedTile);
        }

        if (selectedTile == null) { return; }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float enter = 0.0f;
        Vector3 newVector = new Vector3(0,0,0);

        if (gridPlane.Raycast(ray, out enter))
        {

            Vector3 hitLocation = ray.GetPoint(enter);
            newVector = new Vector3(Mathf.Ceil(hitLocation.x) - 0.5f, 0.05f, Mathf.Ceil(hitLocation.z) - 0.5f);
            
            Vector3 cornerVector = hitLocation - newVector;
            float angle = Mathf.Round(Mathf.Atan2(cornerVector.x, cornerVector.z)/(Mathf.PI / 2)) * 90;

            placeableObjectType tileType = selectedTile.GetComponent<placeableObjectManifest>().objectType;

            setIndicatorLocation(newVector, angle, tileType);

            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                if(tileType == placeableObjectType.fullTile)
                {
                    placeSelectedTile(newVector, Quaternion.Euler(0, 0, 0));
                } else
                {
                    placeSelectedTile(newVector, Quaternion.Euler(0, angle-90, 0));
                }
                
            }

        }




        if (Input.GetKeyDown(KeyCode.Escape))
        {
            setSelectedObject(null);
        }

    }

    private void setIndicatorLocation(Vector3 location, float angle, placeableObjectType tileType)
    {
        selector.transform.position = location;
        if (tileType == placeableObjectType.wallTile)
        {
            selector.transform.rotation = Quaternion.Euler(0, angle - 90, 0);
        } 
        
    }

    private void placeSelectedTile(Vector3 location, Quaternion orientation)
    {
        if (isServer)
        {
            GameObject instantiatedTile = GameObject.Instantiate(selectedTile, location, orientation);
            instantiatedTile.transform.SetParent(placementDestination.transform);
            NetworkServer.Spawn(instantiatedTile);
        }
    }
}
