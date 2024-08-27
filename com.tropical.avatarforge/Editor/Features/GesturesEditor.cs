using UnityEditor;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public class GesturesEditor : FeatureEditor<Gestures>
    {
        ReorderablePropertyList gestureList = new ReorderablePropertyList(null, foldout: false, addName:"Gesture");
        ActionsEditor actionEditor = new ActionsEditor();
        public override void OnInspectorGUI()
        {
            gestureList.list = target.FindPropertyRelative("gestures");
            gestureList.OnElementHeader = (index, element) =>
            {
                //EditorGUILayout.BeginVertical();
                //EditorGUILayout.BeginHorizontal();
                {
                    var sides = element.FindPropertyRelative("sides");
                    var left = element.FindPropertyRelative("left");
                    var right = element.FindPropertyRelative("right");

                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, "Gesture");

                    //GUILayout.Label("Sides", GUILayout.Width(45));
                    EditorGUILayout.PropertyField(sides, new GUIContent(""), GUILayout.Width(96));

                    var sidesValue = (Gestures.GestureItem.SideType)sides.intValue;
                    if(sidesValue != Gestures.GestureItem.SideType.Right)
                    {
                        if(sidesValue != Gestures.GestureItem.SideType.Left)
                            GUILayout.Label("L");
                        left.intValue = (int)(Globals.GestureEnum)EditorGUILayout.EnumPopup((Globals.GestureEnum)left.intValue);
                        if(left.intValue == (int)Globals.GestureEnum.Fist)
                        {
                            GUILayout.Label("Min");
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("minWeightLeft"), GUIContent.none, GUILayout.Width(32));
                        }
                    }
                    if(sidesValue != Gestures.GestureItem.SideType.Left)
                    {
                        if(sidesValue != Gestures.GestureItem.SideType.Right)
                            GUILayout.Label("R");
                        right.intValue = (int)(Globals.GestureEnum)EditorGUILayout.EnumPopup((Globals.GestureEnum)right.intValue);
                        if(right.intValue == (int)Globals.GestureEnum.Fist)
                        {
                            GUILayout.Label("Min");
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("minWeightRight"), GUIContent.none, GUILayout.Width(32));
                        }
                            
                    }
                }
                //EditorGUILayout.EndHorizontal();
                //element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, "Details");
                //EditorGUILayout.EndVertical();

                return element.isExpanded;
            };
            gestureList.OnElementBody = (index, element) =>
            {
                

                actionEditor.SetTarget(element);
                actionEditor.editor = editor;
                actionEditor.setup = setup;
                actionEditor.OnInspectorGUI();
            };
            gestureList.OnAdd = (element) =>
            {
                element.FindPropertyRelative("options").ClearArray();
            };
            gestureList.OnInspectorGUI();
        }

        public override string helpURL => "";
        public override void PreBuild() { }
        public override void Build()
        {
            //Build
            foreach(var item in feature.gestures)
            {
                BuildForLayer(Globals.AnimationLayer.Action);
                BuildForLayer(Globals.AnimationLayer.FX);

                void BuildForLayer(Globals.AnimationLayer layer)
                {
                    if(!AvatarBuilder.AffectsLayer(item, layer))
                        return;

                    //Build generic behaviour item
                    item.name = $"Gesture_{item.sides}_{item.left}_{item.right}";
                    var controller = AvatarBuilder.GetController(layer);
                    if(layer == Globals.AnimationLayer.Action)
                        AvatarBuilder.BuildActionLayer(controller, new ActionItem[] { item }, item.name);
                    else
                        AvatarBuilder.BuildNormalLayer(controller, new ActionItem[] { item }, item.name, layer);
                }
            }
        }
        public override void PostBuild() { }
    }
}