using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class CohortUIManager : MonoBehaviour {
    GameState gameState;
    InputHandler inputHandler;
    public List<Unit_local> activeAllies = new List<Unit_local>();
    GameObject bar;
    GameObject productionButtonsParent;
    Hashtable buttonsAndCosts = new Hashtable();
    Button buttonUnderMouse = null;
    GameObject slaughterButton;
    GameObject chimeButton;
    SpriteRenderer yellowBar;
    SpriteRenderer greyBar;
    SpriteRenderer fadedBar;
    float heldSum = 0;
    float capacitySum = 0;
    int maxHeight;
    int maxWidth;

    void Start() {
        GameObject goliad = GameObject.Find("Goliad");
        gameState = goliad.GetComponent<GameState>();
        inputHandler = goliad.GetComponent<InputHandler>();
        activeAllies = gameState.activeUnits;
        bar = transform.GetChild(0).GetChild(0).gameObject;
        greyBar = transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
        yellowBar = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
        fadedBar = transform.GetChild(0).GetChild(0).GetChild(2).GetComponent<SpriteRenderer>();
        Color fadedColor = fadedBar.color;
        fadedColor.a = 0.5f;
        fadedBar.color = fadedColor;
        productionButtonsParent = transform.GetChild(0).GetChild(1).gameObject;        
        for (int i = 0; i < 6; ++i) {
            Button button = transform.GetChild(0).GetChild(1).GetChild(i).GetComponent<Button>();
            string associatedUnit = button.name.Remove(button.name.IndexOf(" "));
            int productionCost = ((GameObject)Resources.Load("Units/" + associatedUnit)).GetComponent<UnitBlueprint>().costToBuild;
            buttonsAndCosts.Add(button, productionCost);
        }
        slaughterButton = transform.GetChild(1).GetChild(0).gameObject;
        chimeButton = transform.GetChild(1).GetChild(1).gameObject;
        maxHeight = (int) greyBar.size.y;
        maxWidth = (int) greyBar.size.x;
    }

    public void AllChimersChime () {
        List<Cohort> alreadyCalled = new List<Cohort>();
        foreach (Unit_local unit in activeAllies) {
            Cohort thisOnesCohort = unit.cohort;
            if (alreadyCalled.Contains(thisOnesCohort) == false) {
                alreadyCalled.Add(thisOnesCohort);
                thisOnesCohort.ChimeAll();
            }
        }
    }
    
    public void AllDepotsSlaughter () {
        List<Cohort> alreadyCalled = new List<Cohort>();
        foreach (Unit_local unit in activeAllies) {
            Cohort thisOnesCohort = unit.cohort;
            if (alreadyCalled.Contains(thisOnesCohort) == false) {
                alreadyCalled.Add(thisOnesCohort);
                thisOnesCohort.Slaughter();
            }
        }
    }

    public void FocusButton (Button newButton) {
        buttonUnderMouse = newButton;
        ShowCost();
    }

    void UpdateInterface () {
        bool cohortsContainDepot = false;
        bool cohortsContainShepherd = false;
        if (activeAllies.Count <= 0) {
            bar.SetActive(false);
            productionButtonsParent.SetActive(false);
        }
        else {        
            if (bar.activeInHierarchy == false) {
                bar.SetActive(true);
                productionButtonsParent.SetActive(true);
            }
            heldSum = 0;
            capacitySum = 0;
            foreach (Unit_local unit in activeAllies) {
                if (unit.name.Contains("depot")) {
                    cohortsContainDepot = true;
                }
                if (unit.name.Contains("shepherd")) {
                    cohortsContainShepherd = true;
                }
                heldSum += unit.meat;
                capacitySum += unit.stats.meatCapacity;                    
            }
            float magnitude = (Mathf.Pow((capacitySum + 2700) / 210, 2.9f) - Mathf.Pow((capacitySum + 2700) / 530, 3.9f) - 1000) / 3075;
            float xMagnitude = magnitude * maxWidth;
            float yMagnitude = magnitude * maxHeight;
            greyBar.size = new Vector2(Mathf.Clamp(xMagnitude, 20, maxWidth), Mathf.Clamp(yMagnitude * 2, 40, maxHeight));
            yellowBar.size = new Vector2(greyBar.size.x - 7, Mathf.Clamp(heldSum / capacitySum * (greyBar.size.y - 7), 0, maxHeight - 7));
            foreach (Button button in buttonsAndCosts.Keys) {
                int price = (int) buttonsAndCosts[button];       
                if (Input.GetButton("modifier") == true) {
                    price *= 5;
                }         
                if (price > heldSum) {
                    button.interactable = false;
                }
                else {
                    button.interactable = true;
                }
            } 
        }
        slaughterButton.SetActive(cohortsContainDepot);
        chimeButton.SetActive(cohortsContainShepherd);
        gameState.activeUnitsChangedFlag = false;
    }

    void Update() {
        if (Input.GetButtonDown("modifier") == true || Input.GetButtonUp("modifier") == true || gameState.activeUnitsChangedFlag == true) {
            UpdateInterface();
            ShowCost();            
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
        Cohort newCohort = inputHandler.CombineActiveUnits(Task.actions.build);
        if (newCohort != null) {
            StartCoroutine(newCohort.MakeUnit(whatThing));
            UpdateInterface();
            ShowCost();
        }
    }

    public void ShowCost () {
        if (buttonUnderMouse != null) {
            int underMouseCost = (int) buttonsAndCosts[buttonUnderMouse];
            if (Input.GetButton("modifier") == true) {
                underMouseCost *= 5;
            }
            yellowBar.size = new Vector2(greyBar.size.x - 7, Mathf.Clamp((heldSum - underMouseCost) / capacitySum * (greyBar.size.y - 7), 0, greyBar.size.y - 7));
            fadedBar.gameObject.transform.localPosition = yellowBar.gameObject.transform.localPosition + new Vector3(0, yellowBar.size.y, 0);
            fadedBar.size = new Vector2(greyBar.size.x - 7, Mathf.Clamp(underMouseCost / capacitySum * (greyBar.size.y - 7), 0, greyBar.size.y - 7));
            fadedBar.enabled = true;
        }
        else {
            yellowBar.size = new Vector2(greyBar.size.x - 7, Mathf.Clamp(heldSum / capacitySum * (greyBar.size.y - 7), 0, maxHeight - 7));            
            fadedBar.enabled = false;
        }
    }

}
