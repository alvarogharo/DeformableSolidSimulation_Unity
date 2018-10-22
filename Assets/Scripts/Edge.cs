using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class Edge
{
    public float damping;
    public float Stiffness;
    public int indexA;
    public int indexB;
    public  Vertex nodeA;
    public Vertex nodeB;
    public float Length0;
    public float Length;
    public float Volume;
    public Edge()
    {
        nodeA = new Vertex();
        nodeB = new Vertex();
        Stiffness = 10;
    }

    //Calcula las fuerzas que son aplicadas a las aristas
    public void ComputeForces()
    {

        Vector3 dir = nodeA.pos - nodeB.pos;
        Length = dir.magnitude;
        dir = dir * (1.0f / Length);
        Vector3 Force = -(Volume / (Mathf.Pow(Length0, 2))) * Stiffness * (Length - Length0) * (dir / Length);
        nodeA.force += Force;
        nodeB.force -= Force;

    }
}