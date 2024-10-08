using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    public class ConditionsEditor : ActionEditor<Conditions>
    {
        ReorderablePropertyList conditionList = new ReorderablePropertyList(null, foldout: false, addName: "Condition");
        public override void OnInspectorGUI()
        {
            //EditorGUILayout.PropertyField(target.FindPropertyRelative("trigger.combineWithParent"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("requireAllExitConditions"));

            var trigger = target.FindPropertyRelative("trigger");
            var conditions = trigger.FindPropertyRelative("conditions");
            conditionList.list = conditions;
            conditionList.OnElementHeader = DrawTriggerCondition;
            conditionList.OnInspectorGUI();
        }
        bool DrawTriggerCondition(int index, SerializedProperty condition)
        {
            EditorGUIUtility.labelWidth = 64;
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                //Mode
                var mode = condition.FindPropertyRelative("mode");
                EditorGUILayout.PropertyField(mode, GUIContent.none);

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
                        logic.enumValueIndex = System.Convert.ToInt32(EditorGUILayout.EnumPopup((Condition.Logic)logic.intValue));
                        break;
                    case Globals.ParameterEnum.Upright:
                    case Globals.ParameterEnum.AngularY:
                    case Globals.ParameterEnum.VelocityX:
                    case Globals.ParameterEnum.VelocityY:
                    case Globals.ParameterEnum.VelocityZ:
                    case Globals.ParameterEnum.GestureRightWeight:
                    case Globals.ParameterEnum.GestureLeftWeight:
                        logic.enumValueIndex = System.Convert.ToInt32(EditorGUILayout.EnumPopup((Condition.LogicCompare)logic.intValue));
                        break;
                    default:
                        logic.enumValueIndex = System.Convert.ToInt32(EditorGUILayout.EnumPopup((Condition.LogicEquals)logic.intValue));
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
                    case Globals.ParameterEnum.IsOnFriendsList:
                    case Globals.ParameterEnum.Earmuffs:
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
    }
}
