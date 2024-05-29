using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Examples
{
    public class MeshBall : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;

        private MaterialPropertyBlock _propertyBlock;
        private Matrix4x4[] _matrices;
        private Vector4[] _colors;
        private float[] _cutoffs;
        private float[] _metallic;
        private float[] _smoothness;

        private void Awake()
        {
            _matrices = new Matrix4x4[1023];
            _colors = new Vector4[1023];
            _cutoffs = new float[1023];
            _metallic = new float[1023];
            _smoothness = new float[1023];

            for (int i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10,
                    Quaternion.Euler(Random.value * 360, Random.value * 360, Random.value * 360),
                    Vector3.one * Random.Range(0.5f, 1.5f));
                
                _colors[i] = new Vector4(Random.value, Random.value, Random.value, 1);
                _cutoffs[i] = Random.Range(0.3f, 0.8f);
                
                _metallic[i] = Random.value < 0.25f ? 1f : 0f;
                _smoothness[i] = Random.Range(0.05f, 0.95f);
            }
        }

        private void Update()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
                _propertyBlock.SetVectorArray(BaseColorId, _colors);
                _propertyBlock.SetFloatArray(CutoffId, _cutoffs);
                _propertyBlock.SetFloatArray(MetallicId, _metallic);
                _propertyBlock.SetFloatArray(SmoothnessId, _smoothness);
                
                Vector3[] positions = new Vector3[_matrices.Length];
                for (int i = 0; i < positions.Length; i++)
                    positions[i] = _matrices[i].GetColumn(3);   // 矩阵第三列是位移

                SphericalHarmonicsL2[] lightProbes = new SphericalHarmonicsL2[_matrices.Length];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lightProbes, null);
                _propertyBlock.CopySHCoefficientArraysFrom(lightProbes);
            }

            Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, _matrices.Length, _propertyBlock,
                ShadowCastingMode.On, true, 0, null, LightProbeUsage.CustomProvided);
        }
    }
}