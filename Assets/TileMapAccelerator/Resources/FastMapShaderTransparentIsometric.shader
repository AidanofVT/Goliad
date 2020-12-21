Shader "Revision3/FastMapShaderTransparentIsometric"
{
    Properties
    {
		[HideInInspector]_TileMap("Tile Map", 2D) = "white" {}
		[HideInInspector]_TileMapB("Tile MapB", 2D) = "black" {}
		[HideInInspector]_TileMapC("Tile MapC", 2D) = "black" {}
		[HideInInspector]_TileMapD("Tile MapD", 2D) = "black" {}
		[HideInInspector]_EditBuffer("EditBuffer", 2D) = "black" {}
		_TileMapWidth("Tile Map Width", int) = 2048
		_TileMapHeight("Tile Map Height", int) = 2048
		_CellPaddingHorizontal("Cell Padding Horizontal", float) = 0.0
		_CellPaddingVertical("Cell Padding Vertical", float) = 0.0
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
			#pragma enable_d3d11_debug_symbols

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
			sampler2D _TileMapB;
			sampler2D _TileMapC;
			sampler2D _TileMapD;
			sampler2D _EditBuffer;

			uint _TileMapWidth;
			uint _TileMapHeight;
			sampler2D currentTile;
            float4 _MainTex_ST;

			float _CellPaddingHorizontal;
			float _CellPaddingVertical;

			int _SelectedX;
			int _SelectedY;

			int2 AgridID(float2 uv) 
			{
				int2 toret = int2(floor(uv.x), floor(uv.y));

				//To make sure A grid only uses even coordinates
				toret *= 2.0;

				return toret;
			}

			int2 BgridID(float2 uv)
			{

				uv -= float2(0.5 , 0.5);

				int2 toret = int2(floor(uv.x), floor(uv.y));

				//To make sure B grid only uses odd coordinates
				toret *= 2.0;
				toret += 1;

				return toret;
			}

			fixed2 AgridUV(fixed2 uv)
			{
				return fixed2(fmod(uv.x, 1.0) * ( 1.0 + _CellPaddingHorizontal) - (_CellPaddingHorizontal/2.0), fmod(uv.y, 1.0) *  (1.0 + _CellPaddingVertical) - (_CellPaddingVertical / 2.0));
			}

			fixed2 BgridUV(fixed2 uv)
			{
				return fixed2(fmod(uv.x - 0.5, 1.0) * (1.0 + _CellPaddingHorizontal) - (_CellPaddingHorizontal / 2.0), fmod((uv.y) - 0.5, 1.0) * ( 1.0 + _CellPaddingVertical) - (_CellPaddingVertical / 2.0));
			}

			float los(float2 pos, float2 s)
			{
				float2 abspos = abs(pos - float2(0.5, 0.5));

				return sign(abspos.x*s.y + abspos.y*s.x - s.x*s.y);
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = fixed4(0.0,0,0,0);

				//Must do this to fill mesh with interlaced grids
				i.uv *= 0.5;

				fixed2 scaledUV = i.uv * fixed2(_TileMapWidth, _TileMapHeight);

				fixed2 aguv = AgridUV(scaledUV);
				fixed2 bguv = BgridUV(scaledUV);

				int2 agid = AgridID(scaledUV);
				int2 bgid = BgridID(scaledUV);

				float alos = 1.0 - los(aguv, float2(0.5,0.5));
				float blos = 1.0 - los(bguv, float2(0.5,0.5));

				float2 tileUV = float2(agid.x, agid.y);
				tileUV.x /= _TileMapWidth;
				tileUV.y /= _TileMapHeight;

				float2 tileUV2 = float2(bgid.x, bgid.y);

				tileUV2.x /= _TileMapWidth;
				tileUV2.y /= _TileMapHeight; 
				

				//Shader based tile type sampling, including layering value
				fixed4 sc = tex2D(_TileMap, tileUV)*256.0;
				fixed4 scb = tex2D(_TileMapB, tileUV)*256.0;
				fixed4 scc = tex2D(_TileMapC, tileUV)*256.0;
				fixed4 scd = tex2D(_TileMapD, tileUV)*256.0;

				uint sampledTileType = (sc.x) + (sc.y * 256);//Sample tile type from first half of color data
				uint sampledTileTypeb = (scb.x) + (scb.y * 256);
				uint sampledTileTypec = (scc.x) + (scc.y * 256);
				uint sampledTileTyped = (scd.x) + (scd.y * 256);

				uint sampledLayer = sc.z + (sc.w * 256);//Sampled set layer from second half
				uint sampledLayerb = scb.z + (scb.w * 256);
				uint finalLayer = sampledLayer + (_TileMapHeight - agid.y);//Combine grid Y position with layer to get true layer value

				//Do same thing again for B grid
				fixed4 sc2 = tex2D(_TileMap, tileUV2)*256.0;
				fixed4 sc2b = tex2D(_TileMapB, tileUV2)*256.0;
				fixed4 sc2c = tex2D(_TileMapC, tileUV2)*256.0;
				fixed4 sc2d = tex2D(_TileMapD, tileUV2)*256.0;

				uint sampledTileType2 = (sc2.x) + (sc2.y * 256);
				uint sampledTileType2b = (sc2b.x) + (sc2b.y * 256);
				uint sampledTileType2c = (sc2c.x) + (sc2c.y * 256);
				uint sampledTileType2d = (sc2d.x) + (sc2d.y * 256);

				uint sampledLayer2 = sc2.z + (sc2.w * 256);
				uint sampledLayer2b = sc2b.z + (sc2b.w * 256);
				uint finalLayer2 = sampledLayer2 + (_TileMapHeight - bgid.y);


				float3 sampleUV = fixed3(aguv.x, aguv.y, sampledTileType);
				float3 sampleUVb = fixed3(aguv.x, aguv.y, sampledTileTypeb);
				float3 sampleUVc = fixed3(aguv.x, aguv.y, sampledTileTypec);
				float3 sampleUVd = fixed3(aguv.x, aguv.y, sampledTileTyped);

				float3 sampleUV2 = fixed3(bguv.x, bguv.y , sampledTileType2);
				float3 sampleUV2b = fixed3(bguv.x, bguv.y, sampledTileType2b);
				float3 sampleUV2c = fixed3(bguv.x, bguv.y, sampledTileType2c);
				float3 sampleUV2d = fixed3(bguv.x, bguv.y, sampledTileType2d);

				//float c = max(alos, blos);

				float4 c = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUV).xyzw;
				float4 c2 = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUV2).xyzw;
				float4 cb = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUVb).xyzw;
				float4 c2b = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUV2b).xyzw;
				float4 cc = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUVc).xyzw;
				float4 c2c = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUV2c).xyzw;
				float4 cd = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUVd).xyzw;
				float4 c2d = UNITY_SAMPLE_TEX2DARRAY(_TileSetArray, sampleUV2d).xyzw;

				//col = float4(c, c, c, c);

				c2 *= c2.a;
				c *= c.a;
				cb *= cb.a;
				c2b *= c2b.a;
				cc *= cc.a;
				c2c *= c2c.a;
				cd *= cd.a;
				c2d *= c2d.a;
				
				col = max(c, c2);

				//Mix tiles together
				if ((sampledLayer < sampledLayer2) && c.a > 0)
					col = c;
				else if ((sampledLayer2< sampledLayer) && c2.a > 0)
					col = c2;
				else if (cb.a > 0 && c.a + c2.a == 0)
					col = cb;
				else if (c2b.a > 0 && c.a + c2.a == 0)
					col = c2b;
				else if (cc.a > 0 && c.a + c2.a == 0)
					col = cc;
				else if (c2c.a > 0 && c.a + c2.a == 0)
					col = c2c;
				else if (cd.a > 0 && c.a + c2.a == 0)
					col = cd;
				else if (c2d.a > 0 && c.a + c2.a == 0)
					col = c2d;



				if (bgid.x < 0 || bgid.y <= 0 || bgid.x >= _TileMapWidth-1.0 || bgid.y >= (_TileMapHeight - 2.0) )
				{
					col -= c2;
				}

				if (agid.x < 0 || agid.y < 0 || agid.x >= _TileMapWidth - 1.0 || agid.y >= (_TileMapHeight - 1.0))
				{
					col -= c;
				}

		

                return col;
            }
            ENDCG
        }
    }
}
