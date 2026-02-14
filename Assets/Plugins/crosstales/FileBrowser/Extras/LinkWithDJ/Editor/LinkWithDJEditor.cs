#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Crosstales.FB.RTFB
{
   /// <summary>Custom editor for the 'LinkWithDJ'-class.</summary>
   [CustomEditor(typeof(Crosstales.FB.Tool.LinkWithDJ))]
   public class LinkWithDJEditor : Editor
   {
      #region Variables

      private Crosstales.FB.Tool.LinkWithDJ script;

      #endregion


      #region Properties

      //public static bool isPrefabInScene => GameObject.Find("Link With DJ") != null;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.FB.Tool.LinkWithDJ)target;
      }

      public override void OnInspectorGUI()
      {
#if !CT_DJ
         EditorGUILayout.HelpBox($"'DJ PRO' not found! Please install it from the Unity AssetStore.", MessageType.Error);
#endif
         if (GUILayout.Button(new GUIContent(" Learn more", Crosstales.FB.EditorUtil.EditorHelper.Icon_Manual, "Learn more about DJ PRO.")))
            Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.FB.Util.Constants.ASSET_DJ);

         DrawDefaultInspector();

         if (script.isActiveAndEnabled)
         {
            //add stuff if needed
         }
         else
         {
            Crosstales.FB.EditorUtil.EditorHelper.SeparatorUI();
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }
      }

      #endregion
   }
}
#endif
// © 2023 crosstales LLC (https://www.crosstales.com)