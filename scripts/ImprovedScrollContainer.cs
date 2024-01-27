using Godot;

// https://github.com/godotengine/godot/issues/21137
public partial class ImprovedScrollContainer : ScrollContainer {
	private const float Speed = 1.5f;
	private bool _swiping;
	private Vector2 _swipeStart;
	private Vector2 _swipeMouseStart;
	
	public override void _Input(InputEvent @event) {
		if (@event is InputEventMouseButton mouseEvent) {
			if (mouseEvent.IsPressed()) {
				_swiping = true;
				_swipeStart = new Vector2(ScrollHorizontal, ScrollVertical);
				_swipeMouseStart = mouseEvent.Position;
			} else {
				_swiping = false;
			}
		} else if (_swiping && @event is InputEventMouseMotion motionEvent) {
			ReleaseFocus();
			if (DisplayServer.HasFeature(DisplayServer.Feature.VirtualKeyboard)) {
				DisplayServer.VirtualKeyboardHide();
			}
			var delta = Speed * (motionEvent.Position - _swipeMouseStart);
			ScrollHorizontal = (int)(_swipeStart.X - delta.X);
			ScrollVertical = (int)(_swipeStart.Y - delta.Y);
		}
	}
}