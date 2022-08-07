using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Reflection;

#region Interaction Attributes

[AttributeUsage(AttributeTargets.Method)]
public class BRNGServerExecutable : Attribute
{
    // This attribute marks that a method is executable using ICExecute (To pass data from client to server within the interaction continuation system)
}

[AttributeUsage(AttributeTargets.Method)]
public class BRNGInteraction : Attribute
{
    // This attribute marks that a method is an interaction, and will thus appear in context menus.
}

[AttributeUsage(AttributeTargets.Method)]
public class InteractionSetPermission : Attribute
{
    /** This attribute sets the permission neccesary to run an interaction. */
    public PermissionLevel permissionLevel;

    public InteractionSetPermission(PermissionLevel perm_level)
    {
        permissionLevel = perm_level;
    }
}

public class BRNGInteractionData
{
    public string InteractionName;
    public string InteractionGroup;
    public GameObject InteractionTarget;
    public PermissionLevel ActionPermissionLevel;
    public bool isServerOnly;

    public Action interactionExecutionFunction;
    public Action functionCallback;

    public bool canBeRunBy(BRNGPlayerData player)
    {
        return player.permissions >= ActionPermissionLevel;
    }
}

#endregion


public class InteractionServerData
{

}

public class InteractionService : NetworkBehaviour
{
    public worldManager world;
    public ContextMenuSpawner contextMenuLocation;
    private Camera localcamera;

    #region Raycasts
    private class RaycastRequest
    {
        public int priority;
        public Action<RaycastHit> raycastAction;
        public RaycastHit hit;
        public RaycastRequest(int prio, Action<RaycastHit> action, RaycastHit h)
        {
            priority = prio;
            raycastAction = action;
            hit = h;
        }
    }
    private List<RaycastRequest> raycastRequests;

