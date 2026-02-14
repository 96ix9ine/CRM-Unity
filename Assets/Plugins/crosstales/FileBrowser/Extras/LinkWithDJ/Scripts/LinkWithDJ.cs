using UnityEngine;

namespace Crosstales.FB.Tool
{
   /// <summary>
   /// Basic DJ PRO integration.
   /// NOTE: This wrapper needs "DJ PRO" https://assetstore.unity.com/packages/slug/41993?aid=1011lNGT
   /// </summary>
   [HelpURL("https://www.crosstales.com/media/data/assets/FileBrowser/api/class_crosstales_1_1_f_b_1_1_r_t_f_b_1_1_file_browser_r_t_f_b.html")]

   public class LinkWithDJ : MonoBehaviour
   {
      [Tooltip("Play the selected file instantly.")] public bool AutoPlay;
#if CT_DJ

      [Tooltip("Play the selected file instantly.")] public Crosstales.DJ.BasePlayer Player;

      private void Start()
      {
         FileBrowser.Instance.OnOpenFilesComplete += onOpenFilesComplete;
      }

      private void OnDestroy()
      {
         FileBrowser.Instance.OnOpenFilesComplete -= onOpenFilesComplete;
      }

      private void onOpenFilesComplete(bool selected, string singlefile, string[] files)
      {
         if (selected)
         {
            if (Player == null)
               Player = Crosstales.DJ.Turntable.Instance;

            Player.AudioData = FileBrowser.Instance.CurrentOpenSingleFileData;

            if (AutoPlay)
               Player.Play();
         }
      }
#endif
   }
}
// © 2023 crosstales LLC (https://www.crosstales.com)