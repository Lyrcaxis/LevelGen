Shader "Unlit/TilemapShader"
{
    Properties
    {
        _MainTex ("Tilemap", 2D)   = "white" {}   // The tilemap containing the tile ids to draw.
        _TextureAtlas ("Atlas", 2D)  = "white" {} // The texture atlas containing all the tiles.
        _AtlasDims ("Atlas Dimensions", Vector) = (4, 10, 0, 0) // The amount of tiles in the atlas (x = tiles_per_row, y = tiles_per_column).
        _MapDims ("Tilemap Dimension", Vector) = (40, 40, 0, 0) // Also the amount of tiles we're rendering.
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend SrcAlpha OneMinusSrcAlpha

        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            // The amount of tiles we're rendering is equal to the amount of `_MapDims`, but we're rendering (N x N) pixels per pixel in the _MainTex.
            sampler2D _MainTex;      // The tilemap we're sampling, containing the ids of the tiles to draw as half/int. Used to index into the texture atlas.
            float2 _MapDims;         // The dimensions of the tilemap we're sampling -- in pixels (x = tiles_per_row, y = tiles_per_column).
            sampler2D _TextureAtlas; // The texture atlas we're sampling, containing all the tiles in ARGB32 format. Used to draw the tiles to the screen.
            float2 _AtlasDims;       // The dimensions of the texture atlas grid we're sampling -- in tiles (x = sprites_per_row, y = sprites_per_column).
            float4 _Color;           // The color to tint the tiles with.

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // We're rendering (N x N) pixels per pixel in the _MainTex. First we get the tile we're rendering.
                float2 tileCoord = i.uv * _MapDims; // Convert the UV to a pixel coordinate [e.g.: (0-1) --> (0-40)].
                // e.g.: (4.5, 3.2) coord means we're rendering the tile at (4, 3) at the position (0.5, 0.2) inside that tile.
                float tileIndex = tex2D(_MainTex, tileCoord / _MapDims).r; // Get the tile index at the pixel coordinate.
                if (tileIndex < 0) { discard; } // We've marked the empty tiles as -1, so we won't have to render anything now.
                // Get the UV of the tile's corner and add the offset to get the UV of the pixel we're rendering.
                // We want to make it so if tileIndex is 0, we're sampling the top-left-most tile.
                // But since UVs start from the bottom left, we'll need to flip the UV vertically.
                float2 gridIndex = float2(fmod(tileIndex, _AtlasDims.x), floor(tileIndex / _AtlasDims.x));
                float2 gridCoord = float2(frac(tileCoord).x, 1 - frac(tileCoord).y); // % coord --> 0-1 coord.
                float2 atlasUV = (gridIndex + gridCoord) / _AtlasDims;               // Corner + Offset --> UV.
                float4 clr = tex2D(_TextureAtlas, float2(atlasUV.x, 1 - atlasUV.y)); // Sample the texture atlas.
                if (clr.a == 0) { discard; }
                return clr * _Color;
            }
            ENDCG
        }
    }
}
