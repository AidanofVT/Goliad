using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameState : MonoBehaviourPun {
    List<GameObject> alliedUnits = new List<GameObject>();
    List<Vector2> recentPositions = new List<Vector2>();
    public List<Unit_local> activeUnits = new List<Unit_local>();
    public List<Transform> allIconTransforms = new List<Transform>();
    public byte [,] map;
    public int mapOffset;
    public int playerNumber;
    public bool activeUnitsChangedFlag = false;
    int nudgesSent = 0;
    public int smallMoveCount = 0;

    void Awake () {
//this is in Awake rather than Start so that the array gets made before other scripts try to access it.
        int mapSize = GetComponent<setup>().mapSize;
        map = new byte [mapSize,mapSize];
        mapOffset = map.GetLength(0) / 2;
        // StartCoroutine("AllignRemotes");       
    }

    public void activateUnit (Unit_local toAdd) {
        //Debug.Log("Attempting to add object to activeUnits.");
        if (activeUnits.Contains(toAdd) == false) {
            activeUnits.Add(toAdd);
            activeUnitsChangedFlag = true;
        }
    }

    IEnumerator AllignRemotes () {
        float dutyThisFrame;
        int index = 0;
        while (alliedUnits.Count < 1) {
            yield return new WaitForEndOfFrame();
        }
        int lastSecond = (int) Time.time;
        while (true) {
            if ((int) Time.time != lastSecond) {
                // Debug.Log($"Sent {nudgesSent} nudges this second, among {alliedUnits.Count} photonviews. Also recieved {smallMoveCount} SmallMove orders.");
                lastSecond = (int) Time.time;
                nudgesSent = 0;
                smallMoveCount = 0;
            }
            dutyThisFrame = alliedUnits.Count * Time.fixedDeltaTime;
// This makes it so that each unit is alligned roughly once per second.
// This randomization is here so that the overall rate of allignment is closely linked to the number of units. Without it, the allignments per fixedupdate would jump from
// one integer to another when the number of units crosses a threshhold, which is significant because the number per frame is pretty much always less than four.
            int howManyThisFrame = (int) (Random.value + dutyThisFrame);
            int endpoint = (index + howManyThisFrame) % alliedUnits.Count;
            for (; index != endpoint; index = (index + 1) % alliedUnits.Count) {
                try {
                    UpdateUnitRemotes(alliedUnits[index]);
                }
                catch {
                    Debug.Log("Failed to access index " + index + " when there are " + alliedUnits.Count + " units in alliedUnits");
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }

    void UpdateUnitRemotes (GameObject inQuestion) {
        int indexOf = alliedUnits.IndexOf(inQuestion);
        if ((Vector2) inQuestion.transform.position != recentPositions[indexOf]) {
            ++nudgesSent;
            Vector2 unitPosition = inQuestion.transform.position;
            Vector2 unitVelocity = inQuestion.GetComponent<Rigidbody2D>().velocity;
            inQuestion.GetPhotonView().RPC("AuthoritativeNudge", RpcTarget.Others, unitPosition.x, unitPosition.y, unitVelocity.x, unitVelocity.y, PhotonNetwork.ServerTimestamp);
        }
        recentPositions[indexOf] = inQuestion.transform.position;
    }

    public void deactivateUnit (Unit_local toRem) {
        //Debug.Log("Attempting to remove object from activeUnits.");
        if (activeUnits.Contains(toRem)) {
            activeUnits.Remove(toRem);
            activeUnitsChangedFlag = true;
        }
    }

    public void enlivenUnit (GameObject toAdd) {
        // Debug.Log($"Attempting to add {toAdd.name} to aliveUnits.");
        if (toAdd.GetPhotonView().OwnerActorNr == playerNumber) {
            alliedUnits.Add(toAdd);
            recentPositions.Add(toAdd.transform.position);
        }
    }

    public void deadenUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from aliveUnits.");
        if (toRem.GetPhotonView().OwnerActorNr == playerNumber) {
            recentPositions.RemoveAt(alliedUnits.IndexOf(toRem));
            alliedUnits.Remove(toRem);
            deactivateUnit(toRem.GetComponent<Unit_local>());
        }
        allIconTransforms.Remove(toRem.transform.GetChild(4));    
    }

    public List<Unit_local> getActiveUnits () {
        return activeUnits;
    }

    public List<GameObject> getAliveUnits () {
        return alliedUnits;
    }

    public int getPatchValue (float x, float y) {
        return map[Mathf.FloorToInt(x) + mapOffset, Mathf.FloorToInt(y) + mapOffset] / 4;
    }

    public void clearActive () {
        List <Unit_local> thisIsToSupressWarnings = new List<Unit_local> (activeUnits);
        foreach (Unit_local aUnit in thisIsToSupressWarnings) {
            aUnit.deactivate();
        }
        //Debug.Log("Clearactive called. activeUnits size = " + activeUnits.Count);
    }

}
