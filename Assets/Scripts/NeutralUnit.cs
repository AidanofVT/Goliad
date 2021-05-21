using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeutralUnit : MobileUnit_local {

    void Awake () {
        if (photonView.IsMine == false) {
            gameObject.AddComponent<MobileUnit_remote>();
            DestroyImmediate(this);
        }
        else {
            stats = GetComponent<UnitBlueprint>();
        }
    }

    public override void Ignition () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        moveConductor = GetComponent<AidansMovementScript>();
    }

}
