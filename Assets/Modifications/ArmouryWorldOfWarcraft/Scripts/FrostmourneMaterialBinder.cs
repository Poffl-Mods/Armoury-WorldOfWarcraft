using UnityEngine;

namespace ArmouryWorldOfWarcraft.Runtime
{
    public sealed class FrostmourneMaterialBinder : MonoBehaviour
    {
        private void Awake() => BindGameShader();
        private void OnEnable() => BindGameShader();

        private void BindGameShader()
        {
            Shader shader = Shader.Find("Owlcat/Lit");
            if (shader == null)
            {
                Debug.LogError("[ArmouryWorldOfWarcraft] Owlcat/Lit runtime shader was not found.");
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                bool changed = false;
                foreach (Material material in materials)
                {
                    if (material == null || material.shader == shader) continue;
                    material.shader = shader;
                    changed = true;
                }
                if (changed) renderer.materials = materials;
            }

            Debug.Log("[ArmouryWorldOfWarcraft] Frostmourne material rebound; renderers=" + renderers.Length);
        }
    }
}
