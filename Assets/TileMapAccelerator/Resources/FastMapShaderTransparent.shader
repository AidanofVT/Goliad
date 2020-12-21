Shader "Revision3/FastMapShaderTransparent"
{
    Properties
    {
		[HideInInspector]_TileMap("Tile Map", 2D) = "white" {}
		_TileMapSize("Tile Map Size", int) = 2048
		[PerRendererData]_TileSetArray("TileSetArray", 2DArray) = "" {}
		
	}
		SubShader
	{
		Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100
		
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			UNITY_DECLARE_TEX2DARRAY(_TileSetArray);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			sampler2D _TileMap;
			uint _TileMapSize;
			sampler2D currentTile;
            float4 _MainTex_ST;

			int _SelectedX;
			int _SelectedY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                return o;
            }

			

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = fixed4(0,0,0,0);

				//First create scaled UV using draw range var
				float2 scaledUV = float2(i.uv.x*_TileMapSize, i.uv.y*_TileMapSize);

				//"Normalization" of sampling UV to fix annoying seams. 
				float2 normalizedSample = float2(clamp(frac(scaledUV.x), 0.01, 0.99), clamp(frac(scaledUV.y), 0.01, 0.99));

				//Calculate grid ID of current pixel according to Draw Range
				int2 gridID = int2(floor(scaledUV.x), floor(scaledUV.y));

				//Get the tile grid ID and scale turn it into a 0 to 1 uv float2
				float2 tileUV = float2(gridID.x, gridID.y);
				tileUV.x /= _TileMapSize;
				tileUV.y /= _TileMapSize;

				//Improved type sampling, up to 4 million values
				//Sadly only 2048 values allowed per array.
				fixed4 sc = tex2D(_TileMap, tileUV)*256.0;
				uint sampledTileType = (sc.x) + (sc.y * 256) + (sc.z * 65536) + (sc.w * 16777216);

				//Use tile map value and scaledUV to create texture array polling UV, Z component = array index
				float3 arraySampleUV = float3(normalizedSample.x, normalizedSample.y, sampledTileType);

				//Finally sample the tile set texture array and draw the correct tile on grid for data type, using draw range to scale down tiles and fit more data.
				col.xyzw = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, arraySampleUV).xyzw;

                return col;
            }
            ENDCG
        }
    }
}
