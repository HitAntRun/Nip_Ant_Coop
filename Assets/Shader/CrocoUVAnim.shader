// Crocotile UV 애니메이션(uvAnimation.txt)을 유니티에서 재생하는 언릿 셰이더.
// Built-in / URP 둘 다 동작 (LightMode 태그 없는 단일 패스).
// 프레임 정보는 CrocoUVAnimation.cs 가 uvAnimation.txt 를 읽어서 넣어줌.
//
// Crocotile은 "Export UV Animations"로 내보낼 때 애니메이션 타일의 UV를
// (100 + 애니메이션번호, 100 * (꼭짓점번호 + 1)) 형태로 바꿔서 저장한다.
// 예) (100,100) = 0번 애니메이션의 0번 꼭짓점, (100,300) = 2번 꼭짓점
// 이 셰이더는 버텍스 단계에서 그 값을 읽어 현재 프레임의 실제 UV로 바꿔준다.
Shader "Croco/UVAnimUnlit"
{
    Properties
    {
        _MainTex ("Tileset", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        _AnimSpeed ("Speed Multiplier", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull [_Cull]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            #define MAX_FRAMES 16

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Cutoff;
            float _UseVertexColor;
            float _AnimSpeed;

            // 스크립트가 채워주는 값
            float  _AnimFrameCount;            // 프레임 수 (0이면 애니메이션 없음)
            float  _AnimLoop;                  // 전체 길이(초)
            float4 _AnimUV01[MAX_FRAMES];      // 프레임별 꼭짓점 0,1 UV (xy, zw)
            float4 _AnimUV23[MAX_FRAMES];      // 프레임별 꼭짓점 2,3 UV (xy, zw)
            float  _AnimEnds[MAX_FRAMES];      // 각 프레임이 끝나는 시각(누적 초)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float2 uv = v.uv;
                // 애니메이션 타일 표시값: x >= 100
                if (uv.x > 99.5)
                {
                    int corner = clamp((int)round(uv.y / 100.0) - 1, 0, 3);
                    int idx = 0;
                    if (_AnimFrameCount > 0.5 && _AnimLoop > 0)
                    {
                        float t = fmod(_Time.y * _AnimSpeed, _AnimLoop);
                        [unroll]
                        for (int k = 0; k < MAX_FRAMES; k++)
                        {
                            if (k < (int)_AnimFrameCount - 1 && t >= _AnimEnds[k]) idx = k + 1;
                        }
                    }
                    float4 a = _AnimUV01[idx];
                    float4 b = _AnimUV23[idx];
                    uv = corner == 0 ? a.xy : corner == 1 ? a.zw : corner == 2 ? b.xy : b.zw;
                }

                o.uv = TRANSFORM_TEX(uv, _MainTex);
                o.color = lerp(fixed4(1,1,1,1), v.color, _UseVertexColor);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * i.color;
                clip(col.a - _Cutoff);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
