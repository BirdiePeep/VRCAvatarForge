using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tropical.AvatarForge
{
    [Serializable]
    public class ActionMenu : Feature
    {
        //Editor
        public string selectedMenuPath;

        [Serializable]
        public abstract class Control : ActionItem
        {
            public Texture2D icon;
            public string parameter;
            
            [SerializeReference] public List<NonMenuActions> subActions = new List<NonMenuActions>();
            
            //Meta
            public int controlValue = 0;
            public bool foldoutSubActions;

            public virtual bool IsNormalAction() { return true; }
            public virtual bool NeedsControlLayer() { return true; }
            public override void CopyTo(ActionItem clone)
            {
                base.CopyTo(clone);

                var menuClone = clone as Control;
                if(menuClone != null)
                {
                    menuClone.icon = icon;
                    menuClone.parameter = parameter;
                    menuClone.controlValue = controlValue;
                    menuClone.foldoutSubActions = foldoutSubActions;

                    menuClone.subActions.Clear();
                    foreach (var item in subActions)
                        menuClone.subActions.Add(item);
                }
            }
            public virtual void DeepCopyTo(ActionItem clone)
            {
                CopyTo(clone);
            }

            //Build
            public override string GetLayerGroup()
            {
                return parameter;
            }
        }
        [SerializeReference] public List<Control> controls = new List<Control>();

        public interface IGroupedControl
        {
            public bool HasGroup { get; }
            public string Group { get; set; }
            public bool IsGroupDefault { get; set; }
            public bool IsGroupOffState { get; set; }
        }

        [Serializable]
        public class Toggle : Control, IGroupedControl
        {
            public string group;

            public bool HasGroup
            {
                get
                {
                    return !string.IsNullOrEmpty(group);
                }
            }
            public string Group
            {
                get { return group; }
                set { group = value; }
            }
            public bool IsGroupDefault { get => defaultValue; set => defaultValue = value; }
            public bool IsGroupOffState { get => isOffState; set => isOffState = value; }

            [Tooltip("This toggle be enabled by default.")]
            public bool defaultValue;

            [Tooltip("This toggle will be enabled when no other toggle with the same group is turned on.")]
            public bool isOffState = false;

            public override void CopyTo(ActionItem clone)
            {
                base.CopyTo(clone);
                if(clone is Toggle toggle)
                {
                    toggle.group = group;
                    toggle.defaultValue = defaultValue;
                    toggle.isOffState = isOffState;
                }
            }
        }
        [Serializable]
        public class Button : Control
        {
        }
        [Serializable]
        public class Slider : Control
        {
            public AnimationClip clip;
            [Range(0, 1)] public float defaultValue;

            public override bool IsNormalAction() { return false; }
            public override void CopyTo(ActionItem clone)
            {
                base.CopyTo(clone);
                if(clone is Slider slider)
                {
                    slider.clip = clip;
                    slider.defaultValue = defaultValue;
                }
            }
        }
        [Serializable]
        public class SubMenu : Control
        {
            [SerializeReference] public ActionMenu subMenu;

            public override bool IsNormalAction() { return false; }
            public override bool NeedsControlLayer() { return false; }
            public override void CopyTo(ActionItem clone)
            {
                base.CopyTo(clone);
                if(clone is SubMenu subMenu)
                {
                    subMenu.subMenu = this.subMenu;
                }
            }
            public override void DeepCopyTo(ActionItem clone)
            {
                base.DeepCopyTo(clone);

                var subMenu = clone as SubMenu;
                if(subMenu != null)
                {
                    subMenu.subMenu = this.subMenu?.DeepCopy();
                }
            }
            public override bool ShouldBuild()
            {
                return subMenu != null;
            }
        }

        public ActionMenu FindParent(ActionMenu child)
        {
            foreach(var item in controls)
            {
                if(item is SubMenu subMenu)
                {
                    if(subMenu.subMenu == child)
                        return this;
                    else if(subMenu.subMenu != null)
                    {
                        var result = subMenu.subMenu.FindParent(child);
                        if(result != null)
                            return result;
                    }
                }
            }

            return null;
        }
        public ActionMenu DeepCopy()
        {
            //Duplicate each control
            var menu = new ActionMenu();
            foreach(var action in this.controls)
            {
                var newAction = (Control)Activator.CreateInstance(action.GetType());
                action.DeepCopyTo(newAction);
                menu.controls.Add(newAction);
            }
            return menu;
        }
        public Control FindControl(string path)
        {
            var split = path.Split('/');
            if(split.Length == 0)
                return null;

            ActionMenu current = this;
            for(int i=0; i<split.Length; i++)
            {
                var pathItem = split[i];
                foreach(var action in current.controls)
                {
                    if(action.name == pathItem)
                    {
                        //Check if complete
                        if(i == split.Length-1)
                            return action;

                        //Check if submenu
                        if(action is SubMenu subMenu && subMenu.subMenu != null)
                        {
                            current = subMenu.subMenu;
                            break;
                        }

                        //Not found
                        return null;
                    }
                }
            }

            //Not found
            return null;
        }
        public Control FindMenuAction(string name, bool recursive)
        {
            foreach(var action in controls)
            {
                if (action.name == name)
                    return action;
            }
            if(recursive)
            {
                foreach(var action in controls)
                {
                    if(action is SubMenu subMenu && subMenu.subMenu != null)
                    {
                        var result = subMenu.subMenu.FindMenuAction(name, true);
                        if(result != null)
                            return result;
                    }
                }
            }
            return null;
        }
        public Control FindMenuActionOfType(string name, System.Type type, bool recursive)
        {
            foreach(var action in controls)
            {
                if(action.name == name && action.GetType() == type)
                    return action;
            }
            if(recursive)
            {
                foreach(var action in controls)
                {
                    if(action is SubMenu subMenu && subMenu.subMenu != null)
                    {
                        var result = subMenu.subMenu.FindMenuAction(name, true);
                        if(result != null)
                            return result;
                    }
                }
            }
            return null;
        }

        /*[System.Serializable]
        public class ControlGroup
        {
            public string name;
            public string defaultValue;
            public bool isOffState;
            public List<Control> controls = new List<Control>();
        }
        public List<ControlGroup> controlGroups = new List<ControlGroup>();
        public ControlGroup FindControlGroup(string groupName)
        {
            foreach(var group in controlGroups)
            {
                if(group.name == groupName)
                    return group;
            }
            return null;
        }*/
    }
}