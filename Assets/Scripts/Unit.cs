using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit : MonoBehaviourPun {
    protected GameState gameState;
    protected BarManager statusBar;
    public UnitBlueprint stats;
    public Weapon weapon;
    public Cohort soloCohort;
    public Cohort cohort;
    public float facing = 0;
    public int meat = 0;
    public int strikes = 3;

    void Awake () {
        string spriteAddress = gameObject.name;
        spriteAddress = spriteAddress.Remove(spriteAddress.IndexOf("("));
        spriteAddress = "Sprites/" + spriteAddress;
        if (photonView.Owner.ActorNumber == 1) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(spriteAddress + "_white");
        }
        else if (photonView.Owner.ActorNumber == 2) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(spriteAddress + "_orange");
        }
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

    public virtual void Start () {
        statusBar = transform.GetChild(1).GetComponent<BarManager>();
        ignition();
//this needs to be here, rather than in Awake, so that if there's starting meat then the BarManager sees the right abount of meat when it wakes up
        statusBar.gameObject.SetActive(true);
        addMeat(stats.startingMeat);
    }

    public virtual void ignition () {
    }

    [PunRPC]
    public bool addMeat (int toAdd) {
        Debug.Log("uh... addmeat?");
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
