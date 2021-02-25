using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitPositionSorter : IComparer <Unit> {

    enum comparables {compassDirection, distance};
    comparables mode = new comparables();
    Vector2 referencePosition;

    public UnitPositionSorter (Vector2 pointToReference) {
        referencePosition = pointToReference;
    }

    public float DirectionOf (Unit_local inQuestion) {
        Vector2 runRise = (Vector2) inQuestion.transform.position - referencePosition;
        return Mathf.Atan2(runRise.y, runRise.x);
    }

    public float DistanceOf (Unit_local inQuestion) {
        return Vector2.Distance(inQuestion.transform.position, referencePosition);
    }

    public int Compare (Unit a, Unit b) {
        int result = 0;
        if (mode == comparables.compassDirection) {
// positions are sorted into counter-clockwise order
            Vector2 runRiseA = (Vector2) a.gameObject.transform.position - referencePosition;
            float angleA = Mathf.Atan2(runRiseA.y, runRiseA.x);
            Vector2 runRiseB = (Vector2) b.gameObject.transform.position - referencePosition;
            float angleB = Mathf.Atan2(runRiseB.y, runRiseB.x);
            if (angleA < angleB) {
                result = -1;
            }
            else if (angleB < angleA) {
                result = 1;
            }
            else {
                result = 0;
            }
        }
        else if (mode == comparables.distance) {
            float distanceA = Vector2.Distance(a.transform.position, referencePosition);
            float distanceB = Vector2.Distance(b.transform.position, referencePosition);
            if (distanceA < distanceB) {
                result = -1;
            }
            else if (distanceB < distanceA) {
                result = 1;
            }
            else {
                result = 0;
            }
        }
        else {
            throw new InvalidOperationException("Cannot use a relativePositionSorter without first calling DirectionMode() or DistanceMode() on it");            
        }
        return result;
    }

    public void DirectionMode() {
        mode = comparables.compassDirection;
    }

    public void DistanceMode() {
        mode = comparables.distance;
    }

    public void SetReferencePoint (Vector2 pointToReference) {
        referencePosition = pointToReference;
    }

}