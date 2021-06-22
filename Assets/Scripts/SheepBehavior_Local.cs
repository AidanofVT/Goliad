using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SheepBehavior_Local : SheepBehavior_Base {

    AidansMovementScript legs;
    List<GameObject> flock = new List<GameObject>();
    List<GameObject> farFlock = new List<GameObject>();
    float flockSizeFactor = 1;
    List<Transform> dogs = new List<Transform>();
    Vector2 flockCenter;
// noDogsHere is an offset from teh current positon, not an absolute point.
    Vector2 noDogsHere = Vector2.zero;
    public GameObject shepherd;
    public int shepherdMultiplier = 1;
// This represents the sheep's tendency to idle. If a roll to change behavior doesn't exceed this number, the sheep with idle.
    public int idleMargin = 5;
    short eatTime = 2;
// This actually represents an exact point which should be within a square with grass; it's not the index of the square. 
    Vector3 currentMostAppealingPatch;
    enum sheepBehaviors {idling, goingToFood, chewing, goingToSafety};
    sheepBehaviors sheepState;

    void Awake() {
        legs = gameObject.GetComponent<AidansMovementScript>();
// A positive z value should be used as an indicator that the targetPatch variable is inactive
        currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
        flock.Add(gameObject);
    }

    IEnumerator Start2 () {
        yield return new WaitForSeconds(0.2f);
        thisSheep = GetComponent<NeutralUnit>();
        ChangeFaction(photonView.OwnerActorNr);
        thisSheep.facing = Random.Range(-1, 1);
// The trigger collider has to be off upon instantiation so startup can complete before any contacts are registered.
        GetComponents<Collider2D>()[1].enabled = true;
        StartCoroutine(ForgetFlockMates());
        StartCoroutine(Idle(0));
    }

    IEnumerator CalculateDogRepulsion () {
        while (true) {
            Vector2 totalOffSet = Vector2.zero;
            foreach (Transform pusher in dogs) {
                Vector2 offset = transform.position - pusher.position;
                offset = offset.normalized * Mathf.Pow(15 - Mathf.Clamp(offset.magnitude, 5, 15), 2) / 7;
                if (pusher.gameObject.GetPhotonView().OwnerActorNr != thisSheep.stats.factionNumber) {
                    offset *= 3;
                }            
                totalOffSet += offset;
            }
// This is phrased like this because you can't clamp a Vector's magnitude directly.
            totalOffSet *= Mathf.Clamp(90 / totalOffSet.magnitude, 0, 1);
            noDogsHere = totalOffSet;
            yield return new WaitForSeconds(0.2f);
        }
    }

    void Consume () {
        Vector2Int patchIndex = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
        if (mapManager.ExploitPatch(patchIndex) == true) {
            thisSheep.photonView.RPC("AddMeat", RpcTarget.All, 1);
            gameState.photonView.RPC("ReducePatch", RpcTarget.All, patchIndex.x, patchIndex.y);
        }
        StartCoroutine(Idle(0));
    }    

    bool ChangeBehavior () {
        if (SearchForFood() == true || flock.Count > 1 || shepherd != null || dogs.Count > 0) {
            float eatAppeal;
            if (currentMostAppealingPatch.z > 1) {
                eatAppeal = 0;
            }
            else {
                int grassHeightHere = gameState.GetPatchValue(currentMostAppealingPatch.x, currentMostAppealingPatch.y);
                eatAppeal = grassHeightHere * 3 / (Vector3.Distance(transform.position, currentMostAppealingPatch) / 40);
                eatAppeal = Mathf.Clamp(eatAppeal, 1.5f, 30 * grassHeightHere);
            }
            UpdateFlockCenter();
            float conveneAppeal = shepherdMultiplier * Vector2.Distance(flockCenter, transform.position) / flockSizeFactor + (15 * dogs.Count - noDogsHere.magnitude);
            conveneAppeal = Mathf.Pow(conveneAppeal, 1.7f) / 15;
            conveneAppeal = Mathf.Clamp(conveneAppeal, 0, 100);
            if (conveneAppeal < 4 || legs.IsNavigable(flockCenter + noDogsHere, true) == false) {
                conveneAppeal = 0;
                // if (legs.isNavigable(flockCenter + noDogsHere) == false) {
                //     Debug.Log("flock center obstructed");
                // }
                // if (Vector2.Distance(flockCenter, transform.position) < 6) {
                //     Debug.Log("flock center under minimum range");
                // }
            }
            float roll = Random.Range(0, eatAppeal + conveneAppeal + idleMargin);
            // Debug.Log("The results from " + (Vector2) transform.position + " are: eatAppeal is " + eatAppeal + ". ConveneAppeal is " + conveneAppeal + ". The roll is " + roll + ".");
            if (roll <= eatAppeal) {
                StartCoroutine("WalkToFood");
                return true;
            }
            else if (roll <= eatAppeal + conveneAppeal) {
                StartCoroutine("GoForSafety");
                return true;
            }
        }
        //else, we just do it again when idle loops
        return false;
    }

    void DeathProtocal () {
        if (shepherd != null) {
            PhotonView shepView = shepherd.GetPhotonView();
            shepView.RPC("SheepDeparts", shepView.Owner, photonView.ViewID);
        }
    }

    IEnumerator DecayShepherdPower () {
        while (shepherdMultiplier > 1) {
            yield return new WaitForSeconds(7);
            shepherdMultiplier /= 2;
            // Debug.Log("Shepherd power decayed, now = " + shepherdMultiplier);
        }
        yield return null;
    }

    IEnumerator ForgetFlockMates () {
        while (true) {
            yield return new WaitForSeconds(5);
            foreach (GameObject flockMate in farFlock.ToArray()) {
// Removal of destroyed sheep is done in UpdateFlockCenter().
                if (Random.Range(1, 100) <= 7 && flockMate != gameObject) {
                    farFlock.Remove(flockMate);
                    flock.Remove(flockMate);
                    RecalculateFlockSizeFactor();
                    // Debug.Log("Friend lost. Flock size: " + flock.Count);
                }
            }
        }
    }

    void GlanceForFood (float direction, int range, ref int shortGrassIndex, ref int mediumGrassIndex, ref int tallGrassIndex, ref List<Vector2> percievedPatches) {
// What we're doing here is contributing to a single list that's sorted by grass height (with the tallest grass coming at the lowest indexes), and then within each grass-height
// segment is sorted by distance, with the closest at the lowest index. So the zero index will always be the closest example of the tallest grass encountered.
        Vector2 runRise = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
        for (int i = 0; i < range; ++i) {
            Vector2 positionOf = runRise * i + (Vector2) transform.position;
            if (Mathf.Abs(positionOf.x) > gameState.mapOffset || Mathf.Abs(positionOf.y) > gameState.mapOffset) {
                break;
            }
            int grassHeightHere = gameState.GetPatchValue(positionOf.x, positionOf.y);
            if (grassHeightHere > 0 && legs.IsNavigable(positionOf)) {
                int compareIndex;
                int stopComparisonIndex;
                switch (grassHeightHere) {
                    case 1: {
                        compareIndex = shortGrassIndex;
                        stopComparisonIndex = mediumGrassIndex + 1;
                        break;
                    }
                    case 2: {
                        compareIndex = mediumGrassIndex;
                        stopComparisonIndex = tallGrassIndex + 1;
                        break;
                    }
                    case 3: {
                        compareIndex = tallGrassIndex;
                        stopComparisonIndex = 0;
                        break;
                    }
                    default:
                        Debug.Log("PROBLEM: sheep looked at a square which had no grass value between zero and three.");
                        continue;
                }                
//For better optimization, this should be changed to a binary search
                if (percievedPatches.Count > 0) {
                    while (compareIndex >= stopComparisonIndex && Vector2.Distance(transform.position, positionOf) < Vector2.Distance(transform.position, percievedPatches[compareIndex])) {
                        // Debug.Log("Program thinks that " + positionOf.x + "," + positionOf.y + " is closer to " + gameObject.transform.position.x + "," 
                        //             + gameObject.transform.position.y + " than " + percievedPatches[j].x + "," + percievedPatches[j].y + " at index " + j + " is.");
                        --compareIndex;
                    }
                    // Debug.Log("Program thinks that " + percievedPatches[j+1].x + "," + percievedPatches[j+1].y + " at index " + (j + 1) + " is closer to " + gameObject.transform.position.x + "," 
                    // + gameObject.transform.position.y + " than " + positionOf.x + "," + positionOf.y + " is.");
                }                
                ++shortGrassIndex;
                if (grassHeightHere > 1) {
                    ++mediumGrassIndex;
                    if (grassHeightHere > 2) {
                        ++tallGrassIndex;
                    }
                }                    
                //Debug.Log("Inserting " + positionOf.x + "," + positionOf.y + " at index " + j);
                percievedPatches.Insert(compareIndex + 1, positionOf);
            }
        }        
    }

    IEnumerator GoForSafety () {
        // Debug.Log("GoForSafety");
        Vector2 randomOffset = new Vector2 (Random.Range (-1 * flockSizeFactor, flockSizeFactor), Random.Range (-1 * flockSizeFactor, flockSizeFactor));
        Vector2 safeSpotRelative = randomOffset + noDogsHere + (flockCenter - (Vector2) transform.position);
        if (shepherdMultiplier <= 1) {
// This is equivalent to reducing the magnitude of safeSpotRelative by flocksizefactor.
            safeSpotRelative *= Mathf.Clamp((safeSpotRelative.magnitude - flockSizeFactor) / safeSpotRelative.magnitude, 0, 1);
        }
        float toGo = safeSpotRelative.magnitude;
        float ETA = toGo / legs.GetSpeed();
        Vector2 safeSpotAbsolute = safeSpotRelative + (Vector2) transform.position;
        // Debug.Log("running " + safeSpotRelative + " to safety. Expected to go " + toGo + " distance, at a speed of " + tempSpeed);
        float tempSpeed = Mathf.Clamp(2 + toGo * 0.1f, 2, 6);
        thisSheep.Move(safeSpotAbsolute, -1, tempSpeed, -1f);
        sheepState = sheepBehaviors.goingToSafety;
// Normally this coroutine will be stopped by PathEnded(). This is just for in case the sheep gets stuck and can't make it to its destination:
        yield return new WaitForSeconds (ETA * 2);
        StartCoroutine(Idle());
        yield return null;
    }

    [PunRPC]
    public void HearChime (int chimerPhotonID) {
        GameObject chimer = PhotonNetwork.GetPhotonView(chimerPhotonID).gameObject;
        if (chimer != shepherd) {
            int newFactionNumber = chimer.GetPhotonView().Owner.ActorNumber;
            if (shepherd != null) {
                PhotonView shepView = shepherd.GetPhotonView();
                shepView.RPC("SheepDeparts", shepView.Owner, photonView.ViewID);                
            }
            if (newFactionNumber != thisSheep.stats.factionNumber) {
                photonView.RPC("ChangeFaction", RpcTarget.All, newFactionNumber);
            }
            shepherdMultiplier = 1;           
            shepherd = chimer;
        }
        shepherdMultiplier = Mathf.Clamp(shepherdMultiplier *= 2, 1, 256);
        UpdateFlockCenter();
        StopCoroutine("DecayShepherdPower");
        StartCoroutine("DecayShepherdPower");
        // Debug.Log("Chime heard. Shepherd influence is now " + shepherdMultiplier);
    }

    IEnumerator Idle (float idleDuration = 0) {
        // Debug.Log("Idle()");
        sheepState = sheepBehaviors.idling;
        if (legs.GetRunningState() == true) {
            thisSheep.StopMoving(true); //.photonView.RPC("StopMoving", RpcTarget.All, true);
        }
        if (idleDuration > 0) {
            float roll = Random.Range(-1f, 1f);
            if (Mathf.Abs(roll * 11) > 3) {
// This formula generates a number between approximately negative pi and pi, prefering outcomes close to zero.
                float direction = Mathf.Pow(roll, 4) / 0.3f * roll + thisSheep.facing;
                Vector2 runRise = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
                runRise *= (idleDuration / thisSheep.stats.speed) * 0.7f;
                Vector3 wayPoint = transform.position + (Vector3) runRise;
                if (legs.IsNavigable(wayPoint)) {
                    thisSheep.Move(wayPoint, -1, -1f, -1f);
                }
            }
            yield return new WaitForSeconds (idleDuration);
        }
        if (ChangeBehavior() == false) {
            StartCoroutine(Idle(1));
        }
        yield return null;
    }

    void OnTriggerEnter2D(Collider2D thing) {
        if (thing.name.Contains("sheep") && thing.isTrigger == false && flock.Contains(thing.gameObject) == false) {
            flock.Add(thing.gameObject);
            RecalculateFlockSizeFactor();
            // Debug.Log("New friend spotted. Flock count = " + flock.Count);
            if (farFlock.Contains(thing.gameObject)) {
                farFlock.Remove(thing.gameObject);
            }
        }
        else if (thing.name.Contains("dog") && thing.isTrigger == false) {
            dogs.Add(thing.transform);
            if (dogs.Count == 1) {
                StartCoroutine("CalculateDogRepulsion");
            }
            if (thing.gameObject.GetPhotonView().OwnerActorNr != thisSheep.stats.factionNumber) {
                ChangeBehavior();
            }
        }
    }

    void OnTriggerExit2D(Collider2D thing) {
        if (thing.isTrigger == false && flock.Contains(thing.gameObject) == true) {
// Removal of destroyed sheep is done in UpdateFlockCenter().
            farFlock.Add(thing.gameObject);
        }
        else if (thing.isTrigger == false && dogs.Contains(thing.transform)) {
            dogs.Remove(thing.transform);
            if (dogs.Count == 0) {
                StopCoroutine("CalculateDogRepulsion");
                noDogsHere = Vector2.zero;
            }
        }
    }

    void PathEnded () {
        if (sheepState == sheepBehaviors.goingToSafety) {
            StopCoroutine("GoForSafety");
            StartCoroutine(Idle());
        }
        else if (sheepState == sheepBehaviors.goingToFood) {
            StopCoroutine("WalkToFood");
            sheepState = sheepBehaviors.chewing;
            Invoke("Consume", eatTime);
        }
    }

    void RecalculateFlockSizeFactor () {
        flockSizeFactor = Mathf.Clamp(11 - ((flock.Count * 20) / Mathf.Pow(flock.Count, 1.3f)), 1, 10);
    }

    void SafetyNudge () {
// This function just moves the current most appealing patch towards the center of its square, making it more cleare in which patch the sheep has stopped.
        Vector2 squareCenter = new Vector2 ((int) currentMostAppealingPatch.x, (int) currentMostAppealingPatch.y);
        squareCenter.x += Mathf.Sign(squareCenter.x) * 0.5f;
        squareCenter.y += Mathf.Sign(squareCenter.y) * 0.5f;
        Vector2 direction = squareCenter - (Vector2) currentMostAppealingPatch;
        currentMostAppealingPatch += (Vector3) direction * 0.3f;
    }

    bool SearchForFood (int range = 15, bool randomGlance = true) {
        //Debug.Log("SearchForFood");
        if (range <= 0) {
            throw new System.Exception("PROBLEM: searchForFood called with range <= 0!");
        }
        float direction;
        if (randomGlance == true) {
            float roll = Random.Range(-1.0f, 1.0f);
// This formula generates a number between approximately negative pi and pi, prefering outcomes close to zero.
            direction = (Mathf.Pow(roll, 4) / 0.3f * roll) + thisSheep.facing;
        }
        else {
            direction = thisSheep.facing;
        }
        List<Vector2> percievedPatches = new List<Vector2>();
        int shortGrassIndex = -1;
        int mediumGrassIndex = -1;
        int longGrassIndex = -1;
        float clockwise = direction + 0.5f;
        float counterClockwise = direction - 0.5f;
        GlanceForFood(direction, range, ref shortGrassIndex, ref mediumGrassIndex, ref longGrassIndex, ref percievedPatches);
        GlanceForFood(counterClockwise, range, ref shortGrassIndex, ref mediumGrassIndex, ref longGrassIndex, ref percievedPatches);
        GlanceForFood(clockwise, range, ref shortGrassIndex, ref mediumGrassIndex, ref longGrassIndex, ref percievedPatches);
        if (percievedPatches.Count == 0) {
            currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
            //Debug.Log("Failed to find food.");
            return false;
        }
        else {
            if (( (Vector3) percievedPatches[0] - transform.position).magnitude > 15) {
                Debug.Log("PROBLEM: overly-distant destination set.");
            }            
            if ( (int) currentMostAppealingPatch.x != (int) percievedPatches[0].x || (int) currentMostAppealingPatch.y != (int) percievedPatches[0].y) {
                currentMostAppealingPatch = percievedPatches[0];
            }
            SafetyNudge();
            return true;
        }
    }

    [PunRPC]
    public void ShepherdDied () {
        shepherd = null;
        shepherdMultiplier = 0;
        UpdateFlockCenter();
    }

    Vector2 UpdateFlockCenter () {
        Vector2 there = new Vector2(0,0);
        for (int i = flock.Count - 1; i >= 0; --i) {
            GameObject inQuestion = flock[i];
            if (inQuestion == null || inQuestion.activeInHierarchy == false) {
                farFlock.Remove(flock[i]);
                flock.RemoveAt(i);
                RecalculateFlockSizeFactor();
            }
            else {
                there += (Vector2) flock[i].transform.position;
            }
        }
        if (shepherd != null) {
            there += (Vector2) shepherd.transform.position * shepherdMultiplier;
            flockCenter = new Vector2 (there.x / (flock.Count + shepherdMultiplier) + Random.Range(-3, 3), there.y / (flock.Count + shepherdMultiplier) + Random.Range(-3, 3));
        }
        else {
            flockCenter = new Vector2 (there.x / (flock.Count), there.y / (flock.Count));
        }
        // Debug.Log("Flockcenter updated. Now " + ((Vector2) transform.position - flockCenter) + " away. Flock size: " + flock.Count + ". Farflock size: " + farFlock.Count + ". Shepherd power: " + shepherdMultiplier);
        return flockCenter;
    }

    IEnumerator WalkToFood () {
        thisSheep.Move(currentMostAppealingPatch, -1, -1f, -1f);
        int performantCycler = 0;
        bool patchObstructedNow = false;
        bool patchObstructedLastCycle = false;
        float distance = Vector2.Distance(transform.position, currentMostAppealingPatch);
        float stopCheckingTime = Time.time + 1.5f + 2 * distance / legs.GetSpeed();
        sheepState = sheepBehaviors.goingToFood;
        // Debug.Log("Target patch " + distance + " away. ETA: " + ETA);
        while (Time.time < stopCheckingTime) {
            performantCycler = (performantCycler + 1) % 20;
            if (performantCycler % 2 == 0) {
                if (performantCycler == 0){
                    patchObstructedLastCycle = patchObstructedNow;
                    patchObstructedNow = !legs.IsNavigable(currentMostAppealingPatch);
                }
                if (gameState.GetPatchValue(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y)) < 1
                    || (patchObstructedLastCycle == true && patchObstructedNow == true)) {
                    break;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
// This coroutine will normally be stopped by PathEnded before reaching this point.
        StartCoroutine(Idle());
        yield return null;
    }

}