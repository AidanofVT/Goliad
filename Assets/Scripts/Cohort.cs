using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Cohort {

    GameState gameState = GameObject.Find("Goliad").GetComponent<GameState>();
    public List<Unit_local> members = new List<Unit_local>();
    public List<Unit_local> armedMembers = new List<Unit_local>();
    public List<Unit_local> mobileMembers = new List<Unit_local>();
    public List<Unit_local> depotMembers = new List<Unit_local>();
    public List<Unit_local> shepherdMembers = new List<Unit_local>();
    Task masterTask;
    public List<Task> assignments = new List<Task>();
    Hashtable remainingToProvide = new Hashtable();
    Hashtable remainingToAccept = new Hashtable();
    int spawnLocationCycler = 0;
    
    public Cohort (List<Unit_local> recruits = null) {
        if (recruits != null) {
            foreach (Unit_local unit in recruits) {
                unit.changeCohort(this);
            }        
        }
        gameState.activeCohortsChangedFlag = true;
        //Debug.Log("cohort created with " + members.Count + " members");
    }

    public void activate () {
        foreach (Unit_local member in members) {
            member.activate();
        }
        gameState.activeCohorts.Add(this);
        gameState.activeCohortsChangedFlag = true;
    }

    public void addMember (Unit_local recruit) {
        members.Add(recruit);
        if (recruit.stats.isArmed) {
            armedMembers.Add(recruit);
        }
        else if (recruit.name.Contains("depot")) {
            depotMembers.Add(recruit);
        }
        else if (recruit.name.Contains("shepherd")) {
            shepherdMembers.Add(recruit);
        }
        if (recruit.stats.isMobile) {
            mobileMembers.Add(recruit);
        }
    }

    public bool assignTransactionWork (Unit_local baseParty, bool assignByTarget = false) {
        float bestDistance = 999999;
        Hashtable counterParties = null;
        Unit_local otherOne = null;
        if (masterTask.nature == Task.actions.give ^ assignByTarget == true) {
            counterParties = remainingToAccept;
        }
        else {
            counterParties = remainingToProvide;
        }
        foreach (Unit_local reciever in counterParties.Keys) {
            Unit_local maybeThis = reciever;
            float distanceTo = Vector2.Distance(maybeThis.transform.position, baseParty.transform.position);
            if (distanceTo < bestDistance) {
                otherOne = maybeThis;
                bestDistance = distanceTo;
            }
        }
        Task newTask;
        Unit_local actor = baseParty;
        Unit_local target = otherOne;
        if (assignByTarget == true) {
            actor = otherOne;
            target = baseParty;
        }
        if (masterTask.nature == Task.actions.give) {
            if ((int) remainingToAccept[target] <= actor.meat) {
                newTask = new Task(actor.gameObject, Task.actions.give, Vector2.zero, target.gameObject, (int) remainingToAccept[target]);
                remainingToProvide[actor] = (int) remainingToProvide[actor] - (int) remainingToAccept[target];
                remainingToAccept.Remove(target);
            }
            else{
                newTask = new Task(actor.gameObject, Task.actions.give, Vector2.zero, target.gameObject, (int) remainingToProvide[actor]);
                remainingToAccept[target] = (int) remainingToAccept[target] - (int) remainingToProvide[actor];                   
                remainingToProvide.Remove(actor);
            }
        }
        else {
            if ((int) remainingToAccept[actor] <= target.meat) {
                newTask = new Task(actor.gameObject, Task.actions.take, Vector2.zero, target.gameObject, (int) remainingToAccept[actor]);
                remainingToProvide[target] = (int) remainingToProvide[target] - (int) remainingToAccept[actor];
                remainingToAccept.Remove(actor);
            }
            else{
                newTask = new Task(actor.gameObject, Task.actions.take, Vector2.zero, target.gameObject, (int) remainingToProvide[target]);
                remainingToAccept[actor] = (int) remainingToAccept[actor] - (int) remainingToProvide[target];                   
                remainingToProvide.Remove(target);
            }
        }
        actor.work(newTask);
        assignments.Add(newTask);
        return true;
    }

    public int collectiveMeat () {
        int collection = 0;
        foreach (Unit meatHolder in members) {
            collection += meatHolder.meat;
        }
        return collection;
    }

    public void chime () {
        foreach (Unit member in shepherdMembers) {
            member.GetComponent<ShepherdFunction>().chime();
        }
    }

    public void commenceAttack (GameObject getIt) {
        Stop();
        Debug.Log("cohort attacking " + getIt.name);
        foreach (Unit_local unit in armedMembers) {
            Debug.Log("instructing " + unit.name + " to attack");
            unit.task = new Task(unit.gameObject, Task.actions.attack, Vector2.zero, getIt);
            unit.attack(getIt);
        }
    }

    public void commenceTransact (Task transaction) {
        Stop();
        masterTask = transaction;
        Cohort from;
        Cohort to;
        if (masterTask.nature == Task.actions.give) {
            from = this;
            to = masterTask.objectUnit.GetComponent<Unit>().cohort;
        }
        else {
            from = masterTask.objectUnit.GetComponent<Unit>().cohort;
            to = this;
        }
        foreach (Unit_local giver in from.members) {
            if (giver.meat > 0) {
                remainingToProvide.Add(giver, giver.meat);
            }
        }
        Debug.Log("there are " + remainingToProvide.Count + " providers");
        foreach (Unit_local recipient in to.members) {
            if (recipient.roomForMeat() > 0) {
                remainingToAccept.Add(recipient, recipient.roomForMeat());
            }
        }
        Hashtable workers;
        if (masterTask.nature == Task.actions.give) {
            workers = new Hashtable(remainingToProvide);
        }
        else {
            workers = new Hashtable(remainingToAccept);
        }
        foreach (Unit_local worker in workers.Keys) {
            if (remainingToProvide.Count <= 0 || remainingToAccept.Count <= 0) {
                break;
            }
            assignTransactionWork(worker);
        }
    }

    public void deactivate () {
        foreach (Unit_local member in members) {
            member.deactivate();
        }
        gameState.activeCohorts.Remove(this);
        gameState.activeCohortsChangedFlag = true;
    }

    public void Brake () {
        if (masterTask.nature != Task.actions.move) {
            Debug.Log("PROBLEM: Halt command called on a cohort that's not supposed to be moving!");
        }
        List<Task> thisIsToSupressWarnings = new List<Task>(assignments);
        foreach (Task movement in thisIsToSupressWarnings) {
            float toGo = Vector2.Distance(movement.subjectUnit.transform.position, movement.center);
            if (toGo < Mathf.Pow(members.Count, 0.5f)) {
                movement.subjectUnit.GetComponent<AidansMovementScript>().terminatePathfinding(false);
                assignments.Remove(movement);
            }
        }
        if (assignments.Count <= 0) {
            masterTask = null;
        }
    }

    public void makeUnit (string unitType, int batchSize = 1) {
        unitType = "Units/" + unitType;
        int expense = ((GameObject)Resources.Load(unitType)).GetComponent<UnitBlueprint>().costToBuild;
        int purse = collectiveMeat();
        if (Input.GetButton("modifier") == true) {
            batchSize = Mathf.Clamp(purse / expense, 0, 6);
        }
        expense *= batchSize;
        int share = Mathf.CeilToInt(expense / members.Count);
        int covered = 0;
        int loopBreaker = 100;
        int loopItterator = 0;
        while (covered <= expense) {
            int ask = Mathf.Clamp(expense - covered, 0, share);
            if (members[loopItterator].deductMeat(ask) == true) {                
                covered += share;
            }
            else {
                int scrapeBottom = members[loopItterator].meat;
                members[loopItterator].deductMeat(scrapeBottom);
                covered += scrapeBottom;
            }
            if (++loopItterator >= members.Count) {
                loopItterator = 0;
            }
            if (loopBreaker <= 0) {
                Debug.Log("PROBLEM: infinite loop in cohort.makeUnit. Loop broken.");
                return;
            }
        }
        Vector3 [] spots = spawnLocations(unitType);
        while (batchSize > 0 && loopBreaker < 1000) {
//In the future, there needs to be a mechanism to detect whether the space around the factory is obstructed, and probably to move those obstructing units. I'd suggest making makeUnit return a boolean
//which will be false as long as the space is obstructed, and then have the ordering method handle the subsiquent calls and the moving of units.
            try {
                PhotonNetwork.Instantiate(unitType, spots[spots.Length - batchSize], Quaternion.identity);
            }
            catch {
                Debug.Log("Caught it: " + batchSize);
            }
            ++spawnLocationCycler;
            --batchSize;
            ++loopBreaker;
        }
    }

    public void moveCohort (Vector2 goTo, GameObject follow) {
        Stop();
        masterTask = new Task (null, Task.actions.move, goTo, follow);
        List<Unit_local> thisIsToSupressWarnings = new List<Unit_local>(members);
        Task moveTask;
        float longestETA = 0;
        foreach (Unit_local toMove in thisIsToSupressWarnings) {
            if (mobileMembers.Contains(toMove)) {
                moveTask = new Task(toMove.gameObject, Task.actions.move, goTo, follow);
                toMove.work(moveTask);
                assignments.Add(moveTask);
                float ETA = Vector2.Distance(toMove.transform.position, goTo) / toMove.GetComponent<UnitBlueprint>().speed;
                if (ETA > longestETA) {
                    longestETA = ETA;
                }     
            }
            else {
                toMove.changeCohort();
                toMove.deactivate();
            }
        }
        foreach (Unit_local mover in mobileMembers) {
            mover.GetComponent<AidansMovementScript>().speed = Mathf.Clamp(Vector2.Distance(mover.transform.position, goTo) / longestETA, 1, mover.GetComponent<UnitBlueprint>().speed);
        }
    }

    public void MoveCohortBeta (Vector2 goTo, GameObject toFollow) {
        Stop();
        masterTask = new Task (null, Task.actions.move, goTo, toFollow);
        List<Unit_local> thisIsToSupressWarnings = new List<Unit_local>(members);
        foreach (Unit_local toMove in thisIsToSupressWarnings) {
            if (mobileMembers.Contains(toMove) == false) {
                toMove.changeCohort();
                toMove.deactivate();
            }
        }
        UnitPositionSorter vsGoTo = new UnitPositionSorter(goTo);
        vsGoTo.DirectionMode();
        List <Unit_local> unitsByDirection = new List<Unit_local>(members);
        unitsByDirection.Sort(vsGoTo);
        vsGoTo.DistanceMode();
        List <Unit_local> unitsByDistance = new List<Unit_local>(members);
        unitsByDistance.Sort(vsGoTo);
        while (unitsByDirection.Count > 0 && unitsByDistance.Count > 0) {
            Unit_local sliceLeader = unitsByDistance[0];
            int leaderDirectionIndex = unitsByDirection.IndexOf(sliceLeader);
            float leaderDistanceFromDestination = vsGoTo.DistanceOf(sliceLeader);
            float leaderRadius = sliceLeader.GetComponent<CircleCollider2D>().radius;
            int totalUnaccounted = unitsByDirection.Count;
            List<Unit_local> slice = new List<Unit_local> {sliceLeader};
            for (int sign = -1; sign <= 1 && slice.Count < unitsByDirection.Count; sign = sign + 2) {
                // this is a loop-breaker variable
                for (int indexOffset = sign; indexOffset < 1000; indexOffset += sign) {
                    Unit_local inQuestion = unitsByDirection[(leaderDirectionIndex + indexOffset + totalUnaccounted) % totalUnaccounted];
                    float directionOfLeader = vsGoTo.DirectionOf(sliceLeader);
                    float directionInQuestion = vsGoTo.DirectionOf(inQuestion);
                    float circumferencialDistance = Mathf.Abs(directionOfLeader - directionInQuestion);
                    circumferencialDistance = Mathf.Min(circumferencialDistance, 2 * Mathf.PI - circumferencialDistance);
                    circumferencialDistance *= leaderDistanceFromDestination;
                    if (circumferencialDistance < leaderRadius && inQuestion != sliceLeader) {
                        slice.Add(inQuestion);
                    }
                    else {
                        break;
                    }
                }
            }
            slice.Sort(vsGoTo);
            sliceLeader.work(new Task(sliceLeader.gameObject, Task.actions.move, goTo, toFollow));
            for (int followerIndex = 1; followerIndex < slice.Count; ++followerIndex) {
                slice[followerIndex].work(new Task(slice[followerIndex].gameObject, Task.actions.move, goTo, slice[followerIndex - 1].gameObject));
            }
            foreach (Unit_local sliceMember in slice) {
                assignments.Add(sliceMember.task);
                unitsByDirection.Remove(sliceMember);
                unitsByDistance.Remove(sliceMember);
            }    
        }
    }

    public void removeMember (Unit_local reject) {
        members.Remove(reject);
        assignments.Remove(reject.task);
        armedMembers.Remove(reject);
        mobileMembers.Remove(reject);
        depotMembers.Remove(reject);
        shepherdMembers.Remove(reject);
        gameState.activeCohortsChangedFlag = true;
    }

    public void Slaughter () {
        foreach (Unit_local individual in depotMembers) {
            individual.GetComponent<factory_functions>().slaughterSheep();
        }
    }

    Vector3 [] spawnLocations (string type) {
//In the future, this should account for things like obstructing terrain, and also the size of the unit being created.
        Vector3 [] toReturn = new Vector3[6];
        Vector3 spawnSpot = Vector3.zero;
        float unitRadius = ((GameObject)Resources.Load(type)).GetComponent<CircleCollider2D>().radius;
        for (int i = 0; i < members.Count; ++i) {
            spawnSpot += members[i].transform.position;
        }
        spawnSpot /= members.Count;
        for (int i = 0; i < 6; ++i) {
            float distanceAlongCircumference = spawnLocationCycler * Mathf.PI / 3f;
            Vector2 direction = new Vector2 (Mathf.Sin(distanceAlongCircumference), Mathf.Cos(distanceAlongCircumference));
            Vector3 result = spawnSpot + new Vector3(direction.x, direction.y, 0) * unitRadius * 2;
            result.z = -.2f;
            toReturn[i] = result;
            if (++spawnLocationCycler >= 6) {
                spawnLocationCycler = 0;
            }
        }
        return toReturn;
    }

    public void Stop () {
        foreach (Unit_local member in members) {
            member.Stop();
        }
        masterTask = null;
        assignments.Clear();
        remainingToAccept.Clear();
        remainingToProvide.Clear();
    }

    public void taskCompleted (Task completedTask) {
        assignments.Remove(completedTask);
        Unit_local worker = completedTask.subjectUnit.GetComponent<Unit_local>();
        switch (completedTask.nature) {
            case Task.actions.give:
                if (worker.meat > 0 && remainingToAccept.Count > 0) {
                    if (remainingToProvide.Contains(worker) == false) {
                        remainingToProvide.Add(worker, worker.meat);
                    }
                    assignTransactionWork(worker);
                }
                break;
            case Task.actions.take:
                if (worker.roomForMeat() > 0 && remainingToProvide.Count > 0) {
                    if (remainingToAccept.Contains(worker) == false) {
                        remainingToAccept.Add(worker, worker.roomForMeat());
                    }
                    assignTransactionWork(worker);
                }
                break;
            case Task.actions.move:
                if (completedTask.objectUnit == null) {
                    Brake();
                }
                break;
            default:
                break;
        }
    }

    public void TaskAbandoned (Task interruptedTask, int quantityDone) {
        Unit_local party;
        Unit_local counterParty;
        party = interruptedTask.subjectUnit.GetComponent<Unit_local>();
        counterParty = interruptedTask.objectUnit.GetComponent<Unit_local>();
        if (interruptedTask.nature == Task.actions.give) {
            remainingToProvide.Remove(party);
            if (remainingToAccept.Contains(counterParty)) {
                remainingToAccept[counterParty] = (int) remainingToAccept[counterParty] - quantityDone;
            }
            else {
                // THIS IS JUST AN ESTIMATE. There's no good way to know how much meat is en-route to the target.
                remainingToAccept.Add(counterParty, counterParty.stats.meatCapacity - quantityDone);
            }
        }
        else if (interruptedTask.nature == Task.actions.take) {
            remainingToAccept.Remove(party);
            if (remainingToProvide.Contains(counterParty)) {
                remainingToProvide[counterParty] = (int) remainingToProvide[counterParty] - quantityDone;
            }
            else {
                // THIS IS JUST AN ESTIMATE. There's no good way to know how much meat is en-route to the target.
                remainingToProvide.Add(counterParty, counterParty.meat);
            }
        }
        if (remainingToAccept.Count > 0 && remainingToProvide.Count > 0) {
            assignTransactionWork(counterParty, true);
        }
    }
    
}