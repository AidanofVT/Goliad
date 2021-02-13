using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class setup : MonoBehaviourPunCallbacks {
    public GameState gameState;
    public int mapSize = 100;

    void Awake() {
        gameState = gameObject.GetComponent<GameState>();
//10 = ground, 5 = UI, 8 = obstacles, 11 = units
        Physics2D.IgnoreLayerCollision(10, 11);
        Physics2D.IgnoreLayerCollision(10, 8);
        Physics2D.IgnoreLayerCollision(10, 5);
        Physics2D.IgnoreLayerCollision(5, 11);
        Physics2D.IgnoreLayerCollision(5, 8);
        Physics2D.IgnoreLayerCollision(5, 5);
        Physics2D.queriesHitTriggers = false;
    }

    void Start () {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster() {
        Debug.Log("Connected to master. " + PhotonNetwork.CountOfRooms + " rooms open in " + PhotonNetwork.ServerAddress + ". Game version " + PhotonNetwork.AppVersion + ". Player ID = " + PhotonNetwork.LocalPlayer.UserId);
        if (PhotonNetwork.CountOfRooms == 0) {
            PhotonNetwork.CreateRoom(Random.Range(0, 10000).ToString());
        }
        else {
            PhotonNetwork.JoinRandomRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause) {
        Debug.Log("PUN connection failure. CAUSE: " + cause);
    }

    public override void OnJoinedRoom() {
        Debug.Log("Joined room " + PhotonNetwork.CurrentRoom.Name + ". Player number " + PhotonNetwork.LocalPlayer.ActorNumber);
        int me = PhotonNetwork.LocalPlayer.ActorNumber;
        gameState.playerNumber = me;
        int distanceFromCenter = (int) (0.2f * (float) mapSize);
        Vector3 startPlace = Vector3.zero;
        if (me == 1) {
            startPlace = new Vector3 (-distanceFromCenter, distanceFromCenter, -.2f);
        }
        else if (me == 2) {
            startPlace = new Vector3 (distanceFromCenter, -distanceFromCenter, -.2f);
        }
        Camera.main.transform.position = startPlace + new Vector3(0, 0, -10);
        StartCoroutine(step2(startPlace));
    }

    IEnumerator step2 (Vector3 startPlace) {
        GameObject home = PhotonNetwork.Instantiate("Units/depot", startPlace, Quaternion.identity);
        yield return new WaitForSeconds(0);
        home.GetComponent<Unit>().addMeat(300);
        AstarPath.active.UpdateGraphs(new Bounds(Vector3.zero, new Vector3 (4, 4, 1)));
        // GameObject topInner = PhotonNetwork.Instantiate("Units/dog", new Vector3(0, 0, -0.2f), Quaternion.identity);
        // GameObject topPort = PhotonNetwork.Instantiate("Units/dog", new Vector3(3, 0, -0.2f), Quaternion.identity);
        // GameObject topStarboard = PhotonNetwork.Instantiate("Units/dog", new Vector3(6, 0, -0.2f), Quaternion.identity);
        // GameObject bottomInner = PhotonNetwork.Instantiate("Units/dog", new Vector3(9, 0, -0.2f), Quaternion.identity);
        // GameObject bottomPort = PhotonNetwork.Instantiate("Units/dog", new Vector3(12, 0, -0.2f), Quaternion.identity);
        // GameObject bottomStarboard = PhotonNetwork.Instantiate("Units/dog", new Vector3(15, 0, -0.2f), Quaternion.identity);
        // GameObject leftInner = PhotonNetwork.Instantiate("Units/dog", new Vector3(18, 0, -0.2f), Quaternion.identity);
        // GameObject leftPort = PhotonNetwork.Instantiate("Units/dog", new Vector3(21, 0, -0.2f), Quaternion.identity);
        // GameObject leftStarboard = PhotonNetwork.Instantiate("Units/dog", new Vector3(24, 0, -0.2f), Quaternion.identity);
        // GameObject rightInner = PhotonNetwork.Instantiate("Units/dog", new Vector3(27, 0, -0.2f), Quaternion.identity);
        // GameObject rightPort = PhotonNetwork.Instantiate("Units/dog", new Vector3(30, 0, -0.2f), Quaternion.identity);
        // GameObject rightStarboard = PhotonNetwork.Instantiate("Units/dog", new Vector3(33, 0, -0.2f), Quaternion.identity);
        // yield return new WaitForSeconds(0);
        // Unit_local topInnerScript = topInner.GetComponent<Unit_local>();
        // Unit_local topPortScript = topPort.GetComponent<Unit_local>();
        // Unit_local topStarboardScript = topStarboard.GetComponent<Unit_local>();
        // Unit_local bottomInnerScript = bottomInner.GetComponent<Unit_local>();
        // Unit_local bottomPortScript = bottomPort.GetComponent<Unit_local>();
        // Unit_local bottomStarboardScript = bottomStarboard.GetComponent<Unit_local>();
        // Unit_local leftInnerScript = leftInner.GetComponent<Unit_local>();
        // Unit_local leftPortScript = leftPort.GetComponent<Unit_local>();
        // Unit_local leftStarBoardScript = leftStarboard.GetComponent<Unit_local>();
        // Unit_local rightInnerScript = rightInner.GetComponent<Unit_local>();
        // Unit_local rightPortScript = rightPort.GetComponent<Unit_local>();
        // Unit_local rightStarboardScript = rightStarboard.GetComponent<Unit_local>();
        // Cohort newCohort = new Cohort(new List<Unit_local> {topInnerScript, topPortScript, topStarboardScript, bottomInnerScript, bottomPortScript, bottomStarboardScript,
        //                                 leftInnerScript, leftPortScript, leftStarBoardScript, rightInnerScript, rightPortScript, rightStarboardScript});
        // newCohort.MoveCohortBeta(new Vector2(18, 2), null);
    }

    private void OnPlayerConnected() {
        Debug.Log("Someone joined.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("Randomly connecting to a room failed. MESSAGE: " + message);
    }

    void Update () {
        if (Input.GetButtonDown("debug overlay toggle")) {
            DebugStuff.BuildDebugger thiScript = Camera.main.transform.parent.GetComponent<DebugStuff.BuildDebugger>();
            if (thiScript.enabled == false) {
                thiScript.enabled = true;
            }
            else {
                thiScript.enabled = false;
            }
        }
    }

}
