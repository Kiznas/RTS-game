using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BoxFormation : FormationBase {
    [FormerlySerializedAs("_unitWidth")] [SerializeField] private int unitWidth = 5;
    [FormerlySerializedAs("_unitDepth")] [SerializeField] private int unitDepth = 5;
    [FormerlySerializedAs("_hollow")] [SerializeField] private bool hollow = false;
    [FormerlySerializedAs("_nthOffset")] [SerializeField] private float nthOffset = 0;

    public override IEnumerable<Vector3> EvaluatePoints() {
        var middleOffset = new Vector3(unitWidth * 0.5f, 0, unitDepth * 0.5f);

        for (var x = 0; x < unitWidth; x++) {
            for (var z = 0; z < unitDepth; z++) {
                if (hollow && x != 0 && x != unitWidth - 1 && z != 0 && z != unitDepth - 1) continue;
                var pos = new Vector3(x + (z % 2 == 0 ? 0 : nthOffset), 0, z);

                pos -= middleOffset;

                pos += GetNoise(pos);

                pos *= spread;

                yield return pos;
            }
        }
    }
}