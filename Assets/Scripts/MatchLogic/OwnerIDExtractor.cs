using PurrNet;
using PurrNet.Packing;
using UnityEngine;

namespace Resonance.Match
{

    public class OwnerIDExtractor
    {

        public static PlayerID UlongToPlayerId(ulong id)
        {
            return new PlayerID(new PackedULong(id), false);
        }

        public static bool TryExtractPlayerIds(GameObject gameObject, out ulong playerId)
        {
            playerId = 0;
            
            // Get anything that is a NetworkBehaviour, I presume?
            if (!gameObject.TryGetComponent(out NetworkBehaviour controller))
                return false;

            if (controller.owner?.id.value is ulong idPrimitive)
            {
                playerId = idPrimitive;
                return true;
            }

            return false;
        }

        public static bool TryExtractPlayerIds(GameObject first, GameObject second, out ulong firstId, out ulong secondId)
        {
            firstId = 0;
            secondId = 0;

            if (!first.TryGetComponent(out NetworkBehaviour firstController) ||
                !second.TryGetComponent(out NetworkBehaviour secondController))
                return false;

            if (firstController.owner?.id.value is ulong firstIdPrimitive &&
                secondController.owner?.id.value is ulong secondIdPrimitive)
            {
                firstId = firstIdPrimitive;
                secondId = secondIdPrimitive;
                return true;
            }

            return false;
        }
    }
}
