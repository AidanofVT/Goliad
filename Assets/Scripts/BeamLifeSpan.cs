using System.Collections;
using UnityEngine;

public class BeamLifeSpan : MonoBehaviour {
    SpriteRenderer line;

    public void Start () {
        line = gameObject.GetComponent<SpriteRenderer>();
        StartCoroutine("lerpBeam");
    }

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
