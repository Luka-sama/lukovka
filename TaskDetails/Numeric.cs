using Godot;

public partial class Numeric : LineEdit {
	[Export] public int MinValue = -2147483648;
	[Export] public int MaxValue = 2147483647;
	private string _oldText = "0";

	public override void _Ready() {
		TextChanged += OnTextChanged;
	}

	private void OnTextChanged(string newText) {
		if (newText.IsValidInt() && newText != "-0" && newText.ToInt() >= MinValue && newText.ToInt() <= MaxValue) {
			if (newText[0] != '0') {
				_oldText = newText;
			} else {
				Text = newText.ToInt().ToString();
				_oldText = Text;
				CaretColumn = Text.Length;
			}
		} else if (string.IsNullOrEmpty(newText) || (newText == "-" && MinValue < 0)) {
			_oldText = newText;
		} else {
			var cursorPosition = Mathf.Clamp(CaretColumn - 1, 0, _oldText.Length);
			Text = _oldText;
			CaretColumn = cursorPosition;
		}
	}
}