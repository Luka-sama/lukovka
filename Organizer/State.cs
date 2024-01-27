using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class State {
	public static State Default => new() {
		Name = "⌂",
		SelectedFilters = { "NotCompleted" },
	};
	public string Name;
	public readonly List<string> SelectedFilters = new();
	public int RootId { get; set; }
	public int SelectedSort { get; set; }
	public bool DescendingSort { get; set; }
	public bool Expanded { get; set; }
	
	public static State Deserialize(string json) {
		return JsonConvert.DeserializeObject<State>(json);
	}

	public string Serialize() {
		SelectedFilters.Sort();
		return JsonConvert.SerializeObject(this);
	}

	public State Clone(string newName) {
		var state = Deserialize(Serialize());
		state.Name = newName;
		return state;
	}
	
	public bool Equals(State state) {
		string savedNameA = Name, savedNameB = state.Name;
		Name = "";
		state.Name = "";
		var result = (Serialize() == state.Serialize());
		Name = savedNameA;
		state.Name = savedNameB;
		return result;
	}

	public void Save() {
		var json = Serialize();
		App.Request(HttpClient.Method.Post, json, true);
	}

	public void Delete() {
		App.Request(HttpClient.Method.Delete, Name, true);
	}
}