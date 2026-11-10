using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using Swole.DataStructures;
using static Swole.API.Unity.MuscularCharacterSharedData;
using static VeinsToonModelPrep;
using static Swole.API.Unity.MuscularRenderedCharacter;

using Swole;
using Swole.API.Unity;

public class MuscleToonModelPrep2 : MonoBehaviour
{

    [Serializable]
    public class MuscleGroup
    {

        public string name;

        public string shapeName;

        public MuscleToonModelPrep.UVSide uvSide;

    }



}
