using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class InputHandler : MonoBehaviour {

    public Texture2D cursorForGiving;
    public Texture2D cursorForTaking;
    enum commands {neutral, give, take};
    commands commandMode = commands.neutral;

    GameState gameState;
    SelectionRectManager rectManage;
    bool targeting;
    
    void Awake () {
        gameState = GetComponent<GameState>();
        rectManage = GameObject.Find("Player Perspective").transform.GetChild(1).GetComponent<SelectionRectManager>();
    }
    
    void Update() {
        if (Input.GetKeyUp(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1)) {
            if (rectManage.rectOn == false) {
                Collider2D[] detectedThings = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                thingLeftClicked(detectedThings[0].gameObject);
            }
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            commandMode = commands.neutral;
        }
        if (Input.GetKeyUp(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Mouse0) && targeting == false) {
            if (rectManage.rectOn == false) {
                Collider2D[] detectedThings = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                thingRightClicked(detectedThings[0].gameObject);
            }

        }
        // if (Input.GetKeyDown(KeyCode.Mouse1)) {
        //     GameObject underMouse = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0].gameObject;
        //     if (underMouse.tag == "unit" && underMouse.GetComponent<Unit_local>() != null) {
        //         Cohort inQuestion = underMouse.GetComponent<Unit_local>().cohort;
        //         if (inQuestion.members.Count > 1 || gameState.activeCohorts.Contains(inQuestion) == false) {
        //             StartCoroutine(holdTarget(underMouse));
        //         }
        //     }
        // }
        if (Input.GetButtonUp("take") && !Input.GetButtonUp("give")) {
            Cursor.SetCursor(cursorForTaking, Vector2.zero, CursorMode.Auto);
            commandMode = commands.take;
        }
        if (Input.GetButtonUp("give") && !Input.GetButtonUp("take")) {
            Cursor.SetCursor(cursorForGiving, Vector2.zero, CursorMode.Auto);
            commandMode = commands.give;
        }
    }

    // void testThing () {
    // }

    public void thingRightClicked (GameObject thingClicked) {
        if (gameState.activeUnits.Count > 0) {
            Cohort newCohort = gameState.combineActiveCohorts();
            switch (thingClicked.tag) {
                case "unit":
                    if (thingClicked.name == "Icon") {
                        thingClicked = thingClicked.transform.parent.gameObject;
                    }
                    if (thingClicked.GetComponent<PhotonView>().OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber) {                    
                        newCohort.commenceAttack(thingClicked);
                    }
                    else {
                        newCohort.MoveCohort(thingClicked.transform.position, thingClicked);
                    }
                    break;
                case "ground":
                    Vector2 waypoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    newCohort.MoveCohort(waypoint, null);
                    break;
                case "UI":
                    return;
                default: 
                    Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                    break;
            }
        }
    }

    void thingLeftClicked (GameObject thingClicked) {
        switch (thingClicked.tag) {
            case "unit":
                if (thingClicked.name == "Icon") {
                    thingClicked = thingClicked.transform.parent.gameObject;
                }
                Unit_local unit = thingClicked.GetComponent<Unit_local>();
                if (unit != null && thingClicked.name.Contains("sheep") == false) {
                    if (commandMode == commands.neutral) {
                        if (Input.GetButton("modifier") == true) {
                            if (gameState.activeUnits.Contains(unit)) {
                                unit.deactivate();
                            }
                            else {
                                unit.soloCohort.activate();
                            }
                        }
                        else {
                            gameState.clearActive();
                            unit.cohort.activate();
                        }
                    }
                    else {
                        Cohort newCohort = gameState.combineActiveCohorts();
                        if (commandMode == commands.take) {
                            newCohort.commenceTransact(new Task(null, Task.actions.take, Vector2.zero, unit.gameObject));
                        }
                        else if (commandMode == commands.give) {
                            newCohort.commenceTransact(new Task(null, Task.actions.give, Vector2.zero, unit.gameObject));
                        }
                    }
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
                newCohort.commenceTransact(new Task(newCohort.members[0].gameObject,Task.actions.give, Vector2.zero, contact.transform.parent.parent.parent.gameObject));
            }
            else {
                newCohort.commenceTransact(new Task(newCohort.members[0].gameObject,Task.actions.take, Vector2.zero, contact.transform.parent.parent.parent.gameObject));
            }
        }
    }

}