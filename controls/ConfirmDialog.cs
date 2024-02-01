using System;
using Godot;

public partial class ConfirmDialog : ConfirmationDialog {
	private static ConfirmDialog _dialog;
	private Action _callback;
	
	public static void Show(string text, Action callback) {
		_dialog.DialogText = text;
		_dialog._callback = callback;
		_dialog.PopupCentered();
	}
	
	public override void _Ready() {
		if (_dialog != null) {
			GD.PushError("You should have only one instance of ErrorDialog.");
			return;
		}
		_dialog = this;
	}
	
	private void OnConfirmed() {
		_callback();
	}
}