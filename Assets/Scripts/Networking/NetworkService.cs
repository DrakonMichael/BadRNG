using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkService : NetworkManager
{
    public LayerHandler layerPrefab;

    public override void OnClientConnect()
    {
        GameObject[] creatorAssets = Resources.LoadAll<GameObject>("CreatorAssets"); // Get all creator assets

        foreach (GameObject prefab in creatorAssets)
        {
            if (prefab.TryGetComponent(out NetworkIdentity identity))
            {
                NetworkClient.RegisterPrefab(prefab);
            }
        }

        GameObject[] engineAssets = Resources.LoadAll<GameObject>("Engine Resources"); // Get all creator assets

        foreach (GameObject prefab in engineAssets)
        {
            if (prefab.TryGetComponent(out NetworkIdentity identity))
            {
                NetworkClient.RegisterPrefab(prefab);
            }
        }

        NetworkClient.RegisterPrefab(layerPrefab.gameObject);

        base.OnClientConnect();
    }



}
