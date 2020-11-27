using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewManager : MonoBehaviour {
    public bool targetingNow = false;

    public void paintCohort (Unit_local representative) {
        foreach (Unit_local fellow in representative.cohort.members) {
            fellow.transform.GetChild(4).gameObject.SetActive(true);
        }
    }

    public void unpaintCohort (Unit_local representative) {
        foreach (Unit_local fellow in representative.cohort.members) {
            fellow.transform.GetChild(4).gameObject.SetActive(false);
        }
    } 
}
