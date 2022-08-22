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
    public Action<NetworkConnection> ServerOnlyFunctionCallback;
    public Action functionCallback;

    public bool canBeRunBy(BRNGPlayerData player)
    {
        return player.permissions >= ActionPermissionLevel;
    }
}

#endregion

#region Nullable Serialization

[System.Serializable]
public class BNullable<T>
{
    public T value;
    public string typeString;
}

public class BNullableInteger : BNullable<int>
{
    public BNullableInteger(int i)
    {
        typeString = typeof(int).FullName;
        value = i;
    }
}

public class BNullableString : BNullable<string>
{
    public BNullableString(string s)
    {
        typeString = typeof(string).FullName;
        value = s;
    }
}



#endregion

#region Interaction Classes
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
        if (dataToEncode.GetType() == typeof(int))
        {
            dataToEncode = new BNullableInteger((int)dataToEncode);
        }

        if (dataToEncode.GetType() == typeof(string))
        {
            dataToEncode = new BNullableString((string)dataToEncode);
        }

        fastAccess.Add(encodeAsKey, JsonUtility.ToJson(dataToEncode));
    }

    public object decode(string key)
    {
        return fastAccess[key];
    }

    public int decodeInt(string key)
    {
        BNullableInteger nInt = JsonUtility.FromJson<BNullableInteger>(fastAccess[key]);
        return nInt.value;
    }

    public string decodeString(string key)
    {
        BNullableString nInt = JsonUtility.FromJson<BNullableString>(fastAccess[key]);
        return nInt.value;
    }

    public T decodeAs<T>(string key)
    {
        return JsonUtility.FromJson<T>(fastAccess[key]);
    }
}

