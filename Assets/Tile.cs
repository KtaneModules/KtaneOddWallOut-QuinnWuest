internal struct Tile
{
    public int top;
    public int right;
    public int bottom;
    public int left;

    public Tile(int top, int right, int bottom, int left)
    {
        this.top = top;
        this.right = right;
        this.bottom = bottom;
        this.left = left;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Tile))
        {
            return false;
        }

        Tile other = (Tile)obj;
        return top == other.top &&
               right == other.right &&
               bottom == other.bottom &&
               left == other.left;
    }

    public override int GetHashCode()
    {
        int hashCode = -402104261;
        hashCode = hashCode * -1521134295 + top.GetHashCode();
        hashCode = hashCode * -1521134295 + right.GetHashCode();
        hashCode = hashCode * -1521134295 + bottom.GetHashCode();
        hashCode = hashCode * -1521134295 + left.GetHashCode();
        return hashCode;
    }

    public void Deconstruct(out int top, out int right, out int bottom, out int left)
    {
        top = this.top;
        right = this.right;
        bottom = this.bottom;
        left = this.left;
    }
}
