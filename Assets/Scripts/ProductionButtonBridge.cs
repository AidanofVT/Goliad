using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProductionButtonBridge : MonoBehaviour {

    CohortUIManager management;
    public int productionCost;
    Button thisButton;

    void Start () {
        management = transform.parent.parent.parent.GetComponent<CohortUIManager>();
        string unitName = name.Remove(name.IndexOf(" "));
        productionCost = ((GameObject) Resources.Load("Units/" + unitName)).GetComponent<UnitBlueprint>().costToBuild;
        thisButton = GetComponent<Button>();
    }

    void OnMouseEnter() {
        management.focusButton(thisButton);
    }

    void OnMouseExit () {
        management.focusButton(null);
    }
    
}
