using Godot;

public partial class ImprovedTextEdit : TextEdit {
	public override void _Input(InputEvent @event) {
		if (HasFocus() && @event is InputEventKey e && e.IsPressed() && e.Keycode == Key.Tab) {
			if (e.ShiftPressed) {
				FindPrevValidFocus().GrabFocus();
			} else {
				FindNextValidFocus().GrabFocus();
			}
			AcceptEvent();
		}
	}
}