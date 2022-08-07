using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BRNGPlayer
{
    public BRNGPlayerData playerData;
    public NetworkConnection connectionToClient;
}

[System.Serializable]
public class BRNGPlayerData
{
    public string username;
    public int uid;
    public int connectionID;
    public PermissionLevel permissions;
}

public enum PermissionLevel
{
    Host = 3,
    Administrator = 2,
    Moderator = 1,
    Player = 0
}

public class PlayerService : NetworkBehaviour
{
    public List<BRNGPlayer> players;
    public Actor playerObjectPrefab;
    public worldManager placementWorld;

    private int lastConnID = 0;

    #region player data
    public BRNGPlayer getPlayerByUsername(string username)
    {
        foreach(BRNGPlayer p in players)
        {
            if (p.playerData.username == username) { return p; }
        }
        return null;
    }

    public BRNGPlayer getPlayerByUID(int uid)
    {
        foreach (BRNGPlayer p in players)
        {
            if (p.playerData.uid == uid) { return p; }
        }
        return null;
    }

    public BRNGPlayer getPlayerByConnection(NetworkConnection conn)
    {
        foreach (BRNGPlayer p in players)
        {
            if (p.connectionToClient == conn) { return p; }
        }
        return null;
    }
    
    [Client]
    public PlayerNetworkData getLocalPlayer()
    {
        return GameObject.FindGameObjectWithTag("LocalPlayer").GetComponent<PlayerNetworkData>();
    }

    [Server]
    public BRNGPlayer registerPlayer(PlayerNetworkData networkData, NetworkConnection conn)
    {
        BRNGPlayer plr = new BRNGPlayer();
        plr.playerData = new BRNGPlayerData();
        plr.playerData.username = "PLACEHOLDER";
        plr.playerData.permissions = PermissionLevel.Player;
        // If the user is the first one to join they must be the host.
        if (lastConnID == 0)
        {
            plr.playerData.permissions = PermissionLevel.Host;
        }

        
        plr.playerData.uid = lastConnID;
        plr.playerData.connectionID = lastConnID;
        lastConnID++;
        plr.connectionToClient = conn;

        players.Add(plr);

        // We might have different behaviours for this later, but for now, let's just spawn the player's related actor.
        SpawnActor(plr);

        return plr;
    }

    #endregion

    #region Actors

    [Server]
    public void SpawnActor(BRNGPlayer player)
    {
        GameObject newPlayer = GameObject.Instantiate(playerObjectPrefab.gameObject);
        newPlayer.GetComponent<Actor>().setOwnerID(player.playerData.connectionID);
        newPlayer.transform.SetParent(placementWorld.getDefaultLayer().transform);
        newPlayer.transform.position = new Vector3(0, 0, 0);
        NetworkServer.Spawn(newPlayer);
    }

    #endregion

    private void Start()
    {
        players = new List<BRNGPlayer>();
    }
}
