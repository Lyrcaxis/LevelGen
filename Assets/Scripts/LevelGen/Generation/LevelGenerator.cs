using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class LevelGenerator : MonoBehaviour {
	[SerializeField, Range(50, 400)] int worldSize = 50;
	[SerializeField] bool cropEdges = true;
	[SerializeField] bool spawnObjects;
	[SerializeField] bool spawnIslands;
	[SerializeField] Texture2D cliffAtlas;
	[Space]
	[SerializeField] WeightedSelection<TerrainGenerationStrategy> terrainStrategy;
	[SerializeField] WeightedSelection<IsleContinuationStrategy> isleContinuationStrategy;
	[SerializeField] List<SpawnableObjectType> spawnableObjects;
	[SerializeField] IslandGenerationStrategy islandGenerationStrategy;
	[SerializeField] WeightedSelection<TileType> middleTileStrategy;
	[SerializeField] List<BiomeTextureMap> availableBiomes;

	int cropOffset => cropEdges ? 4 : 0; // Adding some offset for the edges. This will be removed before rendering.
	Vector2Int size => Vector2Int.one * (worldSize + cropOffset / 2);


	void Start() => GenerateNewLevel();
	void Update() { if (Input.GetKeyDown(KeyCode.X)) { GenerateNewLevel(); } }

	void GenerateNewLevel() {
		WorldRenderer.instance.Cleanup();

		var sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		// The full map is a [size, size] texture, padded by 1 pixel for better QOL in the generation algorithms. That extra pixel will always remain empty throughout generation.
		var fullMap = CSUtils.RenderUtils.RenderRectangle(CSUtils.CreateRT(size.x, size.y), new(1, 1), new(size.x - 2, size.y - 2), false); // A rect, with 1 pixel padding per side.
		var (terrainLayers, terrainCliffs)   = (new RenderTexture[terrainStrategy.Count], new RenderTexture[terrainStrategy.Count]); // We'll store the terrains' body & cliffs here.
		var (islandLayers, islandCliffs)     = (new RenderTexture[terrainStrategy.Count], new RenderTexture[terrainStrategy.Count]); // We'll store the islands'  body & cliffs here.
		(terrainLayers[0], terrainCliffs[0]) = (fullMap.Copy(), CSUtils.CreateRT(size.x, size.y)); // First terrain occupies the full map.

		// Create a map that maps each biome to the heightmaps of that biome, and another one that maps each heightmap to the order with which it should be rendered.
		// The maps should be initialized with the first 'base ground' terrain layer and its cliffs (even if they're empty, for nice code QOL).
		var biomeMap      = new Dictionary<BiomeType, List<RenderTexture>>() { { BiomeType.Dirt, new() { terrainLayers[0] } }, { BiomeType.None, new() { terrainCliffs[0] } } };
		var biomeOrderMap = new Dictionary<RenderTexture, int>() { { terrainLayers[0], 0 }, { terrainCliffs[0], 1 } };
		var tempRTs = new List<RenderTexture>(); // Finally, create a list we can store some temporary RTs to dispose of at the end of the generation to avoid memory leaks.

		// Generate the base terrain layer for the terrain height step, using the heightmap of the previously generated layer as input.
		var terrainPercentages = terrainStrategy.GetPercentages();	// Get the coverage each terrain layer should occupy.
		for (int i = 1; i < terrainStrategy.Count; i++) {			// The first 'layer' has the whole map available for spawning, so we can start from `i = 1`.
			// The available-for-spawning terrain is the heightmap of the previously generated terrain, which is already set in the 'terrainLayers' array's `i - 1` index.
			var map = GenerateTerrain(terrainLayers[i - 1], terrainPercentages.Skip(i).Sum(), terrainPercentages[i]); // The coverage includes coverage of other height steps.
			(terrainLayers[i], terrainCliffs[i]) = AddTerrainToBiome(map, 10 * i); // Add the terrain to the biome map and cache the heightmaps on their arrays for future use.
		}
		// As post-processing, quickly adjust each layer to exclude any terrain occupied by other terrain layers -- those parts won't be rendered anyway, so it's both nice QOL and performant.
		for (int i = 0; i < terrainStrategy.Count - 1; i++) {
			CSUtils.MaskUtils.IntersectRemove(terrainLayers[i + 1], terrainLayers[i]); // Substract the follow-up terrain's heightmap RenderTexture,
			var edges = CSUtils.EdgeUtils.NeighborsWith(terrainLayers[i], GetTempCopy(terrainLayers[i + 1])); // .. then find the terrain's bordering edges,
			CSUtils.MaskUtils.Intersect(fullMap, CSUtils.MaskUtils.IntersectAdd(edges, terrainLayers[i]));    // .. and include them in the spawnable terrain.
			edges.Release(); // This will ensure that only the terrain that's visible will be populated, and that grass/dirt shapes below cliffs won't end abruptly.
		}

		// Next, generate the grass for each layer's height step. If multiple grass layers are present, the follow-up ones will spawn on top of existing ones.
		for (int i = 0; i < terrainStrategy.Count; i++) {
			var map = terrainLayers[i];    // The first grass layer should have the whole terrain available for spawning on.
			int sortingOrder = 10 * i + 2; // Grass's sorting order should be right after the biomes. There are 2 Biomes per terrain, so +2.
			foreach (var grassLayer in terrainStrategy[i].grassLayers) { // Go through each grass layer, create it, and add to biome map.
				map = CSUtils.TerrainGenUtils.CreateGrass(map, grassLayer.coverage / 100f);	// (Re)Use the map as input for future grass layers.
				AddToBiome(map, GetBiomeType(grassLayer.grassAtlas), sortingOrder++);		// .. and we can add the RT to the appropriate biome type.
			}
		}

		// Then, spawn some isles on each step level, if specified in the inspector.
		if (spawnIslands) {
			for (int i = 0; i < terrainStrategy.Count; i++) { // Spawn a set of islands on each terrain level.
				var map = GetTempCopy(terrainLayers[i]); // First, calculate the available area for spawning the isle on. We want borders of isles to not overlap with the terrain.
				if (i < terrainStrategy.Count - 1) { CSUtils.MaskUtils.IntersectRemove(CSUtils.EdgeUtils.Expand(terrainCliffs[i + 1], GetTempCopy(terrainCliffs[i + 1])), map); }
				map = LeaveSomeSpaceOnBottomEdges(map);  // Above we leave some space on the top, and here we leave some space on the bottom of the height step's spawnable area.

				// Now that we got the available space, let's calculate the parameters for the island generation & biomes.
				var startSortingOrder = 10 * i + terrainStrategy[i].grassLayers.Count + 2;	// +2 for the terrain biomes.
				var maxIterations = CSUtils.TextureUtils.GetActivePixelsCount(map) / 100;	// Make sure we don't spawn WAY too much.
				var iterations = Mathf.CeilToInt(Mathf.Lerp(0, maxIterations, islandGenerationStrategy.intensity / 100f));
				var (minWidth, maxWidth) = (islandGenerationStrategy.minWidthPerIslePiece, islandGenerationStrategy.maxWidthPerIslePiece);

				// Generate the islands for this height with the parameters we've calculated.
				var spawnedIslands = IslandsPopulator.GenerateIslands(map, iterations, islandGenerationStrategy.randomIsleHeights, minWidth, maxWidth);
				(islandLayers[i], islandCliffs[i]) = AddTerrainToBiome(spawnedIslands, startSortingOrder);	// Add the spawned islands to the biome map and cache the heightmaps.

				// And create grass on the isle. No need to do any processing here, since we want the grass to spawn on the island,
				// .. so we can use the island's heightmap RT as input, since it's already processed and smoothed nicely before added to biomes.
				var grassLayers = terrainStrategy[(i + 2) % terrainStrategy.Count].grassLayers;             // Use the 'n + 2'th terrain's grass population strategy,
				var grassLayer = grassLayers[Random.Range(0, grassLayers.Count)];                           // .. and only spawn a single, random grass layer from it,
				var newGrassLayer = CSUtils.TerrainGenUtils.CreateGrass(islandLayers[i], grassLayer.coverage / 100f); // .. for simplicity's sake.
				AddToBiome(newGrassLayer, GetBiomeType(grassLayer.grassAtlas), startSortingOrder + 2);		// Finally, add the spawned grass layer to the biome map.
			}
		}

		// Now that we have the biomes set up, we can spawn the objects, and pass them to the renderer, if specified in the inspector.
		if (spawnObjects) { WorldRenderer.instance.RenderObjects(ObjectPopulator.SpawnObjects(spawnableObjects, size, biomeMap, biomeOrderMap), size); }

		// Once everything is ready, translate the terrain heightmaps to tiles, and render them. This includes base terrain, islands, and grass.
		for (int i = 0; i < terrainStrategy.Count; i++) {   // First for the terrain: Go through each height step and render its main body, cliffs, and islands.
			RenderIsland(terrainLayers[i], terrainCliffs[i], terrainStrategy[i].terrainAtlas);
			if (spawnIslands) { RenderIsland(islandLayers[i], islandCliffs[i], terrainStrategy[(i + 2) % terrainStrategy.Count].terrainAtlas); }
		}
		foreach (var (biome, biomeRTs) in biomeMap) {		// Then for the grass: We can render each biome part separately since they're prepared separately.
			if (biome == BiomeType.None || biome == BiomeType.Dirt) { continue; } // We've already spawned the terrain and isles layers, so skip them now.
			foreach (var biomeRT in biomeRTs) { RenderBiome(biomeRT, biome); }	  // So just gotta go through all grass layers and pass their heightmaps to the renderer.
		}

		sw.Stop();
		Debug.Log($"Elapsed: {sw.ElapsedMilliseconds}ms.");
		CSUtils.ReleaseAll(biomeOrderMap.Keys.Concat(terrainCliffs).Concat(terrainLayers).Concat(tempRTs).Distinct().ToArray()); // Finally, release all RTs to avoid memory leaks.


		// A terrain or an island consists of its main body and its bottom cliffs, so we spawn them separately. First, populate the tilemap for the terrain's main body.
		// Once the main body tiles are created, we can pass them to the CliffsPopulator to populate the cliff tiles. Then we can pass them to the renderer.
		void RenderIsland(RenderTexture heightMapRT, RenderTexture bottomCliffRT, Texture2D atlas) {
			var tileTypes  = TilemapPopulator.RetrieveTilesForHeightmap(heightMapRT, middleTileStrategy);   // Get the tilemap containing the tiles for the main body.
			var cliffTypes = CliffsPopulator.RetrieveTilesForCliffs(bottomCliffRT, tileTypes);              // Then, do the exact same for the cliff tiles.

			var tilemapRT  = CSUtils.TextureUtils.SetPixels(tileTypes.Flatten(cropOffset),  CSUtils.CreateRT(2 * size.x, 2 * size.y)); // Set the tiles as pixels.
			var cliffmapRT = CSUtils.TextureUtils.SetPixels(cliffTypes.Flatten(cropOffset), CSUtils.CreateRT(2 * size.x, 2 * size.y)); // .. same for cliff tiles.

			WorldRenderer.instance.RenderTilemap(tilemapRT,  atlas,      biomeOrderMap[heightMapRT]);       // And finally pass them for the renderer to do its magic.
			WorldRenderer.instance.RenderTilemap(cliffmapRT, cliffAtlas, biomeOrderMap[bottomCliffRT]);     // And voila, our island is fully rendered, split between 2 tilemaps!
		}
		void RenderBiome(RenderTexture heightMapRT, BiomeType biomeType) {
			var grassTiles = TilemapPopulator.RetrieveTilesForHeightmap(heightMapRT, middleTileStrategy).Flatten(cropOffset); // Get their tilemaps and flatten if needed.
			var tilemapRT = CSUtils.TextureUtils.SetPixels(grassTiles, CSUtils.CreateRT(2 * size.x, 2 * size.y));	// Set the tiles as pixels to a new RenderTexture.
			WorldRenderer.instance.RenderTilemap(tilemapRT, GetBiomeAtlas(biomeType), biomeOrderMap[heightMapRT]);	// .. and finally pass them to the renderer.
		}
		RenderTexture GenerateTerrain(RenderTexture availableSpace, float baselineCoverage, float heightPercentage) {
			var map = LeaveSomeSpaceOnBottomEdges(availableSpace); // Make sure the new terrain's borders don't overlap with borders of other terrains.
			// Get the min and max height of the new terrain's bottom cliffs. The new terrain we'll generate will be contained within these limits.
			var minHeight = (1 - baselineCoverage) * size.y;                                 // Starting at the coverage percentage (translated to tile count),
			var maxHeight = Mathf.Min(size.y * 0.9f, minHeight + heightPercentage * size.y); // .. up to a max of either 90% of the map or the allowed height.
			var terrainHeightStarts = new int[size.x]; // This will contain the heights of each row of the heightmap. This is where bottom cliffs will spawn.
			terrainHeightStarts[0] = Mathf.RoundToInt(Mathf.Lerp(minHeight, maxHeight, Random.Range(0.3f, 0.7f))); // The right-most tile - between [min, max].
			for (int i = 1; i < size.x; i++) { // The algorithm goes right pixel-by-pixel, and adds an offset [-2,2] from the height of the previous tile.
				var offsetFromLastPos = (int) isleContinuationStrategy.GetRandomObject();    // Determine whether we'll go up, down, or stay on the same height,
				terrainHeightStarts[i] = GetClampedPosition(terrainHeightStarts[i - 1], offsetFromLastPos); // .. while always staying within [min, max] bounds,
				int GetClampedPosition(int lastPos, int offset) => Mathf.RoundToInt(Mathf.Clamp(lastPos + offset, minHeight, maxHeight)); // .. by clamping the new position.
			}
			return CSUtils.TerrainGenUtils.CreateHeightBasedTerrain(map, terrainHeightStarts); // Finally, pass them to GPU to retrieve the heightmap RT.
		}
		RenderTexture LeaveSomeSpaceOnBottomEdges(RenderTexture availableSpace) {
			var map = GetTempCopy(availableSpace);  // Create a copy of the original texture. Space we can work with is marked with 1s.
			for (int i = 0; i < 2; i++) {           // Find edges of the original map and leave some space on the bottom edges.
				var edges = CSUtils.EdgeUtils.GetOutline(map);  // ..this is so we won't spawn cliffs on top of other lower height cliffs.
				CSUtils.EnumUtils.ContainsFlag(edges, (float) IsleEdgeFlag.BOTTOM); // Get the bottom edges of the available space.
				CSUtils.MaskUtils.IntersectRemove(edges, map);  // Make those edges unavailable for spawning by masking them out from the map.
				edges.Release();                    // This will ensure that our new terrain won't overlap with the bottom-most of the existing terrain level.
			}
			return map;
		}
		(RenderTexture heightMapRT, RenderTexture bottomEdgesRT) AddTerrainToBiome(RenderTexture map, int sortingOrder) {
			var bottomEdgesRT = CSUtils.EnumUtils.ContainsFlag(CSUtils.EdgeUtils.GetOutline(map), (float) IsleEdgeFlag.BOTTOM);
			var heightMapRT = CSUtils.MaskUtils.IntersectRemove(bottomEdgesRT, map); // Separate main body and bottom cliffs.
			AddToBiome(heightMapRT, BiomeType.Dirt, sortingOrder);                   // Add them as biomes. Main body is of dirt biome,
			AddToBiome(bottomEdgesRT, BiomeType.None, sortingOrder + 1);             // .. and the cliffs are non-biome tiles that we shouldn't spawn on.
			return (heightMapRT, bottomEdgesRT); // Finally, return a tuple of the heightmap and the bottom edges in case any system wants to cache them.
		}
		void AddToBiome(RenderTexture heightmap, BiomeType biomeType, int sortingOrder) {
			if (!biomeMap.TryGetValue(biomeType, out var biomeList)) { biomeMap.Add(biomeType, biomeList = new()); }
			biomeList.Add(heightmap);					// Add the heightmap to the list of the biome type grabbed above.
			biomeOrderMap.Add(heightmap, sortingOrder);	// .. and also to the orderDict, with the correct sorting order.
		}
		RenderTexture GetTempCopy(RenderTexture rtToCopy) { var tempRT = rtToCopy.Copy(); tempRTs.Add(tempRT); return tempRT; }	// Returns a copy that'll be disposed later.
		BiomeType GetBiomeType(Texture2D texture)    => availableBiomes.Find(x => x.biomeTexture == texture).biomeType;         // Gets the biome type of a texture atlas.
		Texture2D GetBiomeAtlas(BiomeType biomeType) => availableBiomes.Find(x => x.biomeType == biomeType).biomeTexture;       // Gets the atlas for the specified biome.
	}
}
