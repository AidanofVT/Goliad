using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class hoplite_Weapon : WeaponVisualizer {
    
    public override void Show() {
        Vector2 from = transform.position;
        Vector2 to = thisWeapon.target.transform.position;
        Vector2 direction = from - to;
        float radAngle = Mathf.Atan2(direction.y, direction.x);
        GameObject beam = Instantiate((GameObject) Resources.Load("beam"), (transform.position + thisWeapon.target.transform.position) / 2, Quaternion.identity);
        beam.GetComponent<SpriteRenderer>().size = new Vector2(direction.magnitude, 0.3f);
        beam.GetPhotonView().RPC("lerpBeam", RpcTarget.All);
    }

}
