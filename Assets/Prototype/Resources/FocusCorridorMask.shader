Shader "Hidden/Aberracion/FocusCorridorMask"
{
    Properties
    {
        _Color ("Mask color", Color) = (0, 0, 0, 0.98)
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _CorridorFrame;
            float4 _CorridorShape;

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 corridorPosition : TEXCOORD0;
            };

            Varyings Vert(float4 position : POSITION)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(position);
                float2 offset = position.xy - _CorridorFrame.xy;
                float2 axis = _CorridorFrame.zw;
                float2 normal = float2(-axis.y, axis.x);
                output.corridorPosition = float2(dot(offset, axis), dot(offset, normal));
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float radius = _CorridorShape.z;
                float2 corner = abs(input.corridorPosition) - _CorridorShape.xy + radius;
                float distance = length(max(corner, 0.0)) + min(max(corner.x, corner.y), 0.0) - radius;
                float feather = _CorridorShape.w;
                float mask = smoothstep(-0.25 * feather, 0.75 * feather, distance);
                return float4(_Color.rgb, _Color.a * mask);
            }
            ENDHLSL
        }
    }
}
