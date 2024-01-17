using System;
using Godot;
using Newtonsoft.Json;

public class Task {
	[JsonIgnore] public int NestingLevel;
	public readonly int Id;
	public readonly DateTime Created = DateTime.Now;
	public DateTime Completed;
	public Task Parent;
	public string Text;
	public string Description;
	public int Points;
	public DateTime Date;
	private static int _nextId = 1;

	public Task(int id = 0) {
		Id = (id == 0 ? _nextId : id);
		_nextId = Mathf.Max(Id + 1, _nextId);
	}

	public void Save() {
		App.Tasks[Id] = this;
		App.View.Render();
		var json = JsonConvert.SerializeObject(this);
		// TODO: send json to server
	}

	public void Delete() {
		App.Tasks.Remove(Id);
		App.View.Render();
		// TODO: send delete Id to server
	}
}