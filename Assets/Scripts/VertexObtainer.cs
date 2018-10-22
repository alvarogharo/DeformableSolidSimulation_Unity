using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexObtainer : MonoBehaviour {	

    public Vector3[] getMesh()
    {
        //Pasamos lo vertices del objeto a coordenadas del mundo
        Vector3[] worldPosition = new Vector3[this.gameObject.GetComponent<MeshFilter>().mesh.vertices.Length];
        int i = 0;
        foreach(Vector3 v in this.gameObject.GetComponent<MeshFilter>().mesh.vertices)
        {
            worldPosition[i] = this.transform.TransformPoint(v);
            i++;
        } 
       return worldPosition;
    }
    public void UpdateMesh(Vector3[] v)
    {
        //Pasamos los vertices del objeto de nuevo a coordenadas locales y actualizamos el mesh
        Vector3[] localPosition = new Vector3[this.gameObject.GetComponent<MeshFilter>().mesh.vertices.Length];
        int i = 0;
        foreach (Vector3 vertex in v)
        {
            localPosition[i] = this.transform.InverseTransformPoint(vertex);
            i++;
        }
        this.gameObject.GetComponent<MeshFilter>().mesh.vertices = localPosition;
        this.gameObject.GetComponent<MeshFilter>().mesh.RecalculateNormals();
    }
}
