using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    AidansMovementScript moveConductor;
    public GameObject Goliad;
    GameState gameState;
    ClickHandler clickManager;
    public GameObject blueCircle;
    public Hashtable subordinateUIElements = new Hashtable();

    void Start() {
        gameState = Goliad.GetComponent<GameState>();
        clickManager = Goliad.GetComponent<ClickHandler>();
        moveConductor = gameObject.GetComponent<AidansMovementScript>();
        gameState.enlivenUnit(gameObject);
    }

    public void move (Vector3 target) {
        moveConductor.setDestination(target);
        Debug.Log("Called mover"); 
    }

    void OnMouseOver() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            Debug.Log("Click Registered.");
            activate();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1)) {
            Debug.Log("Right clicked."); 
            clickManager.thingRightClicked(gameObject);
        }  
    }

    public void activate () {
        gameState.activateUnit(gameObject);
        GameObject highlightCircle = Instantiate(blueCircle, transform.position, Quaternion.identity);
        highlightCircle.transform.parent = gameObject.transform;
        subordinateUIElements.Add("highlightCircle", highlightCircle);
        Debug.Log("End of highlight.");
    }

    public void deactivate () {
        gameState.deactivateUnit(gameObject);
        Destroy((GameObject)subordinateUIElements["highlightCircle"]);
        subordinateUIElements.Remove("highlightCircle");
    }
}
