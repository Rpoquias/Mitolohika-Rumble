using UnityEngine;

namespace PixelPlay.OffScreenIndicator
{
    public class OffScreenIndicatorCore
    {
        /// <summary>
        /// Converts a world position into screen coordinates.
        /// </summary>
        public static Vector3 GetScreenPosition(
            Camera mainCamera,
            Vector3 targetPosition)
        {
            return mainCamera.WorldToScreenPoint(targetPosition);
        }

        /// <summary>
        /// Determines whether the target is inside the camera's viewport.
        /// </summary>
    public static bool IsTargetVisible(Vector3 screenPosition)
{
    return screenPosition.z > 0f
        && screenPosition.x >= 0f
        && screenPosition.x <= Screen.width
        && screenPosition.y >= 0f
        && screenPosition.y <= Screen.height;
}
        /// <summary>
        /// Positions an arrow along the screen edges and calculates
        /// its rotation toward the target.
        /// </summary>
        public static void GetArrowIndicatorPositionAndAngle(
            ref Vector3 screenPosition,
            ref float angle,
            Vector3 screenCentre,
            Vector3 screenBounds)
        {
            // Convert the target position to coordinates relative to screen center.
            Vector2 direction = new Vector2(
                screenPosition.x - screenCentre.x,
                screenPosition.y - screenCentre.y
            );

            // Targets behind the camera have inverted screen directions.
            if (screenPosition.z < 0f)
            {
                direction = -direction;
            }

            // Avoid invalid calculations if the target is exactly at screen center.
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();

            // Calculate the arrow's angle relative to the horizontal axis.
            angle = Mathf.Atan2(direction.y, direction.x);

            // Calculate where the direction intersects the screen boundary.
            float halfWidth = screenBounds.x;
            float halfHeight = screenBounds.y;

            float scaleX = halfWidth / Mathf.Max(Mathf.Abs(direction.x), 0.0001f);
            float scaleY = halfHeight / Mathf.Max(Mathf.Abs(direction.y), 0.0001f);

            float scale = Mathf.Min(scaleX, scaleY);

            Vector2 edgePosition = direction * scale;

            // Convert back to screen coordinates.
            screenPosition = new Vector3(
                screenCentre.x + edgePosition.x,
                screenCentre.y + edgePosition.y,
                0f
            );
        }
    }
}