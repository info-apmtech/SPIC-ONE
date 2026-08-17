using System;
using System.Collections.Generic;
using System.Linq;

namespace SPIC.MauiBlazorApp.Shared.FormFlow
{
	public static class FormStepData
	{
		public static Action? OnStateChanged;

		public static void MarkStepError(int stepNo, bool hasError)
		{
			var step = Steps.FirstOrDefault(x => x.StepNo == stepNo);
			if (step != null)
			{
				step.HasError = hasError;
				step.IsEvaluated = true;
			}

			OnStateChanged?.Invoke();
		}

		public static void ClearAllErrors()
		{
			foreach (var step in Steps)
			{
				step.HasError = false;
				step.IsEvaluated = false;
			}

			OnStateChanged?.Invoke();
		}

		public static readonly List<FormStepModel> Steps = new()
		{
			new FormStepModel
			{
				StepNo = 1,
				Title = "Market Details & Territory Coverage",
				Route = "/Register",
				PreviousRoute = "/Dashboard",
				NextRoute = "/Experience"
			},
			new FormStepModel
			{
				StepNo = 2,
				Title = "Provide detailed records of your historical agricultural partnerships and transaction volumes to establish creditworthiness.",
				Route = "/Experience",
				PreviousRoute = "/Register",
				NextRoute = "/AnnualSales"
			},
			new FormStepModel
			{
				StepNo = 3,
				Title = "Please provide the sales figures for the previous fiscal year for all companies.",
				Route = "/AnnualSales",
				PreviousRoute = "/Experience",
				NextRoute = "/Warehouse"
			},
			new FormStepModel
			{
				StepNo = 4,
				Title = "Define logistics hubs to optimize distribution networks and verify operational reach for freight subsidization.",
				Route = "/Warehouse",
				PreviousRoute = "/AnnualSales",
				NextRoute = "/MarketDetails"
			},
			new FormStepModel
			{
				StepNo = 5,
				Title = "Define the agricultural landscape and regional variables for your dealership.",
				Route = "/MarketDetails",
				PreviousRoute = "/Warehouse",
				NextRoute = "/Companies"
			},
			new FormStepModel
			{
				StepNo = 6,
				Title = "Select companies in your region to help us understand the market and provide better logistics.",
				Route = "/Companies",
				PreviousRoute = "/MarketDetails",
				NextRoute = "/Proprietor"
			},
			new FormStepModel
			{
				StepNo = 7,
				Title = "Define the legal ownership structure of your dealership.",
				Route = "/Proprietor",
				PreviousRoute = "/Companies",
				NextRoute = "/SalesPlaning"
			},
			new FormStepModel
			{
				StepNo = 8,
				Title = "Define your projected sales volume for the upcoming fiscal year (April - March).",
				Route = "/SalesPlaning",
				PreviousRoute = "/Proprietor",
				NextRoute = "/Investment"
			},
			new FormStepModel
			{
				StepNo = 9,
				Title = "Register the authorized individuals who will represent the dealership in official digital transactions and contract signings.",
				Route = "/Investment",
				PreviousRoute = "/SalesPlaning",
				NextRoute = "/CreditLimit"
			},
			new FormStepModel
			{
				StepNo = 10,
				Title = "Finalize the commercial credit ceiling based on dealer performance and collateral.",
				Route = "/CreditLimit",
				PreviousRoute = "/Investment",
				NextRoute = "/CreditLimitForGreenStar"
			},
			new FormStepModel
			{
				StepNo = 11,
				Title = "Finalize the commercial credit ceiling based on dealer performance and collateral.",
				Route = "/CreditLimitForGreenStar",
				PreviousRoute = "/CreditLimit",
				NextRoute = "/Enclosures"
			},
			new FormStepModel
			{
				StepNo = 12,
				Title = "Finalize the commercial credit ceiling based on dealer performance and collateral.",
				Route = "/Enclosures",
				PreviousRoute = "/CreditLimitForGreenStar",
				NextRoute = "/FinalSubmission"
			},
			new FormStepModel
			{
				StepNo = 13,
				Title = "Finalize the commercial credit ceiling based on dealer performance and collateral.",
				Route = "/FinalSubmission",
				PreviousRoute = "/Enclosures",
				NextRoute = "/Dashboard"
			}
		};

		private static readonly List<string> RestrictedRoutes = new()
		{
			"/Register",
			"/Proprietor",
			"/Investment"
		};

		public static List<FormStepModel> GetVisibleSteps(bool hasGreenStar, bool isRestricted = false)
		{
			if (isRestricted)
				return Steps.Where(x => RestrictedRoutes.Contains(x.Route, StringComparer.OrdinalIgnoreCase)).ToList();

			if (hasGreenStar)
				return Steps;

			return Steps
				.Where(x => !x.Route.Equals("/CreditLimitForGreenStar", StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		// Step routes remain unchanged. Only the titles change for New Dealer Creation.
		public static string GetDisplayTitle(FormStepModel step, bool isNewDealer)
		{
			if (!isNewDealer)
				return step.Title;

			return step.StepNo switch
			{
				10 => "Provide the SPIC dealership application fee and trade deposit details.",
				11 => "Provide the GFL trade deposit details.",
				12 => "Upload and verify all required dealership documents.",
				13 => "Review the complete application before final submission.",
				_ => step.Title
			};
		}

		public static FormStepModel? GetStepByRoute(string route)
		{
			return Steps.FirstOrDefault(x =>
				x.Route.Equals(route, StringComparison.OrdinalIgnoreCase));
		}

		public static string GetNextRoute(string currentRoute, bool hasGreenStar, bool isRestricted = false)
		{
			if (isRestricted)
			{
				if (currentRoute.Equals("/Register", StringComparison.OrdinalIgnoreCase))
					return "/Proprietor";
				if (currentRoute.Equals("/Proprietor", StringComparison.OrdinalIgnoreCase))
					return "/Investment";
				if (currentRoute.Equals("/Investment", StringComparison.OrdinalIgnoreCase))
					return "/Dashboard";

				return GetStepByRoute(currentRoute)?.NextRoute ?? "/Dashboard";
			}

			if (currentRoute.Equals("/CreditLimit", StringComparison.OrdinalIgnoreCase))
				return hasGreenStar ? "/CreditLimitForGreenStar" : "/Enclosures";

			return GetStepByRoute(currentRoute)?.NextRoute ?? "/Dashboard";
		}

		public static string GetPreviousRoute(string currentRoute, bool hasGreenStar, bool isRestricted = false)
		{
			if (isRestricted)
			{
				if (currentRoute.Equals("/Investment", StringComparison.OrdinalIgnoreCase))
					return "/Proprietor";
				if (currentRoute.Equals("/Proprietor", StringComparison.OrdinalIgnoreCase))
					return "/Register";
				if (currentRoute.Equals("/Register", StringComparison.OrdinalIgnoreCase))
					return "/Dashboard";

				return GetStepByRoute(currentRoute)?.PreviousRoute ?? "/Dashboard";
			}

			if (currentRoute.Equals("/Enclosures", StringComparison.OrdinalIgnoreCase))
				return hasGreenStar ? "/CreditLimitForGreenStar" : "/CreditLimit";

			return GetStepByRoute(currentRoute)?.PreviousRoute ?? "/Dashboard";
		}
	}
}