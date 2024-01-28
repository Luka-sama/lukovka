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
	public int RepeatingEvery;
	public int RepeatingInterval; // 0 - no repeating, 1 - repeating without date, 2 - day, 3 - week, 4 - month, 5 - year
	public bool RepeatingFromCompleted;
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

	public void Complete() {
		Completed = (Completed == DateTime.MinValue ? DateTime.Now.ToUniversalTime() : DateTime.MinValue);
		Save();

		if (Completed != DateTime.MinValue && RepeatingInterval > 0) {
			var newTask = Clone();
			newTask.Completed = DateTime.MinValue;
			var startDate = (RepeatingFromCompleted && StartDate != DateTime.MinValue ? Completed : StartDate);
			var date = (RepeatingFromCompleted && Date != DateTime.MinValue ? Completed : Date);
			if (RepeatingInterval == 1) {
				newTask.StartDate = DateTime.MinValue;
				newTask.Date = DateTime.MinValue;
			} else if (RepeatingInterval == 2) {
				newTask.StartDate = (startDate != DateTime.MinValue ? startDate.AddDays(RepeatingEvery) : startDate);
				newTask.Date = (date != DateTime.MinValue ? date.AddDays(RepeatingEvery) : date);
			} else if (RepeatingInterval == 3) {
				newTask.StartDate = (startDate != DateTime.MinValue ? startDate.AddDays(RepeatingEvery * 7) : startDate);
				newTask.Date = (date != DateTime.MinValue ? date.AddDays(RepeatingEvery * 7) : date);
			} else if (RepeatingInterval == 4) {
				newTask.StartDate = (startDate != DateTime.MinValue ? startDate.AddMonths(RepeatingEvery) : startDate);
				newTask.Date = (date != DateTime.MinValue ? date.AddMonths(RepeatingEvery) : date);
			} else if (RepeatingInterval == 5) {
				newTask.StartDate = (startDate != DateTime.MinValue ? startDate.AddYears(RepeatingEvery) : startDate);
				newTask.Date = (date != DateTime.MinValue ? date.AddYears(RepeatingEvery) : date);
			}
			newTask.Save();
		}
	}

	public void Save() {
		Updated = DateTime.Now.ToUniversalTime();
		App.Tasks[Id] = this;
		App.View.Render();
		App.Request(HttpClient.Method.Post, Serialize());
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
	
	private static Task Deserialize(string json) {
		return JsonConvert.DeserializeObject<Task>(json);
	}

	private string Serialize() {
		return JsonConvert.SerializeObject(this, new JsonSerializerSettings {
			DefaultValueHandling = DefaultValueHandling.Ignore,
		});
	}
	
	private Task Clone() {
		var task = Deserialize(Serialize());
		task.Id = NextId;
		NextId++;
		if (Parent > 0) {
			App.Tasks[Parent].Children.Add(task);
		}
		return task;
	}
	
	private static List<int> GetChildrenIds(Task parent) {
		var result = parent.Children.Select(task => task.Id).ToList();
		foreach (var child in parent.Children) {
			result.AddRange(GetChildrenIds(child));
		}
		return result;
	}
}