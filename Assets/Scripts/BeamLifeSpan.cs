using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BeamLifeSpan : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback {
    SpriteRenderer line;

    public void OnPhotonInstantiate (PhotonMessageInfo info) {
        object[] instantiationData = info.photonView.InstantiationData;
        line = gameObject.GetComponent<SpriteRenderer>();
        line.size = new Vector2 ((float) instantiationData[0], 0.3f);        
    }

    [PunRPC]
    public IEnumerator lerpBeam () {
        float fullWidth = line.size.y;
        float startTime = Time.time;
        float progress = 0;
        line.enabled = true;
        while (progress < 0.4f) {
            progress = ((Time.time - startTime) / 0.4f);
            line.size = new Vector2 (line.size.x, (1 - progress) * fullWidth);
            Color beamColor = line.material.color;
            beamColor.a = 1 - progress;
            line.color = beamColor;
            yield return new WaitForSeconds(0);
        }
        Destroy(gameObject);
        yield return null;
    }
}
