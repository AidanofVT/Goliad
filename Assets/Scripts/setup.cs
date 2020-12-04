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
        Physics2D.IgnoreLayerCollision(10, 5);
        Physics2D.IgnoreLayerCollision(10, 8);
        Physics2D.IgnoreLayerCollision(5, 8);
        Physics2D.IgnoreLayerCollision(5, 11);
        Physics2D.queriesHitTriggers = false;
    }

    void Start () {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster() {
        Debug.Log("Connected to master. " + PhotonNetwork.CountOfRooms + " rooms open. Game version " + PhotonNetwork.AppVersion + ". Player ID = " + PhotonNetwork.LocalPlayer.UserId);
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
        int me = PhotonNetwork.LocalPlayer.ActorNumber ;
        int distanceFromCenter = (int) (0.3f * (float) mapSize);
        Vector3 startPlace = Vector3.zero;
        if (me == 1) {
            startPlace = new Vector3 (-distanceFromCenter, distanceFromCenter, -.2f);
        }
        else if (me == 2) {
            startPlace = new Vector3 (distanceFromCenter, -distanceFromCenter, -.2f);
        }
        GameObject home = PhotonNetwork.Instantiate("Units/homebase", startPlace, Quaternion.identity);
        StartCoroutine("step2", home);
    }

    IEnumerator step2 (GameObject home) {
        yield return new WaitForSeconds(0);
        AstarPath.active.UpdateGraphs(new Bounds(Vector3.zero, new Vector3 (4, 4, 1)));
        // GameObject hoplite = home.GetComponent<factory_functions>().makeUnit("Hoplite");
        // hoplite.transform.position = home.transform.position / 4;
        Vector3 shift = new Vector3(3, 3, 0);
        GameObject dog1 = home.GetComponent<factory_functions>().makeUnit("dog");
        dog1.transform.position = (home.transform.position / 4) + shift;
        GameObject dog2 = home.GetComponent<factory_functions>().makeUnit("dog");
        dog2.transform.position = (home.transform.position / 2) + shift;
        GameObject dog3 = home.GetComponent<factory_functions>().makeUnit("dog");
        dog3.transform.position = (home.transform.position / 4) - shift;
        GameObject dog4 = home.GetComponent<factory_functions>().makeUnit("dog");
        dog4.transform.position = (home.transform.position / 2) - shift;
        yield return new WaitForSeconds(0);
        Unit_local[] northeastMembers = {dog1.GetComponent<Unit_local>(), dog2.GetComponent<Unit_local>()};
        Cohort northeast = new Cohort(new List<Unit_local>(northeastMembers));
        Unit_local[] southwestMembers = {dog3.GetComponent<Unit_local>(), dog4.GetComponent<Unit_local>()};
        Cohort southwest = new Cohort(new List<Unit_local>(southwestMembers));
    }

    private void OnPlayerConnected() {
        Debug.Log("Someone joined.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("Randomly connecting to a room failed. MESSAGE: " + message);
    }

}
