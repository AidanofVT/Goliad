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
                //Debug.Log(Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition)).Length);
                testThing();
                thingLeftClicked(Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0].gameObject);
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse1)) {
            //Debug.Log(Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition)).Length);
            thingRightClicked(Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0].gameObject);
        }
    }

    void testThing () {
        float xRoll = Random.Range(-1.0f, 1.0f);
        float yRoll = Random.Range(-1.0f, 1.0f);
        List <Vector2Int> returnedList = gameState.tileRaycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), new Vector2(xRoll,yRoll), 10);
        Debug.Log("Slope: " + xRoll + "," + yRoll);
        foreach (Vector2Int toChange in returnedList) {
            goliad.GetComponent<MapManager>().exploitPatch(toChange);
        }
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
            case "UI":
                return;
            default: 
                destination = new Vector3(0,0,0);
                Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                break;
        }
        foreach (GameObject unit in gameState.getActiveUnits()) {
            if (unit.GetComponent<MobileUnit>() != null) {
                unit.GetComponent<MobileUnit>().move(destination, optionalTransform);
                Debug.Log("Called unit.move");
            }
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
            case "UI":
                return;
            default:
                Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                break;
        }
    }
}