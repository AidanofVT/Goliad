using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FactoryUnit : Unit {
    int locationCycler = 0;

    void Start () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        MeatReadout = transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
    }

    public void orderMobileUnit() {
        makeUnit("MobileUnitPrefab");
    }

    public void orderSheep () {
        makeUnit("Sheep");
    }

    public void orderShepherd () {
        makeUnit("Shepherd");
    }

    public void makeUnit (string unitType) {
        GameObject toMake = (GameObject)Resources.Load(unitType);
        int batchSize = 1;
        if (Input.GetButton("modifier") == true) {
            batchSize = 7;
        }
        for (; batchSize > 0; batchSize--) {
            if (meat >= toMake.GetComponent<Unit>().cost()) {
//In the future, there needs to be a mechanism to detect whether the space around the factory is obstructed, and probably to move those obstructing units. I'd suggest making makeUnit return a boolean
//which will be false as long as the space is obstructed, and then have the ordering method handle the subsiquent calls and the moving of units.
                bool success = null != Instantiate(toMake, gameObject.transform.position + nextOutputLocation(), gameObject.transform.rotation);
                Debug.Log(success);
                meat -= toMake.GetComponent<Unit>().cost();
            }
            MeatReadout.GetComponent<Text>().text = meat.ToString();
        }
    }

    Vector3 nextOutputLocation () {
//In the future, this should account for things like obstructing terrain, and also the size of the unit being created.
        float distanceAlongCircumferenc = locationCycler * Mathf.PI / 3.5f;
        Vector2 direction = new Vector2 (Mathf.Sin(distanceAlongCircumferenc), Mathf.Cos(distanceAlongCircumferenc));
        Vector3 result = new Vector3(direction.x, direction.y, gameObject.transform.position.z) * 2f;
        ++locationCycler;
        if (locationCycler == 7) {
            locationCycler = 0;
        }
        return result;
    }

    public void slaughterSheep () {
        foreach (Collider2D contact in Physics2D.OverlapCircleAll(transform.position, 15)) {
            if (contact.GetComponent<SheepBehavior>() != null) {
                contact.GetComponent<independentUnit>().die();
            }
        }
    }
}
