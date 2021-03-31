using System;
using System.Threading.Tasks;
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
    List <Unit> remainingToPerish = new List<Unit>();
    int spawnLocationCycler = 0;
    GameObject areaAttackSensor;
    
    public Cohort (List<Unit_local> recruits = null) {
        if (recruits != null) {
            foreach (Unit_local unit in recruits) {
                unit.changeCohort(this);
            }        
        }
        //Debug.Log("cohort created with " + members.Count + " members");
    }

    public void activate () {
        foreach (Unit_local member in members) {
            member.activate();
        }
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
        // Debug.Log("Added " + recruit.name + ". Now " + members.Count + " members.");
    }

    public bool assignTransactionWork (Unit_local baseParty, bool assignByTarget = false) {
        // Debug.Log("AssignTransactionWork");
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
                newTask = new Task(actor, Task.actions.give, Vector2.zero, target, (int) remainingToAccept[target]);
                remainingToProvide[actor] = (int) remainingToProvide[actor] - (int) remainingToAccept[target];
                remainingToAccept.Remove(target);
            }
            else{
                newTask = new Task(actor, Task.actions.give, Vector2.zero, target, (int) remainingToProvide[actor]);
                remainingToAccept[target] = (int) remainingToAccept[target] - (int) remainingToProvide[actor];                   
                remainingToProvide.Remove(actor);
            }
        }
        else {
            if ((int) remainingToAccept[actor] <= target.meat) {
                newTask = new Task(actor, Task.actions.take, Vector2.zero, target, (int) remainingToAccept[actor]);
                remainingToProvide[target] = (int) remainingToProvide[target] - (int) remainingToAccept[actor];
                remainingToAccept.Remove(actor);
            }
            else{
                newTask = new Task(actor, Task.actions.take, Vector2.zero, target, (int) remainingToProvide[target]);
                remainingToAccept[actor] = (int) remainingToAccept[actor] - (int) remainingToProvide[target];                   
                remainingToProvide.Remove(target);
            }
        }
        actor.work(newTask);
        assignments.Add(newTask);
        return true;
    }

    void assignViolentWork (Unit_local worker) {
        UnitRelativePositionSorter comparer = new UnitRelativePositionSorter(worker.transform.position);
        comparer.DistanceMode();
        remainingToPerish.Sort(comparer);
        List<Unit> inRange = new List<Unit>();
        Unit closestHoplite = null;
        Unit closestDog = null;
        Unit closestCourier = null;
        for (int i = 0; i < remainingToPerish.Count && comparer.DistanceOf(remainingToPerish[i]) <= worker.stats.weapon_range; ++i) {
            inRange.Add(remainingToPerish[i]);
            if (closestHoplite == null && remainingToPerish[i].name.Contains("hoplite")) {
                closestHoplite = remainingToPerish[i];
            }
            else if (closestDog == null && remainingToPerish[i].name.Contains("dog")) {
                closestDog = remainingToPerish[i];
            }
            else if (closestCourier == null && remainingToPerish[i].name.Contains("courier")) {
                closestCourier = remainingToPerish[i];
            }
        }
        Unit targetUnit = null;
        if (closestHoplite != null) {
            targetUnit = closestHoplite;
        }
        else if (closestDog != null) {
            targetUnit = closestDog;
        }
        else if (closestCourier != null) {
            targetUnit = closestCourier;
        }
        else {
            targetUnit = remainingToPerish[0];
        }
        Task result = new Task(worker, Task.actions.attack, targetUnit.transform.position, targetUnit);
        worker.work(result);
        assignments.Add(result);
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

    public void chime () {
        foreach (Unit member in shepherdMembers) {
            member.GetComponent<ShepherdFunction>().chime();
        }
    }

    public int collectiveMeat () {
        int collection = 0;
        foreach (Unit meatHolder in members) {
            collection += meatHolder.meat;
        }
        return collection;
    }

    public void commenceAttack (Task attackWork) {
        // Debug.Log("cohort attacking " + getIt.name);
        masterTask = attackWork;
        if (armedMembers.Count > 0) {
            Stop();
            foreach (Unit_local member in members){
                if (member.stats.isArmed == false) {
                        member.changeCohort();
                        member.deactivate();
                }
            }
            if (attackWork.radius == 0) {
// ProcessTargetingCandidate isn't necessary because that logic is implicit in the InputHandler's response to single unit being clicked on.
                remainingToPerish.Add(attackWork.objectUnit);
                attackWork.objectUnit.cohortsAttackingThisUnit.Add(this);
            }
            else {
                Collider2D [] inTargetArea = Physics2D.OverlapCircleAll(attackWork.center, attackWork.radius);
                foreach (Collider2D contact in inTargetArea) {
                    ProcessTargetingCandidate(contact.gameObject);
                }
                areaAttackSensor = (GameObject) GameObject.Instantiate(Resources.Load("Sensor"), attackWork.center, Quaternion.identity);
                areaAttackSensor.GetComponent<SensorBridge>().Setup(this, attackWork.radius);
            }
            foreach (Unit_local attacker in members) {
                if (remainingToPerish.Count <= 0) {
                    break;
                }
                assignViolentWork(attacker);
            }
            
        }
    }

    // public void CommenceHelp () {
// Units should be allocated to tasks with the following order of priorities (1) Attack master's target, (2) Defend master from attackers, (3) Refill ranged units with attack orders, (4) Herd sheep, (5) Move with master.
// Commenceattack(), CommenceTransact(), and MoveCohort() will need slight rewrites to allow different actions within the same cohort.
    // }

    public void commenceTransact (Task transaction) {
        // Debug.Log("CommenceTransact");
        Stop();
        masterTask = transaction;
        Cohort from;
        Cohort to;
        if (transaction.nature == Task.actions.give) {
            from = this;
            to = transaction.objectUnit.GetComponent<Unit>().cohort;
        }
        else {
            from = transaction.objectUnit.GetComponent<Unit>().cohort;
            to = this;
        }
        List<Unit_local> validProviders = null;
        List<Unit_local> validRecievers = null;
        if (to == from) {
            Unit_local singledOut = (Unit_local) transaction.objectUnit;
            if (transaction.nature == Task.actions.take && singledOut.meat > 0) {
                validProviders = new List<Unit_local>(new Unit_local[] {singledOut});
                validRecievers = new List<Unit_local>(members);
                validRecievers.Remove(singledOut);
            }
            else if (transaction.nature == Task.actions.give && singledOut.roomForMeat() > 0) {
                validRecievers = new List<Unit_local>(new Unit_local[] {singledOut});
                validProviders = new List<Unit_local>(members);
                validProviders.Remove(singledOut);
            }
        }
        else {
            validProviders = from.members;
            validRecievers = to.members;
        }
        foreach (Unit_local giver in validProviders) {
            if (giver.meat > 0 && remainingToAccept.ContainsKey(giver) == false) {
                remainingToProvide.Add(giver, giver.meat);
            }
        }
        foreach (Unit_local recipient in validRecievers) {
            if (recipient.roomForMeat() > 0) {
                remainingToAccept.Add(recipient, recipient.roomForMeat());
            }
        }
        Hashtable workers;
        if (transaction.nature == Task.actions.give) {
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
    }

    public void Highlight () {
        foreach (Unit_local member in members) {
            member.Highlight();
        }
    }

    public void HighlightOff () {
        foreach (Unit_local member in members) {
            member.Unhighlight();
        }
    }

    public IEnumerator makeUnit (string unitType, int batchSize = 1) {
        string unitAddress = "Units/" + unitType;
        int expense = ((GameObject)Resources.Load(unitAddress)).GetComponent<UnitBlueprint>().costToBuild;
        int purse = collectiveMeat();
        if (Input.GetButton("modifier") == true) {
            batchSize = Mathf.Clamp(purse / expense, 0, 6);
        }
        expense *= batchSize;
        int share = Mathf.CeilToInt(expense / members.Count);
        int covered = 0;
        int loopBreaker = 0;
        int loopItterator = 0;
        while (covered < expense) {
            int ask = Mathf.Clamp(expense - covered, 0, share);
            if (members[loopItterator].meat >= ask) {                
                members[loopItterator].photonView.RPC("deductMeat", RpcTarget.All, share);
                covered += share;
            }
            else {
                int scrapeBottom = members[loopItterator].meat;
                members[loopItterator].deductMeat(scrapeBottom);
                members[loopItterator].photonView.RPC("deductMeat", RpcTarget.All, scrapeBottom);
                covered += scrapeBottom;
            }
            if (++loopItterator >= members.Count) {
                loopItterator = 0;
            }
            if (++loopBreaker >= 1000) {
                Debug.Log("PROBLEM: infinite loop in cohort.makeUnit. Loop broken.");               
                break;
            }
        }
        Vector3 [] spots = spawnLocations(unitAddress);
        List<GameObject> batch = new List<GameObject>();
        for (int i = 0; i < batchSize; ++i) {
//In the future, there needs to be a mechanism to detect whether the space around the factory is obstructed, and probably to move those obstructing units. I'd suggest making makeUnit return a boolean
//which will be false as long as the space is obstructed, and then have the ordering method handle the subsiquent calls and the moving of units.
            GameObject justCreated = PhotonNetwork.Instantiate(unitAddress, spots[i], Quaternion.identity);
            batch.Add(justCreated);
            spawnLocationCycler = (spawnLocationCycler + 1) % 6;
            if (++loopBreaker >= 1000) {
                Debug.Log("PROBLEM: infinite loop in cohort.makeUnit. Loop broken.");
                yield return null;
            }
        }
        yield return new WaitForSeconds(0);
        if (unitType != "sheep"){ 
            Cohort cohortToJoin;
            if (this == members[0].soloCohort) {
                cohortToJoin = new Cohort(new List<Unit_local>(new Unit_local[] {members[0]}));
            }
            else {
                cohortToJoin = this;
            }
            for (int i = 0; i < batchSize; ++i) {
                batch[i].GetComponent<Unit_local>().changeCohort(cohortToJoin);
            }
            cohortToJoin.activate();
        }
        yield return null;
    }

    public void MoveCohort (Vector2 goTo, Unit_local toFollow) {
        Stop();
        masterTask = new Task (null, Task.actions.move, goTo, toFollow);
        List<Unit_local> thisIsToSupressWarnings = new List<Unit_local>(members);
        float weakLinkETA = 0;
        foreach (Unit_local toMove in thisIsToSupressWarnings) {
            if (toMove.stats.isMobile == false) {
                throw new InvalidOperationException("Cannot move a cohort that includes immobile members.");
            }
            else {
                float thisMoversETA = Vector2.Distance(toMove.transform.position, goTo) / toMove.stats.speed;
                if (thisMoversETA > weakLinkETA) {
                    weakLinkETA = thisMoversETA;
                }
            }
        }
        UnitRelativePositionSorter vsGoTo = new UnitRelativePositionSorter(goTo);
        vsGoTo.DirectionMode();
        List <Unit_local> unitsByDirection = new List<Unit_local>(members);
        (unitsByDirection).Sort(vsGoTo);
        vsGoTo.DistanceMode();
        List <Unit_local> unitsByDistance = new List<Unit_local>(members);
        unitsByDistance.Sort(vsGoTo);
        while (unitsByDirection.Count > 0 && unitsByDistance.Count > 0) {
            Unit_local sliceLeader = unitsByDistance[0];
            int leaderDirectionIndex = unitsByDirection.IndexOf(sliceLeader);
            float leaderDistanceFromDestination = vsGoTo.DistanceOf(sliceLeader);
            float leaderRadius = sliceLeader.bodyCircle.radius;
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
            sliceLeader.work(new Task(sliceLeader, Task.actions.move, goTo, toFollow));
// This line prevents faster units from running ahead.
            ((MobileUnit_local) sliceLeader).moveConductor.speed = Vector2.Distance(sliceLeader.transform.position, goTo) / weakLinkETA;
            for (int followerIndex = 1; followerIndex < slice.Count; ++followerIndex) {
                slice[followerIndex].work(new Task(slice[followerIndex], Task.actions.move, goTo, slice[followerIndex - 1]));
            }
            foreach (Unit_local sliceMember in slice) {
                assignments.Add(sliceMember.task);
                unitsByDirection.Remove(sliceMember);
                unitsByDistance.Remove(sliceMember);
            }    
        }
    }

    public void ProcessTargetingCandidate (GameObject inTargetArea) {
// Ugly, but necessary because sheep PUN ownership does not change when the sheep change factions.
        if (inTargetArea.tag == "unit" &&
        (inTargetArea.GetPhotonView().Owner != PhotonNetwork.LocalPlayer
        || (inTargetArea.name.Contains("sheep") && inTargetArea.GetComponent<SheepBehavior_Base>().alliedFaction != PhotonNetwork.LocalPlayer.ActorNumber))) {
            Unit confirmedTarget = inTargetArea.GetComponent<Unit>();
            remainingToPerish.Add(confirmedTarget);
            confirmedTarget.cohortsAttackingThisUnit.Add(this);
        }
    }

    public void removeMember (Unit_local reject) {
        members.Remove(reject);
        assignments.Remove(reject.task);
        armedMembers.Remove(reject);
        mobileMembers.Remove(reject);
        depotMembers.Remove(reject);
        shepherdMembers.Remove(reject);
        // Debug.Log("Removed " + reject.name + ". Now " + members.Count + " members.");
    }

    public void Slaughter () {
        foreach (Unit_local individual in depotMembers) {
            individual.GetComponent<DepotFunction>().slaughterSheep();
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
            spawnLocationCycler = (spawnLocationCycler + 1) % 6;
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
        remainingToPerish.Clear();
    }

    public void TargetDown (Unit slain) {
        remainingToPerish.Remove(slain);
        List <Unit_local> reassignable = new List<Unit_local>();
        foreach (Task attackOrder in assignments) {
            if (attackOrder.objectUnit == slain) {
                attackOrder.subjectUnit.Stop();
                reassignable.Add(attackOrder.subjectUnit);
            }
        }
        if (remainingToPerish.Count > 0) {
            foreach (Unit_local reassigned in reassignable) {
                assignViolentWork(reassigned);
            }
        }
        else {
            Stop();
        }
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
            case Task.actions.attack:
                remainingToPerish.Remove(completedTask.objectUnit);
                if (remainingToPerish.Count > 0) {
                    assignViolentWork(completedTask.subjectUnit);
                }
                else {
                    Stop();
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