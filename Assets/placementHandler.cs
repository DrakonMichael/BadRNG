using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;
using DG.Tweening;
public class placementHandler : NetworkBehaviour
{
   
    public Material selectionMaterial;
    public Material placementMaterial;
    public worldManager targetWorld;
    public LayerHandler targetLayer;
    public GameObject placementGrid;

    [SerializeField] private float tweenTime = 0.2f;

    private List<GameObject> tiles;
    private GameObject selector;
    private Plane gridPlane;
    private GameObject selectedTile;

    private int layerVerticalOffset = 0;

    private GameObject focusedTile;
    private List<GameObject> focusSelectorVisual;

    private bool dragging = false;
    private float draggingThreshhold = 0.2f;
    private float draggingDebounce = 0.0f;

    private Camera placementCamera;

    private void Start()
    {
        if(!isServer) { return; }
        reloadObjectList();
        gridPlane = new Plane(Vector3.up, 0);
    }

    // For instant change
    private void setPlane(LayerHandler layer, int height)
    {
        gridPlane = new Plane(Vector3.up, layer.transform.position + new Vector3(0, height, 0));
        placementGrid.transform.position = layer.transform.position + new Vector3(0, height, 0);
        placementGrid.transform.GetChild(0).transform.position = layer.transform.position;
    }

    // For gradual change
    private void setPlaneTweened(LayerHandler layer, int height)
    {
        gridPlane = new Plane(Vector3.up, layer.transform.position + new Vector3(0, height, 0));
        placementGrid.transform.DOMove(layer.transform.position + new Vector3(0, height, 0), tweenTime);
        placementGrid.transform.GetChild(0).transform.DOMove(layer.transform.position, tweenTime);
    }

    public void changeLayerSelection(LayerHandler layer)
    {
        if (!isServer) { return; }
        targetLayer = layer;
        reloadObjectList();
        placementGrid.transform.position = layer.transform.position;
        layerVerticalOffset = 0;
        setPlane(targetLayer, layerVerticalOffset);
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

    void handleObjectSelection()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && selectedTile == null)
        {
            RaycastHit hit;
            Ray selectionRay = placementCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(selectionRay, out hit))
            {
                if (hit.transform.GetComponentInChildren<placeableObjectManifest>())
                {
                    focusTile(hit.transform.gameObject);
                }
            }
        }

        if (focusedTile && Input.GetKeyDown(KeyCode.Delete))
        {
            clearFocusTile();
            Destroy(focusedTile);
        }
    }

    private void Update()
    {
        if(!isServer) { return; }

        if(!placementCamera)
        {
            if(GameObject.FindGameObjectWithTag("currentCamera"))
            {
                placementCamera = GameObject.FindGameObjectWithTag("currentCamera").GetComponent<Camera>();
            } else
            {
                return;
            }
        }

        if(!targetLayer)
        {
            targetLayer = targetWorld.getDefaultLayer();
        }


        // If no tile is slated for placement, handle selecting objects instead
        if (selectedTile == null) { handleObjectSelection(); return; }

        // Create ray from camera to mouse
        Ray ray = placementCamera.ScreenPointToRay(Input.mousePosition);
        float enter;

        // If ray intersects building grid..
        if (gridPlane.Raycast(ray, out enter))
        {
            // Where in world space the ray hits the grid
            Vector3 hitLocation = ray.GetPoint(enter);

            // Center of the grid tile where the ray hit
            Vector3 tileCenter = new Vector3(Mathf.Ceil(hitLocation.x) - 0.5f, targetLayer.transform.position.y + layerVerticalOffset, Mathf.Ceil(hitLocation.z) - 0.5f);

            // Vector from the center of the tile to where the ray actually hit.
            Vector3 cornerVector = hitLocation - tileCenter;

            //Angle which is made between top of tile and mouse pointer
            float angle = Mathf.Atan2(cornerVector.x, cornerVector.z);
            
            // Angle which WALL tiles must be rotated to line up correctly.
            float wall_rotationAngle = (Mathf.Round(angle / (Mathf.PI / 2)) * 90)-90;

            // Angle which CORNER tiles must be rotated to line up correctly
            float corner_rotationAngle = (Mathf.Round((angle+(Mathf.PI/4)) / (Mathf.PI / 2)) * 90) - 90;
            

            placeableObjectType tileType = selectedTile.GetComponent<placeableObjectManifest>().objectType;
            if(tileType == placeableObjectType.fullTile)
            {
                setIndicatorLocation(tileCenter, Quaternion.Euler(0, 0, 0));
            } else if(tileType == placeableObjectType.wallTile)
            {
                setIndicatorLocation(tileCenter, Quaternion.Euler(0, wall_rotationAngle, 0));
            } else if(tileType == placeableObjectType.cornerTile)
            {
                setIndicatorLocation(tileCenter, Quaternion.Euler(0, corner_rotationAngle, 0));
            }
            

            // If left mouse button pressed and not hovering over ui element..
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                if(tileType == placeableObjectType.fullTile)
                {
                    float turns = 0f;
                    // Math for if tiles are rotation agnostic
                    if(selectedTile.GetComponent<placeableObjectManifest>().rotationAgnostic)
                    {
                        turns = Mathf.Floor(Random.Range(0, 4)) * 90f;
                    }
                    placeSelectedTile(tileCenter, Quaternion.Euler(0, turns, 0)); ;
                } else if(tileType == placeableObjectType.wallTile)
                {
                    placeSelectedTile(tileCenter, Quaternion.Euler(0, wall_rotationAngle, 0));
                } else if (tileType == placeableObjectType.cornerTile)
                {
                    placeSelectedTile(tileCenter, Quaternion.Euler(0, corner_rotationAngle, 0));
                }
                
            }

        }



        // Deselect tile if escape pressed.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            setSelectedObject(null);
        }

        if(Input.GetKeyDown(KeyCode.PageUp))
        {
            layerVerticalOffset++;
            setPlaneTweened(targetLayer, layerVerticalOffset);
        }

        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            layerVerticalOffset--;
            setPlaneTweened(targetLayer, layerVerticalOffset);
        }

    }

    private void setIndicatorLocation(Vector3 location, Quaternion rotation)
    {
        selector.transform.position = location;
        selector.transform.rotation = rotation;
    }

    private void placeSelectedTile(Vector3 location, Quaternion orientation)
    {
        if (isServer)
        {
            GameObject instantiatedTile = GameObject.Instantiate(selectedTile, location, orientation);
            instantiatedTile.transform.SetParent(targetLayer.transform);
            instantiatedTile.transform.name = instantiatedTile.transform.name.Replace("(Clone)", "").Trim();
            NetworkServer.Spawn(instantiatedTile);
        }
    }
}
