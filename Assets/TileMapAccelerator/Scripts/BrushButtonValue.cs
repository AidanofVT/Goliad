using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapAccelerator.Scripts
{
    public class BrushButtonValue : MonoBehaviour
    {

        public uint Value;

        IsometricInteraction interactionManager;

        public void InitInteractionManager(IsometricInteraction man)
        {
            interactionManager = man;
        }

        public void OnClick()
        {
            interactionManager.SetBrush(Value);
        }
    }
}


