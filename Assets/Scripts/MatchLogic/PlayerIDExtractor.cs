using PurrNet;
using PurrNet.Packing;
using UnityEngine;

namespace Resonance.Match
{

    public class PlayerIDExtractor
    {

        public static PlayerID UlongToPlayerId(ulong id)
        {
            return new PlayerID(new PackedULong(id), false);
        }

        public static bool TryExtractPlayerIds(GameObject gameObject, out ulong playerId)
        {
            playerId = 0;
            if (!gameObject.TryGetComponent(out PlayerController.PlayerController controller))
                return false;

            if (controller.id?.id.value is ulong idPrimitive)
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

            if (!first.TryGetComponent(out PlayerController.PlayerController firstController) ||
                !second.TryGetComponent(out PlayerController.PlayerController secondController))
                return false;

            if (firstController.id?.id.value is ulong firstIdPrimitive &&
                secondController.id?.id.value is ulong secondIdPrimitive)
            {
                firstId = firstIdPrimitive;
                secondId = secondIdPrimitive;
                return true;
            }

            return false;
        }
    }
}
