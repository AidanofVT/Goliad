using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewManager : MonoBehaviour {

    Cohort paintedCohort = null;
    bool stayPainted;
    public List<RectTransform> attendedTransforms = new List<RectTransform>();

    public void attendTo (GameObject focus) {
        attendedTransforms.Add(focus.transform.GetChild(2).GetChild(1).gameObject.GetComponent<RectTransform>());
        attendedTransforms.Add(focus.transform.GetChild(3).GetChild(1).gameObject.GetComponent<RectTransform>());
    }

    public void attendToNoMore (GameObject focus) {
        attendedTransforms.Remove(focus.transform.GetChild(2).GetChild(1).gameObject.GetComponent<RectTransform>());
        attendedTransforms.Remove(focus.transform.GetChild(3).GetChild(1).gameObject.GetComponent<RectTransform>());
    }

    public void paintCohort (Cohort toPaint) {
        foreach (Unit_local fellow in toPaint.members) {
            fellow.transform.GetChild(2).GetChild(0).gameObject.SetActive(true);
        }
        paintedCohort = toPaint;
        StartCoroutine(paintKeep());
    }

    public void unpaintCohort (Cohort toUnpaint) {
        if (stayPainted == false) {
            foreach (Unit_local fellow in toUnpaint.members) {
                fellow.transform.GetChild(2).GetChild(0).gameObject.SetActive(false);
            }
            paintedCohort = null;
            StopCoroutine(paintKeep());
        }
    }

    IEnumerator paintKeep () {
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            stayPainted = true;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1)) {
            stayPainted = false;
            unpaintCohort(paintedCohort);
        }
        yield return null;
    }

    public void resizeUIs () {
        foreach (RectTransform toAlter in attendedTransforms) {
            toAlter.localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
        }
    }
}
