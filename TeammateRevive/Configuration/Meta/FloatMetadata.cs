namespace TeammateRevive.Configuration;

public class FloatMetadata : EntryMetadata
{
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
    public float Step { get; set; } = 1;

    public FloatMetadata(float minValue, float maxValue)
    {
        this.MinValue = minValue;
        this.MaxValue = maxValue;
    }

    public FloatMetadata(float minValue, float maxValue, float step)
    {
        this.MinValue = minValue;
        this.MaxValue = maxValue;
        this.Step = step;
    }
}