#endregion


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
        newData.ServerOnlyFunctionCallback = (NetworkConnection conn) =>
        {
            object[] parameters = new object[1];
            parameters[0] = conn;
            try
            {
                methodInfo.Invoke(script, parameters);
            } catch (TargetInvocationException err)
            {
                Debug.Log(err.InnerException);
            }
        };
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
    [SerializeField] private GameObject ICLayerChangePrefab;
    [SerializeField] private GameObject ICRuntimeHandlePrefab;
    [SerializeField] private GameObject ICSelectWithScriptPrefab;

    List<Action> InteractionStopFunctions;


    #region ICExecute
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
    #endregion
    public void StopAllInteractions()
    {
        foreach(Action a in InteractionStopFunctions) { a(); }
    }

    #region IC Ground Position
    public IEnumerator ICGroundPosition(Action<Vector3> callback)
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
        return coroutine;
    }
    #endregion

    #region IC Change Layer
    public IEnumerator ICChangeLayer(LayerHandler fromLayer, Action<LayerHandler, Vector3> callback)
    {
        IEnumerator yieldingFunction(LayerHandler from, Action<LayerHandler, Vector3> cb)
        {
            GameObject newPosIndicator = GameObject.Instantiate(ICLayerChangePrefab);
            newPosIndicator.SetActive(false);
            bool runLoop = true;
            while (runLoop)
            {
                Ray ray = localcamera.ScreenPointToRay(Input.mousePosition);

                PhysicsRaycast(99, ray, (hit) =>
                {
                    LayerHandler layerHit = hit.transform.GetComponentInParent<LayerHandler>();
                    if (layerHit && from.index != layerHit.index)
                    {
                        if (newPosIndicator != null)
                        {
                            newPosIndicator.SetActive(true);
                            newPosIndicator.transform.position = hit.point;
                            if (Input.GetMouseButtonDown(0))
                            {
                                
                                Destroy(newPosIndicator);
                                cb(layerHit, hit.point);
                                runLoop = false;
                            }
                        }
                    }
                    else
                    {
                        if (newPosIndicator != null)
                            newPosIndicator.SetActive(false);
                    }

                });
                yield return new WaitForEndOfFrame();
            }
        }
        IEnumerator coroutine = yieldingFunction(fromLayer, callback);
        StartCoroutine(coroutine);

        // For Cleanup
        InteractionStopFunctions.Add(() => { StopCoroutine(coroutine); });
        return coroutine;
    }
    #endregion

    #region IC Runtime Handle
    public IEnumerator ICRuntimeHandle(Transform target, bool tileRestricted)
    {
        IEnumerator yieldingFunction(Transform t, bool TR)
        {
            GameObject newHandle = Instantiate(ICRuntimeHandlePrefab, target.position, target.rotation);
            //newHandle.transform.localScale = Vector3.zero;
            RuntimeHandle h = newHandle.GetComponent<RuntimeHandle>();
            h.target = target;

            if(TR)
            {
                h.ScaleMode = RuntimeHandleMode.None;
                h.TranslationMode = RuntimeHandleMode.XYZ;
                h.RotationMode = RuntimeHandleMode.Y;
                h.TranslationSnapping = 1;
                h.RotationSnapping = 90;
            }


            bool runLoop = true;
            while (runLoop)
            {
                if(Input.GetKey(KeyCode.Escape))
                {
                    Debug.Log("Destroy");
                    Destroy(newHandle);
                    runLoop = false;
                }
                yield return new WaitForEndOfFrame();
            }
        }
        IEnumerator coroutine = yieldingFunction(target, tileRestricted);
        StartCoroutine(coroutine);

        
        // For Cleanup
        InteractionStopFunctions.Add(() => { StopCoroutine(coroutine); });

        return coroutine;
    }
    
    public void ICRuntimeHandle(Transform target)
    {
        ICRuntimeHandle(target, false);
    }
    #endregion

    #region IC Select With Script

    private class ScriptSelectObjectIndicator
    {
        public GameObject indicator;
        public GameObject target;
        public GameObject brightIndicator;
        public GameObject dimIndicator;
    }

    public IEnumerator ICGetObjectWithScript(Type scriptType, Action<GameObject> callback)
    {
        IEnumerator yieldingFunction(Type st, Action<GameObject> cb)
        {
            List<ScriptSelectObjectIndicator> objectIndicators = new List<ScriptSelectObjectIndicator>();
                
            foreach(BRNGScript script in world.GetComponentsInChildren<BRNGScript>())
            {
                if(script.GetType() == scriptType)
                {
                    ScriptSelectObjectIndicator indicatorObject = new ScriptSelectObjectIndicator();
                    GameObject indicator = GameObject.Instantiate(ICSelectWithScriptPrefab);
                    indicator.transform.position = script.gameObject.transform.position;
                    indicatorObject.indicator = indicator;
                    indicatorObject.target = script.gameObject;
                    indicatorObject.brightIndicator = indicator.transform.Find("SelectionBright").gameObject;
                    indicatorObject.dimIndicator = indicator.transform.Find("SelectionDim").gameObject;
                    indicatorObject.brightIndicator.SetActive(false);
                    objectIndicators.Add(indicatorObject);
                }
            }

            bool runLoop = true;
            while (runLoop)
            {
                Ray ray = localcamera.ScreenPointToRay(Input.mousePosition);

                
                PhysicsRaycast(99, ray, (hit) =>
                {
                    placeableObjectManifest hitObj = hit.transform.GetComponent<placeableObjectManifest>();
                    if (hitObj)
                    {
                        bool found = false;
                        foreach(BRNGScript script in hitObj.transform.GetComponents<BRNGScript>())
                        {
                            if(script.GetType() == st)
                            {
                                foreach(ScriptSelectObjectIndicator ssoi in objectIndicators)
                                {
                                    if(ssoi.target == hitObj.gameObject)
                                    {
                                        found = true;
                                        ssoi.dimIndicator.SetActive(false);
                                        ssoi.brightIndicator.SetActive(true);
                                    } else
                                    {
                                        ssoi.dimIndicator.SetActive(true);
                                        ssoi.brightIndicator.SetActive(false);
                                    }
                                }

                                if(Input.GetMouseButton(0))
                                {
                                    runLoop = false;
                                    cb(hitObj.gameObject);
                                }
                            }
                        }

                        if(!found)
                        {
                            foreach (ScriptSelectObjectIndicator ssoi in objectIndicators)
                            {
                                ssoi.dimIndicator.SetActive(true);
                                ssoi.brightIndicator.SetActive(false);
                            }
                        }
                    } else
                    {
                        foreach (ScriptSelectObjectIndicator ssoi in objectIndicators)
                        {
                            ssoi.dimIndicator.SetActive(true);
                            ssoi.brightIndicator.SetActive(false);
                        }
                    }
                });
                
                yield return new WaitForEndOfFrame();
            }

            foreach (ScriptSelectObjectIndicator ssoi in objectIndicators)
            {
                Destroy(ssoi.indicator);
            }

        }
        IEnumerator coroutine = yieldingFunction(scriptType, callback);
        StartCoroutine(coroutine);

        // For Cleanup
        InteractionStopFunctions.Add(() => { StopCoroutine(coroutine); });
        return coroutine;
    }
    #endregion

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
