using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ClickHandler : MonoBehaviour {
    GameState gameState;
    ViewManager vManage;
    public GameObject goliad;
    bool targeting;
    
    void Awake () {
        gameState = goliad.GetComponent<GameState>();
            vManage = transform.parent.GetComponent<ViewManager>();
    }
    
    void Update() {
        if (Input.GetKeyUp(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1)) {
            if (gameObject.GetComponent<SelectionRectManager>().rectOn == false) {
                Collider2D[] detectedThings = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                thingLeftClicked(detectedThings[0].gameObject);
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Mouse0) && targeting == false) {
            if (gameObject.GetComponent<SelectionRectManager>().rectOn == false) {
                Collider2D[] detectedThings = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                thingRightClicked(detectedThings[0].gameObject);
            }
        }
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            GameObject underMouse = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0].gameObject;
            if (underMouse.tag == "unit" && underMouse.GetComponent<Unit_local>() != null) {
                Cohort inQuestion = underMouse.GetComponent<Unit_local>().cohort;
                if (inQuestion.members.Count > 1 || gameState.activeCohorts.Contains(inQuestion) == false) {
                    StartCoroutine(holdTarget(underMouse));
                }
            }
        }
    }

    // void testThing () {
    // }

    public void thingRightClicked (GameObject thingClicked) {
        Cohort newCohort = gameState.combineActiveCohorts();
        switch (thingClicked.tag) {
            case "unit":
                if (thingClicked.GetComponent<PhotonView>().OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber) {                    
                    newCohort.commenceAttack(thingClicked);
                }
                else {
                    newCohort.moveCohort(thingClicked);
                }
                break;
            case "ground":
                GameObject waypoint = new GameObject("waypoint");
                waypoint.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                newCohort.moveCohort(waypoint);
                break;
            case "UI":
                return;
            default: 
                Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                break;
        }
    }

    void thingLeftClicked (GameObject thingClicked) {
        Debug.Log(thingClicked.name);
        switch (thingClicked.tag) {
            case "unit":
                Unit_local unit = thingClicked.GetComponent<Unit_local>();
                if (Input.GetButton("excluder") == false) {
                    gameState.clearActive();
                    if (unit != null) {
                        gameState.activeCohorts.Clear();
                        vManage.clearPalette();
                        if (Input.GetButton("modifier")) {
                            unit.changeCohort();
                        }
                        unit.cohort.activate();
                    }
                }
                else if (unit.gameObject.transform.GetChild(3).gameObject.activeInHierarchy == true) {
                    unit.cohort.removeMember(unit);
                    unit.deactivate();
                }
                break;
            case "ground":
                gameState.clearActive();
                gameState.activeCohorts.Clear();
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

    IEnumerator holdTarget (GameObject target) {
        float timeDown = Time.time;
        GameObject targetingUI = target.transform.GetChild(2).GetChild(1).gameObject;
        while (true) {
            if (Time.time - timeDown < 0.1f) {
                if (Input.GetKeyUp(KeyCode.Mouse1)) {
                    break;
                }
            }
            else {
                if (targetingUI.activeInHierarchy == false) {
                    targeting = true;
                    targetingUI.SetActive(true);
                    vManage.attendTo(target);
                }
                if (Input.GetKeyUp(KeyCode.Mouse1)) {
                    m1UpButtonPress(target, targetingUI);
                    break;
                }
            }
            yield return new WaitForSeconds(0);
        }
        targeting = false;
        targetingUI.SetActive(false);
        vManage.attendToNoMore(target);
        StopCoroutine("holdTarget");
        yield return null;
    }

    void m1UpButtonPress (GameObject unit, GameObject buttonContainer) {
        Collider2D contact = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0];
        BoxCollider2D [] buttonColliders = buttonContainer.GetComponentsInChildren<BoxCollider2D>();
        if (contact.gameObject.name.Contains("Button")) {
            Cohort newCohort = gameState.combineActiveCohorts();
            // string toprint = "acting on a cohort composed of ";
            // foreach (Unit_local member in newCohort.members) {
            //     toprint += (member.gameObject.name + ", ");
            // }
            // Debug.Log(toprint);
            if (contact.gameObject.name == "Button--Give") {
                newCohort.commenceTransact(new Task(newCohort.members[0].gameObject, contact.transform.parent.parent.parent.gameObject, Task.actions.give));
            }
            else {
                newCohort.commenceTransact(new Task(newCohort.members[0].gameObject, contact.transform.parent.parent.parent.gameObject, Task.actions.take));
            }
        }
    }

}