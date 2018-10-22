using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class InsideVertex{

    public Vector3 pos;
    public float[] weights;
    public int tetraIndex;
    public InsideVertex() { }

    public InsideVertex(Vector3 pos, float[] weights, int tetraIndex)
    {
        this.pos = pos;
        this.weights = weights;
        this.tetraIndex = tetraIndex;
    }
}