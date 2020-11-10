using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ClickHandler : MonoBehaviour {
    GameState gameState;
    public GameObject goliad;
    
    void Awake () {
        gameState = goliad.GetComponent<GameState>();
    }
    
    void Update() {
        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            if (gameObject.GetComponent<SelectionRectManager>().rectOn == false) {
                thingLeftClicked(Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0].gameObject);
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse1)) {
            thingRightClicked(Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0].gameObject);
        }
    }

    // void testThing () {
    // }

    public void thingRightClicked (GameObject thingClicked) {
        Vector3 destination;
        Transform optionalTransform = null;
        switch (thingClicked.tag) {
            case "unit":
                if (thingClicked.GetComponent<PhotonView>().OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber) {
                    Debug.Log("clickhandler");
                    List<Unit_local> members = new List<Unit_local>();
                    foreach (GameObject gOb in gameState.getActiveUnits()) {
                        members.Add(gOb.GetComponent<Unit_local>());
                    }
                    Cohort newCohort = new Cohort(members);
                    newCohort.commenceAttack(thingClicked);
                    return;
                }
                else {
                    destination = thingClicked.transform.position;
                    optionalTransform = thingClicked.transform;
                }
                break;
            case "ground":
                destination = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                break;
            case "UI":
                return;
            default: 
                destination = new Vector3(0,0,0);
                Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                break;
        }
        foreach (GameObject unit in gameState.getActiveUnits()) {
            if (unit.GetComponent<MobileUnit_local>() != null) {
                unit.GetComponent<MobileUnit_local>().move(destination, optionalTransform);
            }
        }
    }

    void thingLeftClicked (GameObject thingClicked) {
        switch (thingClicked.tag) {
            case "unit":
                gameState.clearActive();
                if (thingClicked.GetComponent<Unit_local>()) {
                    thingClicked.GetComponent<Unit_local>().activate();
                }
                break;
            case "ground":
                gameState.clearActive();
                break;
            case "UI":
                break;
            case "out of bounds":
                break;
            default:
                Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                break;
        }
    }
}