namespace UGSSpace {
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GenerateObjectsDynamically : UGS_GenerateObjectsDynamically
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "LevelGenerator_Icon"; }
    }
#endif

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow / 1.2F;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }

    public override void Start()
    {
        //--------------------------------------------------------------------------
        int sumOf_MaxObjectsAtPositions = 0;
        int sumOf_MaxInstances = 0;
        bool stopEditor = false;

        for (int j = 0; j < possiblePositions.Count; j++)
        {
            sumOf_MaxObjectsAtPositions += possiblePositions[j].maxObjectsAtThisPosition;
        }

        for (int i = 0; i < difficultyLevels.Count; i++)
        {
            for (int j = 0; j < difficultyLevels[i].objectsToInstantiate.Count; j++)
            {
                sumOf_MaxInstances += difficultyLevels[i].objectsToInstantiate[j].maxInstances;
            }

            if (difficultyLevels[i].howManyInstantiate == GenerateObjectsDynamically.DifficultyLevel.HowManyInstantiate.RandomNumberOfObjects)
            {
                if (difficultyLevels[i].maxQuantityToInstantiate > sumOf_MaxInstances)
                {
                    Debug.LogError("<color=yellow>UGame Studio: </color>ERROR!: The number of objects to instantiate is greater than the sum of the 'Max Instances' of this difficulty.", gameObject);
                    stopEditor = true;
                }

                if (difficultyLevels[i].maxQuantityToInstantiate > sumOf_MaxObjectsAtPositions)
                {
                    Debug.LogError("<color=yellow>UGame Studio: </color>ERROR!: The number of objects to instantiate is greater than the sum of the maximum objects at the different positions.", gameObject);
                    stopEditor = true;
                }
            }
            else
            {
                if (difficultyLevels[i].minQuantityToInstantiate > sumOf_MaxInstances)
                {
                    Debug.LogError("<color=yellow>UGame Studio: </color>ERROR!: The number of objects to instantiate is greater than the sum of the 'Max Instances' of this difficulty.", gameObject);
                    stopEditor = true;
                }

                if (difficultyLevels[i].minQuantityToInstantiate > sumOf_MaxObjectsAtPositions)
                {
                    Debug.LogError("<color=yellow>UGame Studio: </color>ERROR!: The number of objects to instantiate is greater than the sum of the maximum objects at the different positions.", gameObject);
                    stopEditor = true;
                }
            }
        }

        for (int i = 1; i < possiblePositions.Count; i++)
        {
            int max_sumOfThePossibleObjectsAtThisPos = 0;
            for (int j = 0; j < difficultyLevels.Count; j++)
            {
                int sumOfThePossibleObjectsAtThisPos = 0;
                for (int k = 0; k < difficultyLevels[j].objectsToInstantiate.Count; k++)
                {
                    if (difficultyLevels[j].objectsToInstantiate[k].indexOfPossiblePosition == i)
                    {
                        sumOfThePossibleObjectsAtThisPos += difficultyLevels[j].objectsToInstantiate[k].maxInstances;
                    }
                }

                if (sumOfThePossibleObjectsAtThisPos > max_sumOfThePossibleObjectsAtThisPos)
                    max_sumOfThePossibleObjectsAtThisPos = sumOfThePossibleObjectsAtThisPos;
            }

            if (max_sumOfThePossibleObjectsAtThisPos > possiblePositions[i].maxObjectsAtThisPosition)
            {
                Debug.LogError("<color=yellow>UGame Studio: </color>Error!, the max objects at the position " + i + " must be at least of " + max_sumOfThePossibleObjectsAtThisPos, gameObject);
                stopEditor = true;
            }
        }

        if (stopEditor)
        {
            EditorApplication.ExecuteMenuItem("Edit/Play");
            return;
        }

        //--------------------------------------------------------------------------
        base.Start();
    }
#endif
}
}
