﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit : MonoBehaviourPun {
    protected GameState gameState;
    protected ViewManager viewManager;
    protected BarManager statusBar;
    protected SpriteRenderer icon;
    protected Sprite defaultIcon;
    protected Sprite highlightedIcon;
    protected GameObject contextCircle;
    protected GameObject blueCircle;
    public UnitBlueprint stats;
    public Weapon weapon;
    public Cohort soloCohort;
    public Cohort cohort;
    public float facing = 0;
    public int meat = 0;
    public int strikes = 3;

    void Awake () {
        string spriteName = gameObject.name;
        spriteName = spriteName.Remove(spriteName.IndexOf("("));
        if (photonView.Owner.ActorNumber == 1) {
            spriteName += "_white";
        }
        else if (photonView.Owner.ActorNumber == 2) {
            spriteName += "_orange";
        }
        gameObject.name = spriteName;
        transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/" + spriteName);
        GetComponent<UnitBlueprint>().factionNumber = photonView.OwnerActorNr;
        if (this.GetType() == typeof(Unit)) {
            if (photonView.IsMine) {
                gameObject.AddComponent<Unit_local>();
            }
            else {
                gameObject.AddComponent<Unit_remote>();
            }
//If this were just Destroy(), then the component would persist through the next update cycle, and components would have trouble finding the instances of the generated subclasses.
            DestroyImmediate(this);            
        }
    }

// remember that Awake() is overridden in children, so any value assignments for the script, rather than the gameobject and it's children, must go here.
    public virtual void Start () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        statusBar = transform.GetChild(1).GetComponent<BarManager>();
//this needs to be here, rather than in Awake, so that if there's starting meat then the BarManager sees the right abount of meat when it wakes up
        statusBar.gameObject.SetActive(true);
        addMeat(stats.startingMeat);
        string spriteAddress = "Sprites/" + gameObject.name;
        defaultIcon = Resources.Load<Sprite>(spriteAddress + "_icon");
        highlightedIcon = Resources.Load<Sprite>(spriteAddress + "_icon" + " (highlighted)");
        icon = transform.GetChild(4).gameObject.GetComponent<SpriteRenderer>();
        icon.sprite = defaultIcon;
        gameState.allIconTransforms.Add(icon.transform);
        viewManager = GameObject.Find("Player Perspective").GetComponent<ViewManager>();
        viewManager.resizeIcons(icon.gameObject);
        contextCircle = transform.GetChild(2).GetChild(0).gameObject;
        blueCircle = transform.GetChild(3).gameObject;
        ignition();
    }

    public virtual void ignition () {
    }

    [PunRPC]
    public bool addMeat (int toAdd) {
        if (meat + toAdd <= stats.meatCapacity) {
            meat += toAdd;
            statusBar.updateBar();
            if (weapon != null) {
                if(weapon.target != null && meat - toAdd < stats.weapon_shotCost && weapon.inRange()) {
                    weapon.StartCoroutine("fire"); 
                } 
            }
            if (photonView.IsMine == true && gameState.activeCohorts.Contains(cohort)) {
                gameState.activeCohortsChangedFlag = true;
            }
            return true;
        }
        else {
            return false;
        }
    }

    [PunRPC]
    public bool deductMeat (int toDeduct) {
        if (meat - toDeduct >= 0) {
            meat -= toDeduct;
            statusBar.updateBar();
            if (meat + toDeduct >= stats.meatCapacity) {
                Debug.Log("1");
                Physics2D.queriesHitTriggers = true;
                Collider2D [] contacts = Physics2D.OverlapPointAll(transform.position);
                Physics2D.queriesHitTriggers = false;
                foreach (Collider2D maybeOrb in contacts) {
                    OrbBehavior_Base yeahOrb = maybeOrb.GetComponent<OrbBehavior_Local>();
                    if (yeahOrb != null && yeahOrb.available == true) {
                        Debug.Log("2");
                        yeahOrb.StartCoroutine("GoForIt", gameObject);
                    }
                }
            }
            return true;
        }
        else {
            return false;
        }
    }

    [PunRPC]
    public virtual void takeHit (int power) {  
    }

    [PunRPC]
    public void deductStrike () {
        if (--strikes <= 0) {
            die();
        }
        statusBar.displayStrikes();
    }

    [PunRPC]
    public virtual void die () {             
    }

    public virtual void Highlight() {
        contextCircle.SetActive(true);
        icon.sprite = highlightedIcon;
    }

    public int roomForMeat () {
        return stats.meatCapacity - meat;
    }

    [PunRPC]
    public void startTurning () {
        StartCoroutine("updateFacing");
    }

    [PunRPC]
    public void stopTurning () {
        StopCoroutine("updateFacing");
    }

    public virtual void Unhighlight () {
        contextCircle.SetActive(false);
        if (blueCircle.activeInHierarchy == false) {
            icon.sprite = defaultIcon;
        }      
    }

    [PunRPC]
    public IEnumerator updateFacing () {
        Vector2 previousPosition = transform.position;
        yield return new WaitForSeconds(0);
        while (true) {
            Vector2 velocityNow = ((Vector2) transform.position - previousPosition) / Time.deltaTime;
            if (velocityNow != Vector2.zero) {
                velocityNow.Normalize();
                facing = Mathf.Atan2(velocityNow.x, velocityNow.y);
                transform.GetChild(0).rotation = Quaternion.AxisAngle(Vector3.forward, facing * -1 - Mathf.PI);
            }
            previousPosition = transform.position;
            yield return new WaitForSeconds(0.1f);
        }        
    }
}
