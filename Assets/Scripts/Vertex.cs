using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
public class Vertex
{
    public float damping;
    public float mass;
    public Vector3 pos;
    public Vector3 force;
    public bool Fixed;
    public Vector3 Vel;


    public Vertex()
    {
        mass = 10;
        Fixed = false;
        Vel = new Vector3(0.0f, 0.0f, 0.0f);
        force = new Vector3(0.0f, 0.0f, 0.0f);
    }

    //Calcula las fuerzas aplicadas a los vertices
    public void ComputeForces(Vector3 gravity, float masa_Nodo)
    {
        mass = masa_Nodo;
        force += -1 * damping * Vel;
        force += mass * gravity;
    }
}