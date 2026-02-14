using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Various helper functions for the file system.</summary>
#if UNITY_EDITOR
   [UnityEditor.InitializeOnLoad]
#endif
   public abstract class FileHelper
   {
      #region Variables

      private static string applicationDataPath = Application.dataPath;
      private static string applicationTempPath = Application.temporaryCachePath;
      private static string applicationPersistentPath = Application.persistentDataPath;
      private static readonly System.Collections.Generic.List<string> fileList = new System.Collections.Generic.List<string>();
      private static readonly System.Collections.Generic.List<string> dirList = new System.Collections.Generic.List<string>();

      private static readonly string RTFBisMissing = $"'Runtime File Browser' not found! Please install it from the Unity AssetStore: {Crosstales.Common.Util.BaseConstants.ASSET_3P_RTFB}";

      #endregion

      #region Properties

      /// <summary>Returns the path to the the "Streaming Assets".</summary>
      /// <returns>The path to the the "Streaming Assets".</returns>
      public static string StreamingAssetsPath
      {
         get
         {
            if (Crosstales.Common.Util.BaseHelper.isAndroidPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
               return $"jar:file://{ApplicationDataPath}!/assets/";

            if (Crosstales.Common.Util.BaseHelper.isIOSBasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
               return $"{ApplicationDataPath}/Raw/";

            return $"{ApplicationDataPath}/StreamingAssets/";
         }
      }

      /// <summary>Returns the Unity application data path.</summary>
      /// <returns>Unity application data path</returns>
      public static string ApplicationDataPath => applicationDataPath;

      /// <summary>Returns the Unity application temporary path.</summary>
      /// <returns>Unity application temporary path</returns>
      public static string ApplicationTempPath => applicationTempPath;

      /// <summary>Returns the Unity application persistent path.</summary>
      /// <returns>Unity application persistent path</returns>
      public static string ApplicationPersistentPath => applicationPersistentPath;

      /// <summary>Returns a temporary file.</summary>
      /// <returns>Temporary file</returns>
      public static string TempFile => System.IO.Path.GetTempFileName();

      /// <summary>Returns the temporary directory path from the device.</summary>
      /// <returns>Temporary directory path of the device</returns>
      public static string TempPath => System.IO.Path.GetTempPath();

      #endregion


      #region Static block

      static FileHelper()
      {
         //Debug.Log("Static block");
         initialize();
      }

      [RuntimeInitializeOnLoadMethod]
      private static void initialize()
      {
         //Debug.Log("initialize");
         applicationDataPath = Application.dataPath;
         applicationTempPath = Application.temporaryCachePath;
         applicationPersistentPath = Application.persistentDataPath;
/*
         if (!isEditorMode)
         {
            GameObject go = new GameObject("_HelperCT");
            go.AddComponent<HelperCT>();
            GameObject.DontDestroyOnLoad(go);
         }
*/
      }

      #endregion


      #region Public methods

      /// <summary>Validates a given path and add missing slash.</summary>
      /// <param name="path">Path to validate</param>
      /// <param name="addEndDelimiter">Add delimiter at the end of the path (optional, default: true)</param>
      /// <param name="preserveFile">Preserves a given file in the path (optional, default: true)</param>
      /// <returns>Valid path</returns>
      public static string ValidatePath(string path, bool addEndDelimiter = true, bool preserveFile = true)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (Crosstales.Common.Util.NetworkHelper.isValidURL(path))
               return path;

            string pathTemp = !preserveFile && ExistsFile(path.Trim()) ? GetDirectoryName(path.Trim()) : path.Trim();

            string result;

            if (!path.StartsWith("/") && (Crosstales.Common.Util.BaseHelper.isWindowsBasedPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor) && !Crosstales.Common.Util.BaseHelper.isMacOSEditor && !Crosstales.Common.Util.BaseHelper.isLinuxEditor)
            {
               result = pathTemp.Replace('/', '\\');

               if (addEndDelimiter)
               {
                  if (!result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS))
                  {
                     result += Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS;
                  }
               }
            }
            else
            {
               result = pathTemp.Replace('\\', '/');

               if (addEndDelimiter)
               {
                  if (!result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX))
                  {
                     result += Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX;
                  }
               }
            }

            return string.Join(string.Empty, result.Split(System.IO.Path.GetInvalidPathChars()));
            //return result;
         }

         return path;
      }

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidateFile(string path)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (Crosstales.Common.Util.NetworkHelper.isValidURL(path))
               return path;

            string result = ValidatePath(path);

            if (result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS) ||
                result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX))
            {
               result = result.Substring(0, result.Length - 1);
            }

            string fileName;
            if (!path.StartsWith("/") && (Crosstales.Common.Util.BaseHelper.isWindowsBasedPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor) && !Crosstales.Common.Util.BaseHelper.isMacOSEditor && !Crosstales.Common.Util.BaseHelper.isLinuxEditor)
            {
               fileName = result.Substring(result.CTLastIndexOf(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS) + 1);
            }
            else
            {
               fileName = result.Substring(result.CTLastIndexOf(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX) + 1);
            }

            string newName = string.Join(string.Empty, fileName.Split(System.IO.Path.GetInvalidFileNameChars())); //.Replace(BaseConstants.PATH_DELIMITER_WINDOWS, string.Empty).Replace(BaseConstants.PATH_DELIMITER_UNIX, string.Empty);

            return result.Substring(0, result.Length - fileName.Length) + newName;
         }

         return path;
      }

      /// <summary>
      /// Checks a given path for invalid characters
      /// </summary>
      /// <param name="path">Path to check for invalid characters</param>
      /// <returns>Returns true if the path contains invalid chars, otherwise it's false.</returns>
      public static bool HasPathInvalidChars(string path)
      {
         return !string.IsNullOrEmpty(path) && path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0;
      }

      /// <summary>
      /// Checks a given file for invalid characters
      /// </summary>
      /// <param name="file">File to check for invalid characters</param>
      /// <returns>Returns true if the file contains invalid chars, otherwise it's false.</returns>
      public static bool HasFileInvalidChars(string file)
      {
         return !string.IsNullOrEmpty(file) && file.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0;
      }

      /// <summary>
      /// Find files inside a path.
      /// </summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <param name="filenames">Filenames for the file search, e.g. "Image.png" (optional)</param>
      /// <returns>Returns array of the found files inside the path (alphabetically ordered). Zero length array when an error occured.</returns>
      public static string[] GetFilesForName(string path, bool isRecursive = false, params string[] filenames)
      {
         if (Crosstales.Common.Util.BaseHelper.isWebPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetFilesForName' is not supported for the current platform!");
         }
         else
         {
            if (!string.IsNullOrEmpty(path))
            {
               if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
               {
#if CT_FB
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
                  Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
                  fbWsa.isBusy = true;
                  UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetFilesForName(path, isRecursive, filenames); }, false);

                  do
                  {
                    //wait
                  } while (fbWsa.isBusy);

                  return fbWsa.Selection.ToArray();
#endif
#else
                  Debug.LogWarning($"'GetFilesForName' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
#endif
               }
               else
               {
                  try
                  {
#if UNITY_ANDROID
#if CT_RTFB
                     fileList.Clear();
                     getFilesRTFB(path, isRecursive, filenames);
                     return fileList.ToArray();
#else
                     Debug.LogWarning(RTFBisMissing);
#endif
#else
                     string _path = ValidatePath(path);

                     if (filenames == null || filenames.Length == 0 || filenames.Any(extension => extension.Equals("*") || extension.Equals("*.*")))
                     {
                        return System.IO.Directory.EnumerateFiles(_path, "*", isRecursive
                           ? System.IO.SearchOption.AllDirectories
                           : System.IO.SearchOption.TopDirectoryOnly).ToArray();
                     }

                     System.Collections.Generic.List<string> files = new System.Collections.Generic.List<string>();

                     foreach (string filename in filenames)
                     {
                        files.AddRange(System.IO.Directory.EnumerateFiles(_path, filename.StartsWith("*.") ? filename : $"*{filename}*", isRecursive
                           ? System.IO.SearchOption.AllDirectories
                           : System.IO.SearchOption.TopDirectoryOnly));
                     }

                     return files.OrderBy(q => q).ToArray();
#endif
                  }
                  catch (System.Exception ex)
                  {
                     Debug.LogWarning($"Could not scan the path for files: {ex}");
                  }
               }
            }
         }

         return System.Array.Empty<string>();
      }

      /// <summary>
      /// Find files inside a path.
      /// </summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <param name="extensions">Extensions for the file search, e.g. "png" (optional)</param>
      /// <returns>Returns array of the found files inside the path (alphabetically ordered). Zero length array when an error occured.</returns>
      public static string[] GetFiles(string path, bool isRecursive = false, params string[] extensions)
      {
         if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if CT_FB
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetFiles(path, isRecursive, extensions); }, false);

            do
            {
              //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning($"'GetFiles' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
            return System.Array.Empty<string>();
#endif
         }

         if (extensions?.Length > 0)
         {
            string[] wildcardExt = new string[extensions.Length];

            for (int ii = 0; ii < extensions.Length; ii++)
            {
               wildcardExt[ii] = $"*.{extensions[ii]}";
            }

            return GetFilesForName(path, isRecursive, wildcardExt);
         }

         return GetFilesForName(path, isRecursive, extensions);
      }

      /// <summary>
      /// Find directories inside.
      /// </summary>
      /// <param name="path">Path to find the directories</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <returns>Returns array of the found directories inside the path. Zero length array when an error occured.</returns>
      public static string[] GetDirectories(string path, bool isRecursive = false)
      {
         if (Crosstales.Common.Util.BaseHelper.isWebPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetDirectories' is not supported for the current platform!");
         }
         else if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if CT_FB
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetDirectories(path, isRecursive); }, false);

            do
            {
              //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning($"'GetDirectories' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
#endif
         }
         else
         {
            if (!string.IsNullOrEmpty(path))
            {
               try
               {
#if UNITY_ANDROID
#if CT_RTFB
                  dirList.Clear();
                  getDirectoriesRTFB(path, isRecursive);
                  return dirList.ToArray();
#else
                  Debug.LogWarning(RTFBisMissing);
#endif
#else
                  string _path = ValidatePath(path);
#if NET_4_6 || NET_STANDARD_2_0
                  return System.IO.Directory.EnumerateDirectories(_path, "*", isRecursive
                     ? System.IO.SearchOption.AllDirectories
                     : System.IO.SearchOption.TopDirectoryOnly).ToArray();
#else
                  return System.IO.Directory.GetDirectories(_path, "*",
                     isRecursive
                        ? System.IO.SearchOption.AllDirectories
                        : System.IO.SearchOption.TopDirectoryOnly);
#endif
#endif
               }
               catch (System.Exception ex)
               {
                  Debug.LogWarning($"Could not scan the path for directories: {ex}");
               }
            }
         }

         return System.Array.Empty<string>();
      }

      /// <summary>
      /// Find all logical drives.
      /// </summary>
      /// <returns>Returns array of the found drives. Zero length array when an error occured.</returns>
      public static string[] GetDrives()
      {
         if (Crosstales.Common.Util.BaseHelper.isWebPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetDrives' is not supported for the current platform!");
         }
         else if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if CT_FB
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetDrives(); }, false);

            do
            {
              //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning($"'GetDrives' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
#endif
         }
         else
         {
#if (!UNITY_WSA && !UNITY_XBOXONE) || UNITY_EDITOR
            try
            {
               return System.IO.Directory.GetLogicalDrives();
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning($"Could not scan the path for directories: {ex}");
            }
#endif
         }

         return System.Array.Empty<string>();
      }

      /// <summary>Copy or move a directory.</summary>
      /// <param name="sourceDir">Source directory path</param>
      /// <param name="destDir">Destination directory path</param>
      /// <param name="move">Move directory instead of copy (default: false, optional)</param>
      public static void CopyDirectory(string sourceDir, string destDir, bool move = false)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'CopyDirectory' is not supported for the current platform!");
         }
         else
         {
            if (!string.IsNullOrEmpty(destDir))
            {
               try
               {
#if UNITY_ANDROID
#if CT_RTFB
                  if (move)
                  {
                     SimpleFileBrowser.FileBrowserHelpers.MoveDirectory(sourceDir, destDir);
                  }
                  else
                  {
                     SimpleFileBrowser.FileBrowserHelpers.CopyDirectory(sourceDir, destDir);
                  }
#else
                  Debug.LogWarning(RTFBisMissing);
#endif
#else
                  if (!ExistsDirectory(sourceDir))
                  {
                     Debug.LogError($"Source directory does not exists: {sourceDir}");
                  }
                  else
                  {
                     //System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destPath));

                     if (ExistsDirectory(destDir))
                     {
                        if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                           Debug.LogWarning($"Overwrite destination directory: {destDir}");

                        DeleteDirectory(destDir);
                     }

                     if (move)
                     {
                        System.IO.Directory.Move(sourceDir, destDir);
                     }
                     else
                     {
                        copyAll(new System.IO.DirectoryInfo(sourceDir), new System.IO.DirectoryInfo(destDir));
                     }
                  }
#endif
               }
               catch (System.Exception ex)
               {
                  Debug.LogError($"Could not {(move ? "move" : "copy")} directory: {ex}");
                  throw;
               }
            }
         }
      }

      /// <summary>Copy or move a file.</summary>
      /// <param name="sourceFile">Source file path</param>
      /// <param name="destFile">Destination file path</param>
      /// <param name="move">Move file instead of copy (default: false, optional)</param>
      public static void CopyFile(string sourceFile, string destFile, bool move = false)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'CopyFile' is not supported for the current platform!");
         }
         else
         {
            if (!string.IsNullOrEmpty(destFile))
            {
               try
               {
#if UNITY_ANDROID
#if CT_RTFB
                  if (move)
                  {
                     SimpleFileBrowser.FileBrowserHelpers.MoveFile(sourceFile, destFile);
                  }
                  else
                  {
                     SimpleFileBrowser.FileBrowserHelpers.CopyFile(sourceFile, destFile);
                  }
#else
                  Debug.LogWarning(RTFBisMissing);
#endif
#else
                  if (!ExistsFile(sourceFile))
                  {
                     Debug.LogError($"Source file does not exists: {sourceFile}");
                  }
                  else
                  {
                     CreateDirectory(System.IO.Path.GetDirectoryName(destFile));

                     if (ExistsFile(destFile))
                     {
                        if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                           Debug.LogWarning($"Overwrite destination file: {destFile}");

                        DeleteFile(destFile);
                     }

                     if (move)
                     {
#if UNITY_STANDALONE || UNITY_EDITOR
                        System.IO.File.Move(sourceFile, destFile);
#else
                        System.IO.File.Copy(sourceFile, destFile);
                        System.IO.File.Delete(sourceFile);
#endif
                     }
                     else
                     {
                        System.IO.File.Copy(sourceFile, destFile);
                     }
                  }
#endif
               }
               catch (System.Exception ex)
               {
                  Debug.LogError($"Could not {(move ? "move" : "copy")} file: {ex}");
                  throw;
               }
            }
         }
      }

      /// <summary>Move a directory.</summary>
      /// <param name="sourceDir">Source directory path</param>
      /// <param name="destDir">Destination directory path</param>
      public static void MoveDirectory(string sourceDir, string destDir)
      {
         CopyDirectory(sourceDir, destDir, true);
      }

      /// <summary>Move a file.</summary>
      /// <param name="sourceFile">Source file path</param>
      /// <param name="destFile">Destination file path</param>
      public static void MoveFile(string sourceFile, string destFile)
      {
         CopyFile(sourceFile, destFile, true);
      }

      /// <summary>Delete a file.</summary>
      /// <param name="file">File to delete</param>
      public static void DeleteFile(string file)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'DeleteFile' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               SimpleFileBrowser.FileBrowserHelpers.DeleteFile(file);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               if (!ExistsFile(file))
               {
                  Debug.LogError($"File does not exists: {file}");
               }
               else
               {
                  System.IO.File.Delete(file);
               }
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not delete file: {ex}");
               throw;
            }
         }
      }

      /// <summary>Delete a directory.</summary>
      /// <param name="dir">Directory to delete</param>
      public static void DeleteDirectory(string dir)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'DeleteDirectory' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               SimpleFileBrowser.FileBrowserHelpers.DeleteDirectory(dir);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               if (!ExistsDirectory(dir))
               {
                  Debug.LogError($"Source directory does not exists: {dir}");
               }
               else
               {
                  System.IO.Directory.Delete(dir, true);
               }
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not delete directory: {ex}");
               throw;
            }
         }
      }

      /// <summary>Checks if the directory exists.</summary>
      /// <returns>True if the directory exists</returns>
      public static bool ExistsFile(string file)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'ExistsFile' is not supported for the current platform!");
         }
         else
         {
#if UNITY_ANDROID
#if CT_RTFB
            return SimpleFileBrowser.FileBrowserHelpers.FileExists(file);
#else
            Debug.LogWarning(RTFBisMissing);
#endif
#else
            return System.IO.File.Exists(file);
#endif
         }

         return false;
      }

      /// <summary>Checks if the directory exists.</summary>
      /// <returns>True if the directory exists</returns>
      public static bool ExistsDirectory(string path)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'ExistsPath' is not supported for the current platform!");
         }
         else
         {
#if UNITY_ANDROID
#if CT_RTFB
            return SimpleFileBrowser.FileBrowserHelpers.DirectoryExists(path);
#else
            Debug.LogWarning(RTFBisMissing);
#endif
#else
            return System.IO.Directory.Exists(path);
#endif
         }

         return false;
      }

      /// <summary>Creates a directory.</summary>
      /// <param name="path">Path to the directory to create</param>
      public static void CreateDirectory(string path)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'CreatePathCreateDirectory' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               SimpleFileBrowser.FileBrowserHelpers.CreateFolderInDirectory(GetDirectoryName(path), GetFileName(path));
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               System.IO.Directory.CreateDirectory(path);
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not create directory: {ex}");
               throw;
            }
         }
      }

      /// <summary>Checks if the path is a directory.</summary>
      /// <param name="path">Path to the directory</param>
      /// <returns>True if the path is a directory</returns>
      public static bool IsDirectory(string path)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'IsDirectory' is not supported for the current platform!");
         }
         else
         {
#if UNITY_ANDROID
#if CT_RTFB
            return SimpleFileBrowser.FileBrowserHelpers.IsDirectory(path);
#else
            Debug.LogWarning(RTFBisMissing);
#endif
#else
            if (ExistsDirectory(path))
               return true;
            if (ExistsFile(path))
               return false;

            string extension = GetExtension(path);
            return extension == null || extension.Length <= 1; // extension includes '.'
#endif
         }

         return false;
      }

      /// <summary>Checks if the path is a file.</summary>
      /// <param name="path">Path to the file</param>
      /// <returns>True if the path is a file</returns>
      public static bool IsFile(string path)
      {
         return !IsDirectory(path);
      }

      /// <summary>Renames a file in a path.</summary>
      /// <param name="path">Path of the file</param>
      /// <param name="newName">New name for the file</param>
      /// <returns>New path of the file</returns>
      public static string RenameFile(string path, string newName)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'RenameFile' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               SimpleFileBrowser.FileBrowserHelpers.RenameFile(path, newName);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), newName);
               System.IO.File.Move(path, newPath);

               return newPath;
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not rename file: {ex}");
               throw;
            }
         }

         return null;
      }

      /// <summary>Renames a directory in a path.</summary>
      /// <param name="path">Path of the directory</param>
      /// <param name="newName">New name for the directory</param>
      /// <returns>New path of the directory</returns>
      public static string RenameDirectory(string path, string newName)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'RenameDirectory' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               return SimpleFileBrowser.FileBrowserHelpers.RenameDirectory(path, newName);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               string newPath = System.IO.Path.Combine(new System.IO.DirectoryInfo(path).Parent.FullName, newName);
               System.IO.Directory.Move(path, newPath);

               return newPath;
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not rename directory: {ex}");
               throw;
            }
         }

         return null;
      }

      /// <summary>Returns the file name for the path.</summary>
      /// <param name="path">Path of the file</param>
      /// <returns>File name for the path</returns>
      public static string GetFileName(string path)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetFilename' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               SimpleFileBrowser.FileBrowserHelpers.GetFilename(path);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               return System.IO.Path.GetFileName(path);
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not get file name: {ex}");
               throw;
            }
         }

         return null;
      }

      /// <summary>Returns the directory name for the path.</summary>
      /// <param name="path">Path of the directory</param>
      /// <returns>Directory name for the path</returns>
      public static string GetDirectoryName(string path)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetDirectoryName' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               return SimpleFileBrowser.FileBrowserHelpers.GetDirectoryName(path);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               return System.IO.Path.GetDirectoryName(path);
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not get directory name: {ex}");
               throw;
            }
         }

         return null;
      }

      /// <summary>Returns the size of a file.</summary>
      /// <param name="path">Path of the file</param>
      /// <returns>Size for the file</returns>
      public static long GetFilesize(string path)
      {
         if (IsFile(path))
         {
            if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
            {
               Debug.LogWarning("'GetFilesize' is not supported for the current platform!");
            }
            else
            {
               try
               {
#if UNITY_ANDROID
#if CT_RTFB
                  return SimpleFileBrowser.FileBrowserHelpers.GetFilesize(path);
#else
                  Debug.LogWarning(RTFBisMissing);
#endif
#else
                  return new System.IO.FileInfo(path).Length;
#endif
               }
               catch (System.Exception ex)
               {
                  Debug.LogError($"Could not get file size: {ex}");
                  throw;
               }
            }
         }

         Debug.LogWarning($"Path is not a file: {path}");

         return 0;
      }

      /// <summary>Returns the extension of a file.</summary>
      /// <param name="path">Path of the file</param>
      /// <returns>Extension of the file</returns>
      public static string GetExtension(string path)
      {
         if (IsFile(path))
         {
            if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
               return path.Substring(path.LastIndexOf("."));

            try
            {
               return System.IO.Path.GetExtension(path).Substring(1);
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not get extension for file: {ex}");
               throw;
            }
         }

         Debug.LogWarning($"Path is not a file: {path}");

         return null;
      }

      /// <summary>Returns the size of a file.</summary>
      /// <param name="path">Path of the file</param>
      /// <returns>Size for the file</returns>
      public static System.DateTime GetLastModifiedDate(string path)
      {
         if (IsFile(path))
         {
            if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
            {
               Debug.LogWarning("'GetLastModifiedDate' is not supported for the current platform!");
            }
            else
            {
               try
               {
#if UNITY_ANDROID
#if CT_RTFB
                  return SimpleFileBrowser.FileBrowserHelpers.GetLastModifiedDate(path);
#else
                  Debug.LogWarning(RTFBisMissing);
#endif
#else
                  return new System.IO.FileInfo(path).LastWriteTime;
#endif
               }
               catch (System.Exception ex)
               {
                  Debug.LogError($"Could not get last modify date: {ex}");
                  throw;
               }
            }
         }

         Debug.LogWarning($"Path is not a file: {path}");

         return System.DateTime.MinValue;
      }

      /// <summary>Reads the text of a file.</summary>
      /// <param name="sourceFile">Source file path</param>
      /// <param name="encoding">Encoding of the text (optional, default: UTF8)</param>
      /// <returns>Text-content of the file</returns>
      public static string ReadAllText(string sourceFile, System.Text.Encoding encoding = null)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'ReadAllText' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               return SimpleFileBrowser.FileBrowserHelpers.ReadTextFromFile(sourceFile);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               if (!ExistsFile(sourceFile))
               {
                  Debug.LogError($"Source file does not exists: {sourceFile}");
               }
               else
               {
                  return System.IO.File.ReadAllText(sourceFile, encoding ?? System.Text.Encoding.UTF8);
               }
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not read file: {ex}");
               throw;
            }
         }

         return null;
      }

      /// <summary>Reads all lines of text from a file.</summary>
      /// <param name="sourceFile">Source file path</param>
      /// <param name="encoding">Encoding of the text (optional, default: UTF8)</param>
      /// <returns>Array of text lines from the file</returns>
      public static string[] ReadAllLines(string sourceFile, System.Text.Encoding encoding = null)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'ReadAllLines' is not supported for the current platform!");
         }
         else
         {
            try
            {
               if (!ExistsFile(sourceFile))
               {
                  Debug.LogError($"Source file does not exists: {sourceFile}");
               }
               else
               {
                  return System.IO.File.ReadAllLines(sourceFile, encoding ?? System.Text.Encoding.UTF8);
               }
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not read file: {ex}");
               throw;
            }
         }

         return null;
      }

      /// <summary>Reads the bytes of a file.</summary>
      /// <param name="sourceFile">Source file path</param>
      /// <returns>Byte-content of the file</returns>
      public static byte[] ReadAllBytes(string sourceFile)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'ReadAllBytes' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               return SimpleFileBrowser.FileBrowserHelpers.ReadBytesFromFile(sourceFile);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               if (!ExistsFile(sourceFile))
               {
                  Debug.LogError($"Source file does not exists: {sourceFile}");
               }
               else
               {
                  return System.IO.File.ReadAllBytes(sourceFile);
               }
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not read file: {ex}");
               throw;
            }
         }

         return null;
      }

      /// <summary>Writes text to a file.</summary>
      /// <param name="destFile">Destination file path</param>
      /// <param name="text">Text-content to write</param>
      /// <param name="encoding">Encoding of the text (optional, default: UTF8)</param>
      public static void WriteAllText(string destFile, string text, System.Text.Encoding encoding = null)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'WriteAllText' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               SimpleFileBrowser.FileBrowserHelpers.WriteTextToFile(destFile, text);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               System.IO.File.WriteAllText(destFile, text, encoding ?? System.Text.Encoding.UTF8);
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not write file: {ex}");
               throw;
            }
         }
      }

      /// <summary>Writes all lines of text to a file.</summary>
      /// <param name="destFile">Destination file path</param>
      /// <param name="lines">Array of text lines to write</param>
      /// <param name="encoding">Encoding of the text (optional, default: UTF8)</param>
      public static void WriteAllLines(string destFile, string[] lines, System.Text.Encoding encoding = null)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'WriteAllLines' is not supported for the current platform!");
         }
         else
         {
            try
            {
               System.IO.File.WriteAllLines(destFile, lines, encoding ?? System.Text.Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not write file: {ex}");
               throw;
            }
         }
      }

      /// <summary>Writes bytes to a file.</summary>
      /// <param name="destFile">Destination file path</param>
      /// <param name="data">Byte-content to write</param>
      public static void WriteAllBytes(string destFile, byte[] data)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'WriteAllBytes' is not supported for the current platform!");
         }
         else
         {
            try
            {
#if UNITY_ANDROID
#if CT_RTFB
               SimpleFileBrowser.FileBrowserHelpers.WriteBytesToFile(destFile, data);
#else
               Debug.LogWarning(RTFBisMissing);
#endif
#else
               System.IO.File.WriteAllBytes(destFile, data);
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not write file: {ex}");
               throw;
            }
         }
      }

      /// <summary>
      /// Shows the location of a path (or file) in OS file explorer.
      /// NOTE: only works on standalone platforms
      /// </summary>
      public static void ShowPath(string path)
      {
         ShowFile(path);
      }

      /// <summary>
      /// Shows the location of a file (or path) in OS file explorer.
      /// NOTE: only works on standalone platforms
      /// </summary>
      public static void ShowFile(string file)
      {
         if (Crosstales.Common.Util.BaseHelper.isStandalonePlatform || Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if UNITY_STANDALONE || UNITY_EDITOR
            string path;

            if (string.IsNullOrEmpty(file) || file.Equals("."))
            {
               path = ".";
            }
            else if ((Crosstales.Common.Util.BaseHelper.isWindowsPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor) && file.Length < 4)
            {
               path = file; //root directory
            }
            else
            {
               path = ValidatePath(GetDirectoryName(file));
            }

            try
            {
               if (ExistsDirectory(path))
               {
#if (ENABLE_IL2CPP && CT_PROC) || (CT_DEVELOP && CT_PROC)
                  using (Crosstales.Common.Util.CTProcess process = new Crosstales.Common.Util.CTProcess())
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                  //using (CTProcess process = new CTProcess())
#endif
                  {
                     process.StartInfo.Arguments = $"\"{path}\"";

                     if (Crosstales.Common.Util.BaseHelper.isWindowsPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor)
                     {
                        process.StartInfo.FileName = "explorer.exe";
#if (ENABLE_IL2CPP && CT_PROC) || (CT_DEVELOP && CT_PROC)
                        process.StartInfo.UseCmdExecute = true;
#endif
                        process.StartInfo.CreateNoWindow = true;
                     }
                     else if (Crosstales.Common.Util.BaseHelper.isMacOSPlatform || Crosstales.Common.Util.BaseHelper.isMacOSEditor)
                     {
                        process.StartInfo.FileName = "open";
                     }
                     else
                     {
                        process.StartInfo.FileName = "xdg-open";
                     }

                     process.Start();
                  }
               }
               else
               {
                  Debug.LogWarning($"Path to file doesn't exist: {path}");
               }
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not show file location: {ex}");
            }
#endif
         }
         else
         {
            Debug.LogWarning("'ShowFileLocation' is not supported on the current platform!");
         }
      }

      /// <summary>
      /// Opens a file with the OS default application.
      /// NOTE: only works for standalone platforms
      /// </summary>
      /// <param name="file">File path</param>
      public static void OpenFile(string file)
      {
         if (Crosstales.Common.Util.BaseHelper.isStandalonePlatform || Crosstales.Common.Util.BaseHelper.isEditor)
         {
            try
            {
#if UNITY_STANDALONE || UNITY_EDITOR
               if (ExistsFile(file))
               {
#if ENABLE_IL2CPP && CT_PROC
                  using (CTProcess process = new CTProcess())
                  {
                     process.StartInfo.Arguments = $"\"{file}\"";

                     if (Crosstales.Common.Util.BaseHelper.isWindowsPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor)
                     {
                        process.StartInfo.FileName = "explorer.exe";
                        process.StartInfo.UseCmdExecute = true;
                        process.StartInfo.CreateNoWindow = true;
                     }
                     else if (Crosstales.Common.Util.BaseHelper.isMacOSPlatform || Crosstales.Common.Util.BaseHelper.isMacOSEditor)
                     {
                        process.StartInfo.FileName = "open";
                     }
                     else
                     {
                        process.StartInfo.FileName = "xdg-open";
                     }

                     process.Start();
                  }
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                  {
                     if (Crosstales.Common.Util.BaseHelper.isMacOSPlatform || Crosstales.Common.Util.BaseHelper.isMacOSEditor)
                     {
                        process.StartInfo.FileName = "open";
                        process.StartInfo.WorkingDirectory = GetDirectoryName(file) + Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX;
                        process.StartInfo.Arguments = $"-t \"{GetFileName(file)}\"";
                     }
                     else if (Crosstales.Common.Util.BaseHelper.isLinuxPlatform || Crosstales.Common.Util.BaseHelper.isLinuxEditor)
                     {
                        process.StartInfo.FileName = "xdg-open";
                        process.StartInfo.WorkingDirectory = GetDirectoryName(file) + Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX;
                        process.StartInfo.Arguments = GetFileName(file);
                     }
                     else
                     {
                        process.StartInfo.FileName = file;
                     }

                     process.Start();
                  }
#endif
               }
               else
               {
                  Debug.LogWarning($"File doesn't exist: {file}");
               }
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not open file: {ex}");
            }
         }
         else
         {
            Debug.LogWarning("'OpenFile' is not supported on the current platform!");
         }
      }

      /*
      /// <summary>Validates a given path and add missing slash.</summary>
      /// <param name="path">Path to validate</param>
      /// <returns>Valid path</returns>
      public static string ValidPath(string path)
      {
          if (!string.IsNullOrEmpty(path))
          {
              string pathTemp = path.Trim();
              string result = null;

              if (isWindowsPlatform)
              {
                  result = pathTemp.Replace('/', '\\');

                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS))
                  {
                      result += BaseConstants.PATH_DELIMITER_WINDOWS;
                  }
              }
              else
              {
                  result = pathTemp.Replace('\\', '/');

                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
                  {
                      result += BaseConstants.PATH_DELIMITER_UNIX;
                  }
              }

              return result;
          }

          return path;
      }

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidFilePath(string path)
      {
          if (!string.IsNullOrEmpty(path))
          {

              string result = ValidPath(path);

              if (result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS) || result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
              {
                  result = result.Substring(0, result.Length - 1);
              }

              return result;
          }

          return path;
      }
      */

      #region Legacy

      /// <summary>
      /// Checks a given path for invalid characters
      /// </summary>
      /// <param name="path">Path to check for invalid characters</param>
      /// <returns>Returns true if the path contains invalid chars, otherwise it's false.</returns>
      [System.Obsolete("Please use 'HasPathInvalidChars' instead.")]
      public static bool PathHasInvalidChars(string path)
      {
         return HasPathInvalidChars(path);
      }

      /// <summary>
      /// Checks a given file for invalid characters
      /// </summary>
      /// <param name="file">File to check for invalid characters</param>
      /// <returns>Returns true if the file contains invalid chars, otherwise it's false.</returns>
      [System.Obsolete("Please use 'HasFileInvalidChars' instead.")]
      public static bool FileHasInvalidChars(string file)
      {
         return HasFileInvalidChars(file);
      }

      /// <summary>Copy or move a directory.</summary>
      /// <param name="sourceDir">Source directory path</param>
      /// <param name="destDir">Destination directory path</param>
      /// <param name="move">Move directory instead of copy (default: false, optional)</param>
      [System.Obsolete("Please use 'CopyDirectory' instead.")]
      public static void CopyPath(string sourceDir, string destDir, bool move = false)
      {
         CopyDirectory(sourceDir, destDir, move);
      }

      #endregion

      #endregion


      #region Private methods

      private static void copyAll(System.IO.DirectoryInfo source, System.IO.DirectoryInfo target)
      {
         CreateDirectory(target.FullName);

         foreach (System.IO.FileInfo fi in source.GetFiles())
         {
            fi.CopyTo(System.IO.Path.Combine(target.FullName, fi.Name), true);
         }

         // Copy each subdirectory using recursion.
         foreach (System.IO.DirectoryInfo sourceSubDir in source.GetDirectories())
         {
            System.IO.DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
            copyAll(sourceSubDir, nextTargetSubDir);
         }
      }
