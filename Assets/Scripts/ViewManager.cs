using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ViewManager : MonoBehaviour {

    public List<Cohort> consideredCohorts = new List<Cohort>();
    public List<Cohort> paintedCohorts = new List<Cohort>();

    List <Transform> transforms = new List<Transform>();
    Camera mainCamera;
    bool power = false;

    void Start () {
        transforms = GameObject.Find("Goliad").GetComponent<GameState>().allIconTransforms;
        mainCamera = Camera.main;
    }

    public void addToPalette (Unit_local toAdd) {
        consideredCohorts.Add(toAdd.cohort);
        if (Input.GetButton("modifier") == false) {
            paintCohort(toAdd.cohort);
        }
        else {
            toAdd.highlight();
        }
    }

    public void removeFromPalette (Unit_local toRem) {
        consideredCohorts.Remove(toRem.cohort);
        unpaintCohort(toRem.cohort);
    }

    public void clearPalette () {
        consideredCohorts.Clear();
        paintedCohorts.Clear();
    }

    public void paintCohort (Cohort toPaint) {
        foreach (Unit_local fellow in toPaint.members) {
            fellow.highlight();
        }
        paintedCohorts.Add(toPaint);
    }

    public void unpaintCohort (Cohort toUnpaint) {
        foreach (Unit_local fellow in toUnpaint.members) {
            fellow.unHighlight();
        }
        paintedCohorts.Remove(toUnpaint);
    }

    void Update () {
        if (Input.GetButtonDown("modifier")) {
            List<Cohort> thisIsToSupressWarnings = new List<Cohort>(paintedCohorts); 
            foreach (Cohort pntedChrt in thisIsToSupressWarnings) {
                unpaintCohort(pntedChrt);
            }
            //at some point this will have to be hooked up to the rectangle manager and altered so that more than one can be painted
            GameObject toPaint = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)).gameObject;
            if (toPaint.GetComponent<Unit_local>() != null) {
                GameObject sprite = toPaint.transform.GetChild(2).GetChild(0).gameObject;
                sprite.SetActive(true);
            }
        }
        if (Input.GetButtonUp("modifier")) {
            foreach (Cohort toPaint in consideredCohorts) {
                paintCohort(toPaint);
            }
        }
    }

    public void resizeIcons (GameObject singleIcon = null) {
        if (mainCamera.orthographicSize < 40) {
            if (power == true) {
                foreach (Transform key in transforms) {
                    key.gameObject.SetActive(false);
                }
                power = false;
            }
        }
        else {
            Vector3 neutralScaleVector = new Vector3 (1, 1, 1); 
            if (singleIcon != null) {
                singleIcon.transform.localScale = neutralScaleVector * mainCamera.orthographicSize / 30;
                singleIcon.SetActive(true);
            }
            else {
                if (power == false) {
                    foreach (Transform key in transforms) {
                        key.gameObject.SetActive(true);
                    }
                    power = true;
                }
                foreach (Transform key in transforms) {    
                    key.localScale = neutralScaleVector * mainCamera.orthographicSize / 30;
                }
            }
        }
    }
}
