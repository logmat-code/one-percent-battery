namespace UGSSpace {
public class CharacterSelectButton : UGS_CharacterSelectButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CharacterSelectButton_Icon"; }
    }
#endif
}
}
