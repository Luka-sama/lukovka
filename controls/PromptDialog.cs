using System;
using Godot;

public partial class PromptDialog : ConfirmationDialog {
	private static PromptDialog _dialog;
	private static LineEdit _lineEdit;
	private Action<string> _callback;
	
	public static void Show(string title, Action<string> callback, string startValue = "") {
		_dialog.Title = title;
		_dialog._callback = callback;
		_dialog.PopupCentered();
		_lineEdit.Text = startValue;
		_lineEdit.GrabFocus();
	}

	public override void _Ready() {
		if (_dialog != null) {
			GD.PushError("You should have only one instance of ErrorDialog.");
			return;
		}
		_dialog = this;
		_lineEdit = GetNode<LineEdit>("%PromptAnswer");
	}

	private void OnConfirmed() {
		_callback(_lineEdit.Text);
		_lineEdit.Text = "";
	}

	private void OnTextSubmitted(string _) {
		GetOkButton().EmitSignal("pressed");
	}
}