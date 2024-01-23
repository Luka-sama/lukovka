using System;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class Task {
	public static int NextId = 1;
	[JsonIgnore] public bool Expanded;
	public int Id;
	[DefaultValue("")] public string Text;
	public DateTime Created;
	public DateTime Updated;
	public DateTime Completed;
	public int Parent;
	[DefaultValue("")] public string Description = "";
	public int Points;
	public bool IsFolder;
	public DateTime Date;

	public static Task Create(string text, int parent) {
		var task = new Task {
			Id = NextId,
			Text = text,
			Created = DateTime.Now,
			Updated = DateTime.Now,
			Parent = parent,
		};
		NextId++;
		return task;
	}

	public void Save() {
		Updated = DateTime.Now;
		App.Tasks[Id] = this;
		App.View.Render();
		var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings {
			DefaultValueHandling = DefaultValueHandling.Ignore,
		});
		App.Request(HttpClient.Method.Post, json);
	}

	public void Delete() {
		NextId = (Id + 1 == NextId ? Id : NextId);
		App.Tasks.Remove(Id);
		App.Request(HttpClient.Method.Delete, Id.ToString());

		var children = App.Tasks.Values.Where(child => child.Parent == Id).ToList();
		if (children.Count < 1) {
			App.View.Render();
		}
		foreach (var child in children) {
			child.Delete();
		}
	}
}