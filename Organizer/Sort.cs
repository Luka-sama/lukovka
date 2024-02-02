using System;
using Godot;

public partial class Sort : GodotObject {
	public Task A;
	public Task B;
	
	public int Standard() {
		if (A.Completed == DateTime.MinValue && B.Completed != DateTime.MinValue) {
			return -1;
		} else if (A.Completed != DateTime.MinValue && B.Completed == DateTime.MinValue) {
			return 1;
		}

		var orderA = (A.Order != 0 ? A.Order : A.Id);
		var orderB = (B.Order != 0 ? B.Order : B.Id);
		return (orderA > orderB ? 1 : -1);
	}

	public int History() {
		if (A.Completed == DateTime.MinValue && B.Completed != DateTime.MinValue) {
			return 1;
		} else if (A.Completed != DateTime.MinValue && B.Completed == DateTime.MinValue) {
			return -1;
		}
		
		return Standard();
	}

	public int ByDate() {
		return ByAnyDate(A.Date, B.Date);
	}

	public int ByCompleted() {
		return ByAnyDate(A.Completed, B.Completed);
	}

	public int ByPriority() {
		if (A.Priority == B.Priority) {
			return Standard();
		}
		return (A.Priority > B.Priority ? -1 : 1);
	}

	public int Custom(string expressionString) {
		var orderA = Customizer.CalcExpression(A, expressionString, this, 0f);
		var orderB = Customizer.CalcExpression(B, expressionString, this, 0f);
		if (Mathf.IsEqualApprox(orderA, orderB)) {
			return Standard();
		}
		return (orderA > orderB ? -1 : 1);
	}

	private int ByAnyDate(DateTime dateA, DateTime dateB) {
		if (dateA != DateTime.MinValue && dateB == DateTime.MinValue) {
			return -1;
		} else if (dateA == DateTime.MinValue && dateB != DateTime.MinValue) {
			return 1;
		}

		if (dateA != dateB) {
			return (dateA > dateB ? 1 : -1);
		}

		return Standard();
	}
}