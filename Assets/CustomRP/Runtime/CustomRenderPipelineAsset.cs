using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline", fileName = "CustomRenderPipelineAsset")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] private bool _useDynamicBatching = true;
        [SerializeField] private bool _useGPUInstancing = true;
        [SerializeField] private bool _useSrpBatchong = true;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipeline(_useDynamicBatching, _useGPUInstancing, _useSrpBatchong);
        }
    }
}