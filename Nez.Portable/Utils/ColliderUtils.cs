using Microsoft.Xna.Framework;
using Nez;
using Nez.PhysicsShapes;
using RectangleF = Nez.RectangleF;

public class ColliderUtils
{
	public static void SetColliderRectangle(Collider collider, RectangleF rectangle)
	{
		collider.LocalOffset = new Vector2(rectangle.X + rectangle.Width / 2f, rectangle.Y + rectangle.Height / 2f);
		collider.Shape = new Box(rectangle.Width, rectangle.Height);
	}

	/// <summary>
	/// Check if the RectangleF has different values than the Collider's RectF (not bounds necessarily)
	/// </summary>
	/// <param name="collider"></param>
	/// <param name="rectangle"></param>
	/// <returns></returns>
	public static bool HasColliderRectangleChanged(Collider collider, RectangleF rectangle)
	{
		if ((int)collider.Bounds.Width != (int)rectangle.Width || (int)collider.Bounds.Height != (int)rectangle.Height)
			return true;

		var rectOffsetX = (int)rectangle.X + (int)(rectangle.Width / 2f);
		var rectOffsetY = (int)rectangle.Y + (int)(rectangle.Height / 2f);

		if ((int)collider.LocalOffset.X != rectOffsetX ||
		    (int)collider.LocalOffset.Y != rectOffsetY)
			return true;

		return false;
	}

	/// <summary>
	/// Gets the local pos the Rectangle, depending on its origin point (e.g. Player.Position)
	/// </summary>
	public static RectangleF GetRectangleLocalPos(Vector2 originPoint, RectangleF rectangleF)
	{
		return new RectangleF(originPoint.X + rectangleF.X, originPoint.Y + rectangleF.Y, rectangleF.Width, rectangleF.Height);
	}

	public static Vector2 GetColliderPos(Collider collider)
	{
		return new Vector2(collider.AbsolutePosition.X + collider.Bounds.Width / 2f,
			collider.AbsolutePosition.Y + collider.Bounds.Height / 2f);
	}
}