    public void PhysicsRaycast(int priority, Ray ray, float range, int layerMask, Action<RaycastHit> callback)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, range, layerMask)) {
            raycastRequests.Add(new RaycastRequest(priority, callback, hit));
        }
    }

    public void PhysicsRaycast(int priority, Ray ray, Action<RaycastHit> callback)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            raycastRequests.Add(new RaycastRequest(priority, callback, hit));
        }
    }

    private void HandleRaycastInteractions()
    {
        int maxPriority = 0;
        foreach (RaycastRequest req in raycastRequests)
        {
            if (req.priority > maxPriority) { maxPriority = req.priority; }
        }

        foreach (RaycastRequest req in raycastRequests)
        {
            if (req.priority >= maxPriority) { req.raycastAction(req.hit); }
        }
        SetupRaycastInteractions();
    }

    private void SetupRaycastInteractions()
    {
        raycastRequests = new List<RaycastRequest>();
    }

    #endregion

    #region Managing Interaction via Right Click



    private BRNGInteractionData assignInteractionData(BRNGScript script, MethodInfo methodInfo)
    {
        BRNGInteractionData newData = new BRNGInteractionData();

        // Get player input controller in case an action needs to be wrapped in a server action call.
        playerInputController inputController = transform.GetComponent<PlayerService>().getLocalPlayer().GetComponent<playerInputController>();

        newData.InteractionTarget = script.gameObject;
        newData.InteractionName = methodInfo.Name;
        newData.InteractionGroup = script.GetType().ToString();

        newData.functionCallback = () => { methodInfo.Invoke(script, new object[0]); };
        newData.interactionExecutionFunction = newData.functionCallback;
        newData.ActionPermissionLevel = PermissionLevel.Host;

        ServerAttribute serverOnlyAttribute = methodInfo.GetCustomAttribute<ServerAttribute>();
        if (serverOnlyAttribute != null)
        {
            newData.interactionExecutionFunction = () => { inputController.TryServerInteraction(newData); };
            newData.isServerOnly = true;
        } else
        {
            newData.isServerOnly = false;
        }

        InteractionSetPermission setPermissionAttribute = methodInfo.GetCustomAttribute<InteractionSetPermission>();
        if (setPermissionAttribute != null)
        {
            newData.ActionPermissionLevel = setPermissionAttribute.permissionLevel;
        }

        return newData;
    }

    public List<BRNGInteractionData> GenerateInteractionData(GameObject obj)
    {
        List<BRNGInteractionData> interactionDataList = new List<BRNGInteractionData>();
        BRNGScript[] scripts = obj.transform.GetComponentsInChildren<BRNGScript>();
        if (scripts.Length > 0)
        {
            foreach (BRNGScript script in scripts)
            {
                Type scriptType = script.GetType();
                foreach (MethodInfo minfo in scriptType.GetMethods())
                {
                    if (minfo.GetCustomAttribute<BRNGInteraction>() != null)
                    {
                        interactionDataList.Add(assignInteractionData(script, minfo));
                    }

                }
            }
        }
        return interactionDataList;
    }

    private void HandleInteractionInteractions()
    {
        Ray ray = localcamera.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButtonDown(1))
        {
            PhysicsRaycast(3, ray, (hit) =>
            {
                List<BRNGInteractionData> interactionDataList = GenerateInteractionData(hit.transform.gameObject);
                if (interactionDataList.Count > 0)
                {
                    GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer");
                    localPlayer.GetComponent<PlayerNetworkData>().getCurrentPlayerWithCallback((BRNGPlayerData plrData) =>
                    {
                        contextMenuLocation.SpawnContextMenu(hit.transform.gameObject.name, interactionDataList, plrData, hit.point);
                        foreach (BRNGInteractionData idata in interactionDataList)
                        {

                            if (idata.canBeRunBy(plrData))
                            {
                                Debug.Log(idata.InteractionName);
                            }
                        }
                    });
                }
            });
        }

    }

    #endregion

    #region Interaction Continuation
    [Space]
    [Header("Interaction Continuation")]
    [SerializeField] private GameObject ICWalkablePrefab;

    List<Action> InteractionStopFunctions;




    public void ICExecute(string methodName, object param1)
    {

        MethodBase callingMethod = (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod();
        BRNGInteraction interactionTag = callingMethod.GetCustomAttribute<BRNGInteraction>();
        if(interactionTag == null)
        {
            Debug.LogError("ICExecute cannot be called from non-interaction methods. (Method does not have BRNGInteraction tag).");
            return;
        }



    }

    public void StopAllInteractions()
    {
        foreach(Action a in InteractionStopFunctions) { a(); }
    }

    public void ICGroundPosition(Action<Vector3> callback)
    {
        IEnumerator yieldingFunction(Action<Vector3> cb)
        {
            GameObject newPosIndicator = GameObject.Instantiate(ICWalkablePrefab);
            newPosIndicator.SetActive(false);
            bool runLoop = true;
            while (runLoop)
            {
                Ray ray = localcamera.ScreenPointToRay(Input.mousePosition);
                
                PhysicsRaycast(99, ray, (hit) =>
                {
                    placeableObjectManifest hitObj = hit.transform.GetComponent<placeableObjectManifest>();
                    if(hitObj && hitObj.objectType == placeableObjectType.fullTile)
                    {
                        newPosIndicator.SetActive(true);
                        newPosIndicator.transform.position = hit.point;
                        if (Input.GetMouseButtonDown(0))
                        {
                            Destroy(newPosIndicator);
                            cb(hit.point);
                            runLoop = false;
                        }
                    } else
                    {
                        newPosIndicator.SetActive(false);
                    }

                });
                yield return new WaitForEndOfFrame();
            }
        }
        IEnumerator coroutine = yieldingFunction(callback);
        StartCoroutine(coroutine);

        // For Cleanup
        InteractionStopFunctions.Add(() => { StopCoroutine(coroutine); });
    }

    #endregion

    private void Start()
    {
        localcamera = Camera.main;
        InteractionStopFunctions = new List<Action>();
        SetupRaycastInteractions();
    }

    private void Update()
    {
        HandleInteractionInteractions();
    }

    private void LateUpdate()
    {
        HandleRaycastInteractions();
    }
}
