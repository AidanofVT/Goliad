using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ViewManager : MonoBehaviour {

    Unit_local unitUnderMouse;
    SelectionRectManager rectManager;
    List <GameObject> icons = new List<GameObject>();
    Camera mainCamera;
    bool iconsEnabled = false;

    void Start () {
        icons = GameObject.Find("Goliad").GetComponent<GameState>().allIcons;
        rectManager = transform.GetChild(1).GetComponent<SelectionRectManager>();
        mainCamera = Camera.main;
    }

    public void AttendTo (Unit_local underMouse) {
        unitUnderMouse = underMouse;
        if (rectManager.rectOn == false) {
            if (Input.GetButton("modifier") == false) {
                unitUnderMouse.cohort.Highlight();
            }
            else {
                unitUnderMouse.Highlight();
            }
        }
    }

    public void Disregard () {
        if (rectManager.rectOn == false) {
            unitUnderMouse.cohort.HighlightOff();
        }
        unitUnderMouse = null;
    }

    public void ResizeIcons (GameObject singleIcon = null) {
        if (mainCamera.orthographicSize > 40) {
            Vector3 neutralScaleVector = new Vector3 (1, 1, 1); 
            if (singleIcon != null) {
                singleIcon.transform.localScale = neutralScaleVector * mainCamera.orthographicSize / 30;
                singleIcon.SetActive(true);
            }
            else {
                if (iconsEnabled == false) {
                    foreach (GameObject key in icons) {
                        key.SetActive(true);
                    }
                    iconsEnabled = true;
                }
                foreach (GameObject key in icons) {    
                    key.transform.localScale = neutralScaleVector * mainCamera.orthographicSize / 30;
                }
            }
        }
        else if (iconsEnabled == true) {
            foreach (GameObject key in icons) {
                key.SetActive(false);
            }
            iconsEnabled = false;
        }
    }

    void Update () {
// This is necessary because highlighting otherwise requires OnMouseEnter().
        if (unitUnderMouse != null) {
            if (Input.GetButtonDown("modifier") && rectManager.rectOn == false) {
                unitUnderMouse.cohort.HighlightOff();
                unitUnderMouse.Highlight();
            }
            if (Input.GetButtonUp("modifier") && rectManager.rectOn == false) {
                unitUnderMouse.cohort.Highlight();
            }
        }
    }

}
