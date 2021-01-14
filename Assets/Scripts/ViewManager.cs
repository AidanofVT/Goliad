using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ViewManager : MonoBehaviour {

    float cameraBaseline;
    public List<Transform> attendedTransforms = new List<Transform>();
    public List<Cohort> consideredCohorts = new List<Cohort>();
    public List<Cohort> paintedCohorts = new List<Cohort>();
    public List<GameObject> consideredSprites = new List<GameObject>();
    public List<GameObject> paintedSprites = new List<GameObject>();
    public Hashtable unitIconPairs = new Hashtable();

    void Start () {
        cameraBaseline = Camera.main.orthographicSize;
    }

    public void addUnitIcon (PhotonView thisViewer) {
        GameObject associatedGob = thisViewer.gameObject;
        string fileName = associatedGob.name + "_miniIcon";
        if (thisViewer.OwnerActorNr == 1) {
            fileName += "_white";
        }
        else if (thisViewer.OwnerActorNr == 2) {
            fileName += "_orange";
        }
        GameObject newIcon = (GameObject) Resources.Load("Sprites/" + fileName);
        newIcon.SetActive(false);
        newIcon.transform.SetParent(transform.GetChild(3));
        unitIconPairs.Add(associatedGob, newIcon);
    }

    public void attendTo (GameObject focus) {
        Transform targetUI = focus.transform.GetChild(2).GetChild(1);
        attendedTransforms.Add(targetUI);
        resizeUIs(targetUI);
        Transform activeUI = focus.transform.GetChild(3).GetChild(1);
        attendedTransforms.Add(activeUI);
        resizeUIs(activeUI);
    }

    public void attendToNoMore (GameObject focus) {
        attendedTransforms.Remove(focus.transform.GetChild(2).GetChild(1).gameObject.GetComponent<RectTransform>());
        attendedTransforms.Remove(focus.transform.GetChild(3).GetChild(1).gameObject.GetComponent<RectTransform>());
    }

    public void addToPalette (Unit_local toAdd) {
        consideredCohorts.Add(toAdd.cohort);
        foreach (Unit_local jess in toAdd.cohort.members) {
            consideredSprites.Add(jess.transform.GetChild(2).GetChild(0).gameObject);
        }
        if (Input.GetKey(KeyCode.Mouse1) == false) {
            if (Input.GetButton("modifier") == false) {
                paintCohort(toAdd.cohort);
            }
            else {
                GameObject sprite = toAdd.transform.GetChild(2).GetChild(0).gameObject;
                sprite.SetActive(true);
                paintedSprites.Add(sprite);
            }
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

    public void removeFromPalette (Unit_local toRem) {
        consideredCohorts.Remove(toRem.cohort);
        foreach (Unit_local jose in toRem.cohort.members) {
            consideredSprites.Remove(jose.transform.GetChild(2).GetChild(0).gameObject);
        }
        if (Input.GetKey(KeyCode.Mouse1) == false) {
            unpaintCohort(toRem.cohort);
        }
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
        if (Input.GetKeyUp(KeyCode.Mouse1)) {
            List<Cohort> thisIsToSupressWarnings = new List<Cohort>(paintedCohorts); 
            foreach (Cohort pntedChrt in thisIsToSupressWarnings) {
                unpaintCohort(pntedChrt);
            }
            foreach (Cohort toPaint in consideredCohorts) {
                paintCohort(toPaint);
            }
        }
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

    public void resizeUIs (Transform singleSubject = null) {
        List <Transform> toResize;
        if (singleSubject != null) {
            toResize = new List<Transform>();
            toResize.Add(singleSubject);
        }
        else {
            toResize = attendedTransforms;
        }
        float scale = Camera.main.orthographicSize / cameraBaseline;
        Vector3 twoDScale = new Vector3 (scale, scale, 1);
        foreach (RectTransform folder in toResize) {
            for (int i = 0; i < folder.childCount; ++i) {
                folder.GetChild(i).transform.localScale = twoDScale;
                //folder.GetChild(i).transform.localPosition *= scale;
            }
        }
    }
}
