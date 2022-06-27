using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

// Script to handle player-based model interaction.
public class Actor : BRNGScript
{
    [SerializeField] private int ownerID;


    private void updateOwnershipIndicator()
    {
        ownershipIndicator = transform.Find("ENG_ownershipIndicator").gameObject;

        if (ownerID == 0)
        {
            ownershipIndicator.SetActive(true);
        }
        else
        {
            ownershipIndicator.SetActive(false);
        }
    }

    public int getOwnerID()
    {
        return ownerID;
    }

    public void setOwnerID(int newID)
    {
        ownerID = newID;
        updateOwnershipIndicator();
    }

    private GameObject ownershipIndicator;
    private void Start()
    {
        updateOwnershipIndicator();
    } 

    [BRNGInteraction(PermissionLevel.Player)]
    public void Move()
    {

    }
}
