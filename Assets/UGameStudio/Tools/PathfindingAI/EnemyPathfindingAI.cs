namespace UGSSpace
{
    [UnityEngine.RequireComponent(typeof(Enemy))]
    public class EnemyPathfindingAI : UGS_EnemyPathfindingAI
    {
#if UNITY_EDITOR
        private void Reset()
        {
            grid = FindAnyObjectByType<PathfindingGrid>();

            targetDetector = gameObject.AddComponent<TargetDetector>();

            if (grid != null && grid._gameAxis == PathfindingGrid.GameAxis._3D)
            {
                directionUsedToRotate = Direction.PositiveBlueAxis;
                targetDetector.viewType = TargetDetector.ViewType._3D;
                targetDetector.viewDirection = TargetDetector.ViewDirection.Forward;
            }
        }
#endif
    }
}
