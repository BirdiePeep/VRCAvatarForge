using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    public class SetBlendshapeEditor : ActionEditor<SetBlendshape>
    {
        ReorderablePropertyList blendshapeList = new ReorderablePropertyList(null, foldout: false, addName: "Blendshape");
        public override void OnInspectorGUI()
        {
            var targetProp = target.FindPropertyRelative("target");
            EditorGUILayout.PropertyField(targetProp);

            //Get mesh
            var skinnedRenderer = (SkinnedMeshRenderer)targetProp.objectReferenceValue;
            if(skinnedRenderer == null)
                return;
            var mesh = skinnedRenderer.sharedMesh;
            if(mesh == null)
                return;

            //var optionsFoldout = target.FindPropertyRelative("optionsFoldout");
            //optionsFoldout.boolValue = EditorGUILayout.Foldout(optionsFoldout.boolValue, "Options");
            //EditorGUI.indentLevel++;
            //if(optionsFoldout.boolValue)
            {
                EditorGUILayout.PropertyField(target.FindPropertyRelative("inverted"));
            }
            //EditorGUI.indentLevel--;

            var popup = new string[mesh.blendShapeCount];
            for(int i = 0; i < mesh.blendShapeCount; i++)
            {
                popup[i] = mesh.GetBlendShapeName(i);
            }

            blendshapeList.list = target.FindPropertyRelative("blendshapes");
            blendshapeList.OnElementHeader = (index, blendshape) =>
            {
                var name = blendshape.FindPropertyRelative("name");
                var weight = blendshape.FindPropertyRelative("weight");
                var popupIndex = ArrayUtility.IndexOf(popup, name.stringValue);

                GUILayout.Label("Blendshape");

                //Property
                EditorGUI.BeginChangeCheck();
                popupIndex = EditorGUILayout.Popup(popupIndex, popup);
                if(EditorGUI.EndChangeCheck())
                    name.stringValue = popup[popupIndex];

                //Value
                weight.floatValue = EditorGUILayout.Slider(weight.floatValue, 0f, 100f);

                return false;
            };
            blendshapeList.OnInspectorGUI();
        }

        public override void Apply(AnimationClip animation, Globals.AnimationLayer layer, bool isEnabled)
        {
            if(action.target == null)
                return;
            string path = AvatarForge.FindPropertyPath(action.target.gameObject);
            if(path == null)
                return;
            AddKeyframes(animation, path, isEnabled);

            //Set base value
            foreach(var blendshape in action.blendshapes)
                action.target.SetBlendShapeWeight(action.target.sharedMesh.GetBlendShapeIndex(blendshape.name), action.inverted ? blendshape.weight : 0f);
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX;
        }
        public void AddKeyframes(AnimationClip animation, string objPath, bool isEnabled)
        {
            if(action.blendshapes != null && isEnabled)
            {
                //Create curves
                foreach(var blendshape in action.blendshapes)
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(new UnityEngine.Keyframe(0f, action.inverted ? 0f : blendshape.weight));
                    animation.SetCurve(objPath, typeof(SkinnedMeshRenderer), $"blendShape.{blendshape.name}", curve);
                }
            }
        }
    }
}
