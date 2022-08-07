using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;
using System;

// Script to handle player-based model interaction.
public class Actor : BRNGScript
{
    [SerializeField] [SyncVar] private int ownerID;

    private GameObject ownershipIndicator;

    [System.NonSerialized] private int savedOwnerID = -1;
    [System.NonSerialized] private InteractionService interactionService;


    private void Start()
    {
        ownershipIndicator = transform.Find("ENG_ownershipIndicator").gameObject;
        interactionService = GameObject.FindGameObjectWithTag("Interaction Service").GetComponent<InteractionService>();
    }


    private void updateOwnershipIndicator()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("LocalPlayer");
        if(!playerObject.GetComponent<PlayerNetworkData>().isOwner()) { return; }
        playerObject.GetComponent<PlayerNetworkData>().getCurrentPlayerWithCallback((BRNGPlayerData data) =>
        {
            if (!ownershipIndicator) { return; }
            if (ownerID == data.connectionID)
            {
                ownershipIndicator.GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                ownershipIndicator.GetComponent<MeshRenderer>().enabled = false;
            }
            
        });

        
    }

    public int getOwnerID()
    {
        return ownerID;
    }

    [Server]
    public void setOwnerID(int newID)
    {
        ownerID = newID;
    }


    private void Update()
    {
        if(ownerID != savedOwnerID)
        {
            
            savedOwnerID = ownerID;
        }
    }


    [BRNGServerExecutable]
    public void MoveServer(InteractionServerData data)
    {
        //TBD: somehow pass data to server :/
    }

    [BRNGInteraction]
    [InteractionSetPermission(PermissionLevel.Player)]
    public void Move()
    { 
        interactionService.ICGroundPosition((Vector3 posToMoveTo) =>
        {
            interactionService.ICExecute(nameof(MoveServer), posToMoveTo);
        });
    }

    [BRNGInteraction]
    [InteractionSetPermission(PermissionLevel.Administrator)]
    [Server]
    public void Delete()
    {
        Destroy(this.gameObject);
    }
}
