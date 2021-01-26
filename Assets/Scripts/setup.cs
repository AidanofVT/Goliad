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
        // string debugOut = "";
        // float w  = Random.Range(0, 1000000);
        // float v  = Random.Range(0, 1000000);
        // for (float i = 0; i < 100; ++i) {
        //     debugOut += (int) ((Mathf.PerlinNoise(w + i / 100, (v + i / 100)) + 0.05f) * 2) + ", ";
        // }
        // Debug.Log(debugOut);
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
        int me = PhotonNetwork.LocalPlayer.ActorNumber ;
        int distanceFromCenter = (int) (0.1f * (float) mapSize);
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
        factory_functions maker = home.GetComponent<factory_functions>();
        home.GetComponent<PhotonView>().RPC("addMeat", RpcTarget.All, 300);
        // if (PhotonNetwork.LocalPlayer.ActorNumber == 1) {
        //     GameObject dogOne = maker.makeUnit("dog");
        //     dogOne.transform.position = Vector2.zero;
        //     yield return new WaitForSeconds(0);
        //     dogOne.GetPhotonView().RPC("addMeat", RpcTarget.All, 9);
        // }
        // else {
        //     GameObject orb1 = PhotonNetwork.Instantiate("orb", new Vector3(2, 0, -0.2f), Quaternion.identity);
        //     //GameObject orb2 = PhotonNetwork.Instantiate("orb", new Vector3(4, 0, -0.2f), Quaternion.identity);
        //     orb1.GetComponent<OrbMeatContainer>().fill(6);
        //     //orb2.GetComponent<OrbMeatContainer>().fill(1);
        //     yield return new WaitForSeconds(0);
        // }
        // GameObject dogTwo = maker.makeUnit("dog");
        // dogTwo.transform.position = home.transform.position + new Vector3 (8, -4, 0);
        // GameObject dogThree = maker.makeUnit("dog");
        // dogThree.transform.position = home.transform.position + new Vector3 (4, 4, 0);
        // GameObject dogFour = maker.makeUnit("dog");
        // dogFour.transform.position = home.transform.position + new Vector3 (4, -4, 0);
        // yield return new WaitForSeconds(0);
        // List <Unit_local> listOfDogs = new List<Unit_local>{dogOne.GetComponent<Unit_local>(), 
        //                                                     dogTwo.GetComponent<Unit_local>(), 
        //                                                     dogThree.GetComponent<Unit_local>(), 
        //                                                     dogFour.GetComponent<Unit_local>()};
        // Cohort cohortOfDogs = new Cohort(listOfDogs);
        // Debug.Log("made it this far");
        // yield return new WaitForSeconds(0);
        // cohortOfDogs.makeUnit("dog");
        // yield return null;
        //home.GetComponent<Unit>().die();
    }

    private void OnPlayerConnected() {
        Debug.Log("Someone joined.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("Randomly connecting to a room failed. MESSAGE: " + message);
    }

    void Update () {
        if (Input.GetButtonDown("toggle")) {
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
