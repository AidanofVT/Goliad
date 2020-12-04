using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    List<GameObject> activeUnits = new List<GameObject>();
//aliveUnits is intended for alive ALLIED units.
    List<GameObject> aliveUnits = new List<GameObject>();
//activeCohorts only exists to facilitate the dissolving of cohorts prior to a new cohort being formed
//there should never be more than one cohort responding to a single order
    public List<Cohort> activeCohorts = new List<Cohort>();

    //NOTE: CPU becomes a limitiation somewhere between one and ten million tiles on-screen. Memory usage is also significant.
    public short [,] map;
    public int mapOffset;

    ViewManager vManage;

    void Awake () {
//this is in Awake rather than Start so that the array gets made before other scripts try to access it.
        int mapSize = GetComponent<setup>().mapSize;
        map = new short [mapSize,mapSize];
        mapOffset = map.GetLength(0) / 2;
        vManage = GameObject.Find("Player Perspective").GetComponent<ViewManager>();
    }

    public Cohort combineActiveCohorts () {
        if (activeCohorts.Count == 1) {
            return activeCohorts[0];
        }
        else {
            List<Unit_local> members = new List<Unit_local>();
            foreach (Cohort selectedCohort in activeCohorts) {
                members.AddRange(selectedCohort.members);
            }
            Cohort newCohort = new Cohort(members);
            activeCohorts.Clear();
            activeCohorts.Add(newCohort);
            return activeCohorts[0];
        }
    }

    public void activateUnit (GameObject toAdd) {
        //Debug.Log("Attempting to add object to activeUnits.");
        if (activeUnits.Contains(toAdd) == false) {
            activeUnits.Add(toAdd);
            vManage.attendTo(toAdd);
        }
    }

    public void deactivateUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from activeUnits.");
        if (activeUnits.Contains(toRem)) {
            activeUnits.Remove(toRem);
            vManage.attendToNoMore(toRem);
        }
    }

    public void enlivenUnit (GameObject toAdd) {
        //Debug.Log("Attempting to add object to aliveUnits.");
        aliveUnits.Add(toAdd);
    }

    public void deadenUnit (GameObject toRem) {
        //Debug.Log("Attempting to remove object from aliveUnits.");
        aliveUnits.Remove(toRem);
        activeUnits.Remove(toRem);
    }

    public List<GameObject> getActiveUnits () {
        return activeUnits;
    }

    public List<GameObject> getAliveUnits () {
        return aliveUnits;
    }

    public int getPatchValue (int x, int y) {
        return map[x + mapOffset, y + mapOffset];
    }

    public void clearActive () {
        List <Cohort> thisIsToSupressWarnings = new List<Cohort> (activeCohorts);
        foreach (Cohort cohort in thisIsToSupressWarnings) {
            cohort.deactivate();
        }
        //Debug.Log("Clearactive called. activeUnits size = " + activeUnits.Count);
    }

}
