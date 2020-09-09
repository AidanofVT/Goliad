using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//                                            OBSOLETE: DELETE WHEN EVERYTHING WORKS WITHOUT IT
public class GroundScript : MonoBehaviour {
    ClickHandler clickHandler;
    public GameObject mouseHijinx;
    GameState gameState;
    public GameObject goliad;

    private void Start() {
        clickHandler = mouseHijinx.GetComponent<ClickHandler>();
        gameState = goliad.GetComponent<GameState>();
    }

    //TODO: Swap this so that the outer criteria is the mouse button being depressed. This probably will need two methods.
    private void OnMouseOver() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            gameState.clearActive();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1)) {
            clickHandler.thingRightClicked(gameObject);
        }  
    }
}
