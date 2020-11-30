using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewManager : MonoBehaviour {

    Cohort paintedCohort = null;
    bool stayPainted;

    public void paintCohort (Cohort toPaint) {
        foreach (Unit_local fellow in toPaint.members) {
            fellow.transform.GetChild(4).gameObject.SetActive(true);
        }
        paintedCohort = toPaint;
        StartCoroutine(paintKeep());
    }

    public void unpaintCohort (Cohort toUnpaint) {
        if (stayPainted == false) {
            foreach (Unit_local fellow in toUnpaint.members) {
                fellow.transform.GetChild(4).gameObject.SetActive(false);
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
}
