using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;

public partial class Organizer : Control {
	public static List<Task> Tasks { get; private set; }
	public static State State { get; private set; } = State.Default;
	private static readonly Dictionary<string, State> States = new();
	private static FontFile _boldFont;
	private static Control _states;
	private static Control _stateManager;
	private static PopupMenu _filterMenu;
	private static OptionButton _sortMenu;
	private static OptionButton _groupingMenu;
	private static Button _expandButton;
	private static ConfirmationDialog _confirmStateDelete;
	private static ConfirmationDialog _enterStateName;
	private static ConfirmationDialog _enterCustomFilter;
	private static string _stateToRemove;
	
	public static void Organize() {
		UpdateStateButtons();
		
		Tasks = App.Tasks.Values
			.Where(HasFilter("NoRootTaskParent") ? IsChildOfRoot : WithHierarchy(IsChildOfRoot))
			.Where(HasFilter("NoHierarchy") ? SumFilter : WithHierarchy(SumFilter))
			.ToList();
		
		var sort = Sort.AllSorts.First(sort => sort.Name == State.SelectedSort);
		Tasks.Sort((a, b) => {
			var cmp = (int)sort.Invoke(null, new object[] { a, b })!;
			if (State.DescendingSort && cmp != 0) {
				cmp = (cmp == 1 ? -1 : 1);
			}
			return cmp;
		});

		if (string.IsNullOrEmpty(State.GroupBy)) {
			return;
		}
		var groups = new Dictionary<string, Task>();
		var groupTasks = new List<Task>();
		var nextId = -1;
		var grouping = Group.AllGroupings.First(grouping => grouping.Name == State.GroupBy);
		foreach (var task in Tasks) {
			var group = (string)grouping.Invoke(null, new object[] { task })!;
			if (groups.TryGetValue(group, out var groupTask)) {
				task.Group = groupTask.Id;
				continue;
			}

			groupTask = new Task() {
				Expanded = true,
				Id = nextId,
				Text = group,
				IsFolder = true,
			};
			groupTasks.Add(groupTask);
			task.Group = groupTask.Id;
			groups.Add(group, groupTask);
			nextId--;
		}
		Tasks.AddRange(groupTasks);
	}

	public override void _Ready() {
		_boldFont = GD.Load<FontFile>("res://fonts/verdanab.ttf");
		_states = GetNode<Control>("%States");
		_stateManager = GetNode<Control>("%StateManager");
		_expandButton = GetNode<Button>("%Expand");
		_confirmStateDelete = GetNode<ConfirmationDialog>("%ConfirmStateDelete");
		_enterStateName = GetNode<ConfirmationDialog>("%EnterStateName");
		_enterCustomFilter = GetNode<ConfirmationDialog>("%EnterCustomFilter");
		
		_sortMenu = GetNode<OptionButton>("%Sort");
		foreach (var sort in Sort.AllSorts) {
			_sortMenu.AddItem(TransformName(sort.Name));
		}

		_groupingMenu = GetNode<OptionButton>("%GroupBy");
		foreach (var grouping in Group.AllGroupings) {
			_groupingMenu.AddItem(TransformName(grouping.Name));
		}

		_filterMenu = GetNode<MenuButton>("%AddFilter").GetPopup();
		_filterMenu.IdPressed += AddFilterByIndex;
		
		States.Add(State.Name, State.Default);
		AddStateButton(State.Name);
		ApplyState(State, true);
		State.Name = "";
	}

	public static bool HasFilter(string filterName) {
		return State.SelectedFilters.Contains(filterName);
	}
	
	public static void RemoveFilter(string filterName) {
		if (!HasFilter(filterName)) {
			return;
		}
		
		if (filterName == "NoHierarchy") {
			RemoveFilter("NoTasksWithChildren");
			State.GroupBy = "";
		}
		
		State.SelectedFilters.Remove(filterName);
		_stateManager.GetNode(filterName.ValidateNodeName()).QueueFree();
		RegenerateFilterList();
		App.View.Render();
	}

	public static void AddOrRestoreState(State state) {
		States.Add(state.Name, state);
		AddStateButton(state.Name);
		UpdateStateButtons();
	}
	
	private static Func<Task, bool> WithHierarchy(Func<Task, bool> filter) {
		return task => filter(task) || task.Children.Any(WithHierarchy(filter));
	}

