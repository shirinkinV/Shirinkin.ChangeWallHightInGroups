namespace Shirinkin.ChangeWallHightInGroups.Contracts;
public class DetailLineDTO
{
    public int ID { get; set; }
    public Point2D Start { get; set; }
    public Point2D End { get; set; }
    public Point2D Center => new Point2D((Start.X + End.X) * 0.5, (Start.Y + End.Y) * 0.5);

    public double Length => Math.Sqrt((Start.X - End.X) * (Start.X - End.X) + (Start.Y - End.Y) * (Start.Y - End.Y));
}
