using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class CameraRenderer
    {
        private ScriptableRenderContext _context;
        private Camera _camera;
        
        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera = camera;

            Setup();
            DrawVisibleGeometry();
            Submit();
        }

        private void Setup()
        {
            // 设置摄像机的全局着色器变量，例如视图投影矩阵 unity_MatrixVP。
            _context.SetupCameraProperties(_camera);
        }

        private void DrawVisibleGeometry()
        {
            // 该 Camera 参数仅为确定是否应该绘制天空盒（ClearFlag 字段）
            _context.DrawSkybox(_camera);
        }

        private void Submit()
        {
            _context.Submit();
        }
    }
}