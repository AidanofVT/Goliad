using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    AidansMovementScript moveConductor;
    public GameObject Goliad;
    GameState gameState;
    UnitSelection selectionManager;
    public GameObject blueCircle;
    public Hashtable subordinateUIElements = new Hashtable();

    void Start() {
        gameState = Goliad.GetComponent<GameState>();
        selectionManager = Goliad.GetComponent<UnitSelection>();
        moveConductor = gameObject.GetComponent<AidansMovementScript>();
    }

    public void move (Vector3 target) {
        moveConductor.setDestination(target);
        Debug.Log("Called mover"); 
    }

    void OnMouseOver() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            Debug.Log("Click Registered.");
            gameState.addActiveUnit(gameObject);
            highlight();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1)) {
            Debug.Log("Right clicked."); 
            selectionManager.thingRightClicked(gameObject);
            Debug.Log("Called Driver");
        }  
    }

    public void highlight () {
        GameObject highlightCircle = Instantiate(blueCircle, transform.position, Quaternion.identity);
        highlightCircle.transform.parent = gameObject.transform;
        subordinateUIElements.Add("highlightCircle", highlightCircle);
        Debug.Log("End of highlight.");
    }
}
