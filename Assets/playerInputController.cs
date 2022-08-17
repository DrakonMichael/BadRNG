using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;



public class playerInputController : NetworkBehaviour
{
    Camera localCamera;
    InteractionService interactionService;


    public void TryServerInteraction(BRNGInteractionData interaction)
    {
        
        CmdTryInteraction(interaction.InteractionTarget, interaction.InteractionGroup, interaction.InteractionName);
    }

    [Command]
    private void CmdTryInteraction(GameObject target, string interactionGroup, string interactionName)
    {

        GameObject utility = GameObject.FindGameObjectWithTag("Interaction Service");
        List<BRNGInteractionData> interactions = utility.GetComponent<InteractionService>().GenerateInteractionData(target);

        foreach(BRNGInteractionData interaction in interactions)
        {
            if(interaction.InteractionGroup == interactionGroup && interaction.InteractionName == interactionName)
            {
                // This is the right interaction, verify it can be executed.
                BRNGPlayer serverPlayer = utility.GetComponent<PlayerService>().getPlayerByConnection(connectionToClient);
                if(serverPlayer.playerData.permissions >= interaction.ActionPermissionLevel)
                {
                    interaction.functionCallback();
                }

                break;
            }
        }


    }

    public void TryServerExecution(string executionName, InteractionServerData passData) {
        passData.serialize();
        CmdTryExecution(executionName, passData);
    }

    [Command]
    private void CmdTryExecution(string executionName, InteractionServerData passData)
    {
        passData.deserialize();

        GameObject utility = GameObject.FindGameObjectWithTag("Interaction Service");
        List<BRNGExecutionData> functions = utility.GetComponent<InteractionService>().GenerateExecutableData(passData.target);

        foreach (BRNGExecutionData interaction in functions)
        {
            if (interaction.ExecutionName == executionName)
            {
                // This is the right interaction, verify it can be executed.
                BRNGPlayer serverPlayer = utility.GetComponent<PlayerService>().getPlayerByConnection(connectionToClient);
                if (serverPlayer.playerData.permissions >= interaction.ExecutionPermissionLevel)
                {
                    interaction.functionCallback(passData);
                }

                break;
            }
        }


    }
}
