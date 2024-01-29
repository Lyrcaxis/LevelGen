using UnityEngine;
using System.Linq;

public static class IslandsPopulator {
	/// <summary> Generates a RenderTexture that marks the positions of a newly generated 'island' with 1s.
	/// <para> The algorithm works by scanning for available areas, finding one that matches the criteria, and renders a randomly sized rectangle. </para>
	/// <para> The above is repeated 'iterations' times, or until there are no more available areas with the specified criteria. </para>
	/// <para> Finally, the map is post-processed with slight noise and smoothing, to create non-repetitive and smooth shapes. </para>
	/// </summary>
	public static RenderTexture GenerateIslands(RenderTexture availableSpace, int iterations, Vector4 isleHeights, int minWidth, int maxWidth) {
		RenderTexture spaces = null, areas = null; // Some reusable RTs, including ones for keeping track of spaces and areas.
		var tempRTs = Enumerable.Range(0, 5).Select(x => CSUtils.CreateRT(availableSpace.width, availableSpace.height)).ToArray();
		var newMap = CSUtils.CreateRT(availableSpace.width, availableSpace.height);
		for (int i = 0; i < iterations; i++) {
			// First, find the available area of each tile.
			spaces = CSUtils.SpatialAvailability.CalculateAvailableSpaces(availableSpace, spaces);
			areas = CSUtils.SpatialAvailability.CalculateAreas(availableSpace, spaces, isleHeights, areas);

			// Then, get a random spawn position for our new shape, including a 'max available' shape.
			(int posX, int posY, int availableWidth, int rndHeight) = GetRandomSpawnPosition();
			if (posX > 0) { // If an available position was returned, render a new shape on that.
				var spawnSize = new Vector2(Random.Range(minWidth, Mathf.Min(availableWidth, maxWidth)), rndHeight);
				var newShape = CSUtils.RenderUtils.RenderRectangle(tempRTs[4], new(posX, posY), spawnSize, false);
				CSUtils.MathUtils.AddTexture(newShape, newMap);
				CSUtils.MaskUtils.IntersectRemove(newShape, availableSpace);
			}
			CSUtils.ClearAll(spaces, areas, tempRTs[4]);
		}
		CSUtils.ReleaseAll(tempRTs.Concat(new[] { spaces, areas }).ToArray());

		// Finally, post-process the generated isles before returning them.
		StyleIsles(newMap);
		SmoothIsles(newMap);
		return newMap;


		(int posX, int posY, int availableWidth, int rndHeight) GetRandomSpawnPosition() {
			int rndComp = Random.Range(0, 4);
			var rndHeight = isleHeights[rndComp];
			var pos = CSUtils.SpatialAvailability.GetRandomPosition(availableSpace, areas, isleHeights, rndComp, minWidth: Random.Range(minWidth, maxWidth));
			if (pos.x < 0) { // ..if no spot was found for those dims, try again with smaller dimensions.
				for (int j = 0; j < rndComp; j++) {
					rndComp--;
					rndHeight = isleHeights[rndComp];
					pos = CSUtils.SpatialAvailability.GetRandomPosition(availableSpace, areas, isleHeights, rndComp, minWidth: minWidth);
					if (pos.x >= 0) { break; }
				}
			}
			return (pos.x, pos.y, pos.z, (int) rndHeight);
		}

		void StyleIsles(RenderTexture map) {
			for (int j = 0; j < 5; j++) { // Apply some noise to the isle edges.
				var outline = CSUtils.EdgeUtils.GetOutline(map.CopyTo(tempRTs[0]), tempRTs[1]).ToBinaryMask();
				var noiseMap = CSUtils.TextureUtils.GetNoiseMap(map.width, map.height, 0.01f, tempRTs[2]);
				var toDiscard = CSUtils.MaskUtils.IntersectRemove(noiseMap, outline);
				CSUtils.MaskUtils.IntersectRemove(toDiscard, map);
				CSUtils.ClearAll(tempRTs[0], tempRTs[1], tempRTs[2]);
			}
		}
		void SmoothIsles(RenderTexture map) {
			var outline1 = CSUtils.EdgeUtils.GetOutline(map.CopyTo(tempRTs[0]), tempRTs[1]);
			var outline2 = outline1.CopyTo(tempRTs[2]);
			var topEdges = CSUtils.EnumUtils.ContainsFlag(outline1, (float) IsleEdgeFlag.TOP);
			var botEdges = CSUtils.EnumUtils.ContainsFlag(outline2, (float) IsleEdgeFlag.BOTTOM);
			CSUtils.MaskUtils.IntersectRemove(CSUtils.MaskUtils.Intersect(topEdges, botEdges), map);
			CSUtils.TerrainGenUtils.SmoothIsolatedIsleShapes(map);
			CSUtils.ClearAll(tempRTs[0], tempRTs[1], tempRTs[2]);
		}
	}
}