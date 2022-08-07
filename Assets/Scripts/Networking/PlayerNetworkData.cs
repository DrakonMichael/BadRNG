using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

// Script to setup network data for a player.
public class PlayerNetworkData : NetworkBehaviour
{
    private BRNGPlayerData playerData;
    private List<Action<BRNGPlayerData>> playerDataCallbacks;

    [Command]
    private void CmdSetupPlayer()
    {
        GameObject utility = GameObject.FindGameObjectWithTag("Interaction Service");
        BRNGPlayer p = utility.GetComponent<PlayerService>().registerPlayer(this, connectionToClient);
        TargetSetPlayerData(connectionToClient, p.playerData);
    }

    private void Start()
    {
        playerDataCallbacks = new List<Action<BRNGPlayerData>>();
        if (isLocalPlayer)
        {
            CmdSetupPlayer();
        }
    }

    
    [TargetRpc]
     public void TargetSetPlayerData(NetworkConnection target, BRNGPlayerData data)
    {
        playerData = data;
        foreach(Action<BRNGPlayerData> callback in playerDataCallbacks)
        {
            callback(data);
        }

    }

    [Command]
    private void CmdRequestPlayerData()
    {
        PlayerService pservice = GameObject.FindGameObjectWithTag("Interaction Service").GetComponent<PlayerService>();
        TargetSetPlayerData(connectionToClient, pservice.getPlayerByConnection(connectionToClient).playerData);
    }

    [Client]
    public bool isOwner()
    {
        return isLocalPlayer;
    }



    [Client]
    public void getCurrentPlayerWithCallback(Action<BRNGPlayerData> callback)
    {
        if(isLocalPlayer)
        {
            CmdRequestPlayerData();
            playerDataCallbacks.Add(callback);
        }
    }


}
