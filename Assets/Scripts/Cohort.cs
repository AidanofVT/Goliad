using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohort {

    GameState gameState = GameObject.Find("Goliad").GetComponent<GameState>();

    public List<Unit_local> members = new List<Unit_local>();
    public List<Unit_local> armedMembers = new List<Unit_local>();
    Hashtable remainingToGive = new Hashtable();
    Hashtable remainingToTake = new Hashtable();
    
    public Cohort (List<Unit_local> recruits) {
        foreach (Unit_local unit in recruits) {
            addMember(unit);
        }
        //Debug.Log("cohort created with " + members.Count + " members");
    }

    public void activate () {
        foreach (Unit_local member in members) {
            member.activate();
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

    public void assignGiveWork (Unit_local worker) {
        Debug.Log("assigning work");
        float bestDistance = 999999;
        Unit_local recipient = null;
        foreach (Unit_local reciever in remainingToTake.Keys) {
            Debug.Log("checking distance to a target");
            Unit_local maybeThis = reciever;
            float distanceTo = Vector2.Distance(maybeThis.transform.position, worker.transform.position);
            if (distanceTo < bestDistance) {
                recipient = maybeThis;
                bestDistance = distanceTo;
            }
        }
        if (recipient != null) {
            int meatRoom = recipient.roomForMeat();
            if (meatRoom <= worker.meat) {
                Debug.Log("putting the unit to work");
                worker.give(recipient.gameObject, meatRoom);
                remainingToTake.Remove(recipient);
                int stillHave = (int) remainingToGive[worker];
                remainingToGive[worker] = stillHave - meatRoom;
            }
            else {
                Debug.Log("putting the unit to work");
                worker.give(recipient.gameObject, worker.meat);
                remainingToGive.Remove(worker);
                int stillOpen = (int) remainingToTake[recipient];
                remainingToTake[recipient] = stillOpen - worker.meat;
            }
        }
        else {
            remainingToGive.Clear();
        }
    }

    public void commenceAttack (GameObject getIt) {
        Debug.Log("cohort attacking " + getIt.name);
        foreach (Unit_local unit in armedMembers) {
            Debug.Log("instructing " + unit.name + " to attack");
            unit.attack(getIt);
        }
    }

    public void commenceGive (Cohort target) {
        Debug.Log("commenceGive in a cohort with " + members.Count + " members");
        foreach (Unit_local member in members) {
            if (member.meat > 0) {
                remainingToGive.Add(member, member.meat);
            }
        }
        foreach (Unit_local recipient in target.members) {
            if (recipient.roomForMeat() > 0) {
                remainingToTake.Add(recipient, recipient.roomForMeat());
            }
        }
        Hashtable thisIsToSupressWarnings = new Hashtable(remainingToGive);
        foreach (Unit_local worker in thisIsToSupressWarnings.Keys) {
            Debug.Log("trying to assign work");
            assignGiveWork(worker);
        }
    }

    public void commenceTake (Cohort target) {

    }

    public void deactivate () {
        foreach (Unit_local member in members) {
            member.deactivate();
        }
        gameState.activeCohorts.Remove(this);
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
        members.Remove(reject);
        if (reject.stats.isArmed) {
            armedMembers.Remove(reject);
        }
        Debug.Log("member removed");
    }

    public void taskCompleted (Task task) {
        switch (task.nature) {
            case Task.actions.give:
            Unit_local worker = task.subjectUnit.GetComponent<Unit_local>();
                if (worker.meat > 0 && remainingToTake.Count > 0) {
                    assignGiveWork(worker);
                }
                break;
            default:
                break;
        }
    }
    
}