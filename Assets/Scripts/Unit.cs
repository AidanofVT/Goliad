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
        gameObject.transform.hasChanged = false;
    }

    public void move (Vector3 target, Transform movingTransform = null) {
        moveConductor.setDestination(target, movingTransform);
        Debug.Log("Called mover"); 
    }

    public void activate () {
        gameState.activateUnit(gameObject);
        GameObject highlightCircle = Instantiate(blueCircle, transform.position, Quaternion.identity);
        highlightCircle.transform.parent = gameObject.transform;
        subordinateUIElements.Add("highlightCircle", highlightCircle);
        Debug.Log("Unit activated.");
    }

    public void deactivate () {
        gameState.deactivateUnit(gameObject);
        Destroy((GameObject)subordinateUIElements["highlightCircle"]);
        subordinateUIElements.Remove("highlightCircle");
    }
}
