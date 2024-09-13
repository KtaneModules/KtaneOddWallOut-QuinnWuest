using System;
using System.Linq;

internal struct Maze : IEquatable<Maze>
{
    public string String;
    public int[] Colors;

    public Maze(string str, int[] colors)
    {
        String = str;
        Colors = colors;
    }

    public override int GetHashCode()
    {
        int hashCode = -1468143167;
        hashCode = hashCode * -1521134295 + String.GetHashCode();
        hashCode = hashCode * -1521134295 + Colors.Length;
        for (var i = 0; i < Colors.Length; i++)
            hashCode = hashCode * -1521134295 + Colors[i];
        return hashCode;
    }

    public bool Equals(Maze other) => other.String == String && other.Colors.SequenceEqual(Colors);
    public override bool Equals(object obj) => obj is Maze && Equals((Maze) obj);
}