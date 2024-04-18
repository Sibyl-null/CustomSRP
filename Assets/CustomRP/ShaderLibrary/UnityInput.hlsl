#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;  

float4x4 unity_MatrixVP;                // 视图 * 投影矩阵
float4x4 unity_MatrixV;                 // 视图矩阵
float4x4 unity_MatrixInvV;              // 逆视图矩阵
float4x4 unity_prev_MatrixM;            // 上一帧的模型矩阵
float4x4 unity_prev_MatrixIM;           // 上一帧的逆模型矩阵
float4x4 glstate_matrix_projection;     // 投影矩阵

real4 unity_WorldTransformParams;

#endif // CUSTOM_UNITY_INPUT_INCLUDED