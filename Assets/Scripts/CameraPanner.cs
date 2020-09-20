﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPanner : MonoBehaviour
{
    GameObject Goliad;
    public float panMultiplier = 0.05f;
    public float zoomMultiplier = 9f;
    public float distanceFactor = 1.1f;
    float distanceMultiplier;
    enum buttonState {up, upLeft, left, leftDown, down, downRight, right, rightUp}

    private void Awake() {
        distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, distanceFactor) / 5;
        Goliad = GameObject.Find("Goliad");
    }

    void Update()
    {
        obeyCameraPanInputs();
        obeyCameraZoomInputs();
        distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, distanceFactor) / 10;   
    }

    void obeyCameraPanInputs () {
        float cameraX = Camera.main.transform.position.x;
        float cameraY = Camera.main.transform.position.y;
        if (Input.GetButton("panRight") == true) {
            cameraX += panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panLeft") == true) {
            cameraX -= panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panUp") == true) {
            cameraY += panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panDown") == true) {
            cameraY -= panMultiplier * distanceMultiplier;
        }
        Vector3 newCamPosition = new Vector3 (cameraX, cameraY, Camera.main.transform.position.z);
                Camera.main.transform.position = newCamPosition;     
    }

    void obeyCameraZoomInputs () {
        if (Input.GetAxis("zoom") != 0) {
            Camera.main.orthographicSize -= Input.GetAxis("zoom") * zoomMultiplier * distanceMultiplier;
            resizeUIs();
        }
    }

    void resizeUIs () {
        foreach (GameObject unit in Goliad.GetComponent<GameState>().getActiveUnits()) {
            unit.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
        }
    }
}
