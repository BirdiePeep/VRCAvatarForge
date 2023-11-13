using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public abstract class ActionEditorBase : EditorBase
    {
        public string displayName;
        public BehaviourItem behaviour;
        public abstract void SetAction(Action action);

        //Building
        public virtual void Apply(AnimationClip animation, Globals.AnimationLayer layer, bool isEnter) { }
        public virtual void Apply(AnimatorController controller, AnimatorState state, AvatarBuilder.StateType stateType, Globals.AnimationLayer layerType) { }
        public virtual bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return false;
        }
        public virtual bool AffectsState(AvatarBuilder.StateType stateType, Globals.AnimationLayer layerType)
        {
            return false;
        }

        //Factory
        public static List<string> editorNames;
        public static List<System.Type> editorTypes;
        public static Dictionary<System.Type, ActionEditorBase> editorLookup;
        public static void InitEditors()
        {
            if(editorLookup != null)
                return;

            editorLookup = new Dictionary<System.Type, ActionEditorBase>();
            editorNames = new List<string>();
            editorTypes = new List<System.Type>();
            foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    if(type.IsSubclassOf(typeof(Action)) && !type.IsAbstract)
                    {
                        var displayName = Regex.Replace(type.Name, "([a-z])([A-Z])", "$1 $2");
                        editorTypes.Add(type);
                        editorNames.Add(displayName);
                        var editor = CreateEditor(type);
                        if(editor != null)
                        {
                            editor.displayName = displayName;
                            editorLookup.Add(type, editor);
                        }
                    }
                }
            }
        }
        public static ActionEditorBase FindEditor(Action action)
        {
            if(action == null)
                return null;

            InitEditors();
            ActionEditorBase editor = null;

            if(editorLookup.TryGetValue(action.GetType(), out editor))
            {
                editor.SetAction(action);
            }
            return editor;
        }
        private static ActionEditorBase CreateEditor(System.Type actionType)
        {
            var editorType = typeof(ActionEditor<>).MakeGenericType(actionType);
            foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    if(type.IsSubclassOf(editorType))
                    {
                        return (ActionEditorBase)System.Activator.CreateInstance(type);
                    }
                }
            }
            return null;
        }
    }
    public abstract class ActionEditor<TYPE> : ActionEditorBase where TYPE : Action
    {
        protected TYPE action;
        public override void SetAction(Action action)
        {
            this.action = action as TYPE;
        }
    }
}