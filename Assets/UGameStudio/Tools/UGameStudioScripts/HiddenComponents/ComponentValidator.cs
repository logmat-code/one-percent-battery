namespace UGSSpace {
using UnityEngine;
public class ComponentValidator : UGS_ComponentValidator
{
#if UNITY_EDITOR
    string component1;
    string component2;

    GameObject obj;

    bool destroy2If1NotExist = false;
    bool destroy1If2NotExist = false;
    bool removeIf1IsNull = false;
    bool removeIf2IsNull = false;

    private void OnDrawGizmosSelected()
    {
        if (obj != null)
        {
            if (this.hideFlags != HideFlags.HideInInspector)
                this.hideFlags = HideFlags.HideInInspector;

            if (Application.isPlaying)
            {
                if (destroy2If1NotExist && obj.GetComponent(component1) == null)
                    Destroy(obj.GetComponent(component2));

                if (destroy1If2NotExist && obj.GetComponent(component2) == null)
                    Destroy(obj.GetComponent(component1));

                if (removeIf1IsNull && obj.GetComponent(component1) == null)
                    Destroy(this);

                if (removeIf2IsNull && obj.GetComponent(component2) == null)
                    Destroy(this);
            }
            else if (Application.isEditor)
            {
                if (destroy2If1NotExist && obj.GetComponent(component1) == null)
                    DestroyImmediate(obj.GetComponent(component2));

                if (destroy1If2NotExist && obj.GetComponent(component2) == null)
                    DestroyImmediate(obj.GetComponent(component1));

                if (removeIf1IsNull && obj.GetComponent(component1) == null)
                    DestroyImmediate(this);

                if (removeIf2IsNull && obj.GetComponent(component2) == null)
                    DestroyImmediate(this);
            }
        }
    }

    public override void Init(GameObject _obj, string _component1, string _component2, bool _destroy2If1NotExist, bool _destroy1If2NotExist, bool _removeIf1IsNull, bool _removeIf2IsNull)
    {
        obj = _obj;
        component1 = _component1;
        component2 = _component2;
        destroy2If1NotExist = _destroy2If1NotExist;
        destroy1If2NotExist = _destroy1If2NotExist;
        removeIf1IsNull = _removeIf1IsNull;
        removeIf2IsNull = _removeIf2IsNull;
        OnDrawGizmosSelected();
    }
#endif
}
}
