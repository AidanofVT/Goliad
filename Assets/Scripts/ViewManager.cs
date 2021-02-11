using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ViewManager : MonoBehaviour {

    public List<Cohort> consideredCohorts = new List<Cohort>();
    public List<Cohort> paintedCohorts = new List<Cohort>();
    public List<GameObject> consideredSprites = new List<GameObject>();
    public List<GameObject> paintedSprites = new List<GameObject>();

    List <Transform> transforms = new List<Transform>();
    Camera mainCamera;
    bool power = false;

    void Start () {
        transforms = GameObject.Find("Goliad").GetComponent<GameState>().allIconTransforms;
        mainCamera = Camera.main;
    }

    public void addToPalette (Unit_local toAdd) {
        consideredCohorts.Add(toAdd.cohort);
        foreach (Unit_local jess in toAdd.cohort.members) {
            consideredSprites.Add(jess.transform.GetChild(2).GetChild(0).gameObject);
        }
        if (Input.GetButton("modifier") == false) {
            paintCohort(toAdd.cohort);
        }
        else {
            GameObject sprite = toAdd.transform.GetChild(2).GetChild(0).gameObject;
            sprite.SetActive(true);
            paintedSprites.Add(sprite);
        }
    }

    public void removeFromPalette (Unit_local toRem) {
        consideredCohorts.Remove(toRem.cohort);
        foreach (Unit_local jose in toRem.cohort.members) {
            consideredSprites.Remove(jose.transform.GetChild(2).GetChild(0).gameObject);
        }
        if (Input.GetKey(KeyCode.Mouse1) == false) {
            unpaintCohort(toRem.cohort);
        }
    }

    public void clearPalette () {
        foreach (GameObject stu in paintedSprites) {
            stu.SetActive(false);
        }
        consideredCohorts.Clear();
        consideredSprites.Clear();
        paintedCohorts.Clear();
        paintedSprites.Clear();
    }

    public void paintCohort (Cohort toPaint) {
        foreach (Unit_local fellow in toPaint.members) {
            GameObject sprite = fellow.transform.GetChild(2).GetChild(0).gameObject;
            sprite.SetActive(true);
            paintedSprites.Add(sprite);
        }
        paintedCohorts.Add(toPaint);
    }

    public void unpaintCohort (Cohort toUnpaint) {
        foreach (Unit_local fellow in toUnpaint.members) {
            GameObject sprite = fellow.transform.GetChild(2).GetChild(0).gameObject;
            sprite.SetActive(false);
            paintedSprites.Remove(sprite);
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
                paintedSprites.Add(sprite);
            }
        }
        if (Input.GetButtonUp("modifier")) {
            List<GameObject> thisIsToSupressWarnings = new List<GameObject>(paintedSprites); 
            foreach (GameObject sprite in thisIsToSupressWarnings) {
                sprite.SetActive(false);
                paintedSprites.Remove(sprite);
            }
            foreach (Cohort toPaint in consideredCohorts) {
                paintCohort(toPaint);
            }
        }
    }

    public void resizeIcons (GameObject singleSubject = null) {
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
            if (singleSubject != null) {
                singleSubject.transform.localScale = neutralScaleVector * mainCamera.orthographicSize / 30;
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
