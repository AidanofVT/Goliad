using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MobileUnit_local : Unit_local {
    protected AidansMovementScript moveConductor;

    public override void give(GameObject toWho, int howMuch) {
        task = new Task(gameObject, toWho, Task.actions.give, howMuch);
        if (Vector2.Distance(transform.position, toWho.transform.position) > 10) {
            Transform toSeek = toWho.transform;
            moveConductor.setDestination(toSeek.position, toSeek, 7);
        }
        else {
            StartCoroutine(dispense());
        }
    }

    protected override void dispenseOutranged() {
        Transform toSeek = task.objectUnit.transform;
        moveConductor.setDestination(toSeek.position, toSeek, 7);        
    }
    
    public override void ignition () {
        StartForLocals();
        moveConductor = GetComponent<AidansMovementScript>();
    }

//if network traffic is an issue in the future, and CPU load isn't too bad, maybe we could put these in MobileUnit_Local too and slow down the photon update rate?
    public virtual void move (Vector3 target, Transform movingTransform = null) {
        weapon.disengage();
        task = new Task (gameObject, null, Task.actions.move);
        moveConductor.setDestination(target, movingTransform);
    }

    public void pathEnded () {
        if (task.nature != Task.actions.move) {
            if (task.objectUnit.activeInHierarchy == false) {
                task = null;
            }
            else {
                switch (task.nature.ToString())
                {
                    case "give":
                        dispense();
                        break;
                    default:
                        Debug.Log("a path-ended message was sent while there was no valid task");
                        break;
                }
            }
        }
        else {
            task = null;
        }
    }

    public virtual void stop () {
        moveConductor.terminatePathfinding();
    }

}