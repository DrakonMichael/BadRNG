using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BRNGScript : NetworkBehaviour
{
    [SerializeField] [SyncVar] private string UUID = "";
    [SerializeField] [SyncVar] private List<string> Modifiers;
    [System.NonSerialized] string UUIDcharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    [System.NonSerialized] const int UUIDLength = 16;
    [System.NonSerialized] protected worldManager world;
    [System.NonSerialized] protected InteractionService interactionService;
    [System.NonSerialized] protected PlayerService playerService;

    protected string GenerateUniqueID()
    {
        string id = this.GetType().ToString() + ".";
        for (int i = 0; i < UUIDLength; i++)
        {
            id += UUIDcharacters[Random.Range(0, UUIDcharacters.Length)];
        }

        return id;
    }

    public virtual void onDeserialization() { }

    public LayerHandler GetCurrentLayer()
    {
        return this.transform.GetComponentInParent<LayerHandler>(true);
    }

    public string GetUUID()
    {
        if(UUID == "")
        {
            UUID = GenerateUniqueID();
        }
        return UUID;
    }

    protected T GetScriptByUUID<T>(string UUID) where T : BRNGScript
    {
        T script = world.GetScriptByUUID<T>(UUID);
        return script;
    }

    private void Awake()
    {
        world = GameObject.FindGameObjectWithTag("World").GetComponent<worldManager>();
        interactionService = GameObject.FindGameObjectWithTag("Interaction Service").GetComponent<InteractionService>();
        playerService = GameObject.FindGameObjectWithTag("Interaction Service").GetComponent<PlayerService>();
    }
}
