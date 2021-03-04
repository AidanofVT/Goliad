using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionCircleManager : MonoBehaviour {

    InputHandler inputHandler;
    LineRenderer lineRenderer;
    Camera camera;
    int fidelity;
    Vector3 [] points;
    public float radius;
    Vector2 placeOnScreen;

    float mouseDown = 1000000;

    void Start() {
        inputHandler = GameObject.Find("Goliad").GetComponent<InputHandler>();
        camera = Camera.main;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 2f;
        lineRenderer.endWidth = 2f;
        radius = 10;
        // string debugOut = "";
        // foreach (Vector3 point in points) {
        //     debugOut += (Vector2) point + ", ";
        // }
        // Debug.Log(debugOut);
    }

    void ComposePoints () {
        points = new Vector3[fidelity + 1];
        float itterativeAngle;
        float xCenterAdjusted = placeOnScreen.x - Screen.width / 2;
        float yCenterAdjusted = placeOnScreen.y - Screen.height / 2;
        for (int i = 0; i < fidelity + 1; ++i) {
            itterativeAngle = (Mathf.PI * 2 / fidelity * i) - Mathf.PI;
            float xInWorld = Mathf.Sin(itterativeAngle) * radius + xCenterAdjusted;
            float yInWorld = Mathf.Cos(itterativeAngle) * radius + yCenterAdjusted;
            points[i] = new Vector3(xInWorld, yInWorld, 0);
        }
    }

    void Update () {
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            mouseDown = Time.time;
            placeOnScreen = Input.mousePosition;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1)) {
            if (lineRenderer.enabled == true) {
                Cohort attackers = inputHandler.combineActiveUnits(Task.actions.attack);
                float adjustedRadius = radius * ((camera.orthographicSize * 2) / Screen.height);
                attackers.commenceAttack(new Task(null, Task.actions.attack, camera.ScreenToWorldPoint(placeOnScreen), null, 0, adjustedRadius));
            }
            mouseDown = 1000000;
            lineRenderer.enabled = false;
        }
        else if (Time.time - mouseDown >= 0.22f) {
            radius = Mathf.Clamp(Mathf.Abs(Vector2.Distance(Input.mousePosition, placeOnScreen)), 1, Mathf.Infinity);
            fidelity = (int) Mathf.Clamp(radius / 6, 20, Mathf.Infinity);
            lineRenderer.positionCount = fidelity + 1;
            ComposePoints();
            lineRenderer.SetPositions(points);
            lineRenderer.enabled = true;
        }
    }

}
