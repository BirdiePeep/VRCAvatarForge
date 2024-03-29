﻿using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Tropical.AvatarForge
{
    public class MergeAnimators : Feature
    {
        public RuntimeAnimatorController baseController;
        public RuntimeAnimatorController additiveController;
        public RuntimeAnimatorController gestureController;
        public RuntimeAnimatorController actionController;
        public RuntimeAnimatorController fxController;

        public RuntimeAnimatorController sittingController;
        public RuntimeAnimatorController tposeController;
        public RuntimeAnimatorController ikposeController;

        public VRCExpressionsMenu menu;
        public VRCExpressionParameters parameters;
    }
}