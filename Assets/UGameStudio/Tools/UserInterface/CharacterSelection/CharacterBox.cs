namespace UGSSpace {
public class CharacterBox : UGS_CharacterBox
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CharacterBox_Icon"; }
    }
#endif
}
}
