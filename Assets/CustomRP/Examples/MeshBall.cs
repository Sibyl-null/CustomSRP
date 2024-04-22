using UnityEngine;

namespace CustomRP.Examples
{
    public class MeshBall : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;

        private MaterialPropertyBlock _propertyBlock;
        private Matrix4x4[] _matrices;
        private Vector4[] _colors;
        private float[] _cutoffs;

        private void Awake()
        {
            _matrices = new Matrix4x4[1023];
            _colors = new Vector4[1023];
            _cutoffs = new float[1023];

            for (int i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10,
                    Quaternion.Euler(Random.value * 360, Random.value * 360, Random.value * 360),
                    Vector3.one * Random.Range(0.5f, 1.5f));
                
                _colors[i] = new Vector4(Random.value, Random.value, Random.value, 1);
                _cutoffs[i] = Random.Range(0.3f, 0.8f);
            }
        }

        private void Update()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
                _propertyBlock.SetVectorArray(BaseColorId, _colors);
                _propertyBlock.SetFloatArray(CutoffId, _cutoffs);
            }

            Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, _matrices.Length, _propertyBlock);
        }
    }
}