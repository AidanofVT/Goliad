using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MobileUnit_local : Unit_local {
    protected AidansMovementScript moveConductor;

    protected override void dispenseOutranged() {
        if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
            Transform toSeek = task.objectUnit.transform;
            moveConductor.setDestination(toSeek.position, toSeek, 7);
        }      
    }
    
    public override void ignition () {
        StartForLocals();
        moveConductor = GetComponent<AidansMovementScript>();
    }

//if network traffic is an issue in the future, and CPU load isn't too bad, maybe we could put these in MobileUnit_remote too and slow down the photon update rate?
    public override void move (Vector2 goTo, GameObject toFollow) {
        Transform leader = null;
        if (toFollow != null) {
            leader = toFollow.transform;
        }
        if (stats.isArmed) {
            weapon.disengage();
        }
        moveConductor.setDestination(goTo, leader, bodyCircle.radius);            
    }

    public virtual void PathEnded () {
        if (task != null) { 
            if (task.nature != Task.actions.move) {
                if (task.objectUnit.activeInHierarchy == false) {
                    task = null;
                }
                else {
                    switch (task.nature.ToString()) {
                        case "give":
                            StartCoroutine(dispense());
                            break;
                        case "take":
                            StartCoroutine(dispense());
                            break;
                        case "attack":
                            break;
                        default:
                            Debug.Log("a path-ended message was sent while there was no valid task");
                            break;
                    }
                }
            }
            else {
                cohort.taskCompleted(task);
                task = null;
            }
        }
    }

    public override void StopMoving () {
        if (moveConductor.isRunning) {
            moveConductor.terminatePathfinding();
        }
    }

}