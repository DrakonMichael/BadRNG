using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class BRNGInteraction : Attribute
{
    /** This attribute marks a public method of a BRNGScript as something which can be interacted with. Attribute parameters signify certain rendering parameters of the method
     * in the interaction dropdown (Such as whether to render it).
     */
    public PermissionLevel permissionLevel;

    public BRNGInteraction(PermissionLevel perm_level)
    {
        permissionLevel = perm_level;
    }
}

public class InteractionService : NetworkBehaviour
{
    public Actor playerObjectPrefab;
    public worldManager world;


    #region Actors

    public void SpawnActor(PlayerNetworkData playerData)
    {
        if (!isServer) { return; }
        GameObject newPlayer = GameObject.Instantiate(playerObjectPrefab.gameObject);
        newPlayer.GetComponent<Actor>().setOwnerID(playerData.ID);
        newPlayer.transform.SetParent(world.getDefaultLayer().transform);
        newPlayer.transform.position = new Vector3(0, 0, 0);
        NetworkServer.Spawn(newPlayer);
    }

    #endregion

    #region raycasts
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
            if(req.priority > maxPriority) { maxPriority = req.priority; }
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

    private void Start()
    {
        SetupRaycastInteractions();
    }

    private void LateUpdate()
    {
        HandleRaycastInteractions();
    }
}
