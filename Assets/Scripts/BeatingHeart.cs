using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatingHeart : MonoBehaviour {
    public GameState gameState;

    void Awake() {
        gameState = gameObject.GetComponent<GameState>();
    }

    void Update()
    {
        
    }
}
