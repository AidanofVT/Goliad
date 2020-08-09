using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionRectManager : MonoBehaviour {
    GameState gameState;
    public GameObject goliad;
    float downTime = 1000000;
    Vector2 mousePosLastFrame;
    Vector2 mouseDownLocation;
    public Transform quadrangle;
    Transform selectorSquare;

    void Awake() {
        gameState = goliad.GetComponent<GameState>();
        quadrangle.gameObject.SetActive(false);
        selectorSquare = quadrangle;
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            downTime = Time.time;
            mouseDownLocation = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            if (selectorSquare.gameObject.activeInHierarchy == true) {
                activateRegion();
            }
            downTime = 1000000;
            selectorSquare.gameObject.SetActive(false);
 
        }
        else if (Time.time - downTime >= 0.15) {
            if (selectorSquare.gameObject.activeInHierarchy == false) {
                selectorSquare.gameObject.SetActive(true);
                selectorSquare.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            Vector2 rectSize = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - mouseDownLocation;
            selectorSquare.gameObject.GetComponent<SpriteRenderer>().size = rectSize;
            //Debug.Log("Input.mousePosition, adjusted: " + (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) + "\n Input.mousePosition" + Input.mousePosition + "\n mouseDownLocation: " + mouseDownLocation + "\n rectSize: "
            // + rectSize + "\n selectorSquare.localScale: " + selectorSquare.localScale);
            selectorSquare.position = new Vector3 (mouseDownLocation.x + (rectSize.x/ 2), mouseDownLocation.y + (rectSize.y/ 2), -1.0f);
            mousePosLastFrame = Input.mousePosition;
        }
    }

    void activateRegion () {
        List<GameObject> toActivate = new List<GameObject>();
        float topExtreme = selectorSquare.position.y + selectorSquare.gameObject.GetComponent<SpriteRenderer>().bounds.size.y/ 2;
        float bottomExtreme = selectorSquare.position.y - selectorSquare.gameObject.GetComponent<SpriteRenderer>().bounds.size.y / 2;
        float leftExtreme = selectorSquare.position.x - selectorSquare.gameObject.GetComponent<SpriteRenderer>().bounds.size.x / 2;
        float rightExtreme = selectorSquare.position.x + selectorSquare.gameObject.GetComponent<SpriteRenderer>().bounds.size.x / 2;
        foreach (GameObject maybeInBounds in gameState.getAliveUnits()) {
            Vector3 thePosition = maybeInBounds.transform.position;
            // Debug.Log("Examining the object at " + thePosition + ". " +
            // "\n topExtreme = " + topExtreme +
            // "\n bottomExtreme = " + bottomExtreme +
            // "\n leftExtreme = " + leftExtreme +
            // "\n rightExtreme = " + rightExtreme);
            if (thePosition.y <= topExtreme &&
                thePosition.y >= bottomExtreme &&
                thePosition.x >= leftExtreme &&
                thePosition.x <= rightExtreme) {
                //Debug.Log("Object at " + thePosition + "accepted for activation.");
                toActivate.Add(maybeInBounds);
            }
        }
        if (toActivate.Count > 0) {
            gameState.clearActive();
            foreach (GameObject aboutToBeActivated in toActivate) {
                aboutToBeActivated.GetComponent<Unit>().activate();
            }
        }
        return;
    }

}
