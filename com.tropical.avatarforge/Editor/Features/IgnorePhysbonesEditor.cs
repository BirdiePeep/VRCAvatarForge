using UnityEngine;
using UnityEditor;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Tropical.AvatarForge
{
    public class IgnorePhysbonesEditor : FeatureEditor<IgnorePhysbones>
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Transforms will be added to the Ignore Transforms list of any VRCPhysBone components that might affect it", MessageType.Info);
            EditorGUILayout.PropertyField(target.FindPropertyRelative("transforms"));
        }

        public override string helpURL => "";
        public override void PreBuild() { }
        public override void Build()
        {
            var components = AvatarBuilder.AvatarRoot.GetComponentsInChildren<VRCPhysBone>(true);
            foreach(var comp in components)
            {
                foreach(var transform in feature.transforms)
                {
                    if(AffectsTransform(comp, transform))
                    {
                        comp.ignoreTransforms.Add(transform); //Ignore
                    }
                }
            }
        }
        public override void PostBuild() { }
        public override int BuildOrder => (int)AvatarBuilder.BuildPriority.IgnorePhysbones;

        public bool AffectsTransform(VRCPhysBone physbone, Transform bone)
        {
            //Find common parent
            var physRoot = physbone.GetRootTransform();
            var boneRoot = bone;
            while(boneRoot != null)
            {
                if(boneRoot == physRoot)
                    break;
                boneRoot = boneRoot.parent;
            }
            return boneRoot != null;
        }
    }
}