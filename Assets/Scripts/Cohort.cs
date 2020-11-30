using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohort {

    GameState gameState = GameObject.Find("Goliad").GetComponent<GameState>();

    public List<Unit_local> members = new List<Unit_local>();
    public List<Unit_local> armedMembers = new List<Unit_local>();
    public GameObject target;
    
    public Cohort (List<Unit_local> recruits) {
        foreach (Unit_local unit in recruits) {
            addMember(unit);
        }
        Debug.Log("cohort created with " + members.Count + " members");
    }

    public void activate (Unit_local caller) {
        foreach (Unit_local member in members) {
            if (member != caller) {
                member.activate (false);
            }
        }
        gameState.activeCohorts.Add(this);
    }

    public void addMember (Unit_local recruit) {
        members.Add(recruit);
        if (recruit.stats.isArmed) {
            armedMembers.Add(recruit);
        }
        recruit.cohort = this;        
    }

    public void commenceAttack (GameObject getIt) {
        Debug.Log("cohort attacking " + getIt.name);
        foreach (Unit_local unit in armedMembers) {
            Debug.Log("instructing " + unit.name + " to attack");
            unit.attack(getIt);
        }
    }

    public void commenceGive (Cohort target) {
        foreach (Unit_local member in members) {
            Unit_local aaa = target.members[0];
            GameObject bbb = aaa.gameObject;
            member.give(bbb, 3);
        }
    }

    public void commenceTake (Cohort target) {

    }

    public void disband () {
        foreach (Unit_local member in members) {
            member.cohort = member.soloCohort;
        }
    }

    public void haltCohort () {

    }

    public void moveCohort (Vector3 destination, Transform optionalTransform = null,  Unit_local representative = null) {
        foreach (Unit_local toMove in members) {
            if (toMove.stats.isMobile) {
                MobileUnit_local moveable = toMove as MobileUnit_local;
                moveable.move(destination, optionalTransform);
            }
            else {
                removeMember(toMove);
            }
        }

    }

    public void removeMember (Unit_local reject) {
        if (reject.stats.isArmed) {
            armedMembers.Remove(reject);
        }
        reject.cohort = reject.soloCohort;
    }
    
}