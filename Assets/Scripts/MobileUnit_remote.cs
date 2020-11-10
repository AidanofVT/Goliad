using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MobileUnit_remote : Unit_remote {
    AidansMovementScript moveConductor;

    void Start () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        transform.GetChild(1).gameObject.SetActive(true);
        statusBar = transform.GetChild(1).GetComponent<BarManager>();
        statusBar.gameObject.GetComponent<SpriteRenderer>().sprite = null;
        stats = GetComponent<UnitBlueprint>();
        moveConductor = GetComponent<AidansMovementScript>();
    }

    public virtual void move (Vector3 target, Transform movingTransform = null) {
        moveConductor.setDestination(target, movingTransform);
    }

    public virtual void stop () {
        moveConductor.terminatePathfinding();
    }
}