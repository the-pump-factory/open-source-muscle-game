// This shader fills the mesh shape with a color that a user can change using the
// Inspector window on a Material.
Shader "Trash/Toon Muscle Old"
{
    // The _BaseColor variable is visible in the Material's Inspector, as a field
    // called Base Color. You can use it to select a custom color. This variable
    // has the default value (1, 1, 1, 1).
    Properties
    {

        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        _Color("Main Tint Color", Color) = (1,1,1,1)
        
		[Header(Inks)]

		_OutlineThickness("Base Outline Thickness", Float) = 0.0002

		_OutlineThicknessMax("Outline Thickness Maximum", Float) = 0.008

		_OutlineThicknessExp("Outline Thickness Exponent", Float) = 1.1

		_OutlineThicknessMinDistance("Minimum Outline Thickness Distance", Float) = 0

		_OutlineThicknessMaxDistance("Maximum Outline Thickness Distance", Float) = 5

		_OutlineFadePower("Outline Distance Fade Power", Float) = 5.45

		_OutlineFadeThreshold("Outline Fade Alpha Threshold", Range(0, 1)) = 0.006

		_LineColor("Line Color", Color) = (0,0,0,1)

        [HideInInspector] _ZTest("ZTest", Int) = 4

    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="4.5"}
        LOD 300

        Pass
        {
            
            Name "Shading"
			Tags { "LightMode" = "UniversalForward" }
            
            ZWrite On
            ZTest [_ZTest]
            Cull Back

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            // To make the Unity shader SRP Batcher compatible, declare all
            // properties related to a Material in a a single CBUFFER block with
            // the name UnityPerMaterial.
            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseColor variable, so that you
                // can use it in the fragment shader.
                half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                // Returning the _BaseColor value.
                clip(-1);
                return _BaseColor;
            }
            ENDHLSL
        }
        
        Pass
		{

			Name "Outline"
			Tags { "LightMode" = "SRPDefaultUnlit" }
            
            ZWrite On
            ZTest [_ZTest]
			Cull Front

			HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

			#pragma vertex vert
			#pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            half _OutlineThickness;

            Varyings vert(Attributes IN) : SV_POSITION 
            {

                float4 pos = IN.positionOS;

                pos.xyz += IN.normalOS * _OutlineThickness;

                //return mul(UNITY_MATRIX_MVP, float4(pos.xyz, 1.0));
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(pos.xyz);
                return OUT;

            }

            half4 _LineColor;

            half4 frag() : SV_TARGET {
                return _LineColor;
            }

			ENDHLSL

		}

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ZTest [_ZTest]
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SHADOW_CUTOFF
            
            #pragma shader_feature_local _HEIGHTMAP
            #pragma shader_feature_local _VERTEX_DISPLACEMENT
            #pragma shader_feature_local_vertex _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma shader_feature_local_fragment _MASKMAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _DETAIL

            #pragma shader_feature_local _HEIGHTMAP
            #pragma shader_feature_local _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT
            #pragma shader_feature_local_vertex _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
            #pragma shader_feature_local_fragment _DEPTHOFFSET_ON
            
            // -------------------------------------
            // ShaderQuality keywords
            #pragma multi_compile_fragment _ _SHADER_QUALITY_HIGH_QUALITY_DEPTH_NORMALS 

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }

    }
}
