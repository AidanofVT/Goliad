using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class TriggerEventController : MonoBehaviour
    {

        public bool IsTriggered { get => isTriggered; }

        private bool isTriggered;
        private short colliderCount = 0;

        public bool canExec = false;

        public ExecOnTrigger triggerFunction;

        public void ForceReset()
        {
            isTriggered = false;
            colliderCount = 0;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            //Used to only execute when the center collider is triggered otherwise trigger action can happen twice
            if(canExec)
                triggerFunction();

            colliderCount++;
            isTriggered = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            colliderCount--;
            isTriggered = colliderCount > 0;
        }

    }
}


