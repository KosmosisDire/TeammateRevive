using System;
using UnityEngine;

public struct SkullData
{
    public Color Color;
    public float Intensity;
    public float Amount;

    public float FractionPerSecond;

    public bool Equals(SkullData other)
    {
        return FloatEqual(this.FractionPerSecond, other.FractionPerSecond)
               && FloatEqual(this.Intensity, other.Intensity)
               && FloatEqual(this.Amount, other.Amount)
               && Color.Equals(other.Color);
    }

    private static bool FloatEqual(float a, float b) => Math.Abs(a - b) < .01;
}