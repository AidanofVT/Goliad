using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviour {

    protected int power;
    protected int range;
    protected int shotCost;
    protected float reloadTime;
    protected CircleCollider2D rangeCircle;
    protected bool treatAsMobile;
    protected AidansMovementScript legs;
    protected Unit_local thisUnit;
    protected float timeOfLastFire;
    public GameObject target;

    void Start() {
        if (transform.parent.gameObject.GetPhotonView().IsMine == false) {
            Destroy(gameObject);
        }        
        else {
            if (GetType() == Type.GetType("Weapon")) {
                string weaponName = transform.parent.gameObject.name;
                weaponName = weaponName.Remove(weaponName.IndexOf("_"));
                weaponName += "_Weapon";
                gameObject.AddComponent(Type.GetType(weaponName));
                DestroyImmediate(this);
            }
            else {
                thisUnit = GetComponentInParent<Unit_local>();
                thisUnit.weapon = this;
                power = thisUnit.stats.weapon_power;
                range = thisUnit.stats.weapon_range;
                shotCost = thisUnit.stats.weapon_shotCost;
                reloadTime = thisUnit.stats.weapon_reloadTime;
                if (GetComponentInParent<UnitBlueprint>().isMobile == true) {
                    treatAsMobile = true;
                    legs = GetComponentInParent<AidansMovementScript>();
                }
                rangeCircle = GetComponent<CircleCollider2D>();
                rangeCircle.radius = range;
            }
        }
    }

    public virtual void engage (GameObject getIt) {
        // Debug.Log("Engaging " + getIt + ".");
        target = getIt;
        rangeCircle.enabled = true;
//this is needed because OnTriggerEnter() works based on movement, and both units might be stationary.
        target.transform.Translate(0,0,1);
        target.transform.Translate(0,0,-1);
        if (inRange() == false) {
            legs.setDestination(target.transform.position, target.transform);
        }
    }

    public bool inRange () {
        return Vector2.Distance(transform.position, target.transform.position) <= range;
    }

    public virtual void Disengage () {
        Debug.Log("disengage");
        StopCoroutine("fire");
        target = null;        
        rangeCircle.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other) {
        // Debug.Log("trigger entered: " + other.gameObject.name);
        if (other.gameObject == target && other.isTrigger == false) {
            StartCoroutine("fire");
            if (treatAsMobile) {
                legs.terminatePathfinding();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.gameObject == target && other.gameObject.activeInHierarchy == true) {
            Debug.Log("ontriggerexit");
            StopCoroutine("fire");
            if (treatAsMobile) {
                legs.setDestination(target.transform.position, target.transform);
            }
        }
    }

    public virtual IEnumerator fire () {
        while (target != null) {
            Debug.Log((Vector2) transform.position + " a");
            if (thisUnit.meat >= shotCost) {
                doIt();
                thisUnit.deductMeat(shotCost);
                yield return new WaitForSeconds(reloadTime);
            }
            else {
                Unit_local provider = null;
                int halfAdjusted = 0;                
                foreach (Unit_local comrade in thisUnit.cohort.armedMembers) {
                    halfAdjusted = (comrade.meat - (comrade.meat % shotCost)) / 2;
                    if (comrade.task.nature == Task.actions.attack
                        && Vector2.Distance(transform.position, comrade.transform.position) < 10
                        && halfAdjusted >= shotCost) {
                            provider = comrade;
                            break;
                    }
                }
                if (provider != null) {
                    Debug.Log((Vector2) transform.position + " b");
                    thisUnit.task.quantity = halfAdjusted;
                    Coroutine dispenseRoutine = StartCoroutine(thisUnit.dispense(thisUnit.gameObject, provider.gameObject));
                    float mark = Time.time;
                    while (Time.time - mark < 8 && thisUnit.meat < shotCost) {
                        Debug.Log((Vector2) transform.position + " b.5");
                        yield return new WaitForSeconds(0.1f);
                    }
                    Debug.Log((Vector2) transform.position + " c");
                    StopCoroutine(dispenseRoutine);
                    if (thisUnit.meat >= shotCost) {
                        Debug.Log((Vector2) transform.position + " d");
                        continue;
                    }
                }
                Debug.Log((Vector2) transform.position + "e");
                StopCoroutine("fire");
                yield return null;
            }
        }
        Debug.Log((Vector2) transform.position + " f");
        Disengage();
        yield return null;
    }

    public virtual void doIt () {

    }

}
