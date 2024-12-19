using Godot;

public partial class ImprovedRichTextLabel : RichTextLabel {
	public override void _Ready() {
		MetaClicked += meta => OS.ShellOpen(meta.AsString());
	}

	public void SetText(string text) {
		Clear();
		var regex = new RegEx();
		regex.Compile("(?i)((https?://|www\\.)[-a-z0-9+&@#/%?=~_|!:,.;()]*[-a-z0-9+&@#/%=~_|()])");
		text = regex.Sub(text, "[url]$1[/url]", true);
		AppendText(text);
	}
}
