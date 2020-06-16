using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundScript : MonoBehaviour {
    UnitSelection selectionManager;
    public GameObject goliad;

    private void Start() {
        selectionManager = goliad.GetComponent<UnitSelection>();
    }

    private void OnMouseOver() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            selectionManager.clearActiveUnits();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1)) {
            selectionManager.thingRightClicked(gameObject);
        }  
    }
}
