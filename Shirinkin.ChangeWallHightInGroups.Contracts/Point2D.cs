
namespace Shirinkin.ChangeWallHightInGroups.Contracts;
public class Point2D
{
    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }

    public Point2D GetNormalized()
    {
        var length = Math.Sqrt(X * X + Y * Y);
        return new Point2D(X / length, Y / length);
    }

    public static Point2D operator -(Point2D left, Point2D right) => new Point2D(left.X - right.X, left.Y - right.Y);
    public static Point2D operator +(Point2D left, Point2D right) => new Point2D(left.X + right.X, left.Y + right.Y);
}
