using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class placementHandler : NetworkBehaviour
{
    public GameObject[] tiles;
    public int[] tileType;
    public GameObject selector;

    private Plane gridPlane;
    private int selected = 0;

    private void Start()
    {
        gridPlane = new Plane(Vector3.up, 0);
    }

    private void Update()
    {

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float enter = 0.0f;
        Vector3 newVector = new Vector3(0,0,0);

        if (gridPlane.Raycast(ray, out enter))
        {

            Vector3 hitLocation = ray.GetPoint(enter);
            newVector = new Vector3(Mathf.Ceil(hitLocation.x) - 0.5f, 0.1f, Mathf.Ceil(hitLocation.z) - 0.5f);
            selector.transform.position = newVector;

            Vector3 cornerVector = hitLocation - newVector;
            float angle = Mathf.Round(Mathf.Atan2(cornerVector.x, cornerVector.z)/(Mathf.PI / 2)) * 90;


            if (Input.GetMouseButtonDown(0))
            {
                if(tileType[selected] == 0)
                {
                    placeSelectedTile(newVector, Quaternion.Euler(0, 0, 0));
                } else
                {
                    placeSelectedTile(newVector, Quaternion.Euler(0, angle-90, 0));
                }
                
            }

        }


        if (Input.GetKeyDown(KeyCode.R))
        {
            selected++;
            selected = selected % tiles.Length;
        }
    }

    private void placeSelectedTile(Vector3 location, Quaternion orientation)
    {
        if (isServer)
        {
            GameObject instantiatedTile = GameObject.Instantiate(tiles[selected], location, orientation);
            NetworkServer.Spawn(instantiatedTile);
        }
    }
}
