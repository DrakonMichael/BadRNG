using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class playerInputController : MonoBehaviour
{

    /**
     * playerInputController - This script is meant to control and gate the flow of inputs.
     * Contains methods to request the usage of player keyboard and mouse inputs.
     * 
     * This script assumes that every script and service written respects the input control feature.
     */

    private struct inputConsumer
    {
        public Component consumer;
        public bool exclusive;
        public inputConsumer(Component c, bool e) { consumer = c; exclusive = e; }
    }

    private List<inputConsumer> keyboardInputConsumers;
    private List<inputConsumer> mouseInputConsumers;

    private void Start()
    {
        keyboardInputConsumers = new List<inputConsumer>();
        mouseInputConsumers = new List<inputConsumer>();
    }

    /**
     * Returns a boolean representing if a given list of input consumers includes an exclusive element.
     * @param consumerList The list of input consumers to check
     * @return whether this list has an exlusive input consumer.
     */
    private bool existsExclusiveConsumer(List<inputConsumer> consumerList)
    {
        foreach(inputConsumer i in consumerList)
        {
            if(i.exclusive == true)
            {
                return true;
            }
        }
        return false;
    }

    /**
     * Returns a boolean representing if a given list of input consumers includes a specific consumer.
     * @param consumerList The list of input consumers to check
     * @param consumer The component to check against
     * @return whether this list has that consumer
     */
    private bool hasConsumer(List<inputConsumer> consumerList, Component consumer)
    {
        foreach(inputConsumer c in consumerList)
        {
            if(c.consumer == consumer)
            {
                return true;
            }
        }
        return false;
    }

    /**
     * Returns a boolean representing whether the current script should accept mouse input.
     * @param consumer The input consumer to register
     * @param exclusive Whether this input consumer should be exclusive (Stop other input consumers from using this input type.
     * @return Whether this consumer should use the input.
     */
    public bool requestMouseInput(Component consumer, bool exclusive)
    {
        if(hasConsumer(mouseInputConsumers, consumer)) { return true; }
        if(!existsExclusiveConsumer(mouseInputConsumers))
        {
            if (exclusive)
            {
                mouseInputConsumers = new List<inputConsumer>();
            }
            mouseInputConsumers.Add(new inputConsumer(consumer, exclusive));
            return true;
        } else
        {
            return false;
        }
    }

    /**
     * Returns a boolean representing whether the current script should accept keyboard input.
     * @param consumer The input consumer to register
     * @param exclusive Whether this input consumer should be exclusive (Stop other input consumers from using this input type.
     * @return Whether this consumer should use the input.
     */
    public bool requestKeyboardInput(Component consumer, bool exclusive)
    {
        if (hasConsumer(keyboardInputConsumers, consumer)) { return true; }
        if (!existsExclusiveConsumer(keyboardInputConsumers))
        {
            if (exclusive)
            {
                keyboardInputConsumers = new List<inputConsumer>();
            }
            keyboardInputConsumers.Add(new inputConsumer(consumer, exclusive));
            return true;
        }
        else
        {
            return false;
        }
    }

    /**
     * Releases mouse input for this consumer
     * @param consumer The input consumer to remove
     */
    public void releaseMouseInput(Component consumer)
    {
        mouseInputConsumers.RemoveAll(inputConsumer => inputConsumer.consumer == consumer);
    }

    /**
     * Releases keyboard input for this consumer
     * @param consumer The input consumer to remove
     */
    public void releaseKeyboardInput(Component consumer)
    {
        keyboardInputConsumers.RemoveAll(inputConsumer => inputConsumer.consumer == consumer);
    }

    
}
