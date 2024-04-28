using NaughtyAttributes;
using UnityEngine;

namespace CustomRP.Examples
{
    [DisallowMultipleComponent]
    public class PerObjectMaterialProperties : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static MaterialPropertyBlock _propertyBlock;
        
        [SerializeField] private Color _baseColor = Color.white;
        [SerializeField, Range(0, 1)] private float _cutoff = 0.5f;
        [SerializeField, Range(0, 1)] private float _metallic = 0f;
        [SerializeField, Range(0, 1)] private float _smoothness = 0.5f;

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
            _propertyBlock.SetFloat(MetallicId, _metallic);
            _propertyBlock.SetFloat(SmoothnessId, _smoothness);
            GetComponent<Renderer>().SetPropertyBlock(_propertyBlock);
        }
        
        [Button]
        public void RandomColor()
        {
            _baseColor = new Color(Random.value, Random.value, Random.value, 1);
            Setup();
        }
    }
}
