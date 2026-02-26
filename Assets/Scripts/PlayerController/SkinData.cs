using System;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.PlayerController
{
    [Serializable]
    public class SkinSlot
    {
        public Mesh mesh;
        public Material[] materials;
    }

    [CreateAssetMenu(fileName = "New Skin", menuName = "Scriptable Objects/Skin Data")]
    public class SkinData : ScriptableObject
    {
        public string skinName;

        /// <summary>
        /// Slots to render when changing skin; must correspond to the order of
        /// SkinnedMeshRenderers set on PlayerMeshRenderer (e.g. body mesh -> body renderer)
        /// </summary>
        public List<SkinSlot> slots = new();
    }
}
