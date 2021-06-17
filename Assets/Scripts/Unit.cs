using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit : MonoBehaviourPun {
    public GameState gameState;
    protected ViewManager viewManager;
    protected BarManager statusBar;
    protected SpriteRenderer icon;
    protected Sprite defaultIcon;
    protected Sprite highlightedIcon;
    protected GameObject contextCircle;
    protected GameObject blueCircle;
    public CircleCollider2D bodyCircle;
    public Rigidbody2D body;
    public UnitBlueprint stats;
    public Weapon weapon;
    public Cohort soloCohort;
    public Cohort cohort;
    public List<Cohort> cohortsAttackingThisUnit = new List<Cohort>();
    public float facing = 0;
    public int meat = 0;
    public int strikes = 3;
// deathThrows is a flag that gets set as soon as the unit's last strike is deducted. This is mainly to prevent incoming damage/death-related RPCs from making a mess.
    public bool deathThrows = false;


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

// Remember that Awake() is overridden in children, so any value assignments for the script, rather than the gameobject and it's children, must go here.
    public virtual void Start () {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.EnlivenUnit(gameObject);
        string spriteAddress = "Sprites/" + gameObject.name;
        defaultIcon = Resources.Load<Sprite>(spriteAddress + "_icon");
        highlightedIcon = Resources.Load<Sprite>(spriteAddress + "_icon" + " (highlighted)");
        icon = transform.GetChild(4).gameObject.GetComponent<SpriteRenderer>();
        icon.sprite = defaultIcon;
        viewManager = GameObject.Find("Player Perspective").GetComponent<ViewManager>();
        viewManager.ResizeIcons(icon.gameObject);
        bodyCircle = GetComponent<CircleCollider2D>();
        body = GetComponent<Rigidbody2D>();
        contextCircle = transform.GetChild(2).GetChild(0).gameObject;
        blueCircle = transform.GetChild(3).gameObject;
        statusBar = transform.GetChild(1).GetComponent<BarManager>();
        statusBar.gameObject.SetActive(true);
// This needs to be here, rather than in Awake, so that if there's starting meat then the BarManager sees the right abount of meat when it wakes up
        Ignition();
    }

    public virtual void Ignition () { }

    [PunRPC]
    public bool AddMeat (int toAdd) {
        // Debug.Log("depositing to " + transform.position);
        if (meat + toAdd <= stats.meatCapacity) {
            meat += toAdd;
            if (name.Contains("sheep")) {
                float finalMagnitude = transform.localScale.x * Mathf.Pow(1.02f, toAdd);
                transform.localScale = new Vector3(finalMagnitude, finalMagnitude, 1);
            }
            else {
                NotifyCohortOfMeatChange(toAdd);
                statusBar.UpdateBar();
                if (blueCircle.activeInHierarchy == true) {
                    gameState.activeUnitsChangedFlag = true;
                }          
                if (weapon != null) {
                    if(weapon.target != null && meat - toAdd < stats.weapon_shotCost && weapon.InRange()) {
                        weapon.StartCoroutine("fire"); 
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
    public bool DeductMeat (int toDeduct) {
        // Debug.Log("withdrawing " + toDeduct + " from " + transform.position);
        if (meat - toDeduct >= 0) {
            meat -= toDeduct;
            if (name.Contains("sheep")) {
                float finalMagnitude = transform.localScale.x * Mathf.Pow(1.02f, toDeduct);
                transform.localScale = new Vector3(finalMagnitude, finalMagnitude, 1);
            }
            else {
                NotifyCohortOfMeatChange(toDeduct * -1);
                statusBar.UpdateBar();
                if (blueCircle.activeInHierarchy == true) {
                    gameState.activeUnitsChangedFlag = true;
                }
            } 
// This is here because otherwise orbs would not seek units which became not-full while in the orbs' search radius. 
            if (meat + toDeduct >= stats.meatCapacity) {
                Physics2D.queriesHitTriggers = true;
                Collider2D [] contacts = Physics2D.OverlapPointAll(transform.position);
                Physics2D.queriesHitTriggers = false;
                foreach (Collider2D maybeOrb in contacts) {
                    OrbBehavior_Base yeahOrb = maybeOrb.GetComponent<OrbBehavior_Local>();
                    if (yeahOrb != null && yeahOrb.available == true) {
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

    void DeathProtocal () {
        foreach (Cohort aggressor in cohortsAttackingThisUnit) {
            aggressor.TargetDown(this);
        }
    }

    [PunRPC]
    public void DeductStrikes (int numStrikes) {
        for (int i = 0; i < numStrikes; ++i) {
            if (--strikes <= 0) {
                StartCoroutine("Die");
                break;
            }
        }
        statusBar.DisplayStrikes();
    }

    [PunRPC]
    public virtual IEnumerator Die () {yield return null;}

    public virtual void Highlight() {
        contextCircle.SetActive(true);
        icon.sprite = highlightedIcon;
    }

    public virtual void Move (Vector2 goTo, int leaderID = -1, float speed = -1, float arrivalThreshholdOverride = -1) {
        throw new InvalidOperationException("Tried to move an immobile unit.");
    }

    protected virtual void NotifyCohortOfMeatChange (int difference) { }

    public int RoomForMeat () {
        return stats.meatCapacity - meat;
    }

    [PunRPC]
    public virtual void StopMoving (bool brakeStop) { }

    public virtual void TakeHit (int power) { }

    public virtual void Unhighlight () {
        contextCircle.SetActive(false);
        if (blueCircle.activeInHierarchy == false) {
            icon.sprite = defaultIcon;
        }
    }

    public IEnumerator UpdateFacing () {
// Without this seperate delay for the first cycle, there would be a .1 second delay between movement starting and the rotation updating.
        yield return new WaitForSeconds(0);
        while (true) {
            Vector2 velocityNow = body.velocity;
            if (velocityNow != Vector2.zero) {
                facing = Mathf.Atan2(velocityNow.x, velocityNow.y);
                transform.GetChild(0).rotation = Quaternion.AxisAngle(Vector3.forward, facing * -1 - Mathf.PI);
            }
            yield return new WaitForSeconds(0.1f);
        }        
    }
}
