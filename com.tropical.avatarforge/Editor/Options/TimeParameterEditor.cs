
namespace Tropical.AvatarForge
{
    public class TimeParameterEditor : ActionEditor<TimeParameter>
    {
        public override void OnInspectorGUI()
        {
            DrawParameterDropDown(target.FindPropertyRelative("parameter"), "Parameter");
        }
    }
}
