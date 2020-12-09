using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MobileUnit_local : Unit_local {
    protected AidansMovementScript moveConductor;

    protected override void dispenseOutranged() {
        if (task.nature == Task.actions.give) {
            Transform toSeek = task.objectUnit.transform;
            moveConductor.setDestination(toSeek.position, toSeek, 7);
        }
        else {
            Transform toSeek = task.subjectUnit.transform;
            task.subjectUnit.GetComponent<AidansMovementScript>().setDestination(toSeek.position, toSeek, 7);
        }        
    }
    
    public override void ignition () {
        StartForLocals();
        moveConductor = GetComponent<AidansMovementScript>();
    }

//if network traffic is an issue in the future, and CPU load isn't too bad, maybe we could put these in MobileUnit_remote too and slow down the photon update rate?
    public override void move (GameObject goTo, float precision = -1) {
        if (stats.isArmed) {
            weapon.disengage();
        }
        task = new Task (gameObject, goTo, Task.actions.move);
        Transform toFollow = null;
        if (goTo.tag == "unit") {
            toFollow = goTo.transform;
        }
        if (precision == -1) {
            moveConductor.setDestination(goTo.transform.position, toFollow);            
        }
        else {
            moveConductor.setDestination(goTo.transform.position, toFollow, precision); 
        }
    }

    public virtual void pathEnded () {
        if (task != null && task.nature != Task.actions.move) {
            if (task.objectUnit.activeInHierarchy == false) {
                task = null;
            }
            else {
                switch (task.nature.ToString())
                {
                    case "give":
                        StartCoroutine(dispense());
                        break;
                    case "take":
                        StartCoroutine(dispense());
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

    public virtual void stop () {
        moveConductor.terminatePathfinding();
    }

}