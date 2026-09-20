namespace UGSSpace {
using UnityEngine;
public class PlayerActivator : UGS_PlayerActivator
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PlayerActivator_Icon"; }
    }
#endif

#if UNITY_EDITOR
    public override void VerifyComponentsToDisable()
    {
        for (int i = 0; i < componentsToDisable.Count; i++)
        {
            if (GameOverManager.GetTypeByName(componentsToDisable[i]) == null)
            {
                Debug.LogError("<color=yellow>UGame Studio: </color>The component with name \"" + componentsToDisable[i] + "\" does not exist. Please be sure to write correctly the name in the PlayerActivator component.", gameObject);
                Debug.Log("<color=yellow>UGame Studio: </color>The Game cannot be played until you correctly type the component names in the PlayerActivator component.", gameObject);
                UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
                return;
            }
        }
    }
#endif
}
}
