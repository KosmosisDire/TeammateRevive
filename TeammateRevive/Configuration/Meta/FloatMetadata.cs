namespace TeammateRevive.Configuration;

public class FloatMetadata : EntryMetadata
{
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
    public float Step { get; set; } = 1;

    public FloatMetadata(float minValue, float maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public FloatMetadata(float minValue, float maxValue, float step)
    {
        MinValue = minValue;
        MaxValue = maxValue;
        Step = step;
    }
}