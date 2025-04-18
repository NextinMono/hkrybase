namespace HekonrayBase
{
    public struct Vector2Int
    {
        public int X,Y;

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(Vector2Int obj1, Vector2Int obj2)
        {
            return obj1.X == obj2.X && obj1.Y == obj2.Y;
        }
        public static bool operator !=(Vector2Int obj1, Vector2Int obj2) => !(obj1 == obj2);
    }
}
