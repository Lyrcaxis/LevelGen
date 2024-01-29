using System.Linq;
using UnityEngine;

public static class TilemapPopulator {
	/// <summary> Retrieves a map with the types of tiles that should be at each position on the heightMap, as a pre-rendering step. </summary>
	/// <remarks> The returned map is double the resolution of the heightmap, allowing us to put more detail in the rendering parts. </remarks>
	public static TileType[,] RetrieveTilesForHeightmap(RenderTexture heightMap, WeightedSelection<TileType> middlePopulationStrategy) {
		var tileMap = new TileType[heightMap.width * 2, heightMap.height * 2];

		// Double the resolution of the heightMap, creating 4 tiles for each heightmap cell.
		var doubleResMap = CSUtils.TextureUtils.DoubleResolution(heightMap);
		var reusableRT  = CSUtils.CreateRT(doubleResMap.width, doubleResMap.height); // Create to reuse per instruction,
		var reusableRT2 = CSUtils.CreateRT(doubleResMap.width, doubleResMap.height); // ..to avoid multiple allocations.
		var edges = CSUtils.EdgeUtils.GetOutline(doubleResMap);

		// Get the outer corners: Get edges, filter by 'Top Left', retrieve indices, assign them to current Map,
		// .. then repeat for each of the corner types, effectively filling the map with all outer edges.
		AddOuterEdgesToTilemap(IsleEdgeFlag.TOP_LEFT,     TileType.TopLeft_Outer);
		AddOuterEdgesToTilemap(IsleEdgeFlag.TOP_RIGHT,    TileType.TopRight_Outer);
		AddOuterEdgesToTilemap(IsleEdgeFlag.BOTTOM_LEFT,  TileType.BotLeft_Outer);
		AddOuterEdgesToTilemap(IsleEdgeFlag.BOTTOM_RIGHT, TileType.BotRight_Outer);
		
		// Get the inner corners: For each diagonal, get all active tiles that contain a neighbor in that direction,
		// ..then, intersect with the ones that aren't edges, and voila! We have the inner corners of that direction!
		AddInnerCornersToTilemap(new(-1, +1), TileType.TopLeft_Inner);
		AddInnerCornersToTilemap(new(+1, +1), TileType.TopRight_Inner);
		AddInnerCornersToTilemap(new(-1, -1), TileType.BotLeft_Inner);
		AddInnerCornersToTilemap(new(+1, -1), TileType.BotRight_Inner);

		// Now, for the sides: Since we want them to alternate between Side1 and Side2, we'll start from the bot-left,
		// ..and check for each position's preceding neighbor before setting the appropriate tile type on that position.
		AddSideEdgesToTilemap(IsleEdgeFlag.LEFT,   TileType.Left2,	TileType.Left1,		new(0, -1));
		AddSideEdgesToTilemap(IsleEdgeFlag.RIGHT,  TileType.Right2,	TileType.Right1,	new(0, -1));
		AddSideEdgesToTilemap(IsleEdgeFlag.TOP,    TileType.Top1,	TileType.Top2,		new(-1, 0));
		AddSideEdgesToTilemap(IsleEdgeFlag.BOTTOM, TileType.Bot1,	TileType.Bot2,		new(-1, 0));

		// Lastly, it's time for the middle tiles: They need to follow the rule of one BigTile per 2x2 tile minus the edges.
		// ..so, we'll scan from the bottom-left onwards, and fill every 2 tiles with the continuation, inferred from the neighbor,
		// ..taking a new random-weighted bot-left tile when neither the left or bot neighbors 'need' a continuation.
		AddMiddleTilesToTilemap();

		CSUtils.ReleaseAll(doubleResMap, edges, reusableRT, reusableRT2);
		return tileMap;



		void AddOuterEdgesToTilemap(IsleEdgeFlag flagType, TileType tileType) {
			CSUtils.ComparisonUtils.Equals(edges.CopyTo(reusableRT), (float) flagType);	// Outer corners are directly obtainable from our EdgeUtils.
			var positions = CSUtils.TextureUtils.GetActivePositions(reusableRT.ToBinaryMask()); // ..so just iterate through their positions,
			foreach (var pos in positions) { tileMap[(int) pos.x, (int) pos.y] = tileType; }	// ..and assign the appropriate tile type.
		}
		void AddInnerCornersToTilemap(Vector2Int neighborDir, TileType tileType) {
			CSUtils.MaskUtils.Invert(edges.CopyTo(reusableRT));								 // Get all active tiles that are not directional edges.
			CSUtils.MaskUtils.Intersect(doubleResMap, reusableRT);							 // Keep only the ones that are active in the current map.
			CSUtils.EdgeUtils.HasNeighbor(doubleResMap, neighborDir, reusableRT2);			 // Get all active tiles that have a neighbor on that direction.
			CSUtils.MaskUtils.IntersectRemove(reusableRT2, reusableRT);						 // Intersect the non-edges with the ones that don't have a neighbor,
			var positions = CSUtils.TextureUtils.GetActivePositions(reusableRT);			 // ..and this leaves us with only the inner corners of that direction,
			foreach (var pos in positions) { tileMap[(int) pos.x, (int) pos.y] = tileType; } // ..whose positions we'll assign the tile type to in the tilemap.
		}
		void AddSideEdgesToTilemap(IsleEdgeFlag flagType, TileType baseTileType, TileType alternateTileType, Vector2Int precedingDir) {
			// Get the positions that are sides of that type, nicely ordered in the order we want to process them.
			CSUtils.ComparisonUtils.Equals(edges.CopyTo(reusableRT), (float) flagType);
			var positions = CSUtils.TextureUtils.GetActivePositions(reusableRT.ToBinaryMask()).OrderByDescending(GridOrderFunc);

			// Then, operate on the ordered list to assign either the base tile type or the alternate tile type,
			// .. depending on whether it's a continuation from an inner corner, an outer corner, or a side tile.
			foreach (var pos in positions) {
				var precedingPos = Vector2Int.RoundToInt(pos + precedingDir);
				tileMap[(int) pos.x, (int) pos.y] = GetSideContinuation(tileMap[precedingPos.x, precedingPos.y]);
			}


			// Helper func allowing us to order the positions based on the preceding dir, making it optimal to iterate on the ordered list later on.
			float GridOrderFunc(Vector2 pos) {
				// The ordering is made in such way that preceded tiles always get handled first, so we'll know what to do with the next tile based on that.
				var xVal = pos.x * Mathf.Sign(precedingDir.x);	// This allows us to give bigger priority to tiles on the opposite direction of what we're traversing.
				var yVal = pos.y * Mathf.Sign(precedingDir.y);	// ..allowing us to process tiles from the preceding dir first. NOTE: `Mathf.Sign` only returns -1 or 1, NOT 0!
				return xVal + yVal * tileMap.GetLength(0);		// Lastly, return a scalar value consisting of the x and y components, containing the pos's order in the array.
			}
			TileType GetSideContinuation(TileType prevTile) => prevTile switch {
				TileType.Left1	=> TileType.Left2,
				TileType.Right1	=> TileType.Right2,
				TileType.Top1	=> TileType.Top2,
				TileType.Bot1	=> TileType.Bot2,
				TileType.Left2	=> TileType.Left1,
				TileType.Right2	=> TileType.Right1,
				TileType.Top2	=> TileType.Top1,
				TileType.Bot2	=> TileType.Bot1,
				TileType.BotRight_Inner or TileType.BotLeft_Inner or TileType.TopRight_Inner or TileType.TopLeft_Inner => alternateTileType,
				_ => baseTileType
			};
		}
		void AddMiddleTilesToTilemap() {
			// Get all the positions of non-edges and order them from-left-to-right, row-by-row, so smaller x and smaller y comes first.
			var nonEdges  = CSUtils.MaskUtils.IntersectRemove(edges, doubleResMap.CopyTo(reusableRT));
			var positions = CSUtils.TextureUtils.GetActivePositions(nonEdges).OrderBy(pos => pos.x + pos.y * tileMap.GetLength(0));

			// These include inner corner tiles, so we should ignore indices that are already set (they're inner corners).
			foreach (var pos in positions) {
				var (x, y) = ((int) pos.x, (int) pos.y);
				if (tileMap[x, y] != TileType.None) { continue; }

				// Infer the continuation for the current 2x2 tile from one of the neighbors, or spawn a new BotLeft corner instead if we must.
				var nextTile = GetRightContinuation(tileMap[x - 1, y]);					 // If it can be inferred from the left neighbor, use it.
				if (nextTile == 0) { nextTile = GetTopContinuation(tileMap[x, y - 1]); } // .. otherwise fall back to infer from the bottom neighbor.
				if (nextTile == 0) { nextTile = GetRandomBotLeftTile(); }				 // .. and if we shouldn't continue off it, assign a random tile.
				tileMap[x, y] = nextTile; // This ensures that 2x2 tiles will be continued, and new bot-left tiles will be assigned to 'new' tile positions.
			}

			TileType GetRightContinuation(TileType leftTile) => leftTile switch {
				TileType.Middle1_BotL => TileType.Middle1_BotR,
				TileType.Middle2_BotL => TileType.Middle2_BotR,
				TileType.Middle3_BotL => TileType.Middle3_BotR,
				TileType.Middle4_BotL => TileType.Middle4_BotR,
				TileType.Middle5_BotL => TileType.Middle5_BotR,
				TileType.Middle6_BotL => TileType.Middle6_BotR,
				TileType.Middle1_TopL => TileType.Middle1_TopR,
				TileType.Middle2_TopL => TileType.Middle2_TopR,
				TileType.Middle3_TopL => TileType.Middle3_TopR,
				TileType.Middle4_TopL => TileType.Middle4_TopR,
				TileType.Middle5_TopL => TileType.Middle5_TopR,
				TileType.Middle6_TopL => TileType.Middle6_TopR,
				_ => 0
			};
			TileType GetTopContinuation(TileType bottomTile) => bottomTile switch {
				TileType.Middle1_BotL => TileType.Middle1_TopL,
				TileType.Middle2_BotL => TileType.Middle2_TopL,
				TileType.Middle3_BotL => TileType.Middle3_TopL,
				TileType.Middle4_BotL => TileType.Middle4_TopL,
				TileType.Middle5_BotL => TileType.Middle5_TopL,
				TileType.Middle6_BotL => TileType.Middle6_TopL,
				_ => 0
			};
			TileType GetRandomBotLeftTile() => middlePopulationStrategy.GetRandomObject();
		}
	}

}
