/*--------------------------------------------------------------------------
Copyright (c) 2022, Gizmo
This is free software; you can redistribute it and/or modify it under the
terms of the MIT license.
--------------------------------------------------------------------------*/
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlendShapesCopy
{
    public static class Config
    {
        public const string dividerName = "=====MMD=====";
        public const string saveFileName = "Anon_Face";
        public static Dictionary<string, string> dictionary = new Dictionary<string, string>()
        {
            ["eyebrow_serious"] = "真面目",
            ["eyebrow_trouble"] = "困る",
            ["eyebrow_anger"] = "怒り",
            ["eyebrow_up"] = "上",
            ["eyebrow_down"] = "下",
            ["eye_sad"] = "悲しい",
            ["eye_close"] = "まばたき",
            ["eye_smile"] = "笑い",
            ["eye_smile.L"] = "ウィンク",
            ["eye_smile.R"] = "ウィンク右",
            ["eye_close.L"] = "ウィンク２",
            ["eye_close.R"] = "ｳｨﾝｸ２右",
            ["eye_> <"] = "はぅ",
            ["eye_nagomi"] = "なごみ",
            ["eye_open"] = "びっくり",
            ["eye_jito"] = "じと目",
            ["mouth_a"] = "あ",
            ["vrc.v_ch"] = "い",
            ["mouth_u"] = "う",
            ["mouth_e"] = "え",
            ["vrc.v_oh"] = "お",
            ["mouth_△"] = "▲",
            ["mouth_^"] = "∧",
            ["mouth_ω"] = "ω",
            ["mouth_wa1"] = "ω□",
            ["mouth_awawa"] = "えー",
            ["mouth_smile"] = "にやり",
            ["mouth_u"] = "う",
            ["mouth_tongue_out"] = "ぺろっ",
            ["cheek2"] = "頬染め",
            ["eye_shirome"] = "白目",
            ["eye_big"] = "瞳大",
            ["eye_small"] = "瞳小",
            ["eye_pleasure"] = "睨む",
        };

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BlendShapesCopy))]
    public class BlendShapesCopyUI : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("How to use", GUILayout.Height(30))) { Application.OpenURL("https://www.youtube.com/watch?v=Yu7I7ua5lU4"); }

            BlendShapesCopy blendShapesCopy = target as BlendShapesCopy;
            SkinnedMeshRenderer smr = blendShapesCopy.GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = null;
            try { mesh = Instantiate(smr.sharedMesh); }
            catch (System.Exception) { }
            EditorGUI.BeginDisabledGroup(smr == null || mesh == null);
            if (GUILayout.Button("Setup!!!!", new GUIStyle(GUI.skin.button) { fontSize = 18 }, GUILayout.Height(40)))
            {
                Vector3[] deltaVertices = new Vector3[mesh.vertexCount];
                Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
                Vector3[] deltaTangents = new Vector3[mesh.vertexCount];

                // Divider
                if (mesh.GetBlendShapeIndex(Config.dividerName) == -1)
                    mesh.AddBlendShapeFrame(Config.dividerName, 1f, deltaVertices, deltaNormals, deltaTangents);

                for (int si = 0; si < mesh.blendShapeCount; si++)
                {
                    string name = mesh.GetBlendShapeName(si);
                    if (Config.dictionary.TryGetValue(name, out string newName))
                    {
                        int frameCount = mesh.GetBlendShapeFrameCount(si);
                        for (int fi = 0; fi < frameCount; fi++)
                        {
                            float weight = mesh.GetBlendShapeFrameWeight(si, fi);
                            mesh.GetBlendShapeFrameVertices(si, fi, deltaVertices, deltaNormals, deltaTangents);
                            mesh.AddBlendShapeFrame(newName, weight, deltaVertices, deltaNormals, deltaTangents);
                        }

                    }

                }

                // Save
                string savePath = EditorUtility.SaveFilePanelInProject("Save Mesh", Config.saveFileName + ".asset", "asset", "");
                if (string.IsNullOrEmpty(savePath)) return;
                AssetDatabase.CreateAsset(mesh, savePath);
                AssetDatabase.SaveAssets();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
#endif

    public class BlendShapesCopy : MonoBehaviour { }
}
