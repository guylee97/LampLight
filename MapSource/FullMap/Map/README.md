# Tiled map sources

- `Level1/Map.tmx`: round 1
- `Level2/Map.tmx`: round 2
- `Level3/Map.tmx`: round 3
- `Shared/`: images shared by the three maps
- `References/`: reference screenshots

Open each round through its `Map.tmx` file. Keep the relative folder structure when
moving or sharing these files.

These TMX files are authoring sources. Unity currently loads the generated files at
`Assets/Resources/Data/map_l1.json` through `map_l3.json`, so editing a TMX does not
update the game until those JSON files are rebuilt.
