using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class Task {
	public static int NextId { get; set; } = 1;
	[JsonIgnore] public bool Expanded;
	[JsonIgnore] public List<Task> Children = new();
	[JsonIgnore] public int Group;
	public int Id;
	[DefaultValue("")] public string Text = "";
	public DateTime Created;
	public DateTime Updated;
	public DateTime Completed;
	public int Parent;
	[DefaultValue("")] public string Description = "";
	[DefaultValue(1)] public int Points = 1;
	public bool IsFolder;
	public DateTime Date;
	public DateTime StartDate;
	public List<string> Tags;
	public int Priority;
	public List<int> TrackedTime;

	public static Task Create(string text, int parent) {
		var task = new Task {
			Id = NextId,
			Text = text,
			Created = DateTime.Now.ToUniversalTime(),
			Updated = DateTime.Now.ToUniversalTime(),
			Parent = parent,
		};
		if (parent > 0) {
			App.Tasks[parent].Children.Add(task);
		}
		NextId++;
		return task;
	}

	public int CountChildren() {
		return Children.Sum(child => 1 + child.CountChildren());
	}

	public void Save() {
		Updated = DateTime.Now.ToUniversalTime();
		App.Tasks[Id] = this;
		App.View.Render();
		var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings {
			DefaultValueHandling = DefaultValueHandling.Ignore,
		});
		App.Request(HttpClient.Method.Post, json);
	}

	public void Delete() {
		var tasksToDelete = GetChildrenIds(this);
		tasksToDelete.Add(Id);
		foreach (var taskId in tasksToDelete) {
			App.Tasks.Remove(taskId);
		}
		App.View.Render();
		var idsAsString = string.Join("\n", tasksToDelete);
		App.Request(HttpClient.Method.Delete, idsAsString);
	}
	
	private static List<int> GetChildrenIds(Task parent) {
		var result = parent.Children.Select(task => task.Id).ToList();
		foreach (var child in parent.Children) {
			result.AddRange(GetChildrenIds(child));
		}
		return result;
	}
}