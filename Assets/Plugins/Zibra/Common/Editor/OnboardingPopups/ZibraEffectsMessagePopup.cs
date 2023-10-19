using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using com.zibra.common.Editor.SDFObjects;

namespace com.zibra.common.Editor
{
    internal class ZibraEffectsMessagePopup : EditorWindow
    {

        public static GUIContent WindowTitle => new GUIContent("Zibra Effects License Message");

        private string Url = "";

        private static void ShowWindow()
        {
            ZibraEffectsMessagePopup window = (ZibraEffectsMessagePopup)GetWindow(typeof(ZibraEffectsMessagePopup));
            window.titleContent = WindowTitle;
            window.Show();
        }

        private void OnEnable()
        {
            var root = rootVisualElement;
            root.Clear();

            int width = 456;
            int height = 442;

            minSize = maxSize = new Vector2(width, height);

            var uxmlAssetPath = AssetDatabase.GUIDToAssetPath("7e8488b56556d8c48a38cd92f80fbd6a");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            visualTree.CloneTree(root);

            root.Q<Button>("ButtonText").clicked += SubscribeClick;
        }

        private void SubscribeClick()
        {
            Application.OpenURL(Url);
        }

        [InitializeOnLoadMethod]
        static void SetupCallback()
        {
            // Don't automatically open any windows in batch mode
            if (Application.isBatchMode)
            {
                return;
            }

            ServerAuthManager.OnProLicenseWarning = OpenProOnbordengWindow;
        }

        static void OpenProOnbordengWindow(string headerText, string bodyText, string url, string buttonText)
        {
            ZibraEffectsMessagePopup popup = CreateInstance<ZibraEffectsMessagePopup>();
            popup.OpenProOnbordengWindowInternal(headerText, bodyText, url, buttonText);
        }

        private void OpenProOnbordengWindowInternal(string headerText, string bodyText, string url, string buttonText)
        {
            var root = rootVisualElement;

            root.Q<Label>("TextTitle").text = headerText;
            root.Q<Label>("TextMessage").text = bodyText;
            if (buttonText == "")
            {
                root.Q<Button>("ButtonText").style.display = DisplayStyle.None;
            }
            else
            {
                root.Q<Button>("ButtonText").text = buttonText;
            }

            Url = url;
            ShowWindow();
        }
    }
}
