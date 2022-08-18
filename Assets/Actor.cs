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
    [InteractionSetPermission(PermissionLevel.Player)]
    public void MoveServer(InteractionServerData data)
    {
        Vector3 moveTo = data.decodeAs<Vector3>("position");
        transform.position = moveTo;
    }

    [BRNGInteraction]
    [InteractionSetPermission(PermissionLevel.Player)]
    public void Move()
    {
        InteractionServerData data = new InteractionServerData();
        interactionService.ICGroundPosition((Vector3 posToMoveTo) =>
        {
            data.encode(posToMoveTo, "position");

            interactionService.ICExecute(this.gameObject, nameof(MoveServer), data);
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
