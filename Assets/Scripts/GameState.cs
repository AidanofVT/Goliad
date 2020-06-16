using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    List<GameObject> activeUnits = new List<GameObject>();

    void Start()
    {
        
    }

    public void addActiveUnit (GameObject toAdd) {
        Debug.Log("Attempting to add object.");
        activeUnits.Add(toAdd);
    }

    public List<GameObject> getActiveUnits () {
        return activeUnits;
    }

    public void clearActive () {
        foreach (GameObject unit in activeUnits) {
            Destroy(unit.GetComponent<Unit>().subordinateUIElements["highlightCircle"] as GameObject);
            unit.GetComponent<Unit>().subordinateUIElements.Remove("highlightCircle");
        }
    }
}