	private static bool SumFilter(Task task) {
		var result = Filter.AllFilters
			.Where(filterInfo => HasFilter(filterInfo.Name))
			.All(filterInfo => (bool)filterInfo.Invoke(null, new object[] { task })!);
		if (!result) {
			return false;
		}
		return State.SelectedFilters
			.Where(filterName => filterName.StartsWith("@"))
			.All(customFilter => Filter.Custom(task, customFilter[1..]));
	}
	
	private static void RegenerateFilterList() {
		_filterMenu.Clear(true);
		foreach (var filter in Filter.AllFilters) {
			if (!HasFilter(filter.Name)) {
				_filterMenu.AddItem(TransformName(filter.Name));
			}
		}
	}

	private static bool IsChildOfRoot(Task task) {
		if (State.RootId == 0) {
			return true;
		}
		
		if (task.Id == State.RootId) {
			return true;
		} else if (!App.Tasks.ContainsKey(task.Parent)) {
			if (task.Parent != 0) {
				App.ShowError($"Orphan task with ID {task.Id}.", true);
			}
			return false;
		}
		return IsChildOfRoot(App.Tasks[task.Parent]);
	}
	
	private static string TransformName(string input) {
		if (input.StartsWith("@")) {
			return input[1..];
		}
		
		var result = new StringBuilder();
		result.Append(input[0]);
		for (var i = 1; i < input.Length; i++) {
			if (char.IsUpper(input[i])) {
				result.Append(' ');
			}
			result.Append(char.ToLower(input[i]));
		}
		return result.ToString();
	}

	private static void ExpandTasks() {
		if (HasFilter("NoHierarchy")) {
			App.ShowError("You can't expand tasks with no hierarchy.");
			return;
		}
		
		State.Expanded = !State.Expanded;
		ApplyExpand();
		App.View.Render();
	}
	
	private static void ApplyExpand() {
		_expandButton.Text = (State.Expanded ? "Collapse" : "Expand");
		foreach (var task in App.Tasks.Values) {
			task.Expanded = State.Expanded && task.Children.Count > 0;
		}
	}

	private static void ChangeSort(long index) {
		var sortName = Sort.AllSorts[(int)index - 1].Name;
		if (State.SelectedSort == sortName) {
			State.DescendingSort = !State.DescendingSort;
		}
		State.SelectedSort = sortName;
		App.View.Render();
	}

	private static void ChangeGrouping(long index) {
		AddFilter("NoHierarchy");
		var groupBy = Group.AllGroupings[(int)index - 1].Name;
		State.GroupBy = (State.GroupBy != groupBy ? groupBy : "");
		if (string.IsNullOrEmpty(State.GroupBy)) {
			_groupingMenu.Select(0);
		}
		App.View.Render();
	}
	
	private static void AddFilterByIndex(long index) {
		var filterName = Filter.AllFilters
			.Where(filter => !HasFilter(filter.Name))
			.ToList()[(int)index].Name;
		AddFilter(filterName);
	}

	private static void AddFilter(string filterName) {
		if (HasFilter(filterName)) {
			return;
		}
		
		switch (filterName) {
			case "Custom":
				var validated = filterName.ValidateNodeName();
				if (State.SelectedFilters.Exists(filter => filter.ValidateNodeName() == validated)) {
					App.ShowError($"There is already a custom filter with content {filterName}.");
					return;
				}
				_enterCustomFilter.PopupCentered();
				_enterCustomFilter.GetNode<LineEdit>("%CustomFilter").GrabFocus();
				return;
			case "NoTasksWithChildren" when !HasFilter("NoHierarchy"):
				App.ShowError("This filter can only be added with no hierarchy filter.");
				return;
			case "NoRootTaskParent" when State.RootId == 0 || App.Tasks[State.RootId].Parent == 0:
				App.ShowError("This filter can only be added with a root task which has a parent.");
				return;
			case "NoHierarchy":
				State.Expanded = false;
				ApplyExpand();
				break;
			case "NotCompleted":
				RemoveFilter("Completed");
				break;
			case "Completed":
			case "CompletedLastDay":
			case "CompletedLastWeek":
			case "CompletedLastMonth":
			case "CompletedLastYear":
				RemoveFilter("NotCompleted");
				RemoveFilter("Completed");
				RemoveFilter("CompletedLastDay");
				RemoveFilter("CompletedLastWeek");
				RemoveFilter("CompletedLastMonth");
				RemoveFilter("CompletedLastYear");
				break;
			case "WithPath":
				RemoveFilter("WithPathWithoutFirst");
				break;
			case "WithPathWithoutFirst":
				RemoveFilter("WithPath");
				break;
		}

		State.SelectedFilters.Add(filterName);
		AddFilterButton(filterName);
		App.View.Render();
		RegenerateFilterList();
	}

