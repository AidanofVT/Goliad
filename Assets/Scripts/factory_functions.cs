using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class factory_functions : MonoBehaviourPun {
    int locationCycler = 0;
    GameState gameState;
    GameObject MeatReadout;

    void Start () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
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
                PhotonNetwork.Instantiate(unitType, gameObject.transform.position + nextOutputLocation(), Quaternion.identity);
                factoryUnit.deductMeat(expense);
            }
            MeatReadout.GetComponent<Text>().text = factoryUnit.meat.ToString();
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

    [PunRPC]
    public void slaughterSheep () {
        Debug.Log("slaughtersheep");
        foreach (Collider2D contact in Physics2D.OverlapCircleAll(transform.position, 15)) {
            if (contact.GetComponent<SheepBehavior_Base>() != null) {
                contact.GetComponent<Unit>().die();
            }
        }
    }

}
