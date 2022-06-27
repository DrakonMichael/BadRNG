using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class PlayerNetworkData : NetworkBehaviour
{
    public PermissionLevel permissions;
    public int ID;
    public string displayName;

    private void Start()
    {
        if(isServer)
        {
            GameObject utility = GameObject.FindGameObjectWithTag("Interaction Service");
            utility.GetComponent<PermissionsService>().registerPlayer(this);
            utility.GetComponent<InteractionService>().SpawnActor(this);
        }
    }
}
