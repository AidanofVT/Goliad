using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SheepBehavior_Local : SheepBehavior_Base
{
    GameObject Goliad;
    GameState gameState;
    AidansMovementScript legs;
    public NeutralUnit thisSheep;

    List<GameObject> flock = new List<GameObject>();
    List<GameObject> farFlock = new List<GameObject>();
    float flockSizeFactor = 1;
    List<Transform> dogs = new List<Transform>();
    Vector2 flockCenter;
    Vector2 noDogsHere = Vector2.zero;
    public GameObject shepherd;
    public int shepherdMultiplier = 1;
    public int idleMargin = 5;
    short eatTime = 2;
    Vector3 currentMostAppealingPatch;
    enum sheepBehaviors {idling, goingToFood, chewing, goingToSafety};
    sheepBehaviors sheepState;

    void Awake() {
        Goliad = GameObject.Find("Goliad");
        gameState = Goliad.GetComponent<GameState>();
        legs = gameObject.GetComponent<AidansMovementScript>();
//a positive z value sholud be used as an indicator that the targetPatch variable is inactive
        currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
        flock.Add(gameObject);
        StartCoroutine("Start2");
    }

    IEnumerator Start2 () {
        yield return new WaitForSeconds(0);
        thisSheep = GetComponent<NeutralUnit>();
        thisSheep.facing = Random.Range(-1, 1);
        InvokeRepeating("forgetFlockMates", 5, 5);
        StartCoroutine(idle(0));
    }

    IEnumerator CalculateDogRepulsion () {
        while (true) {
            Vector2 totalOffSet = Vector2.zero;
            foreach (Transform pusher in dogs) {
                Vector2 offset = transform.position - pusher.position;
                offset = offset.normalized * Mathf.Pow(15 - Mathf.Clamp(offset.magnitude, 5, 15), 2) / 7;
                if (pusher.gameObject.GetPhotonView().OwnerActorNr != alliedFaction) {
                    offset *= 3;
                }            
                totalOffSet += offset;
            }
// This is phrased like so because you can't clamp a Vector's magnitude directly.
            totalOffSet *= Mathf.Clamp(90 / totalOffSet.magnitude, 0, 1);
// This is here to compensate for the use of flockSizeFactor in calculating ConveneAppeal. Without this, smaller flocks would respond to sheep more strongly.
            totalOffSet *= flockSizeFactor;
            noDogsHere = totalOffSet;
            yield return new WaitForSeconds(0.2f);
        }
    }

    void consume () {
        Vector2Int patchIndex = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
        if (Goliad.GetComponent<MapManager>().exploitPatch(patchIndex)) {
            if (thisSheep.addMeat(1) == true) {
                photonView.RPC("Grow", RpcTarget.All, transform.localScale.x * 1.02f);
            }
        }
        StartCoroutine(idle(0));
    }    

    bool changeBehavior () {
        if (searchForFood() == true || flock.Count > 1 || shepherd != null || dogs.Count > 0) {
            float eatAppeal;
            if (currentMostAppealingPatch.z > 1) {
                eatAppeal = 0;
            }
            else {
                int grassHeightHere = gameState.getPatchValue(currentMostAppealingPatch.x, currentMostAppealingPatch.y);
                eatAppeal = grassHeightHere * 3 / (Vector3.Distance(transform.position, currentMostAppealingPatch) / 40);
                eatAppeal = Mathf.Clamp(eatAppeal, 1.5f, 30 * grassHeightHere);
            }
            float conveneAppeal;
            updateFlockCenter();
            if (Vector2.Distance(flockCenter, transform.position) + noDogsHere.magnitude < 4 || legs.isNavigable(flockCenter + noDogsHere, true) == false) {
                conveneAppeal = 0;
                // if (legs.isNavigable(flockCenter + noDogsHere) == false) {
                //     Debug.Log("flock center obstructed");
                // }
                // if (Vector2.Distance(flockCenter, transform.position) < 6) {
                //     Debug.Log("flock center under minimum range");
                // }
            }
            else {
                float distanceFactor = Mathf.Pow(Vector2.Distance(transform.position, flockCenter + noDogsHere), 1.7f) / 15;
                distanceFactor = Mathf.Clamp(distanceFactor, 0, 100);
                conveneAppeal = shepherdMultiplier * distanceFactor / flockSizeFactor;
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
        //else, we just do it again when idle loops again
        return false;
    }

    void deathProtocal () {
        if (shepherd != null) {
            shepherd.GetComponent<ShepherdFunction>().flock.Remove(gameObject);
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

    void forgetFlockMates () {
        foreach (GameObject flockMate in farFlock.ToArray()) {
            if (Random.Range(1, 100) <= 7 && flockMate != gameObject) {
                farFlock.Remove(flockMate);
                flock.Remove(flockMate);
                RecalculateFlockSizeFactor();
                // Debug.Log("Friend lost. Flock size: " + flock.Count);
            }
        }
    }

    void glanceForFood (float direction, int range, ref int shortGrassIndex, ref int mediumGrassIndex, ref int tallGrassIndex, ref List<Vector2> percievedPatches) {
        Vector2 runRise = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
        for (int i = 0; i < range; ++i) {
            Vector2 positionOf = (Vector2) transform.position + runRise * i;
            if (Mathf.Abs(positionOf.x) > gameState.mapOffset || Mathf.Abs(positionOf.y) > gameState.mapOffset) {
                break;
            }
            int grassHeightHere = gameState.getPatchValue(positionOf.x, positionOf.y);
            if (legs.isNavigable(positionOf)) {
                int compareIndex;
                int stopComparisonIndex;
                switch (grassHeightHere) {
                    case 0: {
                        continue;
                    }
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
                    while (compareIndex >= stopComparisonIndex && Vector2.Distance(gameObject.transform.position, positionOf) < Vector2.Distance(gameObject.transform.position, percievedPatches[compareIndex])) {
                        // Debug.Log("Program thinks that " + positionOf.x + "," + positionOf.y + " is closer to " + gameObject.transform.position.x + "," 
                        //             + gameObject.transform.position.y + " than " + percievedPatches[j].x + "," + percievedPatches[j].y + " at index " + j + " is.");
                        --compareIndex;
                    }
                    // Debug.Log("Program thinks that " + percievedPatches[j+1].x + "," + percievedPatches[j+1].y + " at index " + (j + 1) + " is closer to " + gameObject.transform.position.x + "," 
                    // + gameObject.transform.position.y + " than " + positionOf.x + "," + positionOf.y + " is.");
                }
                if (grassHeightHere > 0) {
                    ++shortGrassIndex;
                    if (grassHeightHere > 1) {
                        ++mediumGrassIndex;
                        if (grassHeightHere > 2) {
                            ++tallGrassIndex;
                        }
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
// this is equivalent to reducing the magnitude of safeSpotRelative by flocksizefactor
            safeSpotRelative *= Mathf.Clamp((safeSpotRelative.magnitude - flockSizeFactor) / safeSpotRelative.magnitude, 0, 1);
        }
        float toGo = safeSpotRelative.magnitude;
        legs.speed = Mathf.Clamp(2 + toGo * 0.1f, 2, 6);
        float ETA = toGo / legs.speed;
        // Debug.Log("running " + safeSpotRelative + "to safety. Expected to go " + toGo + " distance, at a speed of " + legs.speed);
        legs.setDestination(safeSpotRelative + (Vector2) transform.position, null);
        sheepState = sheepBehaviors.goingToSafety;
        yield return new WaitForSeconds (ETA * 2);
        legs.speed = thisSheep.stats.speed;
        StartCoroutine(idle());
        yield return null;
    }

    [PunRPC]
    public override void hearChime (int chimerPhotonID) {
        GameObject chimer = PhotonNetwork.GetPhotonView(chimerPhotonID).gameObject;
        if (chimer != shepherd) {
            int newFactionNumber = chimer.GetPhotonView().Owner.ActorNumber;
            if (shepherd == null) {
                photonView.RPC("changeFaction", RpcTarget.All, newFactionNumber);
            }
            else {
                PhotonView shepView = shepherd.GetPhotonView();
                Debug.Log(shepView.Owner);
                shepView.RPC("SheepDeparts", shepView.Owner, photonView.ViewID);
                if (newFactionNumber != shepherd.GetPhotonView().Owner.ActorNumber) {
                    photonView.RPC("changeFaction", RpcTarget.All, newFactionNumber);
                }
            }
            shepherdMultiplier = 1;           
            shepherd = chimer;
        }
        shepherdMultiplier = Mathf.Clamp(shepherdMultiplier *= 2, 1, 256);
        updateFlockCenter();
        StopCoroutine("DecayShepherdPower");
        StartCoroutine("DecayShepherdPower");
        // Debug.Log("Chime heard. Shepherd influence is now " + shepherdMultiplier);
    }

    IEnumerator idle (float idleDuration = 0) {
        // Debug.Log("idle");
        sheepState = sheepBehaviors.idling;
        if (legs.isRunning) {
            // Debug.Log("stopping legs");
            legs.terminatePathfinding();
        }
        if (idleDuration > 0) {
            int roll = Random.Range(0, 11);
            if (roll > 3) {
                float direction = (Mathf.Pow(roll, 4) / 0.3f * roll) + thisSheep.facing;
                Vector2 runRise = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
                runRise *= (idleDuration / thisSheep.stats.speed) * 0.7f;
                Vector3 wayPoint = transform.position + (Vector3) runRise;
                if (legs.isNavigable(wayPoint)) {
                    legs.setDestination(wayPoint);
                }
            }
            yield return new WaitForSeconds (idleDuration);
        }
        if (changeBehavior() == false) {
            StartCoroutine(idle(1));
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
        else if (thing.name.Contains("dog") && thing.isTrigger == false && thing.isTrigger == false) {
            dogs.Add(thing.transform);
            if (dogs.Count == 1) {
                StartCoroutine("CalculateDogRepulsion");
            }
            if (thing.gameObject.GetPhotonView().OwnerActorNr != alliedFaction) {
                changeBehavior();
            }
        }
    }

    void OnTriggerExit2D(Collider2D thing) {
        if (thing.isTrigger == false && flock.Contains(thing.gameObject) == true) {
            farFlock.Add(thing.gameObject);
        }
        else if (thing.isTrigger == false && dogs.Contains(thing.transform)) {
            dogs.Remove(thing.transform);
            if (dogs.Count == 0) {
                StopCoroutine("CalculateDogRepulsion");
            }
            noDogsHere = Vector2.zero;
        }
    }

    void PathEnded () {
        if (sheepState == sheepBehaviors.goingToSafety) {
            legs.speed = thisSheep.stats.speed;
            StopCoroutine("GoForSafety");
            StartCoroutine(idle());
        }
        else if (sheepState == sheepBehaviors.goingToFood) {
            StopCoroutine("WalkToFood");
            Invoke("consume", eatTime);
            sheepState = sheepBehaviors.chewing;
        }
    }

    void RecalculateFlockSizeFactor () {
        flockSizeFactor = Mathf.Clamp(11 - ((flock.Count * 20) / Mathf.Pow(flock.Count, 1.3f)), 1, 10);
    }

    void safetyNudge () {
        Vector2 squareCenter = new Vector2 ((int) currentMostAppealingPatch.x, (int) currentMostAppealingPatch.y);
        squareCenter.x += Mathf.Sign(squareCenter.x) * 0.5f;
        squareCenter.y += Mathf.Sign(squareCenter.y) * 0.5f;
        Vector2 direction = squareCenter - (Vector2) currentMostAppealingPatch;
        currentMostAppealingPatch += (Vector3) direction * 0.3f;
    }

    bool searchForFood (int range = 15, bool randomGlance = true) {
        //Debug.Log("SearchForFood");
        if (range < 0) {
            Debug.Log("PROBLEM: searchForFood called with range <= 0!");
            return false;
        }
        float direction;
        if (randomGlance == true) {
            float roll = Random.Range(-1.0f, 1.0f);
            direction = (Mathf.Pow(roll, 4) / 0.3f * roll) + thisSheep.facing; // this formula generates a number between approximately negative pi and pi, prefering outcomes close to zero
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
        glanceForFood(direction, range, ref shortGrassIndex, ref mediumGrassIndex, ref longGrassIndex, ref percievedPatches);
        glanceForFood(counterClockwise, range, ref shortGrassIndex, ref mediumGrassIndex, ref longGrassIndex, ref percievedPatches);
        glanceForFood(clockwise, range, ref shortGrassIndex, ref mediumGrassIndex, ref longGrassIndex, ref percievedPatches);
        if (percievedPatches.Count == 0) {
            currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
            //Debug.Log("Failed to find food.");
            return false;
        }
        else {
            if (( (Vector3)percievedPatches[0] - transform.position).magnitude > 15) {
                Debug.Log("PROBLEM: long-range destination set.");
            }            
            if ((int) currentMostAppealingPatch.x != (int) percievedPatches[0].x || (int) currentMostAppealingPatch.y != (int) percievedPatches[0].y) {
                currentMostAppealingPatch = percievedPatches[0];
            }
            safetyNudge();
            return true;
        }
    }

    Vector2 updateFlockCenter () {
        Vector2 there = new Vector2(0,0);
        for (int i = 0; i < flock.Count; ++i) {
            GameObject inQuestion = flock[i];
            if (inQuestion == null || inQuestion.activeInHierarchy == false) {
                farFlock.Remove(flock[i]);
                flock.RemoveAt(i);
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
        legs.setDestination(currentMostAppealingPatch);
        // Vector2Int hereInt;      
        // Vector2Int thereInt;
        int performantCycler = 0;
        bool patchObstructedNow = false;
        bool patchObstructedLastCycle = false;
        float startTime = Time.time;
        float distance = Vector2.Distance(transform.position, currentMostAppealingPatch);
        float ETA = 1.5f + 2 * distance / legs.speed;
        sheepState = sheepBehaviors.goingToFood;
        // Debug.Log("Target patch " + distance + " away. ETA: " + ETA);
        while (Time.time - startTime < ETA) {
            // Debug.Log("On my way to " + currentMostAppealingPatch + ". " + (Time.time - startTime) + " elapsed. Now at " + (Vector2) transform.position);
            performantCycler = (performantCycler + 1) % 20;
            if (performantCycler % 2 == 0) {
                if (performantCycler == 0){
                    patchObstructedLastCycle = patchObstructedNow;
                    patchObstructedNow = !legs.isNavigable(currentMostAppealingPatch);
                }
                if (gameState.getPatchValue(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y)) < 1) {
                    // Debug.Log((Vector2) transform.position + ": patch is baren");
                    break;
                }
                else if (patchObstructedLastCycle == true && patchObstructedNow == true) {
                    // Debug.Log((Vector2) transform.position + ": patch obstructed");
                    break;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
        // Debug.Log("Timed out: " + Time.time + ", " + startTime);
        StartCoroutine(idle());
        yield return null;
    }

}