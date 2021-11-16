using System;
using UnityEngine;
using UnityEngine.Animations;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
#endif
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
#endif

namespace GizmoTools
{
    public class GrabSystemSetup : EditorWindow
    {
        private const string version = "1.0.0";
        private static readonly Log log = new Log("GrabSystem");

        [MenuItem("Tools/Gizmo/GrabSystem Setup", priority = 11)]
        public static void ShowWindow()
        {
            var ins = CreateInstance<GrabSystemSetup>();
            ins.titleContent = new GUIContent($"GrabSystem Setup ver{version}");
            ins.Show();
        }

        [MenuItem("Tools/Gizmo/Twitter")]
        public static void OpenTwitter() { Application.OpenURL("https://twitter.com/Gizmo_OAO"); }

        public enum HandOptions
        {
            LeftHand = 0,
            RightHand = 1,
        }

        public class Log
        {
            private string _name;
            public Log(string name)
            {
                _name = name;
            }

            public void Print(object message)
            {
                Debug.Log($"[<color=yellow>{_name}</color>] {message}");
            }

            public void Error(object message)
            {
                Debug.LogError($"[<color=red>{_name}</color>] {message}");
            }
        }

        private Vector2 _scroll;
        public VRCAvatarDescriptor avatarDescriptor;
        public VRCExpressionsMenu expressionsMenu;
        public GameObject item;
        private HandOptions _hand = HandOptions.RightHand;
        public void OnGUI()
        {
            SerializedObject serial = new SerializedObject(this);

            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(serial.FindProperty("avatarDescriptor"), true);
            EditorGUILayout.PropertyField(serial.FindProperty("expressionsMenu"), true);
            EditorGUILayout.PropertyField(serial.FindProperty("item"), true);
            string errorInfo = "";
            if (avatarDescriptor != null)
            {
                errorInfo = CheckAvatar(avatarDescriptor.gameObject);
                if (errorInfo != "")
                {
                    EditorGUILayout.HelpBox(errorInfo, MessageType.Error);
                }
            }
            _hand = (HandOptions)EditorGUILayout.EnumPopup("Hand", _hand);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            EditorGUI.BeginDisabledGroup(avatarDescriptor == null || item == null || errorInfo != "");
            if (GUILayout.Button("Setup"))
            {
                CreateGameObject();
                SetAnimations();
                SetMenu();
            }
            EditorGUI.EndDisabledGroup();

            serial.ApplyModifiedProperties();
        }

        public string GetGameObjectPath(GameObject obj)
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

        private string CheckAvatar(GameObject _avatar)
        {
            Animator anim = _avatar.GetComponent<Animator>();
            if (anim == null) return "Animator component not found";
            if (anim.avatar == null) return "Avatar not found";
            if (!anim.avatar.isHuman) return "Avatar not human";
            if (item != null && avatarDescriptor.transform.Find(GetGameObjectPath(item)) == null) return "Item is not in avatar";
            if (expressionsMenu == null && avatarDescriptor != null) { expressionsMenu = avatarDescriptor.expressionsMenu; }
            return "";
        }

        private void CreateGameObject()
        {
            Animator anim = avatarDescriptor.GetComponent<Animator>();

            HumanBodyBones handBond = _hand == HandOptions.LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;

            string haName = $"GS_HandAnchor_{item.name}";
            GameObject GrabSystem_HandAnchor = new GameObject(haName);
            GrabSystem_HandAnchor.transform.SetParent(anim.GetBoneTransform(handBond), false);

            string oaName = $"GS_OriginalAnchor_{item.name}";
            GameObject oa = new GameObject(oaName);
            oa.transform.SetParent(item.transform.parent, false);
            oa.transform.localPosition = item.transform.localPosition;
            oa.transform.localRotation = item.transform.localRotation;
            oa.transform.localScale = item.transform.localScale;

            GameObject GrabSystem_WorldAnchor = avatarDescriptor.gameObject.transform.Find("GS_WorldAnchor")?.gameObject;
            if (GrabSystem_WorldAnchor == null)
            {
                log.Print("Create WorldAnchor");
                GrabSystem_WorldAnchor = new GameObject("GS_WorldAnchor");
                GrabSystem_WorldAnchor.transform.SetParent(avatarDescriptor.gameObject.transform, false);
            }

            GameObject GrabSystem_Join = GrabSystem_WorldAnchor.transform.Find("GS_Join")?.gameObject;
            if (GrabSystem_Join == null)
            {
                log.Print("Create Join");
                GrabSystem_Join = new GameObject("GS_Join");
                GrabSystem_Join.transform.SetParent(GrabSystem_WorldAnchor.transform, false);
                var positionConstraint = GrabSystem_Join.AddComponent<PositionConstraint>();
                positionConstraint.AddSource(new ConstraintSource() { weight = -1f, sourceTransform = GrabSystem_WorldAnchor.transform });
                positionConstraint.weight = 0.5f;
                positionConstraint.locked = true;
                positionConstraint.constraintActive = true;
                var rotationConstraint = GrabSystem_Join.AddComponent<RotationConstraint>();
                rotationConstraint.AddSource(new ConstraintSource() { weight = -0.5f, sourceTransform = GrabSystem_WorldAnchor.transform });
                rotationConstraint.weight = 1f;
                rotationConstraint.locked = true;
                rotationConstraint.constraintActive = true;
            }

            string itemRootName = $"GS_Item_{item.name}";
            GameObject GrabSystem_Item = new GameObject(itemRootName);
            GrabSystem_Item.transform.SetParent(GrabSystem_Join.transform, false);

            GameObject itemClone = Instantiate(item);
            itemClone.name = $"{item.name} (Clone)";
            //itemClone.SetActive(false);
            itemClone.transform.localPosition = new Vector3(0, 0, 0);
            itemClone.transform.localRotation = new Quaternion(0, 0, 0, 0);
            itemClone.transform.SetParent(GrabSystem_Item.transform);

            var parentConstraint = GrabSystem_Item.AddComponent<ParentConstraint>();
            parentConstraint.AddSource(new ConstraintSource() { weight = 0f, sourceTransform = oa.transform });
            parentConstraint.AddSource(new ConstraintSource() { weight = 1f, sourceTransform = GrabSystem_HandAnchor.transform });
            parentConstraint.weight = 1f;
            parentConstraint.locked = true;
            parentConstraint.constraintActive = true;
        }

