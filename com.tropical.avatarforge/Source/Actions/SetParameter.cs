using UnityEngine;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class SetParameter : Action, IMenuInitialize
    {
        [System.Serializable]
        public class Parameter
        {
            public string parameter;
            public string source;
            public float value = 1f;
            public bool resetOnExit = false;

            public VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType changeType = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
            public float valueMin = 0;
            public float valueMax = 1;
            public float chance = 0.5f;

            public Parameter Clone()
            {
                var result = new Parameter();
                result.parameter = parameter;
                result.source = source;
                result.value = value;
                result.resetOnExit = resetOnExit;
                result.changeType = changeType;
                result.valueMin = valueMin;
                result.valueMax = valueMax;
                result.chance = chance;
                return result;
            }
        }
        public Parameter[] parameters;

        public override Action Clone()
        {
            var result = new SetParameter();
            if(parameters != null)
            {
                result.parameters = new Parameter[parameters.Length];
                for(int i=0; i<parameters.Length; i++)
                    result.parameters[i] = parameters[i].Clone();
            }
            return result;
        }
        public void Initialize()
        {
            parameters = new Parameter[1];
        }
    }
}