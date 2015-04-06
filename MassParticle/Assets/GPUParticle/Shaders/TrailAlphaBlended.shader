﻿Shader "GPUParticle/Trail AlphaBlended" {

Properties {
    _BaseColor ("BaseColor", Vector) = (0.15, 0.15, 0.2, 5.0)
    _FadeTime ("FadeTime", Float) = 0.1
}
Category {
    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
    Blend SrcAlpha One
    AlphaTest Greater .01
    ColorMask RGB
    Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }

    SubShader {
        Pass {
CGPROGRAM
#pragma target 5.0
#pragma vertex vert
#pragma fragment frag 
#include "Trail.cginc"
ENDCG
        }
    }
Fallback Off
}
}