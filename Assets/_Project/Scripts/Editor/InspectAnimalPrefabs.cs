using System;
using UnityEngine;
using UnityEditor;

public static class InspectAnimalPrefabs
{
    public static void Run()
    {
        string[] species = new string[] { "Bear", "Alligator", "Wolf", "Deer", "Wild_Boar" };
        foreach (var s in species)
        {
            string p = $"Assets/Resources/Animals/{s}.prefab";
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null)
            {
                Debug.Log($"[Inspect] {s} NOT FOUND at {p}");
                continue;
            }
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"[Inspect] {s}: {renderers.Length} renderers found.");
            foreach (var r in renderers)
            {
                string meshInfo = "None";
                if (r is MeshRenderer mr)
                {
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        meshInfo = $"Mesh: {mf.sharedMesh.name}, Verts: {mf.sharedMesh.vertexCount}, Bounds: {mf.sharedMesh.bounds.size}";
                    }
                }
                else if (r is SkinnedMeshRenderer smr)
                {
                    if (smr.sharedMesh != null)
                    {
                        meshInfo = $"SkinnedMesh: {smr.sharedMesh.name}, Verts: {smr.sharedMesh.vertexCount}, Bounds: {smr.sharedMesh.bounds.size}";
                    }
                }
                string matInfo = "";
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null)
                    {
                        matInfo += $"[Mat: {m.name}, Shader: {m.shader.name}, Tex: {(m.mainTexture != null ? m.mainTexture.name : "NULL")}] ";
                    }
                    else
                    {
                        matInfo += "[Mat: NULL] ";
                    }
                }
                Debug.Log($"[Inspect] -> {r.name}: {meshInfo} | {matInfo} | LocalScale: {r.transform.localScale}");
            }
        }
    }
}
