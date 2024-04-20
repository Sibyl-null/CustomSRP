using UnityEngine;

namespace CustomRP.Examples
{
    public class MeshBall : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;

        private MaterialPropertyBlock _propertyBlock;
        private Matrix4x4[] _matrices;
        private Vector4[] _colors;

        private void Awake()
        {
            _matrices = new Matrix4x4[1023];
            _colors = new Vector4[1023];

            for (int i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10, Quaternion.identity, Vector3.one);
                _colors[i] = new Vector4(Random.value, Random.value, Random.value, 1);
            }
        }

        private void Update()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
                _propertyBlock.SetVectorArray(BaseColorId, _colors);
            }

            Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, _matrices.Length, _propertyBlock);
        }
    }
}