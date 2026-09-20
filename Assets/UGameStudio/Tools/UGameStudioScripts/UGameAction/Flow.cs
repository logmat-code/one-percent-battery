using UnityEngine;

namespace UGSSpace
{
    namespace UGameActions
    {
        [AddComponentMenu(" Flow:")]
        public class Flow : UGS_Flow
#if UNITY_EDITOR
        , IHierarchyIcon
#endif
        {
#if UNITY_EDITOR
            public string EditorIconPath
            {
                get
                {
                    if (componentIcon != null)
                    {
                        return componentIcon.name;
                    }
                    else
                    {
                        return "UGameActionIcon";
                    }
                }
            }
#endif
        }
    }
}
