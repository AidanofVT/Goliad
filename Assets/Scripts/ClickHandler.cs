using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickHandler : MonoBehaviour {
    GameState gameState;
    public GameObject goliad;

    void Awake () {
        gameState = goliad.GetComponent<GameState>();
    }
    
    void Update() {
        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            if (gameObject.GetComponent<SelectionRectManager>().rectOn == false) {
                thingLeftClicked(probeUnderMouse().collider.gameObject);
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse1)) {
            thingRightClicked(probeUnderMouse().collider.gameObject);
        }
    }

    RaycastHit probeUnderMouse () {
        RaycastHit hit;
        Debug.Log("Casting ray from " + Camera.main.ScreenToWorldPoint(Input.mousePosition));
        Physics.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), new Vector3(0,0,1), out hit);
        return hit;
    }

    public void thingRightClicked (GameObject thingClicked) {
        Vector3 destination;
        Transform optionalTransform = null;
        switch (thingClicked.tag) {
            case "unit":
                destination = thingClicked.transform.position;
                optionalTransform = thingClicked.transform;
                break;
            case "ground":
                destination = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                break;
            default: 
                destination = new Vector3(0,0,0);
                break;
        }
        foreach (GameObject unit in gameState.getActiveUnits()) {
            unit.GetComponent<Unit>().move(destination, optionalTransform);
            Debug.Log("Called unit.move"); 
        }
    }

    void thingLeftClicked (GameObject thingClicked) {
        switch (thingClicked.tag) {
            case "unit":
                gameState.clearActive();
                thingClicked.GetComponent<Unit>().activate();
                break;
            case "ground":
                gameState.clearActive();
                break;
            default:
                Debug.Log("PROBLEM: neither unit nor ground hit by raycast.");
                break;
        }
    }
}