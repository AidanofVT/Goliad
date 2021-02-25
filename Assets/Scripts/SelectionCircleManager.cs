using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionCircleManager : MonoBehaviour {

    LineRenderer lineRenderer;
    int fidelity = 100;
    Vector3 [] points;
    public int radius;
    public Vector2 placeInWorld;

    void Start() {
        points = new Vector3[fidelity + 1];
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = fidelity + 1;
        // lineRenderer.startWidth = 0.5f;
        // lineRenderer.endWidth = 0.5f;
        radius = 10;
        placeInWorld = Vector2.zero;        
        ComposePoints();
        string debugOut = "";
        foreach (Vector3 point in points) {
            debugOut += (Vector2) point + ", ";
        }
        Debug.Log(debugOut);
        lineRenderer.SetPositions(points);
    }

    void ComposePoints () {
        float itterativeAngle;
        for (int i = 0; i < fidelity + 1; ++i) {
            itterativeAngle = -1 * Mathf.PI + Mathf.PI * 2 / fidelity * i;
            float xInWorld = Mathf.Sin(itterativeAngle) * radius + placeInWorld.x;
            float yInWorld = Mathf.Cos(itterativeAngle) * radius + placeInWorld.y;
            points[i] = new Vector3(xInWorld, yInWorld, -5);
        }
    }

}
