using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CohortUIManager : MonoBehaviour {
    GameState gameState;
    public List<Cohort> cohorts = new List<Cohort>();
    GameObject bar;
    GameObject productionButtons;
    GameObject slaughterButton;
    SpriteRenderer yellowBar;
    SpriteRenderer greyBar;
    int maxHeight;
    int maxWidth;

    void Start() {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        cohorts = gameState.activeCohorts;
        bar = transform.GetChild(0).gameObject;
        yellowBar = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
        greyBar = transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
        productionButtons = transform.GetChild(1).gameObject;
        slaughterButton = transform.GetChild(2).gameObject;
        maxHeight = (int) transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>().size.y;
        maxWidth = (int) transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>().size.x;
    }

    void Update() {
        if (gameState.activeCohortsChangedFlag == true) {
            bool cohortsContainDepot = false;
            if (cohorts.Count <= 0) {
                bar.SetActive(false);
                productionButtons.SetActive(false);
            }
            else {
                if (bar.activeInHierarchy == false) {
                    bar.SetActive(true);
                    productionButtons.SetActive(true);
                }
                float heldSum = 0;
                float capacitySum = 0;
                foreach (Cohort activeOne in cohorts) {
                    foreach (Unit individual in activeOne.members) {
                        heldSum += individual.meat;
                        capacitySum += individual.stats.meatCapacity;
                        if (individual.gameObject.name.Contains("depot")) {
                            cohortsContainDepot = true;
                        }
                    }
                }
                float magnitude = (Mathf.Pow((capacitySum + 2700) / 210, 2.9f) - Mathf.Pow((capacitySum + 2700) / 530, 3.9f) - 1000) / 3075;
                float xMagnitude = magnitude * maxWidth;
                float yMagnitude = magnitude * maxHeight;
                greyBar.size = new Vector2(Mathf.Clamp(xMagnitude, 20, maxWidth), Mathf.Clamp(yMagnitude * 2, 40, maxHeight));
                yellowBar.size = new Vector2(greyBar.size.x - 7, Mathf.Clamp(heldSum / capacitySum * (greyBar.size.y - 7), 3, maxHeight - 7));
            }
            slaughterButton.SetActive(cohortsContainDepot);
            gameState.activeCohortsChangedFlag = false;
        }
    }

    public void OrderCourier () {
        OrderThing("courier");
    }

    public void OrderDepot () {
        OrderThing("depot");
    }

    public void OrderDog() {
        OrderThing("dog");
    }

    public void OrderHoplite () {
        OrderThing("hoplite");
    }

    public void OrderSheep () {
        OrderThing("sheep");
    }

    public void OrderShepherd () {
        OrderThing("shepherd");
    }

    public void OrderThing(string whatThing) {
        if (cohorts.Count > 1) {
            gameState.combineActiveCohorts();
        }
        cohorts[0].makeUnit(whatThing);
    }

    public void Slaughter () {
        if (cohorts.Count > 1) {
            gameState.combineActiveCohorts();
        }
    }
}
