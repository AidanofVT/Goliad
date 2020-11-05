using System.Collections.Generic;
using UnityEngine;

//POSSIBLY OBSOLETE: I think that the responsibilities of this class are the same as the responsibilities
    // of the Unit-remote and MobileUnit_remote classes.
//independantUnit is intended for units that can't be commanded, like neutrals and sheep.

public class independentUnit : Unit {

    void Start() {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
    }

}
