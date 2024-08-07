using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.epmanager.popups {
    internal abstract class PopupBase : PopupWindowContent {
        private static readonly StyleSheet _styleSheet = Resources.Load<StyleSheet>("USS/Util");

        protected abstract VisualTreeAsset InitTagUxml { get; }

        public override Vector2 GetWindowSize() {
            return new Vector2(200, 100);
        }

        public override void OnGUI(Rect rect) { }

        public override void OnOpen() {
            this.editorWindow.rootVisualElement.Clear();
            this.InitTagUxml.CloneTree(this.editorWindow.rootVisualElement);
            this.editorWindow.rootVisualElement.styleSheets.Add(_styleSheet);
        }

        public override void OnClose() { }
    }
}