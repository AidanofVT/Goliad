using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickHandler : MonoBehaviour {
    GameState gameState;
    public GameObject goliad;

    void Awake () {
        gameState = goliad.GetComponent<GameState>();
    }
    
    public void thingRightClicked (GameObject thingClicked) {
        Vector3 destination;
        Debug.Log("thingRightClicked called by " + thingClicked.tag);
        switch (thingClicked.tag) {
            case "unit":
                destination = thingClicked.transform.position;
                break;
            case "ground":
                destination = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                break;
            default: 
                destination = new Vector3(0,0,0);
                break;
        }
        foreach (GameObject unit in gameState.getActiveUnits()) {
            unit.GetComponent<Unit>().move(destination);
            Debug.Log("Called unit.move"); 
        }
    }
}