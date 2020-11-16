﻿using UnityEngine;
using Photon.Pun;

public class BarManager : MonoBehaviourPun {
    GameObject leftBlock;
    GameObject middleBlock;
    GameObject rightBlock;
    Unit thisUnit;
    string unitName;
    int denominator;

    void OnEnable () {
        leftBlock = transform.GetChild(0).gameObject;
        middleBlock = transform.GetChild(1).gameObject;
        rightBlock = transform.GetChild(2).gameObject;
        thisUnit = transform.parent.gameObject.GetComponent<Unit>();
        unitName = thisUnit.GetType().ToString();
        denominator = GetComponentInParent<UnitBlueprint>().meatCapacity;
        updateBar();
    }

    public void updateBar () {
        GetComponent<SpriteRenderer>().size = new Vector2(0.75f * (float) thisUnit.meat / (float) denominator, 0.08f);
    }

    [PunRPC]
    public void displayStrikes () {
        switch (thisUnit.strikes) { 
            case 2:
                middleBlock.SetActive(false);
                break;
            case 1:
                rightBlock.SetActive(false);
                leftBlock.SetActive(false);
                middleBlock.SetActive(true);
                break; 
        }
    }

}
