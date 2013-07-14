
Shader "Custom/TronShader" {
	SubShader {
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      struct v2f {
          float4 pos : SV_POSITION;
          fixed4 color : COLOR;
      };
      

      v2f vert (appdata_full v)
      {
          v2f o;
          o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
          o.color.xyz = (v.texcoord.x > 0.9) ? 0.5 : 1.0;
          o.color.xyz = (v.texcoord.y > 0.9) ? 0.5 : 1.0;
          o.color.w = 1.0;
          return o;
      }

      fixed4 frag (v2f i) : COLOR0 { return i.color; }
      ENDCG
    }
  } 
}
