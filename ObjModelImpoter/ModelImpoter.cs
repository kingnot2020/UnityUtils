using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Globalization;

public class ModelImport : AssetPostprocessor
{
    private string _fileName;
    private string _extension;
    private string _fileFullPath;

    //模型导入前设置
    void OnPreprocessModel()
    {
        Debug.Log("ModelImport.OnPreprocessModel");
        _fileName = assetPath.Replace("Assets", "");
        _extension = _fileName.Substring(_fileName.Length - 4);
        _fileFullPath = Application.dataPath + _fileName;

        if (!File.Exists(_fileFullPath) || !_extension.Equals(".obj")) return;

        ModelImporter modelImporter = (ModelImporter)assetImporter;
        modelImporter.preserveHierarchy = false;
        modelImporter.optimizeMeshPolygons = false;
        modelImporter.optimizeMeshVertices = false;
        modelImporter.weldVertices = false;
        modelImporter.importNormals = ModelImporterNormals.None;
        /* 
            You can add extra modelImporter settings here. 
            Bear in mind that:
            - Preserve Hierarchy = false
            - Optimize Mesh = false
            - Weld Vertices = false
            - Import Normals = None
            need to be for vertex colors to be loaded correctly.
        */
    }

    private void OnAssignMaterialModel(Material material, Renderer renderer)
    {
        Debug.Log("ModelImport.OnAssignMaterialModel");

        ModelImporter modelim = assetImporter as ModelImporter;
        //modelim.
    }

    void OnPostprocessModel(GameObject gameObject)
    {
        Debug.Log("ModelImport.OnPostprocessModel");
        if (!File.Exists(_fileFullPath) || !_extension.Equals(".obj")) return;

        string[] lines = File.ReadAllLines(_fileFullPath);

        /*
            Find the order at which Objects Vertex Data is written in the .obj file.
            .obj file "g" lines seem to hold the accurate order.
        */
        List<string> ids = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string[] tokens = lines[i].Split(' ');
            if (tokens[0].Equals("g"))
            {
                ids.Add(tokens[1]);
            }
        }

        // Remove id duplicates.
        ids = ids.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();

        // - MESH FILTERS ORDER ALGORITHM -
        Transform root = gameObject.transform;
        if (ids.Count == 0 && root.childCount > 0)
        {
            Transform child = root.GetChild(0);
            ids.Add(child.name);
        }
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        foreach (string id in ids)
        {
            foreach (Transform child in root)
            {
                if (child.name.Equals(id))
                {
                    meshFilters.Add(child.GetComponent<MeshFilter>());
                }
            }
        }

        MeshFilter scene = meshFilters.Find(m => m.name.Equals("Scene"));
        if (scene)
        {
            // If "Scene" Object exists place it first to read the vertex data.
            meshFilters.Remove(scene);
            meshFilters.Insert(0, scene);
        }

        /*
            Search the .obj file for first "v" line position to start reading there.
            Lines that start with "v" hold each vertex data.
        */
        int vertexFileLine = FindToken(lines, "v");
        int faceFileLine = FindToken(lines, "f");

        bool errorFound = false;
        int totalMeshes = meshFilters.Count;
        List<Vector3> vertices = new List<Vector3>();
        List<Color> vertexColors = new List<Color>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < totalMeshes; i++)
        {
            Mesh currentMesh = meshFilters[i].sharedMesh;

            // - MESH "SCENE" NAME CHANGE -  
            if (currentMesh.name.Equals("Scene")) currentMesh.name = gameObject.name + "_Scene";

            // Face
            int faceEndLine = faceFileLine + currentMesh.triangles.Length / 3;
            for (int j = faceFileLine; j < faceEndLine; j++)
            {
                string[] tokens = lines[j].Split(' ');
                try
                {
                    int a = int.Parse(tokens[1]);//, CultureInfo.InvariantCulture);
                    int b = int.Parse(tokens[2]);//, CultureInfo.InvariantCulture);
                    int c = int.Parse(tokens[3]);//, CultureInfo.InvariantCulture);

                    triangles.Add(a-1);
                    triangles.Add(b-1);
                    triangles.Add(c-1);
                }
                catch (Exception error)
                {
                    Debug.Log("C3DOBJProcessor error = " + error.StackTrace);
                    errorFound = true;
                    break;
                }
            }

            // - MESH VERTEX COLOR READ ALGORITHM - 
            int vertexEndLine = vertexFileLine + currentMesh.vertexCount;
            for (int j = vertexFileLine; j < vertexEndLine; j++)
            {
                string[] tokens = lines[j].Split(' ');
                try
                {
                    float x = float.Parse(tokens[1]);//, CultureInfo.InvariantCulture);
                    float y = float.Parse(tokens[2]);//, CultureInfo.InvariantCulture);
                    float z = float.Parse(tokens[3]);//, CultureInfo.InvariantCulture);

                    float r = float.Parse(tokens[4], CultureInfo.InvariantCulture);
                    float g = float.Parse(tokens[5], CultureInfo.InvariantCulture);
                    float b = float.Parse(tokens[6], CultureInfo.InvariantCulture);

                    vertices.Add(new Vector3(x, y, z));
                    vertexColors.Add(new Color(r, g, b, 1f));
                }
                catch (Exception error)
                {
                    Debug.Log("C3DOBJProcessor error = " + error.StackTrace);
                    errorFound = true;
                    break;
                }
                vertexFileLine++;
            }
            Debug.Log("currentMesh.vertices.Length=" + currentMesh.vertices.Length);
            Debug.Log("vertexColors.Count=" + vertexColors.Count);
            currentMesh.triangles = triangles.ToArray();
            currentMesh.vertices = vertices.ToArray();
            currentMesh.colors = vertexColors.ToArray();  //.SetColors(vertexColors);
            currentMesh.RecalculateNormals();
            currentMesh.RecalculateTangents();
            currentMesh.RecalculateBounds();
            vertexColors.Clear();
        }

        if (!errorFound) Debug.Log(gameObject.name + ".obj Vertex Colors loaded successfully!");
    }

    private int FindToken(string[] lines, string token)
    {
        int currFileLine = -1;
        for (int i = 0; i < lines.Length && currFileLine == -1; i++)
        {
            string[] tokens = lines[i].Split(' ');
            if (tokens[0].Equals(token))
            {
                currFileLine = i;
            }
        }
        return currFileLine;
    }
}