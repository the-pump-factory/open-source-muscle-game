using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using PurrNet;

using Swole.API.Unity;

namespace BulkOut 
{

    public class GrowthChamberSession : NetworkBehaviour
    {

        public SerumColor colorBasis;
        public SerumColor colorGorillex;
        public SerumColor colorStallion;
        public SerumColor colorBull;
        public SerumColor colorMonk;

        protected readonly List<SimplePlayer> players = new List<SimplePlayer>();

        public float timeLeft;
        public float timeStart;

        private void Awake()
        {
            GrowthSerum.colorBasis = colorBasis;
            GrowthSerum.colorGorillex = colorGorillex;
            GrowthSerum.colorStallion = colorStallion;
            GrowthSerum.colorBull = colorBull;
            GrowthSerum.colorMonk = colorMonk; 
        }

    }

}