using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MobileUnit_remote : Unit_remote {
    AidansMovementScript moveConductor;

    public override void ignition () {
        moveConductor = GetComponent<AidansMovementScript>();
        statusBar.GetComponent<SpriteRenderer>().enabled = false;
    }

    public virtual void move (Vector3 target, Transform movingTransform = null) {
        moveConductor.setDestination(target, movingTransform);
    }

    public virtual void stop () {
        moveConductor.terminatePathfinding();
    }
}