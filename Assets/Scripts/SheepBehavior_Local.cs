﻿using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SheepBehavior_Local : SheepBehavior_Base
{
    GameObject Goliad;
    GameState gameState;
    AidansMovementScript legs;
    MobileUnit_remote thisSheep;

    List<GameObject> flock = new List<GameObject>();
    List<GameObject> farFlock = new List<GameObject>();
    Vector2 flockCenter;
    public GameObject shepherd;
    public int shepherdMultiplier = 1;

//a negative value should be used to indicate that the variable is inactive.
    short eatTime = 2;
    Vector3 currentMostAppealingPatch;

    void Awake() {
        Goliad = GameObject.Find("Goliad");
        gameState = Goliad.GetComponent<GameState>();
        legs = gameObject.GetComponent<AidansMovementScript>();
        thisSheep = GetComponent<MobileUnit_remote>();
//a positive z value sholud be used as an indicator that the targetPatch variable is inactive
        currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
        flock.Add(gameObject);
        thisSheep.facing = Random.Range(-1, 1);
        InvokeRepeating("updateFlockCenter", 0.1f, 5);
        InvokeRepeating("idle", 0.2f, 1.0f);
    }

    public void configure (int [] flockPhotonViews, int [] farFlockPhotonViews, Vector2 incomingFlockCenter, int shepherdphotonView, Vector3 incomingMostAppealingPatch) {
        foreach (int id in flockPhotonViews) {
            flock.Add(PhotonNetwork.GetPhotonView(id).gameObject);
        }
        foreach (int id in farFlockPhotonViews) {
            farFlock.Add(PhotonNetwork.GetPhotonView(id).gameObject);
        }
        flockCenter = incomingFlockCenter;
        shepherd = PhotonNetwork.GetPhotonView(shepherdphotonView).gameObject;
        shepherdMultiplier = 3;
        for (int i = 0; i < shepherdMultiplier; ++i) {
            Invoke("decayShepherdPower", 7);
        }
        currentMostAppealingPatch = incomingMostAppealingPatch;
    }

    void idle () {
        //Debug.Log("idling");
        if (searchForFood() == true) {
            CancelInvoke("idle");
            changeBehavior();
        }
        //a 'wander' function should be put here
    }

    void changeBehavior () {
        float eatAppeal = 1.5f + (3 / (Vector3.Distance(transform.position, currentMostAppealingPatch) / 40));
        if (eatAppeal > 80) {
            eatAppeal = 80;
        }
        float conveneAppeal = shepherdMultiplier * Vector2.Distance(transform.position, flockCenter);
        int roll = (int) Random.Range(1, eatAppeal + conveneAppeal);
        //Debug.Log("The results are in: eatAppeal is " + eatAppeal + ", targeting patch " + currentMostAppealingPatch + ". ConveneAppeal is " + conveneAppeal + ". The roll is " + roll + ".");
        if (roll <= eatAppeal) {
            InvokeRepeating("walkToFood", 0, 0.1f);
            InvokeRepeating("checkFoodTarget", 1, 1);
        }
        else {
            goForSafety();
        }
    }

    bool searchForFood (int range = 15, bool randomGlance = true) {
        //Debug.Log("SearchForFood");
        if (range <= 0) {
            return true;
        }
        float direction;
        if (randomGlance == true) {
            float roll = Random.Range(-1.0f, 1.0f);
            direction = (Mathf.Pow(roll, 6) / 0.3f * roll) + thisSheep.facing; // this formula generates a number between approximately negative pi and pi, prefering outcomes close to zero
        }
        else {
            direction = thisSheep.facing;
        }
        List<Vector2> percievedPatches = new List<Vector2>();
        int shortgrassIndex = -1;
        float clockwise = direction + 0.5f;
        float counterClockwise = direction - 0.5f;
        glanceForFood(direction, range, ref shortgrassIndex, ref percievedPatches);
        glanceForFood(counterClockwise, range, ref shortgrassIndex, ref percievedPatches);
        glanceForFood(clockwise, range, ref shortgrassIndex, ref percievedPatches);
        if (percievedPatches.Count == 0) {
            currentMostAppealingPatch = transform.position + new Vector3(0,0,1000);
            //Debug.Log("Failed to find food.");
            return false;
        }
        else {
            if (( (Vector3)percievedPatches[0] - transform.position).magnitude > 15) {
                Debug.Log("PROBLEM: long-range destination set.");
            }
            currentMostAppealingPatch = percievedPatches[0];
            safetyNudge();
            return true;
        }
    }

    void glanceForFood (float direction, int range, ref int shortgrassIndex, ref List<Vector2> percievedPatches) {
        //Debug.Log("GlanceForFood");
        Vector2 runRise = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
        for (int i = 0; i < range; ++i) {
            Vector2 positionOf = new Vector2(transform.position.x, transform.position.y) + runRise * i;
            if (Mathf.Abs(positionOf.x) > gameState.mapOffset || Mathf.Abs(positionOf.y) > gameState.mapOffset) {
                break;
            }
            int foodAt = gameState.getPatchValue(Mathf.FloorToInt(positionOf.x), Mathf.FloorToInt(positionOf.y));
            if (foodAt == 1) {
                int j = shortgrassIndex;
//For better optimization, this should be changed to a binary search
                if (percievedPatches.Count > 0) {
                    while (j >= 0 && Vector2.Distance(gameObject.transform.position, positionOf) < Vector2.Distance(gameObject.transform.position, percievedPatches[j])) {
                        // Debug.Log("Program thinks that " + positionOf.x + "," + positionOf.y + " is closer to " + gameObject.transform.position.x + "," 
                        //             + gameObject.transform.position.y + " than " + percievedPatches[j].x + "," + percievedPatches[j].y + " at index " + j + " is.");
                        --j;
                    }
                    // Debug.Log("Program thinks that " + percievedPatches[j+1].x + "," + percievedPatches[j+1].y + " at index " + (j + 1) + " is closer to " + gameObject.transform.position.x + "," 
                    // + gameObject.transform.position.y + " than " + positionOf.x + "," + positionOf.y + " is.");
                }
                ++shortgrassIndex;
                //Debug.Log("Inserting " + positionOf.x + "," + positionOf.y + " at index " + j);
                percievedPatches.Insert(j + 1, positionOf);
            }
        }        
    }

    void consume () {
        //Debug.Log("Exploiting " + Mathf.FloorToInt(transform.position.x) + Mathf.FloorToInt(transform.position.y));
        if (Goliad.GetComponent<MapManager>().exploitPatch(new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)))) {
            gameObject.GetComponent<Unit>().addMeat(1);
        }
        transform.localScale *= 1.05f;
        CancelInvoke("walkToFood");
        CancelInvoke("checkFoodTarget");
        InvokeRepeating("idle", 0, 1.0f);
    }    

    void checkFoodTarget () {
        //Debug.Log("checkfoodtarget with range " + ((int) Vector3.Distance(transform.position, currentMostAppealingPatch) + 1));
        if (!searchForFood((int) Vector3.Distance(transform.position, currentMostAppealingPatch) + 2, false)) {
            CancelInvoke("walkToFood");
            CancelInvoke("checkFoodTarget");
            InvokeRepeating("idle", 0, 1.0f);
        }
    }

    void goForSafety () {
        int toGo = (int) Vector3.Distance(transform.position, (Vector3) flockCenter);
        if (toGo < 40) {
            legs.speed += 0.1f * toGo;
        }
        else {
            legs.speed = 6;
        }
        Vector2 safeSpot = new Vector2(flockCenter.x + Random.Range(-4, 4), flockCenter.y + Random.Range(-4, 4));
        if (((Vector3) safeSpot - transform.position).magnitude > 15) {
            Debug.Log("PROBLEM: long-range destination set."); 
        }
        thisSheep.move(safeSpot);
        InvokeRepeating("idle", toGo / legs.speed, 2);
        Invoke("resetSpeed", toGo / legs.speed);
    }

    void walkToFood () {
        if (legs.isRunning) {
            //Debug.Log("legs running");
            if (gameState.getPatchValue(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y)) < 1) {
                CancelInvoke("walkToFood");
                CancelInvoke("checkFoodTarget");
                InvokeRepeating("idle", 0, 1.0f);
            }
        }
        else {
            //Debug.Log("legs not running");
            Vector2Int hereInt = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            Vector2Int thereInt = new Vector2Int(Mathf.FloorToInt(currentMostAppealingPatch.x), Mathf.FloorToInt(currentMostAppealingPatch.y));
            if (hereInt != thereInt) {
                thisSheep.move(currentMostAppealingPatch);
                //Debug.Log("set destination");
            }
            else {
                //Debug.Log("eating clock started");
                CancelInvoke("walkToFood");
                CancelInvoke("checkFoodTarget");
                Invoke("consume", eatTime);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D thing) {
        if (thing.gameObject.GetComponent<SheepBehavior_Base>() != null && thing.isTrigger == false && flock.Contains(thing.gameObject) == false) {
            flock.Add(thing.gameObject);
            //Debug.Log("New friend spotted. Flock count = " + flock.Count);
        }
    }

    void OnTriggerExit2D(Collider2D thing) {
        if (thing.gameObject.GetComponent<SheepBehavior_Base>() != null && thing.isTrigger == false && flock.Contains(thing.gameObject) == true) {
            farFlock.Add(thing.gameObject);
            //Debug.Log("New friend spotted. Flock count = " + flock.Count);
        }
        InvokeRepeating("forgetFlockMates", 5, 5);
    }

    [PunRPC]
    public override void hearChime (int chimerPhotonID) {
        Debug.Log("Shepherd changing.");
        GameObject chimer = PhotonNetwork.GetPhotonView(chimerPhotonID).gameObject;
        if (chimer != shepherd) {
            if (shepherd != null) {
                shepherd.GetComponent<ShepherdFunction>().flock.Remove(gameObject);
            }
            shepherdMultiplier = 1;           
            shepherd = chimer;
            if (shepherd.GetComponent<PhotonView>().IsMine == false) {
//the array size is one smaller than the flock, and iterator here is 1, because the sheep is already in it's own flock
                int[] flockPhotonIDs = new int[flock.Count - 1];
                for (int i = 1; i < flock.Count - 1; ++i) {
                    flockPhotonIDs[i] = flock[i].GetPhotonView().ViewID;
                }
                int[] farFlockPhotonIDs = new int[farFlock.Count];
                for (int i = 0; i < farFlock.Count - 1; ++i) {
                    farFlockPhotonIDs[i] = farFlock[i].GetPhotonView().ViewID;
                }
                photonView.RPC("transferOwnership", RpcTarget.All, shepherd.GetPhotonView().Owner, flockPhotonIDs, farFlockPhotonIDs, flockCenter, chimerPhotonID, currentMostAppealingPatch);
            }
            //Debug.Log("New shepherd ID: " + chimerPhotonID + "\nPlayer ID: " + PhotonNetwork.GetPhotonView(chimerPhotonID).Owner.ActorNumber);
        }
        shepherdMultiplier *= 2;
        updateFlockCenter();
        Invoke("decayShepherdPower", 7);
    }

    void decayShepherdPower () {
        shepherdMultiplier /= 2;
        //Debug.Log("Shepherd power decayed, now = " + shepherdPower);
    }

    void resetSpeed () {
        legs.speed = 2;
    }

    void updateFlockCenter () {
        Vector2 there = new Vector2(0,0);
        foreach (GameObject fellow in flock) {
            there += (Vector2) fellow.transform.position;
        }
        if (shepherd != null) {
            there += (Vector2) shepherd.transform.position * shepherdMultiplier;
        }
//we have to deduct 1 from the divisors because we're adding multipliers, and if they're both 1 we want the divisor to still be 1.
        flockCenter = new Vector2 (there.x / (flock.Count + shepherdMultiplier - 1), there.y / (flock.Count + shepherdMultiplier - 1));
        //Debug.Log("Flockcenter updated. Now " + Vector2.Distance(transform.position, flockCenter) + " away at " + flockCenter + ". Shepherd power = " + shepherdMultiplier);
    }

    void safetyNudge () {
        Vector2 squareCenter = new Vector2 ((int) currentMostAppealingPatch.x, (int) currentMostAppealingPatch.y);
        squareCenter.x += Mathf.Sign(squareCenter.x) * 0.5f;
        squareCenter.y += Mathf.Sign(squareCenter.y) * 0.5f;
        Vector2 direction = squareCenter - (Vector2) currentMostAppealingPatch;
        currentMostAppealingPatch += (Vector3) direction * 0.3f;
    }

//in the future, this should include a check for whether the sheep is in sight
    void forgetFlockMates () {
        //List<GameObject> farFlockCopy = farFlock;
        foreach (GameObject flockMate in flock.ToArray()) {
            if (Random.Range(1, 100) <= 10 && flockMate != gameObject) {
                farFlock.Remove(flockMate);
                flock.Remove(flockMate);
                //Debug.Log("Friend lost. Flock size: " + flock.Count);
            }
        }
    }

}