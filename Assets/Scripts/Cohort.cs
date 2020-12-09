using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohort {

    GameState gameState = GameObject.Find("Goliad").GetComponent<GameState>();

    public List<Unit_local> members = new List<Unit_local>();
    public List<Unit_local> armedMembers = new List<Unit_local>();
    Task task;
    public List<Task> assignments = new List<Task>();
    Hashtable remainingToProvide = new Hashtable();
    Hashtable remainingToAccept = new Hashtable();
    
    public Cohort (List<Unit_local> recruits = null) {
        if (recruits != null) {
            foreach (Unit_local unit in recruits) {
                addMember(unit);
            }        
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
    }

    public void assignTransactionWork (Unit_local worker) {
        float bestDistance = 999999;
        Unit_local otherOne = null;
        Hashtable counterParties = null;
        if (task.nature == Task.actions.give) {
            counterParties = remainingToAccept;
        }
        else {
            counterParties = remainingToProvide;
        }
        foreach (Unit_local reciever in counterParties.Keys) {
            Unit_local maybeThis = reciever;
            float distanceTo = Vector2.Distance(maybeThis.transform.position, worker.transform.position);
            if (distanceTo < bestDistance) {
                otherOne = maybeThis;
                bestDistance = distanceTo;
            }
        }
        Task newTask;
        if (task.nature == Task.actions.give) {
            if (otherOne.roomForMeat() <= worker.meat) {
                newTask = new Task(worker.gameObject, otherOne.gameObject, Task.actions.give, otherOne.roomForMeat());
                remainingToAccept.Remove(otherOne);
                remainingToProvide[worker] = (int) remainingToProvide[worker] - otherOne.roomForMeat();
            }
            else{
                newTask = new Task(worker.gameObject, otherOne.gameObject, Task.actions.give, worker.meat);
                remainingToProvide.Remove(worker);
                remainingToAccept[otherOne] = (int) remainingToAccept[otherOne] - worker.meat;                   
            }
        }
        else {
            if (worker.roomForMeat() <= otherOne.meat) {
                newTask = new Task(worker.gameObject, otherOne.gameObject, Task.actions.take, worker.roomForMeat());
                remainingToAccept.Remove(worker);
                remainingToProvide[otherOne] = (int) remainingToProvide[otherOne] - worker.roomForMeat();
            }
            else{
                newTask = new Task(worker.gameObject, otherOne.gameObject, Task.actions.take, otherOne.meat);
                remainingToProvide.Remove(otherOne);
                remainingToAccept[worker] = (int) remainingToAccept[worker] - otherOne.meat;                   
            }
        }
        worker.work(newTask);
        assignments.Add(newTask);
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
                remainingToProvide.Add(giver, giver.meat);
            }
        }
        foreach (Unit_local recipient in to.members) {
            if (recipient.roomForMeat() > 0) {
                remainingToAccept.Add(recipient, recipient.roomForMeat());
            }
        }
        Hashtable workers;
        if (task.nature == Task.actions.give) {
            workers = new Hashtable(remainingToProvide);
        }
        else {
            workers = new Hashtable(remainingToAccept);
        }
        foreach (Unit_local worker in workers.Keys) {
            assignTransactionWork(worker);
            if (remainingToProvide.Count <= 0 || remainingToAccept.Count <= 0) {
                remainingToProvide.Clear();
                remainingToAccept.Clear();
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
        if (task.nature != Task.actions.move) {
            Debug.Log("PROBLEM: Halt command called on a cohort that's not supposed to be moving!");
        }
        List<Task> thisIsToSupressWarnings = new List<Task>(assignments);
        foreach (Task movement in thisIsToSupressWarnings) {
            if (Vector2.Distance(movement.subjectUnit.transform.position, movement.objectUnit.transform.position) < Mathf.Pow(members.Count, 0.5f)) {
                movement.subjectUnit.GetComponent<AidansMovementScript>().terminatePathfinding(false);
                assignments.Remove(movement);
            }
        }
        if (assignments.Count <= 0) {
            task = null;
        }
    }

    public void moveCohort (GameObject goTo) {
        task = new Task (null, goTo, Task.actions.move);
        List<Unit_local> thisIsToSupressWarnings = new List<Unit_local>(members);
        foreach (Unit_local toMove in thisIsToSupressWarnings) {
            if (toMove.stats.isMobile) {
                Task moveTask = new Task(toMove.gameObject, goTo, Task.actions.move);
                toMove.work(moveTask);
                assignments.Add(moveTask);        
            }
            else {
                toMove.changeCohort();
                toMove.deactivate();
            }
        }
    }

    public void removeMember (Unit_local reject) {
        members.Remove(reject);
        assignments.Remove(reject.task);
        if (reject.stats.isArmed) {
            armedMembers.Remove(reject);
        }
        Debug.Log("removed a member from the cohort. there are now " + members.Count + " members");
    }

    public void taskCompleted (Task completedTask) {
        assignments.Remove(completedTask);
        Unit_local worker = completedTask.subjectUnit.GetComponent<Unit_local>();
        switch (completedTask.nature) {
            case Task.actions.give:
                if (worker.meat > 0 && remainingToAccept.Count > 0) {
                    assignTransactionWork(worker);
                }
                break;
            case Task.actions.take:
                if (worker.roomForMeat() > 0 && remainingToProvide.Count > 0) {
                    assignTransactionWork(worker);
                }
                break;
            case Task.actions.move:
                 haltCohort();
                break;
            default:
                break;
        }
    }
    
}