using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class CustomRenderPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // keep empty
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
        }
    }
}
