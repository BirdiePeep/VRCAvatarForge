using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    public class ToggleObjectEditor : ActionEditor<ToggleObject>
    {
        ReorderablePropertyList listDrawer = new ReorderablePropertyList(null, foldout: false, addName:"Object");
        public override void OnInspectorGUI()
        {
            listDrawer.list = target.FindPropertyRelative("targets");
            listDrawer.OnElementHeader = (index, element) =>
            {
                EditorGUILayout.PropertyField(element, GUIContent.none);
                return true;
            };
            listDrawer.OnInspectorGUI();
        }

        public override void Apply(AnimationClip animation, Globals.AnimationLayer layer, bool isEnabled)
        {
            //Is anything defined?
            if(action.targets == null)
                return;

            //Apply each target
            foreach(var target in action.targets)
            {
                if(target == null)
                    continue;

                //Find the path
                string objPath = AvatarForge.FindPropertyPath(target);
                if(objPath == null)
                    continue;

                //Create curve
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, 1f));
                animation.SetCurve(objPath, typeof(GameObject), "m_IsActive", curve);

                //Set initial state
                target.SetActive(false);
            }
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX;
        }
    }
}
