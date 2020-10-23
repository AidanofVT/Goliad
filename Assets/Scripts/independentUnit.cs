using System.Collections.Generic;
using UnityEngine;

//independantUnit is intended for units that can't be commanded, like neutrals and sheep.

public class independentUnit : Unit {

    void Start() {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
    }

    public override void activate () {
    }

    public override void deactivate () {
    }

    public override bool addMeat (int toAdd) {
        if (meat + toAdd < maxMeat) {
            meat += toAdd;
            //transform.localScale.Set(transform.localScale.x + 0.5f, transform.localScale.y + 0.5f, 1);
            transform.localScale = new Vector3(transform.localScale.x + 0.2f, transform.localScale.y + 0.2f, 1);
            Debug.Log("Meat added");
            return true;
        }
        return false;
    }

}
