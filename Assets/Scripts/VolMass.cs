using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class VolMass : MonoBehaviour
{
    string NodeText;

    //Posiciones por defecto de la mala de tetraedros para permitir moverla en funcion de la posicion del objeto a deformar
    public Vector3 defaultPos;
    public Transform initPos;
    public Vector3 offset;

    public GameObject obj;
    InsideVertex[] innerMesh;

    //Variables de simulacion
    public float mass = 0.005f;
    public float NodeDamping = 10f;
    public float EdgeDamping = 100f;
    public Vector3 Gravity = new Vector3(0, -6, 0);
    public float TimeStep = 0.01f;
    public float stiffness = 150.0f;

    //Diccionario para evitar repetir eges
    Dictionary<string, int> EdgesDictionary;
    

    Vector3[] pos = null;
    List<Vertex> nodes = null;
    List<Edge> edges = null;


    //Datos de la malla de tetraedros
    List<Tetrahedron> tetrahedrons = null;
    List<Face> faces = null;
    int[] triangles = null;

    Mesh m;


    private void Start()
    {

        EdgesDictionary = new Dictionary<string, int>();
        nodes = new List<Vertex>();
        edges = new List<Edge>();

        offset = initPos.position - defaultPos;

        //Metodos de la lectura de archivos tetgen
        read_Node();
        read_Face();
        read_Tetra();
        
        //Obtenemos posiciones de nodos
        Vector3[] pos = new Vector3[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)           
        {
            pos[i] = nodes[i].pos;                      
        }

        //Generamos triangulos
        triangles = new int[faces.Count * 3];      
        for (int i = 0; i < faces.Count; i++)
        {            
            triangles[i * 3] = faces[i].x;
            triangles[i * 3 + 1] = faces[i].y;
            triangles[i * 3 + 2] = faces[i].z;
        }
        

        //Creamos la maya
        m = new Mesh();
        m.vertices = pos;
        m.triangles = triangles;
        m.RecalculateNormals();

        //Asigna los vertices a los nodos de del objeto de tetrahedros
        asignVetrices();
    }
   
    public void FixedUpdate()
    {       
        stepSymplectic();

        //Recalculamos la posición de los nodos.
        pos = new Vector3[nodes.Count]; 
     
        for (int i = 0; i < nodes.Count; i++)
        {
            pos[i] = nodes[i].pos;          
        }

        m.vertices = pos;                   


        //Recalculamos las poiciones de objeto a deformar con pesos
        List<Vector3> inner_Pos = new List<Vector3>();
        foreach(InsideVertex i in innerMesh)
        {            
            if (i.tetraIndex != -1)
            {

                Tetrahedron t = tetrahedrons[i.tetraIndex];
                i.pos = i.weights[0] * nodes[t.x].pos + i.weights[1] * nodes[t.y].pos + i.weights[2] * nodes[t.z].pos + i.weights[3] * nodes[t.w].pos;
                
            }
            inner_Pos.Add(i.pos);
        }

        //Se asignan las nuevas posiciones
        obj.GetComponent<VertexObtainer>().UpdateMesh(inner_Pos.ToArray());

        //Recalculamos las normales.
        m.RecalculateNormals();

    }
    private void stepSymplectic()
    {
        foreach (Vertex node in nodes)
        {
            node.force = Vector3.zero;             
        }


        //Calculo de masas, volumenes y fuerzas de nodos
        foreach (Tetrahedron tetra in tetrahedrons)
        {
            float nodeMass = mass * tetra.volume/4;                        
            
            float edgeVolume = tetra.volume / 6;                       
                                                                            

            nodes[tetra.x].ComputeForces(this.Gravity, nodeMass);
            nodes[tetra.y].ComputeForces(this.Gravity, nodeMass);
            nodes[tetra.z].ComputeForces(this.Gravity, nodeMass);
            nodes[tetra.w].ComputeForces(this.Gravity, nodeMass);


            foreach (int index in tetra.edges)
            {
                edges[index].Volume += edgeVolume;
                
            }
        }

        //Calculo de masas, volumenes y fuerzas de aristas
        foreach (Edge a in edges)
        {
            a.ComputeForces();                              

            nodes[a.indexA].force = a.nodeA.force;          
            nodes[a.indexB].force = a.nodeB.force;
        }

        //Nuevas posiciones y velocidades si los nodos no son fijos
        foreach (Vertex node in nodes)
        {
            if (!node.Fixed)                    
            {
                node.Vel += node.force * TimeStep / node.mass;              
                node.pos += node.Vel * TimeStep;                           
                            
            }
        }


    }

    //Pinta la maya de tetraedros en la escena
    void OnDrawGizmos()
    {
        if (edges != null)
        {
            foreach (Edge n in edges)
            {
                Gizmos.DrawLine(n.nodeA.pos, n.nodeB.pos);
            }
        }
    }

    

    //Metodos de lectura de tegen
    void read_Face()            
    {
        StreamReader reader = new StreamReader("./Assets/Tetahedron Mesh/LowTree.1.face");  
        int index = 0;

        while (!reader.EndOfStream)                 
        {
            NodeText = reader.ReadLine();
            
            string[] line = NodeText.Split(' ');

            if (index == 0)             
            {
                faces = new List<Face>();            
            }       
            else
            {
                if (line[0] != "#")                                 
                {

                    int index_aux = 0;
                    int c1 = 0;
                    int c2 = 0;
                    int c3 = 0;
                   
                    for (int i = 0; i < line.Length; i++)
                    {
                        if ((!line[i].Contains(" ")) && (line[i] != ""))       
                        {
                            if (index_aux != 0 && index_aux != 4)
                            {
                                
                                if (index_aux == 1)
                                {
                                    c1 = Int32.Parse(line[i]);
                                }
                                if (index_aux == 2)
                                {
                                    c2 = Int32.Parse(line[i]);
                                }
                                if (index_aux == 3)
                                {
                                    c3 = Int32.Parse(line[i]);
                                }
                            }
                            index_aux++;

                        }
                    }
                    
                    faces.Add(new Face(c1, c2, c3));
                }
                else
                {
                    return;
                }
            }
            index++;
        }
    }

   
    void read_Node()        
    {
        StreamReader reader = new StreamReader("./Assets/Tetahedron Mesh/LowTree.1.node");
        int index = 0;
        while (!reader.EndOfStream)
        {
            NodeText = reader.ReadLine();
            string[] line = NodeText.Split(' ');

            if (index == 0)
            {
                nodes = new List<Vertex>();
            }
            else
            {
                if (line[0] != "#")
                {

                    int index_aux = 0;
                    float x = 0;
                    float y = 0;
                    float z = 0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        if ((!line[i].Contains(" ")) && (line[i] != ""))
                        {
                            if (index_aux != 0 && index_aux != 4)
                            {
                                if (index_aux == 1)
                                {
                                    x = float.Parse(line[i]);
                                }
                                if (index_aux == 2)
                                {
                                    y = float.Parse(line[i]);
                                }
                                if (index_aux == 3)
                                {
                                    z = float.Parse(line[i]);
                                }
                            }
                            index_aux++;

                        }
                    }

                    //Generamos el obje
                    Vertex v = new Vertex();
                    v.pos = new Vector3(x+offset.x, y+offset.y, z+offset.z);
                    v.mass = 0;
                    v.Vel = new Vector3(0, 0, 0);
                    v.force = new Vector3(0.0f, 0.0f, 0.0f);
                    v.damping = NodeDamping;

                    GameObject[] fixers = GameObject.FindGameObjectsWithTag("Fixer");

                    foreach (GameObject f in fixers){
                        if (f.GetComponent<Collider>().bounds.Contains(v.pos))
                        {
                            v.Fixed = true;
                        }
                    }
                    print("MAMA");
                    
                    nodes.Add(v);
                }
                else
                {
                    return;
                }
            }
            index++;
        }
    }
    
    void read_Tetra()
    {
        StreamReader reader = new StreamReader("./Assets/Tetahedron Mesh/LowTree.1.ele");
        print("LOL");
        int index = 0;
        while (!reader.EndOfStream)
        {
            NodeText = reader.ReadLine();
            string[] line = NodeText.Split(' ');

            if (index == 0)
            {
                tetrahedrons = new List<Tetrahedron>();
            }
            else
            {
                if (line[0] != "#")                 
                {
                    int index_aux = 0;
                    int v1 = 0;
                    int v2 = 0;
                    int v3 = 0;
                    int v4 = 0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        if ((!line[i].Contains(" ")) && (line[i] != ""))
                        {
                            if (index_aux != 0)
                            {
                                if (index_aux == 1)
                                {
                                    v1 = Int32.Parse(line[i]);
                                }
                                if (index_aux == 2)
                                {
                                    v2 = Int32.Parse(line[i]);
                                }
                                if (index_aux == 3)
                                {
                                    v3 = Int32.Parse(line[i]);
                                }
                                if (index_aux == 4)
                                {
                                    v4 = Int32.Parse(line[i]);
                                }
                            }
                            index_aux++;

                        }
                    }

                   
                    float volume = Mathf.Abs(Vector3.Dot((nodes[v1].pos - nodes[v4].pos), Vector3.Cross(nodes[v2].pos - nodes[v4].pos, nodes[v3].pos - nodes[v4].pos))) / 6;   
                    Tetrahedron t = new Tetrahedron(v1, v2, v3, v4);
                    t.volume = volume;

                    
                    for(int i=0; i<6; i++)
                    {   
                        
                        int i1 = 0;
                        int i2 = 0;  
                        if (i == 0)             //Edge x->y
                        {
                            i1 = t.x;
                            i2 = t.y;
                        }else if(i == 1)       //Edge x->z
                        {
                            i1 = t.x;
                            i2 = t.z;
                        }else if (i == 2)       //Edge x->w
                        {
                            i1 = t.x;
                            i2 = t.w;
                        }else if (i == 3)      //Edge y->z
                        {
                            i1 = t.y;
                            i2 = t.z;
                        }
                        else if (i == 4)      //Edge y-w
                        {
                            i1 = t.y;
                            i2 = t.w;
                        }else                   //Edge w-><
                        {
                            i1 = t.w;
                            i2 = t.z;
                        }

                        
                        Edge a = new Edge();
                        a.nodeA = nodes[i1];
                        a.indexA = i1;
                        a.nodeB = nodes[i2];
                        a.indexB = i2;
                        a.damping = EdgeDamping;
                        string key = (i1  + " , " + i2);
                        string inverseKey = (i2 + " , " + i1);

                        
                        if (EdgesDictionary.ContainsKey(key) || EdgesDictionary.ContainsKey(inverseKey))          
                       
                        {

                            if (EdgesDictionary.ContainsKey(key))
                            {
                                t.edges.Add(EdgesDictionary[key]);       
                            }else if (EdgesDictionary.ContainsKey(inverseKey))
                            {
                                t.edges.Add(EdgesDictionary[inverseKey]);
                            }
                        }
                        else
                        {   

                            a.Stiffness = stiffness;
                            a.Length = (a.nodeA.pos - a.nodeB.pos).magnitude;
                            a.Length0 = (a.nodeA.pos - a.nodeB.pos).magnitude;
                            edges.Add(a);

                            EdgesDictionary.Add(key, edges.Count-1);

                            t.edges.Add(edges.Count - 1);
                        }
                    }
                    tetrahedrons.Add(t);
                }
                else
                {
                    return;
                }
            }
            index++;
        }
    }



    void asignVetrices()
    {
        Vector3[] pos = obj.GetComponent<VertexObtainer>().getMesh();              
        innerMesh = new InsideVertex[pos.Length];                             
        for (int i = 0 ; i < pos.Length; i++)
        {
            innerMesh[i] = new InsideVertex(pos[i], null, -1);

            foreach (Tetrahedron teta in tetrahedrons)
            {
                Vector3 v =  pos[i];

                if (isInside(nodes[teta.x].pos, nodes[teta.y].pos, nodes[teta.z].pos, nodes[teta.w].pos, v))
                {
                    float[] peso = new float[4];                


                    peso[0] = (Mathf.Abs(Vector3.Dot((v - nodes[teta.w].pos), Vector3.Cross(nodes[teta.y].pos - nodes[teta.w].pos , nodes[teta.z].pos - nodes[teta.w].pos))) / 6) / teta.volume;
                    peso[1] = (Mathf.Abs(Vector3.Dot((nodes[teta.x].pos - nodes[teta.w].pos),Vector3.Cross(v - nodes[teta.w].pos, nodes[teta.z].pos - nodes[teta.w].pos))) / 6) / teta.volume;
                    peso[2] = (Mathf.Abs(Vector3.Dot((nodes[teta.x].pos - nodes[teta.w].pos),Vector3.Cross(nodes[teta.y].pos - nodes[teta.w].pos , v - nodes[teta.w].pos))) / 6) / teta.volume;
                    peso[3] = (Mathf.Abs(Vector3.Dot((nodes[teta.x].pos - v), Vector3.Cross(nodes[teta.y].pos - v, nodes[teta.z].pos - v))) / 6) / teta.volume;
                    
                    innerMesh[i] = new InsideVertex(pos[i], peso, tetrahedrons.IndexOf(teta));

                }                
            }
        }
    }

    public Boolean isInside(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Vector3 x)
    {
        //El método para calcular si un punto está dentro de un Tetrahedron es el siguiente:
        //Para que un punto se encuentre en un tetradro:
        //  -Se calcula el vector normal respecto a la base del Tetrahedron, llamado u.
        //  -Si u.x > u.A
        //  -Y además, si u.x < u.D
        //  -Es decir, si el producto escalar del punto por la normal se encuentra acotado inferior y superiormente
        //   por u.D y u.A, entonces dicho punto está dentro.


        Vector3 ABC = Vector3.Cross(A - B, A - C);
        Vector3 BDC = Vector3.Cross(B - D, B - C);
        Vector3 ADB = Vector3.Cross(A - D, A - B);
        Vector3 DAC = Vector3.Cross(D - A, D - C);

        bool condicion1 = (Vector3.Dot(ABC, D) > Vector3.Dot(ABC, x)) && (Vector3.Dot(ABC, x) > Vector3.Dot(ABC, A));
        bool condicion2 = (Vector3.Dot(BDC, A) > Vector3.Dot(BDC, x)) && (Vector3.Dot(BDC, x) > Vector3.Dot(BDC, B));
        bool condicion3 = (Vector3.Dot(ADB, C) > Vector3.Dot(ADB, x)) && (Vector3.Dot(ADB, x) > Vector3.Dot(ADB, A));
        bool condicion4 = (Vector3.Dot(DAC, B) > Vector3.Dot(DAC, x)) && (Vector3.Dot(DAC, x) > Vector3.Dot(DAC, D));

        return condicion1 &&  condicion2 &&   condicion3   &&   condicion4;
    }
}