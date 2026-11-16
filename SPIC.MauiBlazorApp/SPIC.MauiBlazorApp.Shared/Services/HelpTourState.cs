using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SPIC.Core.Help;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// The running guided tour: which screen section, which step. Started from the guide page, a
    /// home tour card or the "?" button; drawn by HelpTourHost over the real page. Starting a tour
    /// for a screen the user is not on navigates there first (only routes without parameters).
    /// </summary>
    public sealed class HelpTourState
    {
        private readonly NavigationManager _nav;

        public HelpTourState(NavigationManager nav) => _nav = nav;

        public string? Module { get; private set; }
        public string? SectionId { get; private set; }
        public int Index { get; private set; }
        public bool IsRunning => Module is not null && SectionId is not null;

        public event Action? Changed;

        public Task StartAsync(HelpSectionView screen)
        {
            var steps = screen.Section.Screen?.Tour ?? Array.Empty<HelpTourStep>();
            if (steps.Count == 0) return Task.CompletedTask;

            var here = _nav.ToBaseRelativePath(_nav.Uri);
            var onScreen = screen.Section.Routes.Any(r => HelpRoutes.Matches(r, "/" + here));
            if (!onScreen)
            {
                var route = screen.Section.Routes.FirstOrDefault(r => !HelpRoutes.HasParameters(r));
                if (route is null) return Task.CompletedTask; // record screens: tour only from "?" on the record
                _nav.NavigateTo(route);
            }

            Module = screen.Module;
            SectionId = screen.Section.Id;
            Index = 0;
            Changed?.Invoke();
            return Task.CompletedTask;
        }

        public void Next(int count)
        {
            if (!IsRunning) return;
            if (Index >= count - 1) { End(); return; }
            Index++;
            Changed?.Invoke();
        }

        public void Back()
        {
            if (!IsRunning || Index == 0) return;
            Index--;
            Changed?.Invoke();
        }

        public void End()
        {
            if (!IsRunning) return;
            Module = null;
            SectionId = null;
            Index = 0;
            Changed?.Invoke();
        }
    }
}
