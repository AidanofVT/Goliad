using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// NOTICE: Much of the functionality herein is designed to facilitate a future change to synchronized simulations using deterministic physics. It may seem unusual or unwieldy, but it's
// working fine with photontransformviews, so let's keep it like this. The "Nudging" mechanic seen here won't be needed for true deterministic physics, but could be used in a system where
// the object owner uses local avoidance, and the remote instances don't do any navigation at all.

public class GameState : MonoBehaviourPun {
    List<GameObject> alliedUnits = new List<GameObject>();
    Hashtable recentPositions = new Hashtable();
    public List<Unit_local> activeUnits = new List<Unit_local>();
    public List<GameObject> allIcons = new List<GameObject>();
    public byte [,] map;
    public int mapOffset;
    public int playerNumber;
// This isn't just for WHICH units are active, it's also for how much meat the hold.
    public bool activeUnitsChangedFlag = false;
    int nudgesSent = 0;
    public int smallMoveCount = 0;

    void Awake () {
//this is in Awake rather than Start so that the array gets made before other scripts try to access it.
        int mapSize = GetComponent<Setup>().mapSize;
        map = new byte [mapSize,mapSize];
        mapOffset = map.GetLength(0) / 2;
        // StartCoroutine("AllignRemotes");       
    }

    public void ActivateUnit (Unit_local toAdd) {
        //Debug.Log("Attempting to add ) + toAdd.gameObject.name + ( to activeUnits.");
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
            for (; index != endpoint; index = ++index % alliedUnits.Count) {
                UpdateUnitRemotes(alliedUnits[index]);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void ClearActive () {
        List <Unit_local> thisIsToSupressWarnings = new List<Unit_local> (activeUnits);
        foreach (Unit_local aUnit in thisIsToSupressWarnings) {
            aUnit.Deactivate();
        }
        activeUnitsChangedFlag = true;
        //Debug.Log("Clearactive called. activeUnits size = " + activeUnits.Count);
    }

    public void DeactivateUnit (Unit_local toRem) {
        //Debug.Log("Attempting to remove ) + toRem.gameObject.name + ( from activeUnits.");
        if (activeUnits.Contains(toRem)) {
            activeUnits.Remove(toRem);
            activeUnitsChangedFlag = true;
        }
    }

    public void DeadenUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove ) + toRem.gameObject.name + ( from aliveUnits.");
        if (toRem.GetPhotonView().OwnerActorNr == playerNumber) {
            recentPositions.Remove(toRem);
            alliedUnits.Remove(toRem);
            DeactivateUnit(toRem.GetComponent<Unit_local>());
        }
        allIcons.Remove(toRem.transform.GetChild(4).gameObject);    
    }

    public void EnlivenUnit (GameObject toAdd) {
        // Debug.Log($"Attempting to add {toAdd.name} to aliveUnits.");
        if (toAdd.GetPhotonView().OwnerActorNr == playerNumber) {
            alliedUnits.Add(toAdd);
            recentPositions.Add(toAdd, toAdd.transform.position);
        }
        allIcons.Add(toAdd.transform.GetChild(4).gameObject);
    }

    public int GetPatchValue (float x, float y) {
// "map" is populated with a list of numbers corrosponding to ground sprites, not grass heights. There are four sprites per grass height, grouped together, with
// groups ordered from least to most.
        return map[Mathf.FloorToInt(x) + mapOffset, Mathf.FloorToInt(y) + mapOffset] / 4;
    }

    void UpdateUnitRemotes (GameObject inQuestion) {
        int indexOf = alliedUnits.IndexOf(inQuestion);
        if ((Vector2) inQuestion.transform.position != (Vector2) recentPositions[inQuestion]) {
            ++nudgesSent;
            Vector2 unitPosition = inQuestion.transform.position;
            Vector2 unitVelocity = inQuestion.GetComponent<Rigidbody2D>().velocity;
            inQuestion.GetPhotonView().RPC("AuthoritativeNudge", RpcTarget.Others, unitPosition.x, unitPosition.y, unitVelocity.x, unitVelocity.y, PhotonNetwork.ServerTimestamp);
        }
        recentPositions[inQuestion] = inQuestion.transform.position;
    }


}
