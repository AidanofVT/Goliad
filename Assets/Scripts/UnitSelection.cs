using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSelection : MonoBehaviour {
    public GameState gameState;
    float downTime = 1000000;
    Vector2 mousePosLastFrame;
    Vector2 mouseDownLocation;
    public Transform quadrangle;
    Transform selectorSquare = null;

    void Awake() {
        gameState = gameObject.GetComponent<GameState>();
        quadrangle.gameObject.SetActive(false);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            downTime = Time.time;
            mouseDownLocation = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0)) {
            downTime = 1000000;
            selectorSquare.gameObject.SetActive(false);
            selectorSquare = null;
        }
        else if (Time.time - downTime >= 0.1) {
            if (selectorSquare == null) {
                selectorSquare = quadrangle;
                selectorSquare.gameObject.SetActive(true);
                selectorSquare.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
                //selectorSquare.position += new Vector3(0, 0, 1);
            }
            else {
                // selectorSquare.localScale += new Vector3 (xDelta, yDelta, 0);
                Vector2 rectSize = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - mouseDownLocation;
                //"I would think that this would output something in screen terms, but apparently not. Maybe investigate why?"
                selectorSquare.gameObject.GetComponent<SpriteRenderer>().size = rectSize;
                Debug.Log("Input.mousePosition, adjusted: " + (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) + "\n Input.mousePosition" + Input.mousePosition + "\n mouseDownLocation: " + mouseDownLocation + "\n rectSize: "
                 + rectSize + "\n selectorSquare.localScale: " + selectorSquare.localScale);
                //selectorSquare.gameObject.GetComponent<SpriteRenderer>().size = rectSize;
                //Vector3 changeMag = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.ScreenToWorldPoint(mouseDownLocation);
                selectorSquare.position = mouseDownLocation + (rectSize/ 2);
                //selectorSquare.position += new Vector3(0, 0, 1);
            }
            mousePosLastFrame = Input.mousePosition;
        }
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

    public void clearActiveUnits() {
        gameState.clearActive();
    }
}
