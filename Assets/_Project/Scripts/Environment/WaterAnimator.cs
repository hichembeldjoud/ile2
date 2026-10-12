using UnityEngine;

namespace SurvivalIsland.Environment
{
    /// <summary>
    /// Anime la surface de l'eau tropicale (défilement des vagues, clapotis, ondulation).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [AddComponentMenu("Survival Island/Environment/Water Animator")]
    public class WaterAnimator : MonoBehaviour
    {
        [Tooltip("Vitesse de défilement des vagues (axe X et Y).")]
        [SerializeField]
        private Vector2 waveSpeed = new Vector2(0.035f, 0.02f);

        [Tooltip("Nom de la propriété de texture principale à animer.")]
        [SerializeField]
        private string texturePropertyName = "_BaseMap";

        private Material waterMaterial;
        private Vector2 currentOffset = Vector2.zero;
        private int propertyId;

        private void Start()
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                waterMaterial = rend.material;
                propertyId = Shader.PropertyToID(texturePropertyName);
            }
        }

        private void Update()
        {
            if (waterMaterial != null)
            {
                currentOffset += waveSpeed * Time.deltaTime;
                currentOffset.x %= 1f;
                currentOffset.y %= 1f;

                if (waterMaterial.HasProperty(propertyId))
                {
                    waterMaterial.SetTextureOffset(propertyId, currentOffset);
                }
                if (waterMaterial.HasProperty("_BumpMap"))
                {
                    waterMaterial.SetTextureOffset("_BumpMap", currentOffset * 1.6f);
                }
            }
        }
    }
}
