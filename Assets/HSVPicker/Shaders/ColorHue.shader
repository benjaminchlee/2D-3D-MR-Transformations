Shader "ColorPicker/ColorHue"
{
	SubShader
	{
	    Pass
		{	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"		
			
			// vertex input: position, UV
			struct appdata {
			    float4 vertex : POSITION;
			    float4 texcoord : TEXCOORD0;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
	
			struct v2f {
			    float4 pos : SV_POSITION;
			    float4 uv : TEXCOORD0;
				
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			v2f vert(appdata v) {
			    v2f o;
				
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
			    o.pos = UnityObjectToClipPos(v.vertex);
			    o.uv = float4(v.texcoord.xy, 0, 0);
				
			    return o;
			}
			
			half4 frag(v2f input) : COLOR
			{
    			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(inputv);
				
				half p = floor(input.uv.x*6);
				half i = input.uv.x*6-p;
				half4 c = p == 0 ? half4(1, i, 0, 1) :
						  p == 1 ? half4(1-i, 1, 0, 1) :
						  p == 2 ? half4(0, 1, i, 1) :
						  p == 3 ? half4(0, 1-i, 1, 1) :
						  p == 4 ? half4(i, 0, 1, 1) :
						  p == 5 ? half4(1, 0, 1-i, 1) :
						           half4(1, 0, 0, 1);
			    return c;
			}
			ENDCG
	    }
	}
}

