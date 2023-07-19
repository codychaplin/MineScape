public abstract class Biome
{
    public string Name { get; set; }
    public float MinTerrainHeight { get; set; }
    public float MaxTerrainHeight { get; set; }
    public float Variation { get; set; }
    public float Temperature { get; set; }
    public Block SurfaceBlock { get; set; } = Blocks.GRASS;
    public Block FillBlock { get; set; } = Blocks.DIRT;

    public Biome(string name, float minTerrainHeight, float maxTerrainHeight, float variation, float temperature, Block surfaceBlock, Block fillBlock)
    {
        Name = name;
        MinTerrainHeight = minTerrainHeight;
        MaxTerrainHeight = maxTerrainHeight;
        Variation = variation;
        Temperature = temperature;
        SurfaceBlock = surfaceBlock;
        FillBlock = fillBlock;
    }
}