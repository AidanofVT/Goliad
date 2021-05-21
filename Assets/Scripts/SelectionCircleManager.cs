using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionCircleManager : MonoBehaviour {

    InputHandler inputHandler;
    List<Unit_local> activeUnits;
    LineRenderer lineRenderer;
    Camera mainCamera;
    int fidelity;
    Vector3 [] points;
// SelectionCircleManager should be in an object subordinate to a canvas. These variables all refer to pixels, not world-space:
    public float radius;
    Vector2 placeOnScreen;
    float xCenterAdjusted;
    float yCenterAdjusted;

    float mouseDown = 1000000;

    void Start() {
        GameObject goliad = GameObject.Find("Goliad");
        inputHandler = goliad.GetComponent<InputHandler>();
        activeUnits = goliad.GetComponent<GameState>().activeUnits;
        mainCamera = Camera.main;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 2f;
        lineRenderer.endWidth = 2f;
        radius = 10;
    }

    void ComposePoints () {
        radius = Mathf.Clamp(Vector2.Distance(Input.mousePosition, placeOnScreen), 1, Mathf.Infinity);
        fidelity = (int) Mathf.Clamp(radius / 6, 20, Mathf.Infinity);
        points = new Vector3[fidelity + 1];
        float itterativeAngle;
        float xOfPoint;
        float yOfPoint;
        for (int i = 0; i < fidelity + 1; ++i) {
            itterativeAngle = (Mathf.PI * 2 / fidelity * i) - Mathf.PI;
            xOfPoint = Mathf.Sin(itterativeAngle) * radius + xCenterAdjusted;
            yOfPoint = Mathf.Cos(itterativeAngle) * radius + yCenterAdjusted;
            points[i] = new Vector3(xOfPoint, yOfPoint, 0);
        }
    }

    void Update () {
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            mouseDown = Time.time;
            placeOnScreen = Input.mousePosition;
            xCenterAdjusted = placeOnScreen.x - Screen.width / 2;
            yCenterAdjusted = placeOnScreen.y - Screen.height / 2;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1)) {
            if (lineRenderer.enabled == true && activeUnits.Count > 0) {
                Cohort attackers = inputHandler.CombineActiveUnits(Task.actions.attack);
                if (attackers != null) {
                    float worldSpaceRadius = radius * ((mainCamera.orthographicSize * 2) / Screen.height);
                    attackers.CommenceAttack(new Task(null, Task.actions.attack, mainCamera.ScreenToWorldPoint(placeOnScreen), null, 0, worldSpaceRadius));
                }
            }
            mouseDown = 1000000;
            lineRenderer.enabled = false;
        }
        else if (Time.time - mouseDown >= 0.22f) {            
            ComposePoints();
            lineRenderer.positionCount = fidelity + 1;
            lineRenderer.SetPositions(points);
            lineRenderer.enabled = true;
        }
    }

}
