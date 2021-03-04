using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameState : MonoBehaviourPun {
    List<GameObject> alliedUnits = new List<GameObject>();
    public List<Unit_local> activeUnits = new List<Unit_local>();
    public List<Transform> allIconTransforms = new List<Transform>();
    public byte [,] map;
    public int mapOffset;
    public int playerNumber;
    public bool activeUnitsChangedFlag = false;

    void Awake () {
//this is in Awake rather than Start so that the array gets made before other scripts try to access it.
        int mapSize = GetComponent<setup>().mapSize;
        map = new byte [mapSize,mapSize];
        mapOffset = map.GetLength(0) / 2;        
    }

    public void activateUnit (Unit_local toAdd) {
        //Debug.Log("Attempting to add object to activeUnits.");
        if (activeUnits.Contains(toAdd) == false) {
            activeUnits.Add(toAdd);
            activeUnitsChangedFlag = true;
        }
    }

    public void deactivateUnit (Unit_local toRem) {
        //Debug.Log("Attempting to remove object from activeUnits.");
        if (activeUnits.Contains(toRem)) {
            activeUnits.Remove(toRem);
            activeUnitsChangedFlag = true;
        }
    }

    public void enlivenUnit (GameObject toAdd) {
        //Debug.Log("Attempting to add object to aliveUnits.");
        if (toAdd.GetPhotonView().OwnerActorNr == playerNumber) {
            alliedUnits.Add(toAdd);
        }
    }

    public void deadenUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from aliveUnits.");
        if (toRem.GetPhotonView().OwnerActorNr == playerNumber) {
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
