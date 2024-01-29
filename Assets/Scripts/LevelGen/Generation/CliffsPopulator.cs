using System.Linq;
using UnityEngine;

public static class CliffsPopulator {
	/// <summary> Retrieves a map with the types of cliff tiles that should be at each position on the heightMap, as a pre-rendering step. </summary>
	/// <remarks> The returned map is double the resolution of the heightmap, allowing us to put more detail in the rendering parts. </remarks>
	public static CliffType[,] RetrieveTilesForCliffs(RenderTexture cliffsMap, TileType[,] heightMap) {
		var tileMap = new CliffType[cliffsMap.width * 2, cliffsMap.height * 2];

		// Double the resolution of the heightMap, creating 4 tiles for each heightmap cell.
		var doubleResMap = CSUtils.TextureUtils.DoubleResolution(cliffsMap);
		var edges = CSUtils.EdgeUtils.GetOutline(doubleResMap);
		var cornerRTs = new RenderTexture[4];

		// Get the outer corners: Get edges, filter by 'Top Left', retrieve indices, assign them to current Map,
		// .. then repeat for each of the corner types, effectively filling the map with all outer edges.
		cornerRTs[0] = AddOuterEdgesToTilemap(IsleEdgeFlag.TOP_LEFT,     CliffType.TopLeft);
		cornerRTs[1] = AddOuterEdgesToTilemap(IsleEdgeFlag.TOP_RIGHT,    CliffType.TopRight);
		cornerRTs[2] = AddOuterEdgesToTilemap(IsleEdgeFlag.BOTTOM_LEFT,  CliffType.BotLeft);
		cornerRTs[3] = AddOuterEdgesToTilemap(IsleEdgeFlag.BOTTOM_RIGHT, CliffType.BotRight);

		// As for the rest of the tiles, we can now easily infer the correct cliff tile type for them,
		// .. by checking the type of the left neighbor tile (that's already set for corners).
		var nonCornersRT = AddNonCornerTilesToTilemap();

		CSUtils.ReleaseAll(cornerRTs);
		CSUtils.ReleaseAll(doubleResMap, edges, nonCornersRT);
		return tileMap;



		RenderTexture AddOuterEdgesToTilemap(IsleEdgeFlag flagType, CliffType tileType) {
			var outerEdges = CSUtils.ComparisonUtils.Equals(edges.Copy(), (float) flagType);	 // Outer corners are directly obtainable from our EdgeUtils,
			var positions  = CSUtils.TextureUtils.GetActivePositions(outerEdges.ToBinaryMask()); // .. and we can retrieve them from the GPU from TilemapUtils,
			// ..so we iterate through the positions to infer and assign the appropriate tile type, based on the neighbor on the top.
			foreach (var pos in positions) { tileMap[(int) pos.x, (int) pos.y] = GetAppropriateCliffType(Vector2Int.RoundToInt(pos)); }
			return outerEdges; // Finally, return the binary masks marking those corners for future use.

			CliffType GetAppropriateCliffType(Vector2Int pos) {
				switch (flagType) {
					case IsleEdgeFlag.TOP_LEFT:
					case IsleEdgeFlag.TOP_RIGHT:
						switch (heightMap[pos.x, pos.y + 1]) {
							case TileType.Bot1: { return CliffType.Top1; }
							case TileType.Bot2: { return CliffType.Top2; }
							default: { return tileType; }
						}
					case IsleEdgeFlag.BOTTOM_LEFT:
					case IsleEdgeFlag.BOTTOM_RIGHT:
						switch (heightMap[pos.x, pos.y + 2]) {
							case TileType.Bot1: { return CliffType.Bot1; }
							case TileType.Bot2: { return CliffType.Bot2; }
							default: { return tileType; }
						}
					default: { return 0; }
				}
			}
		}
		RenderTexture AddNonCornerTilesToTilemap() {
			// First, get all the active non-corner tiles on the map, and inverse intersect their combination to get the non-corners.
			var allCorners = CSUtils.CreateRT(doubleResMap.width, doubleResMap.height);
			for (int i = 0; i < 4; i++) { CSUtils.MaskUtils.IntersectAdd(cornerRTs[i], allCorners); }
			var nonCorners = CSUtils.MaskUtils.IntersectRemove(allCorners, doubleResMap);

			// Get all the positions of non-corners and order them from-left-to-right, row-by-row, so smaller x and smaller y comes first.
			var positions = CSUtils.TextureUtils.GetActivePositions(nonCorners).OrderBy(pos => pos.x + pos.y * tileMap.GetLength(0));

			// Now, go through the populate with the correct tile type, we'll infer it from the neighbor on the left.
			// This works as-is because corners already have the appropriate tile set for them, so continuing with alternates is a breeze.
			foreach (var pos in positions) {
				var (x, y) = ((int) pos.x, (int) pos.y);
				tileMap[x, y] = GetRightContinuation(tileMap[x - 1, y]);
			}
			return nonCorners;


			CliffType GetRightContinuation(CliffType leftTile) => leftTile switch {
				CliffType.TopLeft	=> CliffType.Top1,
				CliffType.Top1		=> CliffType.Top2,
				CliffType.Top2		=> CliffType.Top1,
				CliffType.Left1		=> CliffType.Mid1,
				CliffType.Mid1		=> CliffType.Mid2,
				CliffType.Mid2		=> CliffType.Mid1,
				CliffType.Left2		=> CliffType.Mid3,
				CliffType.Mid3		=> CliffType.Mid4,
				CliffType.Mid4		=> CliffType.Mid3,
				CliffType.BotLeft	=> CliffType.Bot1,
				CliffType.Bot1		=> CliffType.Bot2,
				CliffType.Bot2		=> CliffType.Bot1,
				_ => 0
			};
		}
	}
}