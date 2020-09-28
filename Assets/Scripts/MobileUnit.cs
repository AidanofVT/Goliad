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
        Debug.Log("Called mover"); 
    }

    private void Update() {
        Vector2Int placeNow = new Vector2Int((int) transform.position.x, (int) transform.position.y);
        int offset = gameState.map.GetLength(0) / 2;
        if (gameState.map[placeNow.x + offset, placeNow.y + offset] >= 1) {
            Goliad.GetComponent<MapManager>().exploitPatch(placeNow);
        }
    }

}