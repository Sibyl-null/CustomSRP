using UnityEngine;

namespace CustomRP.Runtime
{
    [System.Serializable]
    public class ShadowSettings
    {
        public enum TextureSize
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192
        }
        
        [System.Serializable]
        public struct Directional   // used for directional light shadows
        {
            // 使用单个纹理来包含多个阴影贴图
            public TextureSize atlasSize;
            
            // 阴影级联设置
            [Range(1, 4)] public int cascadesCount;
            [Range(0f, 1f)] public float cascadeRatios1;
            [Range(0f, 1f)] public float cascadeRatios2;
            [Range(0f, 1f)] public float cascadeRatios3;
            
            public Vector3 CascadeRatios => new Vector3(cascadeRatios1, cascadeRatios2, cascadeRatios3);
        }
        
        [Min(0f)]
        public float maxDistance = 100f;
        public Directional directional = new()
        {
            atlasSize = TextureSize._1024,
            cascadesCount = 4,
            cascadeRatios1 = 0.1f,
            cascadeRatios2 = 0.25f,
            cascadeRatios3 = 0.5f
        };
    }
}