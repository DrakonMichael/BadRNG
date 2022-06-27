using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum PermissionLevel
{
    Host,
    Administrator,
    Moderator,
    Player
}

public class PermissionsService : NetworkBehaviour
{

    public Dictionary<int, NetworkConnection> connectionMap;

    private void Start()
    {
        connectionMap = new Dictionary<int, NetworkConnection>();
    }

    public void registerPlayer(PlayerNetworkData player)
    {
        int newPlayerID = connectionMap.Count;
        player.ID = newPlayerID;
        player.displayName = "User " + newPlayerID;
        player.permissions = PermissionLevel.Administrator;
        connectionMap.Add(newPlayerID, player.transform.GetComponent<NetworkIdentity>().connectionToClient);
    }
}
