namespace UGSSpace {
    using UnityEngine;
    using System;
    using System.Reflection;
    using UGameStudioGDK;
    public class GameOverManager : UGS_GameOverManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "GameOverManager_Icon"; }
        }
#endif

#if UNITY_EDITOR
        public static Type GetTypeByName(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name == name)
                        return type;
                }
            }

            return null;
        }

        public override void Awake()
        {
            for (int i = 0; i < componentsOfPlayerToDisable.Count; i++)
            {
                if (GetTypeByName(componentsOfPlayerToDisable[i]) == null)
                {
                    if(UnityEditor.EditorUtility.DisplayDialog(
                         PlayerPrefs.GetString("ugs_lang") == "English" ? "Error in the GameOverManager component." : "Error en el componente GameOverManager.",
                         PlayerPrefs.GetString("ugs_lang") == "English" ? "The component with name \"" + componentsOfPlayerToDisable[i] + "\" does not exist. Please make sure to correctly write the name in the GameOverManager component.\n\nThe game cannot be played until the component names are correctly written in the GameOverManager.\nCheck every word!" : "El componente con el nombre \"" + componentsOfPlayerToDisable [i] + "\" no existe. Por favor, asegúrate de escribir correctamente el nombre en el componente de GameOverManager.\n\nEl juego no puede ser jugado hasta que se escriban correctamente los nombres de los componentes en el GameOverManager.\n¡Revisa cada palabra!",
                        PlayerPrefs.GetString("ugs_lang") == "English" ? "I'm going to check it out! " : "¡Voy a revisarlo!"))
                    {
                        UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
                        UnityEditor.Selection.activeGameObject = gameObject;
                    }
                }
            }
            base.Awake();
        }
#endif
    }
}