#if CT_RTFB && UNITY_ANDROID
      private static void getFilesRTFB(string path, bool isRecursive = false, params string[] filenames)
      {
         SimpleFileBrowser.FileSystemEntry[] result = SimpleFileBrowser.FileBrowserHelpers.GetEntriesInDirectory(path, false);

         foreach (SimpleFileBrowser.FileSystemEntry entry in result)
         {
            string filePath = entry.Path;

            if (IsFile(filePath))
            {
               if (filenames == null || filenames.Length == 0)
               {
                  fileList.Add(filePath);
               }
               else
               {
                  foreach (string part in filenames)
                  {
                     if (part.StartsWith("*."))
                     {
                        if (filePath.CTEndsWith(part.Replace("*", string.Empty)))
                        {
                           fileList.Add(filePath);
                           break;
                        }
                     }
                     else
                     {
                        if (GetFileName(filePath).CTContains(part.Replace("*", string.Empty)))
                        {
                           fileList.Add(filePath);
                           break;
                        }
                     }
                  }
               }
            }
            else
            {
               if (isRecursive)
                  getFilesRTFB(filePath, isRecursive, filenames);
            }
         }
      }

      private static void getDirectoriesRTFB(string path, bool isRecursive = false)
      {
         SimpleFileBrowser.FileSystemEntry[] result = SimpleFileBrowser.FileBrowserHelpers.GetEntriesInDirectory(path, false);

         foreach (SimpleFileBrowser.FileSystemEntry entry in result)
         {
            string dirPath = entry.Path;

            if (IsDirectory(dirPath))
            {
               dirList.Add(dirPath);

               if (isRecursive)
                  getDirectoriesRTFB(dirPath, isRecursive);
            }
         }
      }
#endif

      #endregion
   }
}
// © 2015-2023 crosstales LLC (https://www.crosstales.com)