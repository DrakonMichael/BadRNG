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
    public GameObject alignmentGrid;
    public GameObject runtimeHandle;
    public EditorModeController modeController;



    public InteractionService interactionService;

    [Space]
    [Header("Grid values")]
    [SerializeField] private float tweenTime = 0.2f;

    private int alignmentGridScale;
    private int alignmentGridHeightChange;
    [SerializeField]
    private float alignmentGridLineWidthMultiplier = 0.005f;
    private bool alignmentGridLock;

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
    private List<GameObject> runtimeHandles;

    [Space]
    [Header("Network Editing")]
    [SerializeField]
    private float tilePositionUpdateRate;
    private float tilePosUpdateSecondsElapsed;

    

    private void Start()
    {
        if(!isServer) { return; }
        reloadObjectList();
        gridPlane = new Plane(Vector3.up, 0);
        runtimeHandles = new List<GameObject>();

        modeController.addModeEventListener((string mode) => { 
            if(mode != "placement")
            {
                setSelectedObject(null);
                clearFocusTile();
            }
        });
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

    public void resetGridPosition()
    {
        if (!isServer) { return; }
        placementGrid.transform.position = targetLayer.transform.position;
        setPlane(targetLayer, layerVerticalOffset);
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

    [ClientRpc]
    private void RpcSetNewObjectTransform(GameObject networkedPlaceableObject, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        networkedPlaceableObject.transform.position = pos;
        networkedPlaceableObject.transform.rotation = rot;
        networkedPlaceableObject.transform.localScale = scale;
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

        if (runtimeHandles.Count != 0)
        {
            foreach (GameObject o in runtimeHandles)
            {
                RuntimeHandle handle = o.GetComponent<RuntimeHandle>();
                RpcSetNewObjectTransform(handle.target.gameObject, handle.target.position, handle.target.rotation, handle.target.localScale);

                Destroy(o);
            }
        }

        focusSelectorVisual = new List<GameObject>();
        runtimeHandles = new List<GameObject>();
    }


    private void focusTile(GameObject tile)
    {
        clearFocusTile();
        focusedTile = tile;

        GameObject newHandle = Instantiate(runtimeHandle, tile.transform.position, tile.transform.rotation);
        newHandle.transform.localScale = Vector3.zero;
        newHandle.GetComponent<RuntimeHandle>().target = tile.transform;
        runtimeHandles.Add(newHandle);

        if(tile.GetComponent<placeableObjectManifest>().objectType != placeableObjectType.prop)
        {
            RuntimeHandle h = newHandle.GetComponent<RuntimeHandle>();
            h.ScaleMode = RuntimeHandleMode.None;
            h.TranslationMode = RuntimeHandleMode.XYZ;
            h.RotationMode = RuntimeHandleMode.Y;
            h.TranslationSnapping = 1;
            h.RotationSnapping = 90;
        }

        foreach (Renderer r in focusedTile.GetComponentsInChildren<Renderer>())
        {
            GameObject newVisual = Instantiate(r.gameObject, r.transform.position, r.transform.rotation);
            focusSelectorVisual.Add(newVisual);
            newVisual.GetComponent<Renderer>().material = selectionMaterial;
            newVisual.transform.SetParent(tile.transform);
        }
    }

    void handleObjectSelection()
    {
        // Deselect tile if escape pressed.
        if (focusedTile && Input.GetKeyDown(KeyCode.Escape))
        {
            clearFocusTile();
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && selectedTile == null)
        {
            Ray selectionRay = placementCamera.ScreenPointToRay(Input.mousePosition);
            interactionService.PhysicsRaycast(1, selectionRay, (hit) => {
                if (hit.transform.GetComponentInChildren<placeableObjectManifest>())
                {
                    focusTile(hit.transform.gameObject);
                }
            });
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

        tilePosUpdateSecondsElapsed += Time.deltaTime;
        if (tilePosUpdateSecondsElapsed > tilePositionUpdateRate)
        {
            foreach (GameObject o in runtimeHandles)
            {
                RuntimeHandle handle = o.GetComponent<RuntimeHandle>();
                if(handle.target.gameObject != null)
                {
                    RpcSetNewObjectTransform(handle.target.gameObject, handle.target.position, handle.target.rotation, handle.target.localScale);
                }
                
            }
            tilePosUpdateSecondsElapsed = 0;
        }
        

        if(modeController.getActiveMode() != "placement") { return; }

        if(!placementCamera)
        {
            placementCamera = Camera.main;
        }

        if(!targetLayer)
        {
            targetLayer = targetWorld.getDefaultLayer();
        }

        resetGridPosition();

        // If LeftAlt is pressed, handle creating the alignment grid.
        if(Input.GetKeyDown(KeyCode.LeftAlt))
        {
            if (!alignmentGridLock)
            {
                alignmentGrid.SetActive(true);
                alignmentGrid.transform.position = placementGrid.transform.position;
                alignmentGridScale = 1;
                alignmentGridHeightChange = 0;
            }

            alignmentGridLock = false;
            alignmentGrid.GetComponent<Renderer>().material.SetColor("_LineColor", new Color(1f, 1f, 0f, 0f));
        }

        if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            if(!alignmentGridLock)
            {
                alignmentGrid.SetActive(false);
                foreach (GameObject handle in runtimeHandles)
                {
                    handle.GetComponent<RuntimeHandle>().TranslationSnapping = 0f;
                }
            }
        }

        if (Input.GetKey(KeyCode.LeftAlt))
        {
            if(Input.mouseScrollDelta.y > 0)
            {
                alignmentGridScale++;
                alignmentGrid.transform.position = placementGrid.transform.position + new Vector3(0, (float)alignmentGridHeightChange / (float)alignmentGridScale, 0);
            } else if(Input.mouseScrollDelta.y < 0)
            {
                alignmentGridScale--;
                if(alignmentGridScale < 1)
                {
                    alignmentGridScale = 1;
                }
                alignmentGrid.transform.position = placementGrid.transform.position + new Vector3(0, (float)alignmentGridHeightChange / (float)alignmentGridScale, 0);
            }

            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                alignmentGridHeightChange++;
                alignmentGrid.transform.DOMove(placementGrid.transform.position + new Vector3(0, (float)alignmentGridHeightChange / (float)alignmentGridScale, 0), tweenTime);
            }
                

            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                alignmentGridHeightChange--;
                alignmentGrid.transform.DOMove(placementGrid.transform.position + new Vector3(0, (float)alignmentGridHeightChange / (float)alignmentGridScale, 0), tweenTime);
            }

            alignmentGrid.GetComponent<Renderer>().material.SetFloat("_GridXYSize", 1000 * alignmentGridScale);
            alignmentGrid.GetComponent<Renderer>().material.SetFloat("_LineWidth", alignmentGridLineWidthMultiplier * alignmentGridScale);

            if(Input.GetKeyDown(KeyCode.S))
            {
                alignmentGridLock = true;
                alignmentGrid.GetComponent<Renderer>().material.SetColor("_LineColor", new Color(0f, 1f, 0f, 0f));
            }

            foreach(GameObject handle in runtimeHandles)
            {
                handle.GetComponent<RuntimeHandle>().TranslationSnapping = 1f / (float)alignmentGridScale;
            }

            return;
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
            } else if(tileType == placeableObjectType.prop)
            {
                setIndicatorLocation(hitLocation, Quaternion.Euler(0, 0, 0));
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
                } else if (tileType == placeableObjectType.prop)
                {
                    placeSelectedTile(hitLocation, Quaternion.Euler(0, 0, 0)); ;
                }
                
            }

        }


        // Deselect tile if escape pressed.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            setSelectedObject(null);
        }



        if (Input.GetKeyDown(KeyCode.PageUp))
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
