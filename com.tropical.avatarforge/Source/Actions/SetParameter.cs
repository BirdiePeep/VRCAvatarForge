using System.Collections.Generic;
using UnityEngine;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class SetParameter : Action
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
            //public bool isZeroValid = true;

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
                //result.isZeroValid = isZeroValid;
                return result;
            }
        }
        [SerializeReference] public List<Parameter> parameters = new List<Parameter>();

        public override Action Clone()
        {
            var result = new SetParameter();
            foreach(var param in parameters)
                result.parameters.Add(param.Clone());
            return result;
        }
    }
}