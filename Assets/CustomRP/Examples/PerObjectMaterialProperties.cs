using UnityEngine;

namespace CustomRP.Examples
{
    [DisallowMultipleComponent]
    public class PerObjectMaterialProperties : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock _propertyBlock;
        
        [SerializeField] private Color _baseColor = Color.white;

        private void OnValidate()
        {
            SetColor();
        }

        private void Awake()
        {
            SetColor();
        }

        private void SetColor()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor(BaseColorId, _baseColor);
            GetComponent<Renderer>().SetPropertyBlock(_propertyBlock);
        }
    }
}
