using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ClickHandler : MonoBehaviour {
    GameState gameState;
    ViewManager vManage;
    public GameObject goliad;
    
    void Awake () {
        gameState = goliad.GetComponent<GameState>();
            vManage = transform.parent.GetComponent<ViewManager>();
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
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            GameObject underMouse = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0].gameObject;
            if (underMouse.tag == "unit" && underMouse.GetComponent<Unit_local>() != null) {
                StartCoroutine(holdTarget(underMouse));
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
                    newCohort.moveCohort(thingClicked.transform.position, thingClicked.transform);
                }
                break;
            case "ground":
                newCohort.moveCohort(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                break;
            case "UI":
                return;
            default: 
                newCohort.moveCohort(new Vector3(0,0,0));
                Debug.Log("PROBLEM: nothing with a valid tag hit by raycast.");
                break;
        }
    }

    void thingLeftClicked (GameObject thingClicked) {
        switch (thingClicked.tag) {
            case "unit":
                gameState.clearActive();
                if (thingClicked.GetComponent<Unit_local>()) {
                    gameState.activeCohorts.Clear();
                    thingClicked.GetComponent<Unit_local>().activate(true);
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
        Debug.Log(targetingUI.name);
        while (true) {
            if (Time.time - timeDown < 0.1f) {
                if (Input.GetKeyUp(KeyCode.Mouse1)) {
                    break;
                }
            }
            else {
                if (targetingUI.activeInHierarchy == false) {
                    targetingUI.SetActive(true);
                    vManage.attendedTransforms.Add(targetingUI.transform.parent.GetComponent<RectTransform>());
                }
                if (Input.GetKeyUp(KeyCode.Mouse1)) {
                    m1UpButtonPress(target, targetingUI);
                    break;
                }
            }
            yield return new WaitForSeconds(0);
        }
        Debug.Log("104");
        targetingUI.SetActive(false);
        vManage.attendedTransforms.Remove(targetingUI.transform.parent.GetComponent<RectTransform>());
        StopCoroutine("holdTarget");
        yield return null;
    }

    void m1UpButtonPress (GameObject unit, GameObject buttonContainer) {
        Collider2D contact = Physics2D.OverlapPointAll(Camera.main.ScreenToWorldPoint(Input.mousePosition))[0];
        BoxCollider2D [] buttonColliders = buttonContainer.GetComponentsInChildren<BoxCollider2D>();
        if (contact.gameObject.name.Contains("Button")) {
            Cohort newCohort = gameState.combineActiveCohorts();
            string toprint = "acting on a cohort composed of ";
            foreach (Unit_local member in newCohort.members) {
                toprint += (member.gameObject.name + ", ");
            }
            Debug.Log(toprint);
            if (contact.gameObject.name == "Button--Give") {
                newCohort.commenceGive(unit.GetComponent<Unit>().cohort);
            }
            else {
                newCohort.commenceTake(unit.GetComponent<Unit>().cohort);
            }
        }
    }

}