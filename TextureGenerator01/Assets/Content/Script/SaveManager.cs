using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SaveManager
{

    public enum Extension
    {
        PNG,
        JPG,
        EXR
    }
    public static void SaveTexture2D(string path, Texture2D tex, Extension extension = Extension.PNG, bool askConfirmation = false, bool notifyOnSaved = false, bool refreshAssetDatabase = false, bool pingAsset = false)
    {
        bool canSave = true;

        if (askConfirmation && File.Exists(path))
        {
            if (!EditorUtility.DisplayDialog("Save texture", "File " + path + " already exists.\n\nOverwrite existing file?", "Yes", "No"))
            {
                canSave = false;
            }
        }

        if (canSave)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            bytes = null;

            if (refreshAssetDatabase)
            {
                AssetDatabase.Refresh();
            }

            if (notifyOnSaved)
            {
                EditorUtility.DisplayDialog("Success", "Texture saved as " + path, "Ok");
            }

            if (pingAsset)
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture>(path));
            }
        }
    }

}
