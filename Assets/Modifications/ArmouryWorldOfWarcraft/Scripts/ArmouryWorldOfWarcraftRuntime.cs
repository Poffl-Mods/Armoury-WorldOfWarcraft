using Kingmaker.Modding;
using UnityEngine;

namespace ArmouryWorldOfWarcraft.Runtime
{
    public static class ArmouryWorldOfWarcraftRuntime
    {
        [OwlcatModificationEnterPoint]
        public static void Initialize(OwlcatModification modification)
        {
            FrostmourneSoulProgressionController.EnsureController();
            WorldOfWarcraftArmouryCacheController.EnsureController();
            modification.OnSetEnabled += enabled => { if (enabled) { FrostmourneSoulProgressionController.EnsureController(); WorldOfWarcraftArmouryCacheController.EnsureController(); } };
            Debug.Log("[ArmouryWorldOfWarcraft] Runtime initialized.");
        }
    }
}
