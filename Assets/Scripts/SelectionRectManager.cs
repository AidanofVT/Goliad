using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionRectManager : MonoBehaviour {
    GameState gameState;
    ViewManager vManage;
    SpriteRenderer thisRenderer;
    BoxCollider2D thisCollider;
    List <Unit_local> candidates = new List<Unit_local>();
    float downTime = 1000000;
    Vector2 mousePosLastFrame;
    Vector2 mouseDownLocation;
    public bool rectOn = false;

    void Awake() {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        vManage = transform.parent.GetComponent<ViewManager>();
        thisRenderer = GetComponent<SpriteRenderer>();
        thisCollider = GetComponent<BoxCollider2D>();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            downTime = Time.time;
            mouseDownLocation = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            if (rectOn == true) {
                activateRegion();
                candidates.Clear();
            }
            downTime = 1000000;
// This is invoked so that the InputHandler sees the rectangle "on" on the frame when the mouse button is released.
            Invoke("off", 0);
        }
        else if (Time.time - downTime >= 0.22) {
            if (rectOn == false) {
                on();
                // transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            Vector2 farCornerLocation = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 rectSize = farCornerLocation - mouseDownLocation;
            rectSize = new Vector2(Mathf.Abs(rectSize.x), Mathf.Abs(rectSize.y));
            thisRenderer.size = rectSize;
            thisCollider.size = rectSize;
            transform.position = (farCornerLocation - mouseDownLocation) / 2 + mouseDownLocation;
            transform.position  += new Vector3(0, 0, 8);
            mousePosLastFrame = Input.mousePosition;
            if (Input.GetButtonDown("modifier")) {
                List<Cohort> alreadyOff = new List<Cohort>();
                foreach (Unit_local goSolo in candidates) {
                    Cohort candidatesCohort = goSolo.cohort;
                    if (alreadyOff.Contains(candidatesCohort) == false) {
                        goSolo.cohort.HighlightOff();
                        alreadyOff.Add(candidatesCohort);
                    }
                    goSolo.Highlight();
                }
            }
            if (Input.GetButtonUp("modifier")) {
                List<Cohort> alreadyOn = new List<Cohort>();
                foreach (Unit_local goSolo in candidates) {
                    Cohort candidatesCohort = goSolo.cohort;
                    if (alreadyOn.Contains(candidatesCohort) == false) {
                        goSolo.cohort.Highlight();
                        alreadyOn.Add(goSolo.cohort);
                    }
                }
            }
        }
        else {
            rectOn = false;
        }
    }

    void activateRegion () {
        Cohort aCohort;
        gameState.clearActive();
        if (Input.GetButton("modifier") == false) {
            foreach (Unit_local aboutToBeActivated in candidates) {
                aCohort = aboutToBeActivated.cohort;
                if (gameState.activeCohorts.Contains(aCohort) == false) {
                    aCohort.activate();
                }
            }
        }
        else {
            foreach (Unit_local aboutToBeActivated in candidates) {
                aboutToBeActivated.soloCohort.activate();
            }
        }     
        return;
    }

    void off () {
        thisCollider.enabled = false;
        thisRenderer.enabled = false;
        rectOn = false;
    }

    void on () {
        thisCollider.enabled = true;
        thisRenderer.enabled = true;
        rectOn = true;
    }


    void OnTriggerEnter2D(Collider2D other) {
        Unit_local touchedUnit = other.GetComponent<Unit_local>();
        if (touchedUnit != null) {
            if (Input.GetButton("modifier") == false) {
                Cohort maybeOn = touchedUnit.cohort;
                bool lightsOn = true;
                foreach (Unit_local inQuestion in candidates) {
                    if (inQuestion.cohort.Equals(maybeOn)) {
                        lightsOn = false;
                    }
                }
                if (lightsOn == true) {
                    maybeOn.Highlight();
                }
            }
            else {
                touchedUnit.Highlight();
            }
            candidates.Add(touchedUnit);
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        Unit_local departedUnit = other.GetComponent<Unit_local>();
        if (departedUnit != null) {
            candidates.Remove(departedUnit);
            if (Input.GetButton("modifier") == false) {
                Cohort maybeExtingiush = departedUnit.cohort;         
                bool lightsOut = true;
                foreach (Unit_local inQuestion in candidates) {
                    if (inQuestion.cohort.Equals(maybeExtingiush)) {
                        lightsOut = false;
                    }
                }
                if (lightsOut == true) {
                    maybeExtingiush.HighlightOff();
                }
            }
            else {
                departedUnit.Unhighlight();
            }
        }
    }

}
