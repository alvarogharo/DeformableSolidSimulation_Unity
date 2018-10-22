using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class Tetrahedron
{
    public int x;
    public int y;
    public int z;
    public int w;
    public float volume;
    public List<int> edges;
    public Tetrahedron(int x, int y, int z, int w)
    {
        edges = new List<int>();
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
}