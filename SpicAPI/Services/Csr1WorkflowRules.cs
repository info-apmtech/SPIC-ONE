using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace SpicAPI.Services
{
    public static class Csr1WorkflowRules
    {
        public static DateTime AllowedProgramMonth(DateTime date) =>
            new DateTime(date.Year, date.Month, 1).AddMonths(date.Day <= 26 ? 1 : 2);
        public static bool IsThreeLevelApplication(DateTime date) => date.Day > 26;
        public static bool CanApplyThreeLevel(string role) =>
            role.Equals("MO", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("SMDO", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("SMD", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("RMDO", StringComparison.OrdinalIgnoreCase);
        public static string? NextStatus(bool three, string current, string role, string scope)
        {
            if (!three) return null;
            bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            bool state = Eq(scope, "State");
            if (Eq(current, "Validated"))
                return (state ? Eq(role, "SMM") : Eq(role, "RMM")) ? "Level1Approved" : null;
            if (Eq(current, "Level1Approved"))
                return Eq(role, "MDAdmin") ? "Level2Approved" : null;
            if (Eq(current, "Level2Approved"))
                return Eq(role, "MDHOD") || Eq(role, "CSMD-HOD") ? "Approved" : null;
            return null;
        }
    }
}
