using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileUnit : Unit {
    AidansMovementScript moveConductor;

    void Start () {
        Goliad = GameObject.Find("Goliad");
        gameState = Goliad.GetComponent<GameState>();
        moveConductor = gameObject.GetComponent<AidansMovementScript>();
        gameState.enlivenUnit(gameObject);
        gameObject.transform.hasChanged = false;
    }
    
    public void move (Vector3 target, Transform movingTransform = null) {
        moveConductor.setDestination(target, movingTransform);
    }

}