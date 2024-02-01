using Godot;

public partial class ErrorDialog : AcceptDialog {
	private static ErrorDialog _dialog;
	
	public static void Show(string text, bool writeToConsole = false) {
		if (writeToConsole) {
			GD.PushError(text);
		}
		_dialog.DialogText = text;
		_dialog.Size = new Vector2I(400, 100);
		_dialog.PopupCentered();
	}
	
	public override void _Ready() {
		if (_dialog != null) {
			GD.PushError("You should have only one instance of ErrorDialog.");
			return;
		}
		_dialog = this;
	}
}