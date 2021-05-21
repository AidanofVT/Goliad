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
    
    public Cohort CombineActiveUnitsOld (Task.actions intent) {
// It would be nice if this function would destroy any of the cohorts that are left with only one member, switching that one member to its' solocohort.
        if (activeUnits.Count <= 0) {
            return null;
        }
        else {
            Cohort firstCohort = activeUnits[0].cohort;
            bool newCohortNecessary = false;
            List<Unit_local> eligibleUnits = new List<Unit_local>();
            List<Unit_local> thisIsToSuppressWarnings = new List<Unit_local> (activeUnits);
            foreach (Unit_local individual in thisIsToSuppressWarnings) {
                bool unitIsEligible = false;
                switch (intent) {
                    case Task.actions.move:
                        unitIsEligible = individual.stats.isMobile;
                        break;
                    case Task.actions.attack:
                        unitIsEligible = individual.stats.isArmed;
                        break;
    // Leaving unconditional because the cohort should remain unchanged as long as at least one unit is eligable.
                    case Task.actions.take:
                    case Task.actions.give:
                    case Task.actions.build:
                        unitIsEligible = true;
                        break;
                    default:
                        break;
                }
                if (unitIsEligible == true) {
                    eligibleUnits.Add(individual);
                    if (individual.cohort != firstCohort) {
                        newCohortNecessary = true;
                    }
                }
                else {
                    newCohortNecessary = true;
                    individual.Deactivate();
                }
            }
            if (eligibleUnits.Count != firstCohort.members.Count) {
                newCohortNecessary = true;
            }
// We're going to all this trouble because if we create cohorts unnecessarily, stuff happens like unit spawn positions not appearing to increment.
            if (newCohortNecessary == true || firstCohort == activeUnits[0].soloCohort) {
                Cohort newCohort = new Cohort();
                foreach (Unit_local changing in eligibleUnits) {
                    changing.ChangeCohort(newCohort);
                }
                // Debug.Log($"New cohort, {newCohort.members.Count} large.");
                return newCohort;
            }
            else {
                return firstCohort;
            }
        }
    }

    public Cohort CombineActiveUnits (Task.actions forWhat) {
// PROBLEM: this breaks up cohorts in the process of considering a new cohort, even if the units involved won't be a part of the new cohort.
        List<Cohort> effectedCohorts = new List<Cohort>();
        List<Unit_local> eligibleUnits = new List<Unit_local>();
        List<Unit_local> thisIsToSuppressWarnings = new List<Unit_local> (activeUnits);
        foreach (Unit_local individual in thisIsToSuppressWarnings) {
            bool unitIsEligible = false;
            switch (forWhat) {
                case Task.actions.move:
                    unitIsEligible = individual.stats.isMobile;
                    break;
                case Task.actions.attack:
                    unitIsEligible = individual.stats.isArmed;
                    break;
// Leaving unconditional because the cohort should remain unchanged as long as at least one unit is eligable.
                case Task.actions.take:
                case Task.actions.give:
                case Task.actions.build:
                    unitIsEligible = true;
                    break;
                default:
                    break;
            }
            if (unitIsEligible == true) {
                eligibleUnits.Add(individual);
                if (effectedCohorts.Contains(individual.cohort) == false) {
                    effectedCohorts.Add(individual.cohort);
                }
            }
            else {
                individual.Deactivate();
            }
        }
        Cohort candidateCohort;
        bool newCohortWorks = false;
        if (effectedCohorts.Count == 1 && eligibleUnits.Count == effectedCohorts[0].members.Count) {
            candidateCohort = effectedCohorts[0];
        }
        else {
            candidateCohort = new Cohort(eligibleUnits);
        }
        switch (forWhat) {
            case Task.actions.move:
                newCohortWorks = candidateCohort.mobileMembers.Count > 0;
                break;
            case Task.actions.attack:
                newCohortWorks = candidateCohort.armedMembers.Count > 0;
                break;
            case Task.actions.take:
                newCohortWorks = candidateCohort.orbs < candidateCohort.orbCapacity;
                break;
            case Task.actions.give:
            case Task.actions.build:
                newCohortWorks = candidateCohort.orbs > 0;
                break;
            default:
                break;
        }
        if (newCohortWorks == true) {
            return candidateCohort;
        }
        else {
            return null;
        }
    }

    void Update() {
        if (Input.GetKeyUp(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1)) {
            if (rectManage.rectOn == false) {
                Collider2D[] detectedThings = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                ThingLeftClicked(detectedThings[0].gameObject);
            }
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            commandMode = commands.neutral;
        }
        if (Input.GetKeyUp(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Mouse0) && targetingCircle.enabled == false) {
            if (rectManage.rectOn == false) {
                Collider2D[] detectedThings = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                ThingRightClicked(detectedThings[0].gameObject);
            }
        }
        if (activeUnits.Count > 0) {
            if (Input.GetButtonUp("take") && !Input.GetButtonUp("give")) {
                Cursor.SetCursor(cursorForTaking, Vector2.zero, CursorMode.Auto);
                commandMode = commands.take;
            }
            if (Input.GetButtonUp("give") && !Input.GetButtonUp("take")) {
                Cursor.SetCursor(cursorForGiving, Vector2.zero, CursorMode.Auto);
                commandMode = commands.give;
            }
        }
    }

    public void ThingRightClicked (GameObject thingClicked) {
        if (gameState.activeUnits.Count > 0) {
            switch (thingClicked.tag) {
                case "unit":
                case "unit icon":
                    if (thingClicked.name == "Icon") {
                        thingClicked = thingClicked.transform.parent.gameObject;
                    }
                    if (thingClicked.tag == "unit" && thingClicked.GetComponent<Unit>().stats.factionNumber != PhotonNetwork.LocalPlayer.ActorNumber) {
                        Cohort attackingCohort = CombineActiveUnits(Task.actions.attack);
                        if (attackingCohort != null) {           
                            attackingCohort.CommenceAttack(new Task(null, Task.actions.attack, thingClicked.transform.position, thingClicked.GetComponent<Unit>()));
                        }
                    }
                    else {
                        Cohort followingCohort = CombineActiveUnits(Task.actions.move);
                        if (followingCohort != null) {
                            followingCohort.MoveCohort(thingClicked.transform.position, thingClicked.GetComponent<Unit_local>());
                        }
                    }
                    break;
                case "ground":
                    Cohort movingCohort = CombineActiveUnits(Task.actions.move);
                    if (movingCohort != null) {
                        movingCohort.MoveCohort(Camera.main.ScreenToWorldPoint(Input.mousePosition), null);
                    }
                    break;
                case "UI":
                    return;
                default: 
                    Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                    break;
            }
        }
    }

    void ThingLeftClicked (GameObject thingClicked) {
        switch (thingClicked.tag) {
            case "unit":
            case "unit icon":
                if (thingClicked.name == "Icon") {
                    thingClicked = thingClicked.transform.parent.gameObject;
                }
                Unit_local unit = thingClicked.GetComponent<Unit_local>();
                if (unit != null && thingClicked.name.Contains("sheep") == false) {
                    if (commandMode == commands.neutral) {
                        if (Input.GetButton("modifier") == true) {
                            if (gameState.activeUnits.Contains(unit)) {
                                unit.Deactivate();
                            }
                            else {
                                unit.soloCohort.Activate();
                            }
                        }
                        else {
                            gameState.ClearActive();
                            unit.cohort.Activate();
                        }
                    }
                    else {
                        Task.actions action = Task.actions.help;
                        if (commandMode == commands.take) {
                            action = Task.actions.take;
                        }
                        else if (commandMode == commands.give) {
                            action = Task.actions.give;
                        }
                        Cohort transactingCohort = CombineActiveUnits(action);
                        if (transactingCohort != null) {
                            transactingCohort.CommenceTransact(new Task(null, action, Vector2.zero, unit));
                        }
                    }
                }
                break;
            case "ground":
                gameState.ClearActive();
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