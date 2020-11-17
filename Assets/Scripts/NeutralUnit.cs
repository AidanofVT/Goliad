using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeutralUnit : MobileUnit_local {

    void Awake () {
        if (photonView.IsMine == false) {
            gameObject.AddComponent<MobileUnit_remote>();
            DestroyImmediate(this);
        }
        stats = GetComponent<UnitBlueprint>();
    }

    public override void ignition () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        moveConductor = GetComponent<AidansMovementScript>();
    }

    public override void activate () {

    }

    public override void deactivate () {

    }

}
