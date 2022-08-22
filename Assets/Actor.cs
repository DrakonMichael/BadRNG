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

    [BRNGServerExecutable]
    [InteractionSetPermission(PermissionLevel.Player)]
    public void ChangeLayerServer(InteractionServerData data)
    {
        int layerIndex = data.decodeInt("index");
        worldManager world = GameObject.FindGameObjectWithTag("World").GetComponent<worldManager>();
        if (world.getLayer(layerIndex) != null)
        {
            Vector3 moveTo = data.decodeAs<Vector3>("position");
            transform.position = moveTo;
            this.transform.SetParent(world.getLayer(layerIndex).transform);
            RpcPropogateChangeLayer(world.getLayer(layerIndex).gameObject, moveTo);

            GameObject utility = GameObject.FindGameObjectWithTag("Interaction Service");
            PlayerService ps = utility.GetComponent<PlayerService>();

            world.hideAllLayersBut(ps.getPlayerByConnID(ownerID), world.getLayer(layerIndex));
            
        }
    }

    [ClientRpc]
    private void RpcPropogateChangeLayer(GameObject layer, Vector3 pos)
    {
        transform.position = pos;
        this.transform.SetParent(layer.transform);
    }

    [BRNGInteraction]
    [InteractionSetPermission(PermissionLevel.Moderator)]
    public void ChangeLayer()
    {
        InteractionServerData data = new InteractionServerData();
        interactionService.ICChangeLayer(this.transform.GetComponentInParent<LayerHandler>(), (LayerHandler layer, Vector3 pos) =>
        {
            data.encode(layer.index, "index");
            data.encode(pos, "position");
            interactionService.ICExecute(this.gameObject, nameof(ChangeLayerServer), data);
        }
        );
    }

    [BRNGInteraction]
    [InteractionSetPermission(PermissionLevel.Administrator)]
    [Server]
    public void Delete(NetworkConnection client)
    {
        Destroy(this.gameObject);
    }
}
