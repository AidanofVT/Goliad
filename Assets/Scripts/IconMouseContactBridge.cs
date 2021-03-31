using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconMouseContactBridge : MonoBehaviour {
    
    Unit_local thisUnit;

    void Start () {
        thisUnit = transform.parent.GetComponent<Unit_local>();
    }

    void OnMouseEnter() {
        thisUnit.OnMouseEnter();
    }

    void OnMouseExit() {
        thisUnit.OnMouseExit();
    }

}
