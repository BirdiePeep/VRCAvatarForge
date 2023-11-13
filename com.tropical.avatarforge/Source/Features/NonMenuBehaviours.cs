using System.Collections.Generic;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public class NonMenuBehaviours : BehaviourGroup
    {
        [SerializeReference] public List<CustomBehaviour> behaviours = new List<CustomBehaviour>();
    }
}