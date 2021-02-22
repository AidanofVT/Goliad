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
    List<Transform> dogs = new List<Transform>();
    Vector2 flockCenter;
    public GameObject shepherd;
    public int shepherdMultiplier = 1;
    public int idleMargin = 5;
    short eatTime = 2;
    Vector3 currentMostAppealingPatch;

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

    IEnumerator idle (float idleDuration = 0) {
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

    bool changeBehavior () {
        if (searchForFood() == true || flock.Count > 1 || shepherd != null) {
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
            if (Vector2.Distance(flockCenter, transform.position) < 4) {
                conveneAppeal = 0;
                // if (legs.isNavigable(updateFlockCenter()) == false) {
                //     Debug.Log("flock center obstructed");
                // }
                // if (Vector2.Distance(flockCenter, transform.position) < 6) {
                //     Debug.Log("flock center under minimum range");
                // }
            }
            else {
                // float flockSizeFactor = Mathf.Clamp(10 - flock.Count * 50 / Mathf.Pow(flock.Count, 1.8f), 1, 10);
                // float distanceFactor = Mathf.Clamp(Mathf.Pow(Vector2.Distance(transform.position, flockCenter), 1.7f), 1, 100);
                conveneAppeal = shepherdMultiplier * Mathf.Pow(Vector2.Distance(transform.position, flockCenter), 1.7f) / 20;
            }
            float roll = Random.Range(0, eatAppeal + conveneAppeal + idleMargin);
            // Debug.Log("The results are in: eatAppeal is " + eatAppeal + ". ConveneAppeal is " + conveneAppeal + ". The roll is " + roll + ".");
            if (roll <= eatAppeal) {
                StartCoroutine(WalkToFood());
                return true;
            }
            else if (roll <= eatAppeal + conveneAppeal) {
                StartCoroutine(GoForSafety());
                return true;
            }
        }
        //else, we just do it again when idle loops again
        return false;
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

    void consume () {
        //Debug.Log("Exploiting " + Mathf.FloorToInt(transform.position.x) + Mathf.FloorToInt(transform.position.y));
        if (Goliad.GetComponent<MapManager>().exploitPatch(new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)))) {
            if (gameObject.GetComponent<Unit>().addMeat(1) == true) {
                photonView.RPC("Grow", RpcTarget.All, transform.localScale.x * 1.02f);
            }
        }
        // CancelInvoke("walkToFood");
        // CancelInvoke("checkFoodTarget");
        StartCoroutine(idle(0));
    }    

    IEnumerator GoForSafety () {
        Debug.Log("GoForSafety");
        float toGo = Vector3.Distance(transform.position, (Vector3) flockCenter);
        legs.speed = Mathf.Clamp(2 + toGo * 0.1f, 2, 6);
        float ETA = toGo / legs.speed;
        legs.setDestination(flockCenter, null);
        yield return new WaitForSeconds(ETA);
        legs.speed = thisSheep.stats.speed;
        StartCoroutine(idle());
        yield return null;
    }

    IEnumerator WalkToFood () {
        legs.setDestination(currentMostAppealingPatch);
        Vector2Int hereInt;      
        Vector2Int thereInt;
        int performantCycler = 0;
        bool patchObstructedNow = false;
        bool patchObstructedLastCycle = false;
        while (true) {
            performantCycler = (performantCycler + 1) % 20;
            if (performantCycler % 2 == 0) {
                if (performantCycler == 0){
                    patchObstructedLastCycle = patchObstructedNow;
                    patchObstructedNow = legs.isNavigable(currentMostAppealingPatch);
                }
                if (gameState.getPatchValue(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y)) < 1
                    || (patchObstructedLastCycle == true && patchObstructedNow == true)) {
                        StartCoroutine(idle(0));
                        Debug.Log("patch is baren");
                        break;
                }
            }
            hereInt = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            thereInt = new Vector2Int(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y));
            if (hereInt == thereInt) {
                Invoke("consume", eatTime);
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
        yield return null;
    }

    void OnTriggerEnter2D(Collider2D thing) {
        if (thing.gameObject.GetComponent<SheepBehavior_Base>() != null && thing.isTrigger == false && flock.Contains(thing.gameObject) == false) {
            flock.Add(thing.gameObject);
            //Debug.Log("New friend spotted. Flock count = " + flock.Count);
            if (farFlock.Contains(thing.gameObject)) {
                farFlock.Remove(thing.gameObject);
            }
        }
        else if (thing.name.Contains("dog") && thing.isTrigger == false && thing.isTrigger == false) {
            dogs.Add(thing.transform);
            updateFlockCenter();
            if (thing.gameObject.GetPhotonView().IsMine == false) {
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
        }
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
                shepherd.GetComponent<ShepherdFunction>().flock.Remove(gameObject);
                if (newFactionNumber != shepherd.GetPhotonView().Owner.ActorNumber) {
                    photonView.RPC("changeFaction", RpcTarget.All, newFactionNumber);
                }
            }
            shepherdMultiplier = 1;           
            shepherd = chimer;
        }
        shepherdMultiplier = Mathf.Clamp(shepherdMultiplier *= 2, 1, 1024);
        updateFlockCenter();
        StopCoroutine("DecayShepherdPower");
        StartCoroutine("DecayShepherdPower");
        // Debug.Log("Chime heard. Shepherd influence is now " + shepherdMultiplier);
    }

    IEnumerator DecayShepherdPower () {
        while (shepherdMultiplier > 1) {
            yield return new WaitForSeconds(5);
            shepherdMultiplier /= 2;
            // Debug.Log("Shepherd power decayed, now = " + shepherdMultiplier);
        }
        yield return null;
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
            float randomOffset = Random.Range(-3, 3) * transform.localScale.x;
            flockCenter = new Vector2 (there.x / (flock.Count) + randomOffset, there.y / (flock.Count) + Random.Range(-3, 3));
        }
        Vector2 totalOffSet = Vector2.zero;
        foreach (Transform pusher in dogs) {
            Vector2 offset = transform.position - pusher.position;
            offset = offset.normalized * Mathf.Pow(15 - Mathf.Clamp(offset.magnitude, 5, 15), 2) / 4;
            if (pusher.gameObject.GetPhotonView().IsMine == false) {
                offset *= 3;
            }
            totalOffSet += offset;
        }
        totalOffSet *= Mathf.Clamp(90 / totalOffSet.magnitude, 0, 1);
        flockCenter += totalOffSet;
        // Debug.Log("Flockcenter updated. Now " + ((Vector2) transform.position - flockCenter) + " away. Flock size: " + flock.Count + ". Farflock size: " + farFlock.Count + ". Shepherd power: " + shepherdMultiplier);
        return flockCenter;
    }

    void safetyNudge () {
        Vector2 squareCenter = new Vector2 ((int) currentMostAppealingPatch.x, (int) currentMostAppealingPatch.y);
        squareCenter.x += Mathf.Sign(squareCenter.x) * 0.5f;
        squareCenter.y += Mathf.Sign(squareCenter.y) * 0.5f;
        Vector2 direction = squareCenter - (Vector2) currentMostAppealingPatch;
        currentMostAppealingPatch += (Vector3) direction * 0.3f;
    }

    void forgetFlockMates () {
        foreach (GameObject flockMate in flock.ToArray()) {
            if (Random.Range(1, 100) <= 10 && flockMate != gameObject) {
                farFlock.Remove(flockMate);
                flock.Remove(flockMate);
                //Debug.Log("Friend lost. Flock size: " + flock.Count);
            }
        }
    }

    void deathProtocal () {
        if (shepherd != null) {
            shepherd.GetComponent<ShepherdFunction>().flock.Remove(gameObject);
        }
    }

}