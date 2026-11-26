Shader "UI/IrisShutter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color      ("Tint", Color) = (1,1,1,1)
        _BladeColor ("Blade Color", Color) = (0.13,0.13,0.14,1)
        _EdgeColor  ("Blade Edge", Color)  = (0.02,0.02,0.02,1)
        _Blades     ("Blade Count", Float) = 9
        _Open       ("Open (0=closed, 1=open)", Range(0,1)) = 1
        _Rotate     ("Rotation (rad)", Float) = 0
        _Twist      ("Blade Curve", Float) = 3
        _Aspect     ("Aspect (w/h)", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            fixed4 _Color, _BladeColor, _EdgeColor;
            float _Blades, _Open, _Rotate, _Twist, _Aspect;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float TAU = 6.2831853;
                float2 p = i.uv - 0.5;
                p.x *= _Aspect;                         // 비율 보정 (프레임 1400x650)
                float r   = length(p);
                float ang = atan2(p.y, p.x) + _Rotate;
                float seg = TAU / _Blades;

                // 구멍(정다각형) : _Open=1이면 프레임 모서리까지 덮을 만큼 크게
                float halfDiag = 0.5 * sqrt(_Aspect * _Aspect + 1.0);
                float apothem  = _Open * halfDiag * 1.08;
                float local    = frac(ang / seg) * seg - seg * 0.5;
                float polyR    = apothem / cos(local);
                float d        = r - polyR;             // >0 이면 날 영역
                float w        = fwidth(d) + 1e-5;
                float bladeMask = smoothstep(-w, w, d);

                // 날 경계선: 반지름에 따라 휘어지게(나선)
                float bladeAng = (ang + _Twist * r) / seg;
                float idx  = floor(bladeAng);
                float f    = frac(bladeAng);
                float lw   = fwidth(bladeAng) * 1.5;
                float seam = 1.0 - smoothstep(0.0, lw, f);

                // 날마다 밝기 조금씩 다르게 + 바깥쪽 살짝 밝게
                float shade = 0.8 + 0.2 * frac(sin(idx * 12.9898) * 43758.5453);
                shade *= lerp(0.85, 1.1, saturate(r / halfDiag));

                // 구멍 테두리 어둡게
                float rim = 1.0 - smoothstep(0.0, 0.025, d);

                float3 col = _BladeColor.rgb * shade;
                col = lerp(col, _EdgeColor.rgb, saturate(max(seam, rim * 0.7)));

                return fixed4(col * i.color.rgb, bladeMask * i.color.a * _BladeColor.a);
            }
            ENDCG
        }
    }
}