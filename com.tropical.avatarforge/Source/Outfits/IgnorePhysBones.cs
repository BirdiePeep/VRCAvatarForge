using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Tropical.AvatarForge
{
    public class IgnorePhysBones : MonoBehaviour, IPrefabAttachProcess
    {
        public void OnAttach(AvatarForge setup, GameObject target)
        {
            Search(target, target, true);
        }
        public void OnRemove(AvatarForge setup, GameObject target)
        {
            Search(target, target, false);
        }

        //Search up the chain for dynamic bones
        void Search(GameObject searchObj, GameObject target, bool attach)
        {
            //Search for dynamic bones
            var comps = searchObj.GetComponents<VRCPhysBone>();
            foreach(var comp in comps)
            {
                //Does this affect us?
                if(IsParent(target.transform, comp.GetRootTransform()))
                {
                    if(attach)
                        AddIgnore(target.transform, comp);
                    else
                        RemoveIgnore(target.transform, comp);
                }
            }

            //Move up
            if (searchObj.transform.parent != null)
                Search(searchObj.transform.parent.gameObject, target, attach);
        }
        bool IsParent(Transform target, Transform parent)
        {
            Transform transform = target;
            while(transform != null)
            {
                if (transform == parent)
                    return true;
                transform = transform.parent;
            }
            return false;
        }
        void AddIgnore(Transform transform, VRCPhysBone dynamics)
        {
            //Add if not already
            if(!dynamics.ignoreTransforms.Contains(transform))
                dynamics.ignoreTransforms.Add(transform);

            //Cleanup nulls
            for (int i = 0; i < dynamics.ignoreTransforms.Count; i++)
            {
                if (dynamics.ignoreTransforms[i] == null)
                {
                    dynamics.ignoreTransforms.RemoveAt(i);
                    i--;
                }
            }
        }
        void RemoveIgnore(Transform transform, VRCPhysBone dynamics)
        {
            if(dynamics.ignoreTransforms.Contains(transform))
            {
                //Remove
                dynamics.ignoreTransforms.Remove(transform);
            }
        }
    }
}