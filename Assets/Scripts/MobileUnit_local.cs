using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MobileUnit_local : Unit_local {
    protected AidansMovementScript moveConductor;

    void Awake() {
        stats = GetComponent<UnitBlueprint>();
    }

    public override void ignition () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        moveConductor = GetComponent<AidansMovementScript>();
    }

//if network traffic is an issue in the future, and CPU load isn't too bad, maybe we could put these in MobileUnit_Local too and slow down the photon update rate?
    public virtual void move (Vector3 target, Transform movingTransform = null) {
        moveConductor.setDestination(target, movingTransform);
    }

    public virtual void stop () {
        moveConductor.terminatePathfinding();
    }

}