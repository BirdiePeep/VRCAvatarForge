using UnityEditor;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public class ActionsEditor : EditorBase
    {
        BehaviourItem behaviour;
        ReorderablePropertyList actionList = new ReorderablePropertyList("Actions", foldout: false, addName:"Action");
        public override void Inspector_Body()
        {
            behaviour = (BehaviourItem)GetManagedReferenceValue(target);

            //Actions
            ActionEditorBase.InitEditors();
            actionList.list = target.FindPropertyRelative("actions");
            actionList.headerColor = AvatarForgeEditor.SubHeaderColor;
            actionList.showHeader = true;
            actionList.OnPreAdd = (element) =>
            {
                var popup = new AddListItemPopup();
                popup.list = element;
                popup.size = new Vector2(150, 200);
                popup.options = new AddListItemPopup.Option[ActionEditorBase.editorTypes.Count];
                for(int i = 0; i < ActionEditorBase.editorTypes.Count; i++)
                    popup.options[i] = new AddListItemPopup.Option(ActionEditorBase.editorNames[i], ActionEditorBase.editorTypes[i]);
                popup.Show();

                return null;
            };
            actionList.OnElementHeader = (index, element) =>
            {
                var action = (Action)GetManagedReferenceValue(element);
                var editor = ActionEditorBase.FindEditor(action);
                if(editor == null)
                {
                    EditorGUILayout.LabelField($"Unable to find editor for action '{action.GetType().FullName}'");
                    return false;
                }
                else
                {
                    EditorGUI.indentLevel += 1;
                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(editor.displayName));
                    EditorGUI.indentLevel -= 1;
                }

                return element.isExpanded;
            };
            actionList.OnElementBody = (index, element) =>
            {
                var action = (Action)GetManagedReferenceValue(element);

                ActionEditorBase editor = ActionEditorBase.FindEditor(action);
                if(editor != null)
                {
                    editor.editor = this.editor;
                    editor.setup = this.setup;
                    editor.behaviour = behaviour;
                    editor.SetTarget(element);
                    editor.OnInspectorGUI();
                }
            };
            actionList.OnInspectorGUI();

            //Timing
            var foldoutTiming = target.FindPropertyRelative("foldoutTiming");
            if(BeginCategory("Timing", foldoutTiming))
                DrawTiming();
            EndCategory();

            //Triggers
            var foldoutTriggers = target.FindPropertyRelative("foldoutTriggers");
            if(BeginCategory("Triggers", foldoutTriggers))
                DrawTriggers();
            EndCategory();
        }

        void DrawTiming()
        {
            //if(setup == null)
            //    Debug.Log("wut");

            EditorGUILayout.PropertyField(target.FindPropertyRelative("fadeIn"));

            EditorGUI.BeginDisabledGroup(!behaviour.HasExit());
            EditorGUILayout.PropertyField(target.FindPropertyRelative("hold"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("fadeOut"));
            EditorGUI.EndDisabledGroup();

            DrawParameterDropDown(target.FindPropertyRelative("timeParameter"), "Time Parameter", required:false);
        }

        #region Triggers
        ReorderablePropertyList triggerList = new ReorderablePropertyList(null, foldout:false, addName:"Trigger");
        ReorderablePropertyList conditionList = new ReorderablePropertyList(null, foldout:false, addName:"Condition");
        void DrawTriggers()
        {
            var triggers = target.FindPropertyRelative("triggers");
            triggerList.list = triggers;
            triggerList.OnElementHeader = DrawTrigger;
            triggerList.OnElementBody = DrawTriggerBody;
            triggerList.OnInspectorGUI();
        }
        bool DrawTrigger(int index, SerializedProperty trigger)
        {
            //Type
            EditorGUILayout.PropertyField(trigger.FindPropertyRelative("type"), new GUIContent("Trigger"));

            //Default condition
            /*if(hasDefaultTriggerConditions)
            {
                EditorGUILayout.PropertyField(trigger.FindPropertyRelative("useDefaultCondition"), new GUIContent("Include Default Condition"));
            }*/

            return true;
        }
        void DrawTriggerBody(int index, SerializedProperty trigger)
        {
            var conditions = trigger.FindPropertyRelative("conditions");
            if(conditions.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Triggers without any conditions default to true.", MessageType.Warning);
            }

            //Conditions
            conditionList.list = conditions;
            conditionList.OnElementHeader = DrawTriggerCondition;
            conditionList.OnInspectorGUI();
        }
        bool DrawTriggerCondition(int index, SerializedProperty condition)
        {
            EditorGUIUtility.labelWidth = 64;
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                //Type
                var type = condition.FindPropertyRelative("type");
                EditorGUILayout.PropertyField(type, GUIContent.none);
                var typeValue = (Globals.ParameterEnum)type.intValue;

                //Parameter
                if((Globals.ParameterEnum)type.intValue == Globals.ParameterEnum.Custom)
                {
                    EditorGUILayout.PropertyField(condition.FindPropertyRelative("parameter"), GUIContent.none);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(typeValue.ToString());
                    EditorGUI.EndDisabledGroup();
                }

                //Logic
                var logic = condition.FindPropertyRelative("logic");
                switch(typeValue)
                {
                    case Globals.ParameterEnum.Custom:
                        logic.enumValueIndex = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BehaviourItem.Condition.Logic)logic.intValue));
                        break;
                    case Globals.ParameterEnum.Upright:
                    case Globals.ParameterEnum.AngularY:
                    case Globals.ParameterEnum.VelocityX:
                    case Globals.ParameterEnum.VelocityY:
                    case Globals.ParameterEnum.VelocityZ:
                    case Globals.ParameterEnum.GestureRightWeight:
                    case Globals.ParameterEnum.GestureLeftWeight:
                        logic.enumValueIndex = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BehaviourItem.Condition.LogicCompare)logic.intValue));
                        break;
                    default:
                        logic.enumValueIndex = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BehaviourItem.Condition.LogicEquals)logic.intValue));
                        break;
                }

                //Value
                var value = condition.FindPropertyRelative("value");
                switch(typeValue)
                {
                    case Globals.ParameterEnum.Custom:
                    case Globals.ParameterEnum.Upright:
                    case Globals.ParameterEnum.AngularY:
                    case Globals.ParameterEnum.VelocityX:
                    case Globals.ParameterEnum.VelocityY:
                    case Globals.ParameterEnum.VelocityZ:
                    case Globals.ParameterEnum.GestureRightWeight:
                    case Globals.ParameterEnum.GestureLeftWeight:
                        value.floatValue = EditorGUILayout.FloatField(value.floatValue);
                        break;
                    case Globals.ParameterEnum.GestureLeft:
                    case Globals.ParameterEnum.GestureRight:
                        value.floatValue = System.Convert.ToInt32(EditorGUILayout.EnumPopup((Globals.GestureEnum)(int)value.floatValue));
                        break;
                    case Globals.ParameterEnum.Viseme:
                        value.floatValue = System.Convert.ToInt32(EditorGUILayout.EnumPopup((Globals.VisemeEnum)(int)value.floatValue));
                        break;
                    case Globals.ParameterEnum.TrackingType:
                        value.floatValue = System.Convert.ToInt32(EditorGUILayout.EnumPopup((Globals.TrackingTypeEnum)(int)value.floatValue));
                        break;
                    case Globals.ParameterEnum.AFK:
                    case Globals.ParameterEnum.MuteSelf:
                    case Globals.ParameterEnum.InStation:
                    case Globals.ParameterEnum.IsLocal:
                    case Globals.ParameterEnum.Grounded:
                    case Globals.ParameterEnum.Seated:
                        EditorGUI.BeginDisabledGroup(true);
                        value.floatValue = 1;
                        EditorGUILayout.TextField("True");
                        EditorGUI.EndDisabledGroup();
                        break;
                    case Globals.ParameterEnum.VRMode:
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.IntField(1);
                        EditorGUI.EndDisabledGroup();
                        break;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;
            return true;
        }
        #endregion
    }
}