        private void SetAnimations()
        {
            // 获取 VRCAvatarDescriptor
            if (!avatarDescriptor.customizeAnimationLayers) return;

            // 获取 FX_Layer
            AnimatorController controller = Array.Find(
              avatarDescriptor.baseAnimationLayers,
              c => c.type == VRCAvatarDescriptor.AnimLayerType.FX
            ).animatorController as AnimatorController;
            if (controller == null)
            {
                log.Error("FX Layer not found");
                return;
            }
            log.Print($"Controller: {controller.name}");

            // 创建 GrabSystem 参数
            string enableName = $"GS_{item.name}";
            if (Array.Find(controller.parameters, p => p.name == enableName) == null)
            {
                log.Print($"AddParameter: {enableName}");
                controller.AddParameter(enableName, AnimatorControllerParameterType.Bool);
            }

            // 创建 手势 参数
            log.Print($"Hand: {_hand}");
            string handParameterName = _hand == HandOptions.LeftHand ? "GestureLeft" : "GestureRight";
            if (Array.Find(controller.parameters, p => p.name == handParameterName) == null)
            {
                log.Print($"AddParameter: {handParameterName}");
                controller.AddParameter(handParameterName, AnimatorControllerParameterType.Int);
            }

            // 创建 Layer
            log.Print($"Item: {item.name}");
            string itemLayerName = $"GS_{item.name}";
            int itemLayerIndex = Array.FindIndex(controller.layers, l => l.name == itemLayerName);
            if (itemLayerIndex > -1)
            {
                log.Print($"RemoveLayer: {itemLayerName}");
                controller.RemoveLayer(itemLayerIndex);
            }
            log.Print($"AddLayer: {itemLayerName}");
            AnimatorControllerLayer itemLayer = new AnimatorControllerLayer()
            {
                name = itemLayerName,
                defaultWeight = 1F,
                stateMachine = CreateItemStateMachine(handParameterName, enableName),
            };
            controller.AddLayer(itemLayer);
        }

        private AnimatorStateMachine CreateItemStateMachine(string handParameterName, string enableName)
        {
            AnimatorStateMachine sm = new AnimatorStateMachine();
            AnimatorState idleState = sm.AddState("Idle", new Vector3(30, 230, 0));
            sm.defaultState = idleState;
            AnimatorState resetState = sm.AddState("Reset", new Vector3(270, 140, 0));
            AnimatorState grabState = sm.AddState("Grab", new Vector3(270, 230, 0));
            AnimatorState dropState = sm.AddState("Drop", new Vector3(510, 230, 0));

            CreateSavePath();
            resetState.motion = CreateAnimation("Reset");
            grabState.motion = CreateAnimation("Grab");
            dropState.motion = CreateAnimation("Drop");

            AnimatorStateTransition idle2grad = idleState.AddTransition(grabState);
            idle2grad.exitTime = idle2grad.duration = 0;
            idle2grad.hasExitTime = false;
            idle2grad.AddCondition(AnimatorConditionMode.If, 0, enableName);
            idle2grad.AddCondition(AnimatorConditionMode.Equals, 1, handParameterName);

            AnimatorStateTransition grad2reset = grabState.AddTransition(resetState);
            grad2reset.exitTime = grad2reset.duration = 0;
            grad2reset.hasExitTime = false;
            grad2reset.AddCondition(AnimatorConditionMode.IfNot, 0, enableName);

            AnimatorStateTransition grab2drop = grabState.AddTransition(dropState);
            grab2drop.exitTime = grab2drop.duration = 0;
            grab2drop.hasExitTime = false;
            grab2drop.AddCondition(AnimatorConditionMode.Equals, 2, handParameterName);

            AnimatorStateTransition drop2grab = dropState.AddTransition(grabState);
            drop2grab.exitTime = drop2grab.duration = 0;
            drop2grab.hasExitTime = false;
            drop2grab.AddCondition(AnimatorConditionMode.Equals, 1, handParameterName);

            AnimatorStateTransition drop2reset = dropState.AddTransition(resetState);
            drop2reset.exitTime = drop2reset.duration = 0;
            drop2reset.hasExitTime = false;
            drop2reset.AddCondition(AnimatorConditionMode.IfNot, 0, enableName);

            AnimatorStateTransition reset2idle = resetState.AddTransition(idleState, true);
            reset2idle.exitTime = reset2idle.duration = 0;
            reset2idle.hasExitTime = true;
            return sm;
        }

