using UnityEditor;
using UnityEngine;
using System.IO;

public static class SceneViewScreenshot
{
    [MenuItem("Tools/Capture Scene View 1920x1080")]
    static void Capture()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        var cam = sv.camera;
        var rt = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();

        File.WriteAllBytes("Assets/SceneShot.png", tex.EncodeToPNG());
        AssetDatabase.Refresh();

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}