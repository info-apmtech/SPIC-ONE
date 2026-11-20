using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.FormFlow
{
    public static class FormStepChecker
    {
        // Evaluate presence of data for all steps and mark FormStepData accordingly.
        public static async Task EvaluateStepsAsync(HttpClient http, int dealerId, bool isExistingMaintenanceFlow = false)
        {
            try
            {
                // Single consolidated server call that returns per-step completion.
                // For approved New Dealer maintenance (flow=existing-maintenance), the server evaluates
                // steps using existing maintenance validation rules when the dealer is eligible.
                var url = $"api/DealerRegistration/{dealerId}/step-completion-summary";
                if (isExistingMaintenanceFlow)
                    url += "?flow=existing-maintenance";

                var summaries = await http.GetFromJsonAsync<List<StepSummary>>(url);
                if (summaries == null)
                {
                    // Treat as all missing
                    foreach (var s in FormStepData.Steps)
                        FormStepData.MarkStepError(s.StepNo, true);
                    return;
                }

                foreach (var s in summaries)
                {
                    // API returns IsComplete; HasError = !IsComplete
                    FormStepData.MarkStepError(s.StepNo, !s.IsComplete);
                }
            }
            catch
            {
                // On any failure mark all steps as error
                foreach (var s in FormStepData.Steps)
                    FormStepData.MarkStepError(s.StepNo, true);
            }
        }

        private class StepSummary
        {
            public int StepNo { get; set; }
            public bool IsComplete { get; set; }
        }
    }
}
