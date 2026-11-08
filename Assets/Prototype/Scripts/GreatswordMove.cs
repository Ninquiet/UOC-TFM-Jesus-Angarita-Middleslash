using System;
using UnityEngine;

namespace Aberracion
{
    [Serializable]
    public struct GreatswordMove
    {
        public string name;
        [Min(.01f)] public float windup;
        [Min(.01f)] public float strike;
        [Min(.01f)] public float recovery;
        [Min(0)] public float linkDelay;
        [Min(0)] public float advance;
        [Range(10, 180)] public float arc;
        [Min(0)] public float damageScale;
        public bool hitsMultipleTargets;
        public Vector3 raisedAngles;
        public Vector3 impactAngles;

        public GreatswordMove(string name, float windup, float strike, float recovery, float linkDelay,
            float advance, float arc, float damageScale, bool hitsMultipleTargets,
            Vector3 raisedAngles, Vector3 impactAngles)
        {
            this.name = name;
            this.windup = windup;
            this.strike = strike;
            this.recovery = recovery;
            this.linkDelay = linkDelay;
            this.advance = advance;
            this.arc = arc;
            this.damageScale = damageScale;
            this.hitsMultipleTargets = hitsMultipleTargets;
            this.raisedAngles = raisedAngles;
            this.impactAngles = impactAngles;
        }
    }
}
