using System.Globalization;
using Godot;

public partial class Numeric : LineEdit {
	[Export] public int MinValue = -2147483648;
	[Export] public int MaxValue = 2147483647;
	[Export] public bool IsDouble;
	private string _oldText = "0";

	public override void _Ready() {
		TextChanged += OnTextChanged;
	}

	private void OnTextChanged(string newText) {
		const NumberStyles intStyle = NumberStyles.AllowLeadingSign;
		const NumberStyles doubleStyle = intStyle | NumberStyles.AllowDecimalPoint;

		bool isValid;
		decimal parsed;
		if (IsDouble) {
			isValid = decimal.TryParse(newText, doubleStyle, CultureInfo.InvariantCulture, out parsed);
		} else {
			isValid = int.TryParse(newText, intStyle, CultureInfo.InvariantCulture, out var parsedInt);
			parsed = parsedInt;
		}
		
		if (isValid && parsed >= MinValue && parsed <= MaxValue) {
			var parsedAsString = parsed.ToString(CultureInfo.InvariantCulture);
			if (IsDouble && newText[0] != '+' && (newText == "0" || newText.TrimStart('0') == newText) ||
			    newText == parsedAsString) {
				_oldText = newText;
			} else {
				Text = parsedAsString;
				_oldText = Text;
				CaretColumn = newText.Length;
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