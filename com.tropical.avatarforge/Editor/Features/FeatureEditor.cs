using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tropical.AvatarForge
{
    public abstract class FeatureEditorBase : EditorBase
    {
        public string displayName;
        public abstract void SetFeature(Feature feature);
        public abstract string helpURL { get; }

        //Build
        public virtual int BuildOrder => 0;
        public abstract void PreBuild();
        public abstract void Build();
        public abstract void PostBuild();

        //Factory
        public static List<string> editorNames;
        public static List<System.Type> editorTypes;
        public static Dictionary<System.Type, FeatureEditorBase> editorLookup;
        public static void InitEditors()
        {
            if(editorLookup != null)
                return;

            editorLookup = new Dictionary<System.Type, FeatureEditorBase>();
            editorNames = new List<string>();
            editorTypes = new List<System.Type>();
            foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    if(type.IsSubclassOf(typeof(Feature)) && !type.IsAbstract)
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
        public static FeatureEditorBase FindEditor(Feature feature)
        {
            if(feature == null)
                return null;
            InitEditors();

            FeatureEditorBase editor = null;
            if(editorLookup.TryGetValue(feature.GetType(), out editor))
                editor.SetFeature(feature);
            return editor;
        }
        private static FeatureEditorBase CreateEditor(System.Type objType)
        {
            var editorType = typeof(FeatureEditor<>).MakeGenericType(objType);
            foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    if(type.IsSubclassOf(editorType))
                    {
                        return (FeatureEditorBase)System.Activator.CreateInstance(type);
                    }
                }
            }
            return null;
        }
    }
    public abstract class FeatureEditor<TYPE> : FeatureEditorBase where TYPE : Feature
    {
        public TYPE feature;
        public override void SetFeature(Feature feature)
        {
            this.feature = feature as TYPE;
        }
    }
}
