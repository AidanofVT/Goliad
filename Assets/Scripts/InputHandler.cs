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
    LineRenderer targetingCircle;

    List<Unit_local> activeUnits;
    
    void Awake () {
        gameState = GetComponent<GameState>();
        activeUnits = gameState.activeUnits;
        GameObject playerPerspective = GameObject.Find("Player Perspective");
        rectManage = playerPerspective.transform.GetChild(1).GetComponent<SelectionRectManager>();
        targetingCircle = playerPerspective.transform.GetChild(0).GetChild(2).GetChild(0).GetComponent<LineRenderer>();
    }
    
    public Cohort combineActiveUnits (Task.actions intent) {
        Cohort newCohort = new Cohort();
        foreach (Unit_local individual in activeUnits) {
            bool unitIsEligible = false;
            switch (intent) {
                case Task.actions.move:
                    unitIsEligible = individual.stats.isMobile;
                    break;
                case Task.actions.attack:
                    unitIsEligible = individual.stats.isArmed;
                    break;
                case Task.actions.take:
                    unitIsEligible = individual.roomForMeat() > 0;
                    break;
                case Task.actions.give:
                    unitIsEligible = individual.meat > 0;
                    break;
                case Task.actions.build:
                    unitIsEligible = individual.meat > 0;
                    break;
                default:
                    break;
            }
            if (unitIsEligible == true) {
                individual.changeCohort(newCohort);
            }
        }
        return newCohort;
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
        if (Input.GetKeyUp(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Mouse0) && targetingCircle.enabled == false) {
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

    public void thingRightClicked (GameObject thingClicked) {
        if (gameState.activeUnits.Count > 0) {
            switch (thingClicked.tag) {
                case "unit":
                    if (thingClicked.name == "Icon") {
                        thingClicked = thingClicked.transform.parent.gameObject;
                    }
                    if (thingClicked.GetComponent<PhotonView>().OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber) {
                        Cohort attackingCohort = combineActiveUnits(Task.actions.attack);                
                        attackingCohort.commenceAttack(new Task(null, Task.actions.attack, thingClicked.transform.position, thingClicked.GetComponent<Unit>()));
                    }
                    else {
                        Cohort followingCohort = combineActiveUnits(Task.actions.move);
                        followingCohort.MoveCohort(thingClicked.transform.position, thingClicked.GetComponent<Unit_local>());
                    }
                    break;
                case "ground":
                    Vector2 waypoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Cohort movingCohort = combineActiveUnits(Task.actions.move);
                    movingCohort.MoveCohort(waypoint, null);
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
                        if (commandMode == commands.take) {
                            Cohort takingCohort = combineActiveUnits(Task.actions.take);
                            takingCohort.commenceTransact(new Task(null, Task.actions.take, Vector2.zero, unit));
                        }
                        else if (commandMode == commands.give) {
                            Cohort givingCohort = combineActiveUnits(Task.actions.give);
                            givingCohort.commenceTransact(new Task(null, Task.actions.give, Vector2.zero, unit));
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

}