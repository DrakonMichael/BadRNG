using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

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
public class InteractionDeferExecution : Attribute
{
    // This attribute marks that a method should be deferred in execution.
    // By default, all executions are non-deferred, which means the client which called the method sees the change immediately by calling the execution method themselves.
    // Adding this tag makes sure the client recieves state updates about the interaction only from the server.
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

public class BRNGExecutionData
{
    public string ExecutionName;
    public PermissionLevel ExecutionPermissionLevel;
    public Action<InteractionServerData> functionCallback;
    public bool canBeRunBy(BRNGPlayerData player)
    {
        return player.permissions >= ExecutionPermissionLevel;
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

[System.Serializable]
public class InteractionServerData
{
    public GameObject target;
    Type callingType;
    MethodBase callingMethod;

    public string[] keys;
    public string[] values;

    [System.NonSerialized]
    Dictionary<string, string> fastAccess;

    public InteractionServerData()
    {
        MethodBase method = (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod();

        // Second boolean statement stops this from running on the server.
        if (method.GetCustomAttribute<BRNGInteraction>() == null && method.DeclaringType.Name != "GeneratedNetworkCode")
        {
            Debug.LogWarning("InteractionServerData constructed from non-interaction. This will cause breaking bugs if called to ICExecute. (Called from " + method.Name + " in " + method.DeclaringType.Name + ")");
        }

        callingMethod = method;
        callingType = method.DeclaringType;

        keys = new string[0];
        values = new string[0];

        fastAccess = new Dictionary<string, string>();
    }

    public void serialize()
    {
        int numberOfEntries = fastAccess.Count;
        keys = new string[numberOfEntries];
        values = new string[numberOfEntries];

        int i = 0;
        foreach (KeyValuePair<string, string> entry in fastAccess)
        {
            keys[i] = entry.Key;
            values[i] = entry.Value;
            i++;
        }
    }

    public void deserialize()
    {
        fastAccess = new Dictionary<string, string>();
        for (int i = 0; i < keys.Length; i++)
        {
            fastAccess.Add(keys[i], values[i]);
        }
    }

    public MethodBase getCallingMethod()
    {
        return callingMethod;
    }

    public Type getCallingType()
    {
        return callingType;
    }

    public void encode(object dataToEncode, string encodeAsKey)
    {
        fastAccess.Add(encodeAsKey, JsonUtility.ToJson(dataToEncode));
    }

    public object decode(string key)
    {
        return fastAccess[key];
    }

    public T decodeAs<T>(string key)
    {
        return JsonUtility.FromJson<T>(fastAccess[key]);
    }
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

    private BRNGExecutionData assignExecutionData(BRNGScript script, MethodInfo methodInfo)
    {
        BRNGExecutionData newData = new BRNGExecutionData();

        // Get player input controller in case an action needs to be wrapped in a server action call.
        playerInputController inputController = transform.GetComponent<PlayerService>().getLocalPlayer().GetComponent<playerInputController>();

        newData.ExecutionName = methodInfo.Name;
        newData.functionCallback = (InteractionServerData exeData) => {
            object[] parameters = new object[1];
            parameters[0] = exeData;
            try
            {
                methodInfo.Invoke(script, parameters);
            } catch (TargetInvocationException e)
            {
                Debug.LogError("Method invocation failed for " + methodInfo.Name + ". (" + e.InnerException + ")");
            }
        };

        newData.ExecutionPermissionLevel = PermissionLevel.Host;
        InteractionSetPermission setPermissionAttribute = methodInfo.GetCustomAttribute<InteractionSetPermission>();
        if (setPermissionAttribute != null)
        {
            newData.ExecutionPermissionLevel = setPermissionAttribute.permissionLevel;
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

    [Server]
    public List<BRNGExecutionData> GenerateExecutableData(GameObject obj)
    {
        List<BRNGExecutionData> interactionDataList = new List<BRNGExecutionData>();
        BRNGScript[] scripts = obj.transform.GetComponentsInChildren<BRNGScript>();
        if (scripts.Length > 0)
        {
            foreach (BRNGScript script in scripts)
            {
                Type scriptType = script.GetType();
                foreach (MethodInfo minfo in scriptType.GetMethods())
                {
                    if (minfo.GetCustomAttribute<BRNGServerExecutable>() != null)
                    {
                        BRNGExecutionData newData = new BRNGExecutionData();
                        newData.ExecutionName = minfo.Name;
                        
                        interactionDataList.Add(assignExecutionData(script, minfo));
                    }

                }
            }
        }
        return interactionDataList;
    }

    [Client]
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

                    BRNGPlayerData plrData = localPlayer.GetComponent<PlayerNetworkData>().getStalePlayerData();
                    contextMenuLocation.SpawnContextMenu(hit.transform.gameObject.name, interactionDataList, plrData, hit.point);
                    
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



    public void ICExecute(GameObject target, string methodName, InteractionServerData ServerData)
    {
        ServerData.target = target;
        MethodBase callingMethod = ServerData.getCallingMethod();
        Type methodClass = ServerData.getCallingType();

        bool callerFound = false;
        bool targetFound = false;

        MethodInfo callerMethod = null;
        MethodInfo targetMethod = null;
        

        foreach (MethodInfo mInfo in methodClass.GetMethods())
        {
            
            if(mInfo.Name == callingMethod.Name)
            {
                if (mInfo.GetCustomAttribute<BRNGInteraction>() == null)
                {
                    Debug.LogError("ICExecute cannot be called from non-interaction methods. (Method does not have BRNGInteraction tag).");
                    return;
                }

                callerMethod = mInfo;
                callerFound = true;
            }

            // target
            if (mInfo.Name == methodName)
            {
                if(mInfo.GetCustomAttribute<BRNGServerExecutable>() == null) {
                    Debug.LogError("ICExecute target must be marked as server executable functions (Target method does not have BRNGServerExecutable tag).");
                    return;
                }
                targetMethod = mInfo;
                targetFound = true;
            }
        }

        if(targetFound && callerFound)
        {
            // Try to execute on the server
            transform.GetComponent<PlayerService>().getLocalPlayer().transform.GetComponent<playerInputController>().TryServerExecution(methodName, ServerData);

            // Non-deferred execution
            if(targetMethod.GetCustomAttribute<InteractionDeferExecution>() == null)
            {
                object[] parameters = new object[1];
                parameters[0] = ServerData;
                targetMethod.Invoke(target.GetComponent(methodClass), parameters);
            }


        } else
        {
            if(!targetFound) { Debug.LogError("No method of name " + callingMethod.Name + " detected in " + methodClass.Name + " (Use nameof() or check spelling of the method)."); }
            if(!callerFound) { Debug.LogError("No method of name " + callingMethod.Name + " detected in " + methodClass.Name + " (Is the method private?)."); }
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
                        if(newPosIndicator != null)
                        {
                            newPosIndicator.SetActive(true);
                            newPosIndicator.transform.position = hit.point;
                            if (Input.GetMouseButtonDown(0))
                            {
                                Destroy(newPosIndicator);
                                cb(hit.point);

                                runLoop = false;
                            }
                        }
                    } else
                    {
                        if (newPosIndicator != null)
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
