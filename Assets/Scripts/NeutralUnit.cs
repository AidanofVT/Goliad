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

    public override void ignition () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        moveConductor = GetComponent<AidansMovementScript>();
    }

    public override void activate () {

    }

    public override void deactivate () {

    }

    public void OnMouseEnter () {

    }

    public void OnMouseExit () {
        
    }

    public void OnTriggerEnter2D(Collider2D other) {
        
    }

    public override void pathEnded () {

    }

}
