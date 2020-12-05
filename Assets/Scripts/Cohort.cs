using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohort {

    GameState gameState = GameObject.Find("Goliad").GetComponent<GameState>();

    public List<Unit_local> members = new List<Unit_local>();
    public List<Unit_local> armedMembers = new List<Unit_local>();
    Task task;
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

    public void assignTransactionWork (Unit_local worker) {
        float bestDistance = 999999;
        Unit_local otherOne = null;
        Hashtable counterParties = null;
        if (task.nature == Task.actions.give) {
            counterParties = remainingToTake;
        }
        else {
            counterParties = remainingToGive;
        }
        foreach (Unit_local reciever in counterParties.Keys) {
            Unit_local maybeThis = reciever;
            float distanceTo = Vector2.Distance(maybeThis.transform.position, worker.transform.position);
            if (distanceTo < bestDistance) {
                otherOne = maybeThis;
                bestDistance = distanceTo;
            }
        }
        if (task.nature == Task.actions.give) {
            if (otherOne.roomForMeat() <= worker.meat) {
                worker.give(otherOne.gameObject, otherOne.roomForMeat());
                remainingToTake.Remove(otherOne);
                remainingToGive[worker] = (int) remainingToGive[worker] - otherOne.roomForMeat();
            }
            else{
                worker.give(otherOne.gameObject, worker.meat);
                remainingToGive.Remove(worker);
                remainingToTake[otherOne] = (int) remainingToTake[otherOne] - worker.meat;                   
            }
        }
        else {
            if (worker.roomForMeat() <= otherOne.meat) {
                worker.take(otherOne.gameObject, worker.roomForMeat());
                remainingToTake.Remove(worker);
                remainingToGive[otherOne] = (int) remainingToGive[otherOne] - worker.roomForMeat();
            }
            else{
                worker.take(otherOne.gameObject, otherOne.meat);
                remainingToGive.Remove(otherOne);
                remainingToTake[worker] = (int) remainingToTake[worker] - otherOne.meat;                   
            }
        }
    }

    public void commenceAttack (GameObject getIt) {
        Debug.Log("cohort attacking " + getIt.name);
        foreach (Unit_local unit in armedMembers) {
            Debug.Log("instructing " + unit.name + " to attack");
            unit.attack(getIt);
        }
    }

    public void commenceTransact (Task transaction) {
        task = transaction;
        Cohort from;
        Cohort to;
        if (task.nature == Task.actions.give) {
            from = this;
            to = task.objectUnit.GetComponent<Unit>().cohort;
        }
        else {
            from = task.objectUnit.GetComponent<Unit>().cohort;
            to = this;
        }
        foreach (Unit_local giver in from.members) {
            if (giver.meat > 0) {
                remainingToGive.Add(giver, giver.meat);
            }
        }
        foreach (Unit_local recipient in to.members) {
            if (recipient.roomForMeat() > 0) {
                remainingToTake.Add(recipient, recipient.roomForMeat());
            }
        }
        Hashtable workers;
        if (task.nature == Task.actions.give) {
            workers = new Hashtable(remainingToGive);
        }
        else {
            workers = new Hashtable(remainingToTake);
        }
        foreach (Unit_local worker in workers.Keys) {
            assignTransactionWork(worker);
            if (remainingToGive.Count <= 0 || remainingToTake.Count <= 0) {
                remainingToGive.Clear();
                remainingToTake.Clear();
                task = null;
                break;
            }
        }
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
                    assignTransactionWork(worker);
                }
                break;
            default:
                break;
        }
    }
    
}