        // TODO: 写得很笨
        private void CreateSavePath()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Gizmo/GrabSystem"))
                AssetDatabase.CreateFolder("Assets/_Gizmo", "GrabSystem");
            if (!AssetDatabase.IsValidFolder("Assets/_Gizmo/GrabSystem/Animations"))
                AssetDatabase.CreateFolder("Assets/_Gizmo/GrabSystem", "Animations");
            if (!AssetDatabase.IsValidFolder($"Assets/_Gizmo/GrabSystem/Animations/{avatarDescriptor.name}"))
                AssetDatabase.CreateFolder("Assets/_Gizmo/GrabSystem/Animations", avatarDescriptor.name);
        }

        private AnimationCurve GetAnimationCurve(float value)
        {
            return new AnimationCurve()
            {
                keys = new Keyframe[2] { new Keyframe() { time = 0f, value = value }, new Keyframe() { time = 1f / 60, value = value } }
            };
        }

        private Motion CreateAnimation(string name)
        {
            string filename = $"Assets/_Gizmo/GrabSystem/Animations/{avatarDescriptor.name}/{name}.anim";

            AnimationClip clip = new AnimationClip();
            string itemPath = $"GS_WorldAnchor/GS_Join/GS_Item_{item.name}";

            switch (name)
            {
                case "Reset":
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = GetGameObjectPath(item), propertyName = "m_IsActive", type = typeof(GameObject) }, GetAnimationCurve(1));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = itemPath, propertyName = "m_Enabled", type = typeof(ParentConstraint) }, GetAnimationCurve(1));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = itemPath, propertyName = "m_Sources.Array.data[0].weight", type = typeof(ParentConstraint) }, GetAnimationCurve(1));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = itemPath, propertyName = "m_Sources.Array.data[1].weight", type = typeof(ParentConstraint) }, GetAnimationCurve(0));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = $"{itemPath}/{item.name} (Clone)", propertyName = "m_IsActive", type = typeof(GameObject) }, GetAnimationCurve(0));
                    break;
                case "Grab":
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = GetGameObjectPath(item), propertyName = "m_IsActive", type = typeof(GameObject) }, GetAnimationCurve(0));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = itemPath, propertyName = "m_Enabled", type = typeof(ParentConstraint) }, GetAnimationCurve(1));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = itemPath, propertyName = "m_Sources.Array.data[0].weight", type = typeof(ParentConstraint) }, GetAnimationCurve(0));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = itemPath, propertyName = "m_Sources.Array.data[1].weight", type = typeof(ParentConstraint) }, GetAnimationCurve(1));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = $"{itemPath}/{item.name} (Clone)", propertyName = "m_IsActive", type = typeof(GameObject) }, GetAnimationCurve(1));
                    break;
                case "Drop":
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = GetGameObjectPath(item), propertyName = "m_IsActive", type = typeof(GameObject) }, GetAnimationCurve(0));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = itemPath, propertyName = "m_Enabled", type = typeof(ParentConstraint) }, GetAnimationCurve(0));
                    AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = $"{itemPath}/{item.name} (Clone)", propertyName = "m_IsActive", type = typeof(GameObject) }, GetAnimationCurve(1));
                    break;
                default:
                    return null;
            }

            AssetDatabase.CreateAsset(clip, filename);
            return clip;
        }

        private void SetMenu()
        {
            if (!avatarDescriptor.customExpressions) return;

            // Add Parameter
            List<VRCExpressionParameters.Parameter> parameters = new List<VRCExpressionParameters.Parameter>(avatarDescriptor.expressionParameters.parameters);
            string enableName = $"GS_{item.name}";
            if (parameters.Find(x => x.name == enableName) == null)
                parameters.Add(new VRCExpressionParameters.Parameter() { name = enableName, defaultValue = 0, saved = true, valueType = VRCExpressionParameters.ValueType.Bool });
            avatarDescriptor.expressionParameters.parameters = parameters.ToArray();

            // Set Menu
            if (expressionsMenu != null)
            {
                if (expressionsMenu.controls.Find(x => x.name == enableName) == null)
                    expressionsMenu.controls.Add(new VRCExpressionsMenu.Control()
                    {
                        name = enableName,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRCExpressionsMenu.Control.Parameter() { name = enableName },
                        value = 1,
                    });
            }
        }
    }
}