	private static void AddCustomFilter() {
		var lineEdit = _enterCustomFilter.GetNode<LineEdit>("%CustomFilter");
		var customFilter = "@" + lineEdit.Text;
		lineEdit.Text = "";
		State.SelectedFilters.Add(customFilter);
		AddFilterButton(customFilter);
		App.View.Render();
	}

	private static void AddFilterButton(string filterName) {
		var filterButton = new Button();
		filterButton.Name = filterName.ValidateNodeName();
		filterButton.Text = TransformName(filterName);
		filterButton.Pressed += () => RemoveFilter(filterName);
		_stateManager.AddChild(filterButton);
		_stateManager.MoveChild(filterButton, -2);
	}
	
	private static void UpdateStateButtons() {
		foreach (var child in _states.GetChildren().SkipLast(1)) {
			if (child is not Button stateButton) {
				continue;
			}
			var state = States[stateButton.Name];
			if (State.Equals(state)) {
				stateButton.AddThemeFontOverride("font", _boldFont);
			} else {
				stateButton.RemoveThemeFontOverride("font");
			}
		}
	}

	private static void AddState() {
		var lineEdit = _enterStateName.GetNode<LineEdit>("%StateName");
		var stateName = lineEdit.Text;
		lineEdit.Text = "";
		if (States.ContainsKey(stateName)) {
			App.ShowError($"There is already a state with name {stateName}.");
			return;
		} else if (stateName.ValidateNodeName() != stateName) {
			App.ShowError($"The name {stateName} contains characters that are not allowed (. : @ / \" %).");
			return;
		}

		var state = State.Clone(stateName);
		AddOrRestoreState(state);
		state.Create();
	}

	private static void AddStateButton(string stateName) {
		var stateButton = new Button();
		stateButton.Name = stateName;
		stateButton.Text = stateName;
		stateButton.Pressed += () => ChangeState(stateName);
		_states.AddChild(stateButton);
		_states.MoveChild(stateButton, -2);
	}

	private static void AskStateName() {
		_enterStateName.PopupCentered();
		_enterStateName.GetNode<LineEdit>("%StateName").GrabFocus();
	}

	private static void StateNameSubmitted(string _) {
		_enterStateName.GetOkButton().EmitSignal("pressed");
	}

	private static void CustomFilterSubmitted(string _) {
		_enterCustomFilter.GetOkButton().EmitSignal("pressed");
	}

	private static void RemoveState() {
		_states.GetNode(_stateToRemove).QueueFree();
		States[_stateToRemove].Delete();
		States.Remove(_stateToRemove);
	}

	private static void ApplyState(State newState, bool first = false) {
		if (!first) {
			foreach (var filterName in State.SelectedFilters) {
				_stateManager.GetNode(filterName.ValidateNodeName()).Free();
			}
		}
		State = newState;
		foreach (var filterName in State.SelectedFilters) {
			AddFilterButton(filterName);
		}
		
		var sort = Sort.AllSorts.First(sort => sort.Name == State.SelectedSort);
		_sortMenu.Select(Array.IndexOf(Sort.AllSorts, sort) + 1);
		
		if (!string.IsNullOrEmpty(State.GroupBy)) {
			var grouping = Group.AllGroupings.First(grouping => grouping.Name == State.GroupBy);
			_groupingMenu.Select(Array.IndexOf(Group.AllGroupings, grouping) + 1);
		} else {
			_groupingMenu.Select(0);
		}
		
		ApplyExpand();
		RegenerateFilterList();
		UpdateStateButtons();
	}

	private static void ChangeState(string stateName) {
		var newState = States[stateName];
		if (stateName != State.Default.Name && State.Equals(newState)) {
			_stateToRemove = stateName;
			_confirmStateDelete.DialogText = $"Delete state {stateName}?";
			_confirmStateDelete.PopupCentered();
			return;
		}
		
		ApplyState(newState.Clone(""));
		App.View.Render();
	}
}