using System.Collections.Generic;

internal struct Maze
{
    public string str;
    public int[] colors;

    public Maze(string str, int[] colors)
    {
        this.str = str;
        this.colors = colors;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Maze))
        {
            return false;
        }

        Maze other = (Maze)obj;
        return str == other.str &&
               EqualityComparer<int[]>.Default.Equals(colors, other.colors);
    }

    public override int GetHashCode()
    {
        int hashCode = -1468143167;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(str);
        hashCode = hashCode * -1521134295 + EqualityComparer<int[]>.Default.GetHashCode(colors);
        return hashCode;
    }

    public void Deconstruct(out string str, out int[] colors)
    {
        str = this.str;
        colors = this.colors;
    }
}