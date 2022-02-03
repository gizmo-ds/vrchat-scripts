using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;

public class AnimationTool : EditorWindow
{
    [MenuItem("Tools/Gizmo/AnimationTool", priority = 11)]
    public static void showWindow()
    {
        CreateInstance<AnimationTool>().Show();
    }

    [MenuItem("Tools/Gizmo/Twitter")]
    public static void openTwitter()
    {
        Application.OpenURL("https://twitter.com/Gizmo_OAO");
    }

    public GameObject[] transformObjects = { };
    public bool transformGroupEnabled = true;
    public bool transformLocalPosition = true;
    public bool transformLocalScale = true;
    public bool transformLocalRotation = true;

    public GameObject[] behaviourObjects = { };
    public bool behaviourKeyGroupEnabled = false;

    public void OnGUI()
    {
        AnimationClip anim = new AnimationClip();
        SerializedObject serial = new SerializedObject(this);
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        transformGroupEnabled = EditorGUILayout.ToggleLeft("Add TransformKey", transformGroupEnabled);
        if (transformGroupEnabled)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            transformLocalPosition = EditorGUILayout.Toggle("LocalPosition", transformLocalPosition);
            transformLocalRotation = EditorGUILayout.Toggle("LocalRotation", transformLocalRotation);
            transformLocalScale = EditorGUILayout.Toggle("LocalScale", transformLocalScale);
            GUILayout.EndVertical();

            EditorGUILayout.PropertyField(serial.FindProperty("transformObjects"), true);
            EditorGUILayout.Separator();
            if (GUILayout.Button("Clear Objects"))
            {
                transformObjects = new GameObject[] { };
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        behaviourKeyGroupEnabled = EditorGUILayout.ToggleLeft("Add BehaviourKey", behaviourKeyGroupEnabled);
        if (behaviourKeyGroupEnabled)
        {
            EditorGUILayout.PropertyField(serial.FindProperty("behaviourObjects"), true);
            EditorGUILayout.Separator();

            if (GUILayout.Button("Clear Objects"))
            {
                behaviourObjects = new GameObject[] { };
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();

        if (GUILayout.Button("Create AnimationClip"))
        {
            if (transformGroupEnabled)
            {
                for (int i = 0; i < transformObjects.Length; i++)
                {
                    GameObject obj = transformObjects[i];
                    string gameObjectPath = getGameObjectPath(obj);

                    if (transformLocalPosition)
                    {
                        addTransform(anim, gameObjectPath, "m_LocalPosition.x", obj.transform.localPosition.x);
                        addTransform(anim, gameObjectPath, "m_LocalPosition.y", obj.transform.localPosition.y);
                        addTransform(anim, gameObjectPath, "m_LocalPosition.z", obj.transform.localPosition.z);
                    }
                    if (transformLocalRotation)
                    {
                        addTransform(anim, gameObjectPath, "m_LocalRotation.x", obj.transform.localRotation.x);
                        addTransform(anim, gameObjectPath, "m_LocalRotation.y", obj.transform.localRotation.y);
                        addTransform(anim, gameObjectPath, "m_LocalRotation.z", obj.transform.localRotation.z);
                        addTransform(anim, gameObjectPath, "m_LocalRotation.w", obj.transform.localRotation.w);
                    }
                    if (transformLocalScale)
                    {
                        addTransform(anim, gameObjectPath, "m_LocalScale.x", obj.transform.localScale.x);
                        addTransform(anim, gameObjectPath, "m_LocalScale.y", obj.transform.localScale.y);
                        addTransform(anim, gameObjectPath, "m_LocalScale.z", obj.transform.localScale.z);
                    }
                }
            }

            if (behaviourKeyGroupEnabled)
            {
                for (int i = 0; i < behaviourObjects.Length; i++)
                {
                    GameObject obj = behaviourObjects[i];
                    string gameObjectPath = getGameObjectPath(obj);
                    AnimationUtility.SetEditorCurve(anim, new EditorCurveBinding
                    {
                        path = gameObjectPath,
                        propertyName = "m_Enabled",
                        type = typeof(Behaviour)
                    }, new AnimationCurve()
                    {
                        keys = new Keyframe[1] { new Keyframe() { time = 0f, value = 1f } }
                    });
                }
            }

            string savePath = EditorUtility.SaveFilePanelInProject("Save AnimationClip", "NewAnimationClip.anim", "anim", "");
            if (string.IsNullOrEmpty(savePath)) return;
            AssetDatabase.CreateAsset(anim, savePath);
        }
        EditorGUILayout.Separator();

        serial.ApplyModifiedProperties();
    }

    private void addTransform(AnimationClip anim, string path, string name, float value)
    {
        AnimationUtility.SetEditorCurve(anim, new EditorCurveBinding
        {
            path = path,
            propertyName = name,
            type = typeof(Transform)
        }, new AnimationCurve()
        {
            keys = new Keyframe[1] { new Keyframe() { time = 0f, value = value } }
        });
    }

    private string getGameObjectPath(GameObject obj)
    {
        GameObject _obj = obj;
        string _path = _obj.name;
        while (_obj.transform.parent != null)
        {
            _obj = _obj.transform.parent.gameObject;
            _path = $"{_obj.name}/{_path}";
        }
        int index = _path.IndexOf('/');
        if (index > -1) _path = _path.Substring(index + 1);
        return _path;
    }

    // by RetroGEO
    [MenuItem("GameObject/Copy Path %#c", false, -753)]
    private static void CopyPath()
    {
        var go = Selection.activeGameObject;

        if (go == null)
        {
            return;
        }

        var path = go.name;

        while (go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
            path = string.Format("{0}/{1}", go.name, path);
        }

        EditorGUIUtility.systemCopyBuffer = path;
    }
}
