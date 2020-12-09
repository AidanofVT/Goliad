using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class factory_functions : MonoBehaviourPun {
    int locationCycler = 0;
    GameState gameState;
    //GameObject MeatReadout;

    void Start () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
    }

    public void orderCourier () {
        makeUnit("courier");
    }
    
    public void orderDog () {
        makeUnit("Dog");
    }

    public void orderHoplite () {
        makeUnit("Hoplite");
    }

    public void orderSheep () {
        makeUnit("Sheep");
    }

    public void orderShepherd () {
        makeUnit("Shepherd");
    }

    public GameObject makeUnit (string unitType) {
        GameObject toReturn = null;
        unitType = "Units/" + unitType;
        int expense = ((GameObject)Resources.Load(unitType)).GetComponent<UnitBlueprint>().costToBuild;
        Unit factoryUnit = gameObject.GetComponent<Unit>();
        int batchSize = 1;
        if (Input.GetButton("modifier") == true) {
            batchSize = 7;
        }
        for (; batchSize > 0; batchSize--) {
            if (factoryUnit.meat >= expense) {
//In the future, there needs to be a mechanism to detect whether the space around the factory is obstructed, and probably to move those obstructing units. I'd suggest making makeUnit return a boolean
//which will be false as long as the space is obstructed, and then have the ordering method handle the subsiquent calls and the moving of units.
                if (unitType != "Units/Sheep") {
                    toReturn = PhotonNetwork.Instantiate(unitType, gameObject.transform.position + nextOutputLocation(), Quaternion.identity);
                }
                else {
                    Debug.Log("creating sheep");
                    toReturn = PhotonNetwork.Instantiate(unitType, gameObject.transform.position + nextOutputLocation(), Quaternion.identity);
                }
                factoryUnit.deductMeat(expense);
            }
            //MeatReadout.GetComponent<Text>().text = factoryUnit.meat.ToString();
        }
        return toReturn;
    }

    Vector3 nextOutputLocation () {
//In the future, this should account for things like obstructing terrain, and also the size of the unit being created.
        float distanceAlongCircumference = locationCycler * Mathf.PI / 3.5f;
        Vector2 direction = new Vector2 (Mathf.Sin(distanceAlongCircumference), Mathf.Cos(distanceAlongCircumference));
        Vector3 result = new Vector3(direction.x, direction.y, gameObject.transform.position.z) * 2f;
        ++locationCycler;
        if (locationCycler == 7) {
            locationCycler = 0;
        }
        return result;
    }

    [PunRPC]
    public void slaughterSheep () {
        foreach (Collider2D contact in Physics2D.OverlapCircleAll(transform.position, 10)) {
            if (contact.GetComponent<SheepBehavior_Base>() != null) {
                contact.GetComponent<Unit>().die();
            }
        }
    }

}
