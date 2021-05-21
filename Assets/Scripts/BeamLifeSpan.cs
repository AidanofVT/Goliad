using System.Collections;
using UnityEngine;

public class BeamLifeSpan : MonoBehaviour {
    SpriteRenderer line;

    public void Start () {
        line = gameObject.GetComponent<SpriteRenderer>();
        StartCoroutine("LerpBeam");
    }

    public IEnumerator LerpBeam () {
        Vector2 startValue = new Vector2(line.size.x, line.size.y);
        Vector2 endValue = new Vector2(line.size.y, 0);
        float startTime = Time.time;
        float progress = 0;
        while (progress < 0.4f) {
            progress = ((Time.time - startTime) / 0.4f);
            line.size = Vector2.Lerp(startValue, endValue, progress);
            Color beamColor = line.material.color;
            beamColor.a = 1 - progress;
            line.color = beamColor;
            yield return new WaitForSeconds(0);
        }
        Destroy(gameObject);
        yield return null;
    }
}
