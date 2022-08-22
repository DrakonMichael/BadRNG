using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Portal : BRNGScript
{
    [SerializeField] [SyncVar] public string DestinationID = "";
    [System.NonSerialized] public PositionMarker2D Destination;

    [BRNGServerExecutable]
    [InteractionSetPermission(PermissionLevel.Administrator)]
    public void LinkServer(InteractionServerData data)
    {
        string destID = data.decodeString("destinationID");
        DestinationID = destID;
        Destination = GetScriptByUUID<PositionMarker2D>(destID);
    }

    [BRNGInteraction]
    [InteractionSetPermission(PermissionLevel.Administrator)]
    public void Link()
    {
        InteractionServerData data = new InteractionServerData();
        interactionService.ICGetObjectWithScript(typeof(PositionMarker2D), (GameObject target) => {
            PositionMarker2D marker = target.GetComponent<PositionMarker2D>();
            data.encode(marker.GetUUID(), "destinationID");
            interactionService.ICExecute(this.gameObject, nameof(LinkServer), data);
        });
    }

    [ClientRpc]
    private void RpcPropogateChangeLayer(GameObject actor, GameObject layer, Vector3 pos)
    {
        actor.transform.position = pos;
        actor.transform.SetParent(layer.transform);
    }

    [BRNGInteraction]
    [InteractionSetPermission(PermissionLevel.Player)]
    [Server]
    public void Use(NetworkConnection client)
    {
        if (DestinationID != "")
        {
            Destination = GetScriptByUUID<PositionMarker2D>(DestinationID);
        }

        BRNGPlayer plr = playerService.getPlayerByConnection(client);
        foreach(Actor actor in world.GetComponentsInChildren<Actor>())
        {
            if(actor.getOwnerID() == plr.playerData.connectionID)
            {
                actor.gameObject.transform.position = Destination.transform.position;
                if (actor.GetCurrentLayer().index != Destination.GetCurrentLayer().index)
                {
                    actor.transform.SetParent(Destination.GetCurrentLayer().transform);
                    RpcPropogateChangeLayer(actor.gameObject, Destination.GetCurrentLayer().gameObject, Destination.transform.position);
                }
                
                world.hideAllLayersBut(plr, Destination.GetCurrentLayer());
            }
        }
    }
}
