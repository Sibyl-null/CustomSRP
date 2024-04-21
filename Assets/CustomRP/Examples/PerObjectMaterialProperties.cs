using UnityEngine;

namespace CustomRP.Examples
{
    [DisallowMultipleComponent]
    public class PerObjectMaterialProperties : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        private static MaterialPropertyBlock _propertyBlock;
        
        [SerializeField] private Color _baseColor = Color.white;
        [SerializeField, Range(0, 1)] private float _cutoff = 0.5f;

        private void OnValidate()
        {
            Setup();
        }

        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor(BaseColorId, _baseColor);
            _propertyBlock.SetFloat(CutoffId, _cutoff);
            GetComponent<Renderer>().SetPropertyBlock(_propertyBlock);
        }
    }
}
