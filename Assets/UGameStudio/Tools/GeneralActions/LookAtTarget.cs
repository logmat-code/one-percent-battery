namespace UGSSpace {
public class LookAtTarget : UGS_LookAtTarget
{
#if UNITY_EDITOR
    private void Reset()
    {
        if (enemyToGetStates == null && GetComponent<UGS_Enemy>() != null)
            enemyToGetStates = GetComponent<UGS_Enemy>();
    }
#endif
}
}
