using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MobileUnit_local : Unit_local {
    public AidansMovementScript moveConductor;

    protected override void DispenseOutranged() {
        Transform toSeek = task.objectUnit.transform;
        Move(toSeek.position, toSeek.gameObject.GetPhotonView().ViewID, -1, 7);
    }
    
    public override void Ignition () {
        StartForLocals();
        moveConductor = GetComponent<AidansMovementScript>();
        AddMeat(stats.startingMeat);
    }

    public override void Move (Vector2 goTo, int leaderID = -1, float speed = -1, float arrivalThreshholdOverride = -1) {
        double whenToStart = PhotonNetwork.Time + PhotonNetwork.GetPing() * 0.0015;
        int noiseStartPoint = Random.Range(0, 100);
        // Vector2 nowGoing = body.velocity;
        // Vector2 nowAt = transform.position;
        // photonView.RPC("Move", RpcTarget.Others, whenToStart, nowGoing.x, nowGoing.y, nowAt.x, nowAt.y, goTo.x, goTo.y, noiseStartPoint, leaderID, speed, arrivalThreshholdOverride);
        moveConductor.Go(goTo, whenToStart, noiseStartPoint, leaderID, speed, arrivalThreshholdOverride); 
    }

    public virtual void PathEnded () {
        if (task != null) { 
            if (task.nature != Task.actions.move) {
// I can't remember what the case is where there is a task, it's not movement, and there's no objectUnit. I'm commenting this out. See what breaks:
                // if (task.objectUnit == null) {
                //     task = null;
                // }
                // else {
                    switch (task.nature.ToString()) {
                        case "give":
                        case "take":
                            StartCoroutine("Dispense", null);
                            break;
                        case "attack":
                            break;
                        default:
                            break;
                    }
                // }
            }
            else {
                cohort.TaskCompleted(task);
                task = null;
            }
        }
    }

    [PunRPC]
    public override void StopMoving (bool brakeStop) {
        if (moveConductor.GetRunningState() == true) {
            moveConductor.TerminatePathfinding(false, brakeStop);
        }
    }

}