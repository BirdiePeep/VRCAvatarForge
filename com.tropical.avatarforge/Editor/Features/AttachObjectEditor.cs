using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRC.Core;

namespace Tropical.AvatarForge
{
    public class AttachObjectEditor : FeatureEditor<AttachObject>
    {
        public override void OnInspectorGUI()
        {
            
            EditorGUILayout.PropertyField(target.FindPropertyRelative("source"));
            var attachTarget = target.FindPropertyRelative("attachTarget");
            EditorGUILayout.PropertyField(attachTarget);
            AttachObject.Target type = (AttachObject.Target)attachTarget.intValue;
            switch(type)
            {
                case AttachObject.Target.Root:
                    break;
                case AttachObject.Target.Path:
                {
                    EditorGUILayout.PropertyField(target.FindPropertyRelative("path"));
                    EditorGUILayout.HelpBox("Searches by either path or unique name", MessageType.Info);
                    break;
                }
                case AttachObject.Target.HumanoidBone:
                    EditorGUILayout.PropertyField(target.FindPropertyRelative("humanBone"));
                    break;
            }
            EditorGUILayout.PropertyField(target.FindPropertyRelative("mergeTransforms"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("keepWorldPosition"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("attachChildrenInstead"));
        }

        public override string helpURL => "";
        public override void PreBuild()
        {
            Transform source = feature.source != null ? feature.source : feature.gameObject.transform;
            Transform dest = null;
            switch(feature.attachTarget)
            {
                case AttachObject.Target.Root:
                    dest = AvatarBuilder.AvatarRoot;
                    break;
                case AttachObject.Target.Path:
                {
                    dest = AvatarBuilder.AvatarRoot.Find(feature.path);
                    if(dest == null)
                    {
                        dest = FindRecursive(AvatarBuilder.AvatarRoot, feature.path);
                        if(dest == null)
                        {
                            Debug.LogError($"AttachObject: Unable to find destination transform at path:'{feature.path}'");
                            return;
                        }
                    }
                    break;
                }
                case AttachObject.Target.HumanoidBone:
                {
                    var animator = AvatarBuilder.AvatarRoot.gameObject.GetComponent<Animator>();
                    dest = animator.GetBoneTransform(feature.humanBone);
                    if(dest == null)
                    {
                        Debug.LogError($"AttachObject: Unable to find destination transform for humanoid bone:'{feature.humanBone}'");
                        return;
                    }
                    break;
                }
            }

            //Attach
            AttachTransform(source, dest);
        }
        public override void Build() { }
        public override void PostBuild() { }
        public override int BuildOrder => (int)AvatarBuilder.BuildPriority.AttachObjects;

        void AttachTransform(Transform source, Transform dest)
        {
            var newObjects = new List<Transform>();

            //Attach source or source's children
            if(feature.attachChildrenInstead)
            {
                var children = new Transform[source.childCount];
                for(int i = 0; i < source.childCount; i++)
                    children[i] = source.GetChild(i);
                foreach(var child in children)
                    Attach(child, dest);
            }
            else
            {
                Attach(source, dest);
            }

            //Attach
            void Attach(Transform source, Transform dest)
            {
                if(feature.mergeTransforms)
                    MergeTransforms(dest, source, feature.keepWorldPosition, newObjects);
                else
                {
                    source.SetParent(dest, feature.keepWorldPosition);
                    newObjects.Add(source);
                }
            }

            //Attach bones
            foreach(var obj in newObjects)
            {
                var skinned = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach(var renderer in skinned)
                    AttachBones(dest, renderer);
            }

            //Destroy instance garbage
            if(!newObjects.Contains(source))
                GameObject.DestroyImmediate(source.gameObject);
        }
        void MergeTransforms(Transform parent, Transform attachment, bool worldPositionStays, List<Transform> newObjects)
        {
            //Does this exist on the base prefab
            var existing = parent.Find(attachment.name);
            if(existing != null)
            {
                //Continue search
                var children = new Transform[attachment.childCount];
                for(int i = 0; i < attachment.childCount; i++)
                    children[i] = attachment.GetChild(i);
                foreach(var child in children)
                {
                    MergeTransforms(existing, child, worldPositionStays, newObjects);
                }
            }
            else
            {
                //Add
                attachment.SetParent(parent.transform, worldPositionStays);
                newObjects.Add(attachment);
            }
        }
        void AttachBones(Transform armature, SkinnedMeshRenderer dest)
        {
            //Root
            if(dest.rootBone != null)
                dest.rootBone = FindRecursive(armature, dest.rootBone.name);

            //Find bones
            var bones = (Transform[])dest.bones.Clone();
            for(int i = 0; i < dest.bones.Length; i++)
            {
                var boneName = bones[i].name;
                var sourceBone = FindRecursive(armature, boneName);
                if(sourceBone != null)
                {
                    bones[i] = sourceBone;
                }
                else
                    Debug.LogError($"Unable to find matching bone '{boneName}'");
            }
            dest.bones = bones;
        }
        Transform FindRecursive(Transform self, string name)
        {
            //Find
            var result = self.Find(name);
            if(result != null)
                return result;

            //Recusive
            foreach(Transform child in self)
            {
                result = FindRecursive(child, name);
                if(result != null)
                    return result;
            }
            return null;
        }
    }
}