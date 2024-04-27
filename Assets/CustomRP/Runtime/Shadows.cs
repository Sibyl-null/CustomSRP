using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private const string BufferName = "Shadows";
        
        private readonly CommandBuffer _buffer = new() { name = BufferName };
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShadowSettings _shadowSettings;

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            _context = context;
            _cullingResults = cullingResults;
            _shadowSettings = shadowSettings;
        }
        
        private void ExecuteBuffer() 
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}