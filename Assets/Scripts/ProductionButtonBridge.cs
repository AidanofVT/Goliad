using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProductionButtonBridge : MonoBehaviour {

    CohortUIManager management;
    Button thisButton;

    void Start () {
        management = transform.parent.parent.parent.GetComponent<CohortUIManager>();
        thisButton = GetComponent<Button>();
    }

    void OnMouseEnter() {
        management.FocusButton(thisButton);
    }

    void OnMouseExit () {
        management.FocusButton(null);
    }
    
}
