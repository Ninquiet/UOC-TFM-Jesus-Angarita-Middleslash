using System;
using UnityEngine;

namespace Aberracion
{
    [Serializable]
    public struct EnemyFistVolume
    {
        public Transform center;
        public Transform extentX;
        public Transform extentY;
        public Transform extentZ;

        public bool IsWired => center && extentX && extentY && extentZ;
        public Vector3 Position => center.position;

        public Vector3 HalfExtents => new Vector3(
            Vector3.Distance(center.position, extentX.position),
            Vector3.Distance(center.position, extentY.position),
            Vector3.Distance(center.position, extentZ.position));

        public Quaternion Rotation => Quaternion.LookRotation(extentZ.position - center.position,
            extentY.position - center.position);
    }
}
