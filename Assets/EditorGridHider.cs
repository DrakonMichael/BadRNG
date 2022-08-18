using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorGridHider : MonoBehaviour
{
    public EditorModeController editorController;
    public GameObject grid;
    [SerializeField]
    float cameraDistanceToMoveGrid;

    void Start()
    {
        editorController.addModeEventListener((string mode) =>
        {
            if(mode == "placement")
            {
                grid.SetActive(true);
            } else
            {
                grid.SetActive(false);
            }
        });
    }

}
