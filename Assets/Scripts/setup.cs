using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class setup : MonoBehaviour {
    public GameState gameState;

    void Awake() {
        gameState = gameObject.GetComponent<GameState>();
//10 = ground, 5 = UI, 8 = obstacles, 11 = units
        Physics2D.IgnoreLayerCollision(10, 11);
        Physics2D.IgnoreLayerCollision(10, 5);
        Physics2D.IgnoreLayerCollision(10, 8);
        Physics2D.IgnoreLayerCollision(5, 8);
        Physics2D.IgnoreLayerCollision(5, 11);
        Physics2D.IgnoreLayerCollision(11, 8);
        Physics2D.queriesHitTriggers = false;
    }

}
