using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Cohort {

    public List<Unit_local> members = new List<Unit_local>();
    public List<Unit_local> armedMembers = new List<Unit_local>();
    public List<Unit_local> mobileMembers = new List<Unit_local>();
    public List<Unit_local> depotMembers = new List<Unit_local>();
    public List<Unit_local> shepherdMembers = new List<Unit_local>();
    public Task masterTask;
    public List<Task> assignments = new List<Task>();
    Hashtable remainingToProvide = new Hashtable();
    Hashtable remainingToAccept = new Hashtable();
    List <Unit> remainingToPerish = new List<Unit>();
    int spawnLocationCycler = 0;
    SensorBridge areaAttackSensor;
    
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

// This should not be called except by ChangeCohort() in Unit_local
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

    public bool assignTransactionWork (Unit_local needsCounterparty, bool assignByTarget = false) {
        // Debug.Log("AssignTransactionWork");
        Hashtable counterParties = null;
        Unit_local closestCounterparty = null;
// We have to get a little weird with these, because closestCounterparty, which one of these two things will reference, can't be decided until we determine what the counterparties are,
// which is done in the same IF statement that determines what the provider is. I know it's weird.
        Func<Unit_local> provider;
        Func<Unit_local> reciever;
        if (masterTask.nature == Task.actions.give ^ assignByTarget == true) {
            counterParties = remainingToAccept;            
            provider = () => needsCounterparty;
            reciever = () => closestCounterparty;
        }
        else {
            counterParties = remainingToProvide;
            provider = () => closestCounterparty;
            reciever = () => needsCounterparty;
        }
        float bestDistance = 999999;
        foreach (Unit_local recieverCandidate in counterParties.Keys) {
            float distanceTo = Vector2.Distance(recieverCandidate.transform.position, needsCounterparty.transform.position);
            if (distanceTo < bestDistance) {
                closestCounterparty = recieverCandidate;
                bestDistance = distanceTo;
            }
        }
        Hashtable leftoverFactor;
        Hashtable exhaustedFactor;
        Unit_local unitWithLeftovers;
        Unit_local limitingUnit;
        int quantityToPass = 0;
        if (reciever().roomForMeat() == provider().meat) {
            quantityToPass = provider().meat;
            remainingToAccept.Remove(reciever);
            remainingToProvide.Remove(provider());
        }
        else {
            if (reciever().roomForMeat() < provider().meat) {
                leftoverFactor = remainingToProvide;
                exhaustedFactor = remainingToAccept;
                limitingUnit = reciever();
                unitWithLeftovers = provider();
                quantityToPass = reciever().roomForMeat();
            }
            else {
                leftoverFactor = remainingToAccept;
                exhaustedFactor = remainingToProvide;
                limitingUnit = provider();
                unitWithLeftovers = reciever();
                quantityToPass = provider().meat;
            }
            leftoverFactor[unitWithLeftovers] = (int) leftoverFactor[unitWithLeftovers] - (int) exhaustedFactor[limitingUnit];
            exhaustedFactor.Remove(limitingUnit);
        }
        Task newTask;
        if (assignByTarget == false) {
            newTask = new Task(needsCounterparty, masterTask.nature, Vector2.zero, closestCounterparty, quantityToPass);
            needsCounterparty.work(newTask);
        }
        else {
            newTask = new Task(closestCounterparty, masterTask.nature, Vector2.zero, needsCounterparty, quantityToPass);
            closestCounterparty.work(newTask);
        }
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
        Unit targetUnit;
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
                movement.subjectUnit.photonView.RPC("StopMoving", RpcTarget.All, true);
                assignments.Remove(movement);
            }
        }
        if (assignments.Count <= 0) {
            masterTask = null;
        }
    }

    public void chimeAll () {
        foreach (Unit member in shepherdMembers) {
            member.GetComponent<ShepherdFunction>().chime();
        }
    }

    public int collectiveMeat () {
        int sum = 0;
        foreach (Unit meatHolder in members) {
            sum += meatHolder.meat;
        }
        return sum;
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
            if (attackWork.dataA == 0) {
// ProcessTargetingCandidate isn't necessary because that logic is implicit in the InputHandler's response to single unit being clicked on.
                remainingToPerish.Add(attackWork.objectUnit);
                attackWork.objectUnit.cohortsAttackingThisUnit.Add(this);
            }
            else {
                Collider2D [] inTargetArea = Physics2D.OverlapCircleAll(attackWork.center, attackWork.dataA);
                foreach (Collider2D contact in inTargetArea) {
                    ProcessTargetingCandidate(contact.gameObject);
                }
                GameObject newSensor = (GameObject) GameObject.Instantiate(Resources.Load("Sensor"), attackWork.center, Quaternion.identity);
                areaAttackSensor = newSensor.GetComponent<SensorBridge>();
                areaAttackSensor.Setup(this, attackWork.dataA);
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
        List<Unit_local> providerCandidates = null;
        List<Unit_local> recieverCandidates = null;
        if (to == from) {
            Unit_local singledOut = (Unit_local) transaction.objectUnit;
            if (transaction.nature == Task.actions.take) {
                providerCandidates = new List<Unit_local>(new Unit_local[] {singledOut});
                recieverCandidates = new List<Unit_local>(members);
                recieverCandidates.Remove(singledOut);
            }
            else if (transaction.nature == Task.actions.give) {
                recieverCandidates = new List<Unit_local>(new Unit_local[] {singledOut});
                providerCandidates = new List<Unit_local>(members);
                providerCandidates.Remove(singledOut);
            }
        }
        else {
            providerCandidates = from.members;
            recieverCandidates = to.members;
        }
        foreach (Unit_local giver in providerCandidates) {
// Watch out for malfunctions: I can't remember why I had that second criteria, so I'm removing it to see what breaks. Make a note if you find out.
            if (giver.meat > 0) { // && remainingToAccept.ContainsKey(giver) == false) {
                remainingToProvide.Add(giver, giver.meat);
            }
        }
        foreach (Unit_local recipient in recieverCandidates) {
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

    public IEnumerator makeUnit (string unitType) {
        string unitAddress = "Units/" + unitType;
        int expense = ((GameObject)Resources.Load(unitAddress)).GetComponent<UnitBlueprint>().costToBuild;
        int purse = collectiveMeat();
        int orderSize;
        if (Input.GetButton("modifier") == true) {
            orderSize = 6;
        }
        else {
            orderSize = 1;
        }
        int batchSize = Mathf.Clamp(purse / expense, 0, orderSize);
        expense *= batchSize;
        int share = Mathf.CeilToInt(expense / (float) members.Count);
        Debug.Log(share);
        int covered = 0;
        int loopBreaker = 0;
        List<Unit_local> thisIsToSupressWarnings = new List<Unit_local>(members);
        for (int index = 0; covered < expense; index = index % thisIsToSupressWarnings.Count) {
            int ask = Mathf.Clamp(expense - covered, 0, share);
            if (thisIsToSupressWarnings[index].meat > ask) {                
                thisIsToSupressWarnings[index].photonView.RPC("deductMeat", RpcTarget.All, share);
                covered += share;
                // Debug.Log("Covering " + share + " out of " + expense);
                ++index;
            }
            else {
                int scrapeBottom = thisIsToSupressWarnings[index].meat;
                if (scrapeBottom > 0) {
                    thisIsToSupressWarnings[index].photonView.RPC("deductMeat", RpcTarget.All, scrapeBottom);
                    covered += scrapeBottom;
                }
                thisIsToSupressWarnings.RemoveAt(index);
            }
            if (++loopBreaker > 1000) {
                throw new System.Exception("Loop broken with " + thisIsToSupressWarnings.Count + " in the list.");
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
            if (++loopBreaker > 1000) {
                throw new System.Exception("Loop broken.");
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
                Unit_local justMade = batch[i].GetComponent<Unit_local>();
                justMade.changeCohort(cohortToJoin);
                justMade.activate();                
                if (++loopBreaker > 1000) {
                    throw new System.Exception("Loop broken.");
                }
            }
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
                throw new InvalidOperationException("Cannot move a cohort that includes immobile members. Units should be sorted out in Gamestate.CombineActiveCohorts.");
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
// This is a list of units sorted by their compass-direction from the destination point.
        (unitsByDirection).Sort(vsGoTo);
        vsGoTo.DistanceMode();
        List <Unit_local> unitsByDistance = new List<Unit_local>(members);
        unitsByDistance.Sort(vsGoTo);
// Units are removed from unitsByDirection (and unitsByDistance) as they are assigned to groups, so this is a way of saying "while there are unassigned units."
        while (unitsByDirection.Count > 0) {
// We take the current closest unit to the distination... (previous cycles of grouping will have removed closer units)
            Unit_local sliceLeader = unitsByDistance[0];
            int leaderDirectionIndex = unitsByDirection.IndexOf(sliceLeader);
            float leaderDistanceFromDestination = vsGoTo.DistanceOf(sliceLeader);
            float leaderRadius = sliceLeader.bodyCircle.radius;
            int totalUnaccounted = unitsByDirection.Count;
            List<Unit_local> slice = new List<Unit_local> {sliceLeader};
// That unit will be the group leader. We check the units clockwise and counter-clockwise from its' position on the imaginary circle of unit positions aronud the destination.
            for (int sign = -1; sign <= 1 && slice.Count < unitsByDirection.Count; sign = sign + 2) {
                // this is a loop-breaker variable
                for (int indexOffset = sign; indexOffset < 1000; indexOffset += sign) {
                    Unit_local inQuestion = unitsByDirection[(leaderDirectionIndex + indexOffset + totalUnaccounted) % totalUnaccounted];
// These are all in radians...
                    float directionOfLeader = vsGoTo.DirectionOf(sliceLeader);
                    float directionInQuestion = vsGoTo.DirectionOf(inQuestion);
                    float circumferencialDistance = Mathf.Abs(directionOfLeader - directionInQuestion);
                    circumferencialDistance = Mathf.Min(circumferencialDistance, 2 * Mathf.PI - circumferencialDistance);
// ...until here, when circumferentialDistance becomes a measure of real distance.
                    circumferencialDistance *= leaderDistanceFromDestination;
// In both directions, we stop checking when the next-closest (by direction) unit doesn't fall within the shadow cast by the leader, if you imagine the destination as a light source.
                    if (circumferencialDistance < leaderRadius && inQuestion != sliceLeader) {
                        slice.Add(inQuestion);
                    }
                    else {
                        break;
                    }
                }
            }
            slice.Sort(vsGoTo);
// This line makes it so that all groups arive at the same time.
// PROBLEM: this will make fast units move slugishly for very short journeys.
            float leaderSpeed = Mathf.Clamp(Vector2.Distance(sliceLeader.transform.position, goTo) / weakLinkETA, 0, sliceLeader.stats.speed);
            sliceLeader.work(new Task(sliceLeader, Task.actions.move, goTo, toFollow, -1, leaderSpeed));
            for (int followerIndex = 1; followerIndex < slice.Count; ++followerIndex) {
                slice[followerIndex].work(new Task(slice[followerIndex], Task.actions.move, goTo, sliceLeader));
            }
            foreach (MobileUnit_local sliceMember in slice) {
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
            if (remainingToPerish.Contains(confirmedTarget) == false) {
                remainingToPerish.Add(confirmedTarget);
                // Debug.Log("Added " + confirmedTarget + ".");
                confirmedTarget.cohortsAttackingThisUnit.Add(this);
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
        if (areaAttackSensor != null) {
            areaAttackSensor.TearDown();
        }
        masterTask = null;
        assignments.Clear();
        remainingToAccept.Clear();
        remainingToProvide.Clear();
        remainingToPerish.Clear();
    }

    public void TargetDown (Unit slain) {
        // Debug.Log("Targetdown: " + slain.gameObject);
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