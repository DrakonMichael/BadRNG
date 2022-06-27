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
}
