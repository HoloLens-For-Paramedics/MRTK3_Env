// TextMesh Pro copyright © 2021 Unity Technologies ApS
// Licensed under the Unity Companion License for Unity-dependent projects--see http://www.unity3d.com/legal/licenses/Unity_Companion_License.
// Unless expressly provided otherwise, the Software under this license is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.

// Simplified SDF shader:
// - No Shading Option (bevel / bump / env map)
// - No Glow Option
// - Softness is applied on both side of the outline

// Graphics Tools Additions
// - Single Pass Instanced Stereo Rendering Support
// - Support for Clipping Primitives (Plane, Sphere, Box)
// - _SrcBlend and _DstBlend Properties
// - ZWrite and ZTest Properties
// - SRP Batcher Support
// - Text Color Inversion Support for Accessibility

Shader "Graphics Tools/Text Mesh Pro" {

Properties {
    _FaceColor      ("Face Color", Color) = (1,1,1,1)
    _FaceDilate           ("Face Dilate", Range(-1,1)) = 0

    _OutlineColor   ("Outline Color", Color) = (0,0,0,1)
    _OutlineWidth         ("Outline Thickness", Range(0,1)) = 0
    _OutlineSoftness      ("Outline Softness", Range(0,1)) = 0

    _UnderlayColor  ("Border Color", Color) = (0,0,0,.5)
    _UnderlayOffsetX      ("Border OffsetX", Range(-1,1)) = 0
    _UnderlayOffsetY      ("Border OffsetY", Range(-1,1)) = 0
    _UnderlayDilate       ("Border Dilate", Range(-1,1)) = 0
    _UnderlaySoftness     ("Border Softness", Range(0,1)) = 0

    _WeightNormal         ("Weight Normal", float) = 0
    _WeightBold           ("Weight Bold", float) = .5

    _ShaderFlags          ("Flags", float) = 0
    _ScaleRatioA          ("Scale RatioA", float) = 1
    _ScaleRatioB          ("Scale RatioB", float) = 1
    _ScaleRatioC          ("Scale RatioC", float) = 1

    _MainTex              ("Font Atlas", 2D) = "white" {}
    _TextureWidth         ("Texture Width", float) = 512
    _TextureHeight        ("Texture Height", float) = 512
    _GradientScale        ("Gradient Scale", float) = 5
    _ScaleX               ("Scale X", float) = 1
    _ScaleY               ("Scale Y", float) = 1
    _PerspectiveFilter    ("Perspective Correction", Range(0, 1)) = 0.875
    _Sharpness            ("Sharpness", Range(-1,1)) = 0

    _VertexOffsetX        ("Vertex OffsetX", float) = 0
    _VertexOffsetY        ("Vertex OffsetY", float) = 0

    _ClipRect             ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
    _ClipRectRadii        ("Clip Rect Radii", vector) = (10, 10, 10, 10)
    _MaskSoftnessX        ("Mask SoftnessX", float) = 0
    _MaskSoftnessY        ("Mask SoftnessY", float) = 0
    
    _StencilComp          ("Stencil Comparison", Float) = 8
    _Stencil              ("Stencil ID", Float) = 0
    _StencilOp            ("Stencil Operation", Float) = 0
    _StencilWriteMask     ("Stencil Write Mask", Float) = 255
    _StencilReadMask      ("Stencil Read Mask", Float) = 255
    
    _CullMode             ("Cull Mode", Float) = 0
    _ColorMask            ("Color Mask", Float) = 15

    [Enum(Microsoft.MixedReality.GraphicsTools.Editor.DepthWrite)] _ZWrite                          ("Depth Write", Float) = 0     // Off
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth Test", Float) = 4      // "LessEqual"

    [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 1                 // "One"
    [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 10           // "OneMinusSrcAlpha"
    [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Source Blend Alpha", Float) = 1      // "One"
    [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Destination Blend Alpha", Float) = 1 // "One"
}

SubShader {
    Tags 
    {
        "Queue"="Transparent"
        "IgnoreProjector"="True"
        "RenderType"="Transparent"
    }


    Stencil
    {
        Ref [_Stencil]
        Comp [_StencilComp]
        Pass [_StencilOp] 
        ReadMask [_StencilReadMask]
        WriteMask [_StencilWriteMask]
    }

    Cull [_CullMode]
    ZWrite[_ZWrite]
    Lighting Off
    Fog { Mode Off }
    ZTest [_ZTest]
    Blend[_SrcBlend][_DstBlend],[_SrcBlendAlpha][_DstBlendAlpha]
    ColorMask [_ColorMask]

    Pass {
        CGPROGRAM
        #pragma vertex VertShader
        #pragma fragment PixShader
        #pragma shader_feature_local __ OUTLINE_ON
        #pragma shader_feature_local __ UNDERLAY_ON UNDERLAY_INNER
        #pragma shader_feature_local __ MULTISAMPLE_ON

        #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
        #pragma multi_compile_local _ _UI_CLIP_RECT_ROUNDED _UI_CLIP_RECT_ROUNDED_INDEPENDENT
        #pragma multi_compile_local __ UNITY_UI_ALPHACLIP

        #pragma multi_compile_local __ _CLIPPING_PLANE _CLIPPING_SPHERE _CLIPPING_BOX
        #pragma multi_compile_local __ _INVERT_TEXT_COLOR

        #include "UnityCG.cginc"
        #include "UnityUI.cginc"
        #include "GraphicsToolsCommon.hlsl"

#if defined(_CLIPPING_PLANE) || defined(_CLIPPING_SPHERE) || defined(_CLIPPING_BOX)
        #define _CLIPPING_PRIMITIVE
#else
        #undef _CLIPPING_PRIMITIVE
#endif

        // Direct include from TMPro_Properties.cginc for SRP support and portability.
 CBUFFER_START(UnityPerMaterial)

        // UI Editable properties
        uniform sampler2D    _FaceTex;                    // Alpha : Signed Distance
        uniform float        _FaceUVSpeedX;
        uniform float        _FaceUVSpeedY;
        uniform fixed4        _FaceColor;                    // RGBA : Color + Opacity
        uniform float        _FaceDilate;                // v[ 0, 1]
        uniform float        _OutlineSoftness;            // v[ 0, 1]
        
        uniform sampler2D    _OutlineTex;                // RGBA : Color + Opacity
        uniform float        _OutlineUVSpeedX;
        uniform float        _OutlineUVSpeedY;
        uniform fixed4        _OutlineColor;                // RGBA : Color + Opacity
        uniform float        _OutlineWidth;                // v[ 0, 1]
        
        uniform float        _Bevel;                        // v[ 0, 1]
        uniform float        _BevelOffset;                // v[-1, 1]
        uniform float        _BevelWidth;                // v[-1, 1]
        uniform float        _BevelClamp;                // v[ 0, 1]
        uniform float        _BevelRoundness;            // v[ 0, 1]
        
        uniform sampler2D    _BumpMap;                    // Normal map
        uniform float        _BumpOutline;                // v[ 0, 1]
        uniform float        _BumpFace;                    // v[ 0, 1]
        
        uniform samplerCUBE    _Cube;                        // Cube / sphere map
        uniform fixed4         _ReflectFaceColor;            // RGB intensity
        uniform fixed4        _ReflectOutlineColor;
        uniform float3      _EnvMatrixRotation;
        uniform float4x4    _EnvMatrix;
        
        uniform fixed4        _SpecularColor;                // RGB intensity
        uniform float        _LightAngle;                // v[ 0,Tau]
        uniform float        _SpecularPower;                // v[ 0, 1]
        uniform float        _Reflectivity;                // v[ 5, 15]
        uniform float        _Diffuse;                    // v[ 0, 1]
        uniform float        _Ambient;                    // v[ 0, 1]
        
        uniform fixed4        _UnderlayColor;                // RGBA : Color + Opacity
        uniform float        _UnderlayOffsetX;            // v[-1, 1]
        uniform float        _UnderlayOffsetY;            // v[-1, 1]
        uniform float        _UnderlayDilate;            // v[-1, 1]
        uniform float        _UnderlaySoftness;            // v[ 0, 1]
        
        uniform fixed4         _GlowColor;                    // RGBA : Color + Intensity
        uniform float         _GlowOffset;                // v[-1, 1]
        uniform float         _GlowOuter;                    // v[ 0, 1]
        uniform float         _GlowInner;                    // v[ 0, 1]
        uniform float         _GlowPower;                    // v[ 1, 1/(1+4*4)]
        
        // API Editable properties
        uniform float         _ShaderFlags;
        uniform float        _WeightNormal;
        uniform float        _WeightBold;
        
        uniform float        _ScaleRatioA;
        uniform float        _ScaleRatioB;
        uniform float        _ScaleRatioC;
        
        uniform float        _VertexOffsetX;
        uniform float        _VertexOffsetY;
        
        uniform float        _MaskID;
        uniform sampler2D    _MaskTex;
        uniform float4        _MaskCoord;

        // #if defined(UNITY_UI_CLIP_RECT)
        uniform float4        _ClipRect;    // bottom left(x,y) : top right(z,w)
        // #if defined(_UI_CLIP_RECT_ROUNDED) || defined(_UI_CLIP_RECT_ROUNDED_INDEPENDENT)
        float4                _ClipRectRadii;
        
        uniform float        _MaskSoftnessX;
        uniform float        _MaskSoftnessY;
        
        // Font Atlas properties
        uniform sampler2D    _MainTex;
        uniform float        _TextureWidth;
        uniform float        _TextureHeight;
        uniform float         _GradientScale;
        uniform float        _ScaleX;
        uniform float        _ScaleY;
        uniform float        _PerspectiveFilter;
        uniform float        _Sharpness;
CBUFFER_END

        struct vertex_t {
            UNITY_VERTEX_INPUT_INSTANCE_ID
            float4    vertex            : POSITION;
            float3    normal            : NORMAL;
            fixed4    color            : COLOR;
#if UNITY_VERSION >= 202300 // Unity 6 (2023)+
            float4	texcoord0		: TEXCOORD0;
#else
            float2    texcoord0        : TEXCOORD0;
#endif
            float2    texcoord1        : TEXCOORD1;
        };

        struct pixel_t {
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
            float4    vertex            : SV_POSITION;
            fixed4    faceColor        : COLOR;
            fixed4    outlineColor    : COLOR1;
            float4    texcoord0        : TEXCOORD0;            // Texture UV, Mask UV
            half4    param            : TEXCOORD1;            // Scale(x), BiasIn(y), BiasOut(z), Bias(w)
            half4    mask            : TEXCOORD2;            // Position in clip space(xy), Softness(zw)
            #if (UNDERLAY_ON | UNDERLAY_INNER)
            float4    texcoord1        : TEXCOORD3;            // Texture UV, alpha, reserved
            half2    underlayParam    : TEXCOORD4;            // Scale(x), Bias(y)
            #endif
#if defined(_CLIPPING_PRIMITIVE)
            float3 worldPosition    : TEXCOORD5;
#endif
        };

#if UNITY_VERSION >= 202300 // Unity 6 (2023)+
		float _UIMaskSoftnessX;
        float _UIMaskSoftnessY;
        int _UIVertexColorAlwaysGammaSpace;
#endif

        pixel_t VertShader(vertex_t input)
        {
            pixel_t output;

            UNITY_INITIALIZE_OUTPUT(pixel_t, output);
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            
#if UNITY_VERSION >= 202300 // Unity 6 (2023)+
            float bold = step(input.texcoord0.w, 0);
#else
            float bold = step(input.texcoord1.y, 0);
#endif

            float4 vert = input.vertex;
            vert.x += _VertexOffsetX;
            vert.y += _VertexOffsetY;
            float4 vPosition = UnityObjectToClipPos(vert);

            float2 pixelSize = vPosition.w;
            pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
            
            float scale = rsqrt(dot(pixelSize, pixelSize));
#if UNITY_VERSION >= 202300 // Unity 6 (2023)+
            scale *= abs(input.texcoord0.w) * _GradientScale * (_Sharpness + 1);
#else
            scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
#endif
            if(UNITY_MATRIX_P[3][3] == 0) scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

            float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
            weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

            float layerScale = scale;

            scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
            float bias = (0.5 - weight) * scale - 0.5;
            float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

#if UNITY_VERSION >= 202300 // Unity 6 (2023)+
            if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
            {
                input.color.rgb = UIGammaToLinear(input.color.rgb);
            }
#endif
            float opacity = input.color.a;
            #if (UNDERLAY_ON | UNDERLAY_INNER)
            opacity = 1.0;
            #endif

            fixed4 faceColor = fixed4(input.color.rgb, opacity) * _FaceColor;
            #if (_INVERT_TEXT_COLOR)
            // Force the face color to white so that `Blend OneMinusDstColor OneMinusSrcColor` performs a true inverted blend.
            faceColor.rgb = fixed3(1, 1, 1);
            #endif
            faceColor.rgb *= faceColor.a;

            fixed4 outlineColor = _OutlineColor;
            //#if (_INVERT_TEXT_COLOR)
            // TBD, need to design how the outline color should be altered. 
            //outlineColor.rgb = fixed3(1, 1, 1) - outlineColor.rgb;
            //#endif
            outlineColor.a *= opacity;
            outlineColor.rgb *= outlineColor.a;
            outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));

            #if (UNDERLAY_ON | UNDERLAY_INNER)
            layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
            float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

            float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
            float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
            float2 layerOffset = float2(x, y);
            #endif

            // Generate UV for the Masking Texture
            float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
            float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

            // Populate structure for pixel shader
            output.vertex = vPosition;
            output.faceColor = faceColor;
            output.outlineColor = outlineColor;
            output.texcoord0 = float4(input.texcoord0.x, input.texcoord0.y, maskUV.x, maskUV.y);
            output.param = half4(scale, bias - outline, bias + outline, bias);
            output.mask = vert;
// #if UNITY_VERSION >= 202300 // Unity 6 (2023)+
//             const half2 maskSoftness = half2(max(_UIMaskSoftnessX, _MaskSoftnessX), max(_UIMaskSoftnessY, _MaskSoftnessY));
//             output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * maskSoftness + pixelSize.xy));
// #else
//             output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
// #endif
            #if (UNDERLAY_ON || UNDERLAY_INNER)
            output.texcoord1 = float4(input.texcoord0 + layerOffset, input.color.a, 0);
            output.underlayParam = half2(layerScale, layerBias);
            #endif
#if defined(_CLIPPING_PRIMITIVE)
            output.worldPosition = mul(UNITY_MATRIX_M, vert).xyz;
#endif

            return output;
        }

        float4 Tex2DMultisample(sampler2D tex, float2 uv)
        {
            float2 dx = ddx(uv) * 0.25;
            float2 dy = ddy(uv) * 0.25;

            float4 sample0 = tex2D(tex, uv + dx + dy);
            float4 sample1 = tex2D(tex, uv + dx - dy);
            float4 sample2 = tex2D(tex, uv - dx + dy);
            float4 sample3 = tex2D(tex, uv - dx - dy);
        
            return (sample0 + sample1 + sample2 + sample3) * 0.25;
        }

        // PIXEL SHADER
        fixed4 PixShader(pixel_t input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            
            #if MULTISAMPLE_ON
            half d = Tex2DMultisample(_MainTex, input.texcoord0.xy).a * input.param.x;
            #else
            half d = tex2D(_MainTex, input.texcoord0.xy).a * input.param.x;
            #endif

            half4 c = input.faceColor * saturate(d - input.param.w);

            #ifdef OUTLINE_ON
            c = lerp(input.outlineColor, input.faceColor, saturate(d - input.param.z));
            c *= saturate(d - input.param.y);
            #endif

            #if (UNDERLAY_ON | UNDERLAY_INNER)
            float4 underlayColor = float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a);
            //#if (_INVERT_TEXT_COLOR)
            // TBD, need to design how the underlay color should be altered. 
            //underlayColor.rgb = float3(1, 1, 1) - underlayColor.rgb;
            //#endif
            #endif

            #if UNDERLAY_ON
            d = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
            c += underlayColor * saturate(d - input.underlayParam.y) * (1 - c.a);
            #endif

            #if UNDERLAY_INNER
            half sd = saturate(d - input.param.z);
            d = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
            c += underlayColor * (1 - saturate(d - input.underlayParam.y)) * sd * (1 - c.a);
            #endif

            #if defined(UNITY_UI_CLIP_RECT)
            //half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
            //c *= m.x * m.y;
            c *= GTUnityUIClipRect(input.mask.xy, _ClipRect, _ClipRectRadii);
            #endif

            #if (UNDERLAY_ON | UNDERLAY_INNER)
            c *= input.texcoord1.z;
            #endif

            // Primitive clipping.
#if defined(_CLIPPING_PRIMITIVE)
            float primitiveDistance = 1.0;
#if defined(_CLIPPING_PLANE)
            primitiveDistance = min(primitiveDistance, GTPointVsPlane(input.worldPosition, _ClipPlane) * _ClipPlaneSide);
#endif
#if defined(_CLIPPING_SPHERE)
            primitiveDistance = min(primitiveDistance, GTPointVsSphere(input.worldPosition, _ClipSphereInverseTransform) * _ClipSphereSide);
#endif
#if defined(_CLIPPING_BOX)
            primitiveDistance = min(primitiveDistance, GTPointVsBox(input.worldPosition, _ClipBoxInverseTransform) * _ClipBoxSide);
#endif
            c *= step(0.0, primitiveDistance);
#endif

            #if UNITY_UI_ALPHACLIP
            clip(c.a - 0.001);
            #endif

            return c;
        }
        ENDCG
    }
}

CustomEditor "Microsoft.MixedReality.GraphicsTools.Editor.TextMeshProShaderGUI"
}
