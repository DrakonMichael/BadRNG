using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HideOnClient : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(!isServer)
        {
            this.gameObject.SetActive(false);
        }
    }
}
