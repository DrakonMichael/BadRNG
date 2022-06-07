using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class placementHandler : NetworkBehaviour
{
    public GameObject tile;



    private void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            Debug.Log("piss and shit");
        }
    }

    private void placeSelectedTile(Vector3 location)
    {
        if (isServer)
        {
            GameObject instantiatedTile = GameObject.Instantiate(tile, location, Quaternion.Euler(0, 0, 0));
            NetworkServer.Spawn(instantiatedTile);
        }
    }
}
