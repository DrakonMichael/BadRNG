using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorModeController : MonoBehaviour
{
    List<EditorModeButton> buttons = new List<EditorModeButton>();
    EditorModeButton activeMode = null;
    List<Action<string>> callbacks = new List<Action<string>>();

    private void Start()
    {
        foreach(Transform child in transform)
        {
            EditorModeButton modeButton = child.GetComponent<EditorModeButton>();
            if(modeButton)
            {
                buttons.Add(modeButton);
            }
        }
    }

    public void setActiveMode(EditorModeButton mode)
    {
        activeMode = mode;
        foreach(Action<string> cb in callbacks)
        {
            cb(getActiveMode());
        }
    }

    public string getActiveMode()
    {
        if(activeMode != null)
        {
            return activeMode.modeID;
        }
        return "None";
    }

    public void addModeEventListener(Action<string> cb)
    {
        callbacks.Add(cb);
    }

    public void deactivateOtherModes()
    {
        setActiveMode(null);
        foreach(EditorModeButton button in buttons)
        {
            button.forceDeactivate();
        }
    }

}
