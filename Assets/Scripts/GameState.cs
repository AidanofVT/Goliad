﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameState : MonoBehaviourPun {
    List<GameObject> activeUnits = new List<GameObject>();
    List<GameObject> alliedUnits = new List<GameObject>();
    public List<Transform> allIconTransforms = new List<Transform>();
//activeCohorts only exists to facilitate the dissolving of cohorts prior to a new cohort being formed
//there should never be more than one cohort responding to a single order
    public List<Cohort> activeCohorts = new List<Cohort>();

    //NOTE: If you want to go bigger by using a smaller sort of number, you'll have to do something in the shader, because it needs things passed to it as 32-bit words. 
    public byte [,] map;
    public int mapOffset;
    int playerNumber;
    public bool activeCohortsChangedFlag = false;

    void Awake () {
//this is in Awake rather than Start so that the array gets made before other scripts try to access it.
        int mapSize = GetComponent<setup>().mapSize;
        map = new byte [mapSize,mapSize];
        mapOffset = map.GetLength(0) / 2;        
    }

     void Start () {
        playerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
     }

    public Cohort combineActiveCohorts () {
        if (activeCohorts.Count == 1) {
            return activeCohorts[0];
        }
        else {
            Cohort newCohort = new Cohort();
            foreach (Cohort selectedCohort in activeCohorts) {
                List<Unit_local> thisIsAlsoToSupressWarnings = new List<Unit_local>(selectedCohort.members);
                foreach (Unit_local individual in thisIsAlsoToSupressWarnings) {
                    individual.changeCohort(newCohort);
                }
            }
            activeCohorts.Clear();
            activeCohorts.Add(newCohort);
            return activeCohorts[0];
        }
    }

    public void activateUnit (GameObject toAdd) {
        //Debug.Log("Attempting to add object to activeUnits.");
        if (activeUnits.Contains(toAdd) == false) {
            activeUnits.Add(toAdd);
        }        
    }

    public void deactivateUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from activeUnits.");
        if (activeUnits.Contains(toRem)) {
            activeUnits.Remove(toRem);
        }
    }

    public void enlivenUnit (GameObject toAdd) {
        //Debug.Log("Attempting to add object to aliveUnits.");
        if (toAdd.GetPhotonView().OwnerActorNr == playerNumber) {
            alliedUnits.Add(toAdd);
        }
        allIconTransforms.Add(toAdd.transform.GetChild(4));    
    }

    public void deadenUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from aliveUnits.");
        if (toRem.GetPhotonView().OwnerActorNr == playerNumber) {
            alliedUnits.Remove(toRem);
            activeUnits.Remove(toRem);
        }
        allIconTransforms.Remove(toRem.transform.GetChild(4));    
    }

    public List<GameObject> getActiveUnits () {
        return activeUnits;
    }

    public List<GameObject> getAliveUnits () {
        return alliedUnits;
    }

    public int getPatchValue (float x, float y) {
        return map[Mathf.FloorToInt(x) + mapOffset, Mathf.FloorToInt(y) + mapOffset] / 4;
    }

    public void clearActive () {
        List <Cohort> thisIsToSupressWarnings = new List<Cohort> (activeCohorts);
        foreach (Cohort cohort in thisIsToSupressWarnings) {
            cohort.deactivate();
        }
        //Debug.Log("Clearactive called. activeUnits size = " + activeUnits.Count);
    }

}
