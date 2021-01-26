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
    protected Unit thisUnit;
    protected float timeOfLastFire;
    public GameObject target;

    void Start() {
        if (GetType() == Type.GetType("Weapon")) {
            string weaponName = transform.parent.gameObject.name;
            weaponName = weaponName.Remove(weaponName.IndexOf("("));
            weaponName += "_Weapon";
            gameObject.AddComponent(Type.GetType(weaponName));
            DestroyImmediate(this);
        }
        else {
            thisUnit = GetComponentInParent<Unit>();
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

    public virtual void engage (GameObject getIt) {
        //Debug.Log("Engaging.");
        target = getIt;
        rangeCircle.enabled = true;
//this is needed because ontriggerenter works based on movement, and both units might be stationary.
        target.transform.Translate(0,0,1);
        target.transform.Translate(0,0,-1);
        if (inRange() == false) {
            legs.setDestination(target.transform.position, target.transform);
        }
    }

    public bool inRange () {
        return Vector2.Distance(transform.position, target.transform.position) <= range;
    }

    public virtual void disengage () {
        StopCoroutine("fire");
        rangeCircle.enabled = false;
        target = null;
    }

    void OnTriggerEnter2D(Collider2D other) {
        //Debug.Log("trigger entered: " + other.gameObject.name);
        if (other.gameObject == target && other.isTrigger == false) {
            StartCoroutine("fire");
            if (treatAsMobile) {
                legs.terminatePathfinding();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.gameObject == target) {
            StopCoroutine("fire");
            if (treatAsMobile) {
                legs.setDestination(target.transform.position, target.transform);
            }
        }
    }

    public virtual IEnumerator fire () {
        // Debug.Log("Firing.");
        while (target != null) {
            if (thisUnit.meat >= shotCost) {
                doIt();
                thisUnit.deductMeat(shotCost);
                yield return new WaitForSeconds(reloadTime);
            }
            else {
                StopCoroutine("fire");
                yield return null;
            }
        }
        target = null;
        rangeCircle.enabled = false;
        yield return null;
    }

    public virtual void doIt () {

    }

}
