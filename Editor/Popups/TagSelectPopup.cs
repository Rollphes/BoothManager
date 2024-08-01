using io.github.rollphes.boothManager.client;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.boothManager.popups {
    internal class TagSelectPopup : PopupWindowContent {
        private readonly Client _client;

        public TagSelectPopup(Client client) {
            this._client = client;

        }
        public override Vector2 GetWindowSize() {
            return new Vector2(200, 100);
        }

        public override void OnGUI(Rect rect) { }

        public override void OnOpen() {
            var label = new Label("This is PopupContentExample");
            this.editorWindow.rootVisualElement.Add(label);
        }

        public override void OnClose() { }
    }
}
