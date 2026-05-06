using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.MauiBlazorApp.Shared.FormFlow
{
	public class FormStepModel
	{
		public int StepNo { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Route { get; set; } = string.Empty;
		public string PreviousRoute { get; set; } = string.Empty;
		public string NextRoute { get; set; } = string.Empty;
		public bool HasError { get; set; }
	}
}
