using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohort {

    List<Unit_local> members = new List<Unit_local>();
    List<Unit_local> armedMembers = new List<Unit_local>();
    GameObject target;
    
    public Cohort (List<Unit_local> recruits) {
        members = recruits;
        foreach (Unit_local unit in recruits) {
            if (unit.stats.isArmed) {
                armedMembers.Add(unit);
            }
            unit.cohort = this;
        }
        Debug.Log("cohort created with " + members.Count + " members");
    }

    public void commenceAttack (GameObject getIt) {
        Debug.Log("cohort launching attack");
        foreach (Unit_local unit in armedMembers) {
            unit.attack(getIt);
        }
    }
    
}