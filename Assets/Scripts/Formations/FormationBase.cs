using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class FormationBase : MonoBehaviour {
    [FormerlySerializedAs("_noise")] [SerializeField] [Range(0, 1)] protected float noise = 0;
    [FormerlySerializedAs("Spread")] [SerializeField] protected float spread = 1;
    public abstract IEnumerable<Vector3> EvaluatePoints();

    public Vector3 GetNoise(Vector3 pos) {
        var noise = Mathf.PerlinNoise(pos.x * this.noise, pos.z * this.noise);

        return new Vector3(noise, 0, noise);
    }
}