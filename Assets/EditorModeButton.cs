using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorModeButton : MonoBehaviour
{
    public string modeID;
    public CanvasRenderer ControllingPanel;
    private EditorModeController modeController;
    private bool active = false;

    private Color ActiveColor;
    private Color InactiveColor;

    private void Start()
    {
        InactiveColor = new Color(55f / 255f, 55f / 255f, 55f / 255f, 135f / 255f);
        ActiveColor = new Color(0, 0, 0, 135f / 255f);
        modeController = this.GetComponentInParent<EditorModeController>();
        this.GetComponent<Button>().onClick.AddListener(() =>
        {
            if(active)
            {
                modeController.deactivateOtherModes();
                ControllingPanel.gameObject.SetActive(false);
                this.transform.GetComponent<Image>().color = InactiveColor;
                active = false;
            } else
            {
                modeController.deactivateOtherModes();
                ControllingPanel.gameObject.SetActive(true);
                this.transform.GetComponent<Image>().color = ActiveColor;
                modeController.setActiveMode(this);
                active = true;
            }
        }); 
    }

    public bool isActive()
    {
        return active;
    }

    public void forceDeactivate()
    {
        ControllingPanel.gameObject.SetActive(false);
        this.transform.GetComponent<Image>().color = InactiveColor;
        active = false;
    }

}
