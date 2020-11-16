using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviour {

    protected int power;
    protected int range;
    protected float reloadTime;
    protected CircleCollider2D rangeCircle;
    protected bool treatAsMobile;
    protected AidansMovementScript legs;
    protected Unit thisUnit;
    protected float timeOfLastFire;
    protected GameObject target;

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
            reloadTime = thisUnit.stats.weapon_reloadTime;
            if (GetComponentInParent<UnitBlueprint>().isMobile == true) {
                treatAsMobile = true;
                legs = GetComponentInParent<AidansMovementScript>();
            }
            rangeCircle = GetComponent<CircleCollider2D>();
            rangeCircle.radius = range;
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject == target) {
            StartCoroutine("fire");
            if (treatAsMobile) {
                legs.Invoke("terminatePathfinding", 0.5f);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other == target) {
            StopCoroutine("fire");
            if (treatAsMobile) {
                legs.setDestination(target.transform.position, target.transform);
            }
        }
    }

    public virtual void engage (GameObject getIt) {
        target = getIt;
        rangeCircle.enabled = true;
        if (Vector2.Distance(transform.position, target.transform.position) > range) {
            legs.setDestination(target.transform.position, target.transform);
        }
    }

    public virtual IEnumerator fire () {
        while (target != null) {
            doIt();
            yield return new WaitForSeconds(reloadTime);
        }
        target = null;
        rangeCircle.enabled = false;
        yield return null;
    }

    public virtual void doIt () {

    }

}
