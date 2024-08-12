using System;
using System.Collections.Generic;
using System.Linq;

namespace QobuzDownloaderX.Shared.Tools
{
    internal class PerformersParser
    {
        private readonly Dictionary<string, List<string>> _performers;

        public PerformersParser(string performersFullString)
        {
            _performers = new Dictionary<string, List<string>>();

            if (!string.IsNullOrEmpty(performersFullString))
            {
                _performers = performersFullString
                    .Split(new string[] { " - " }, StringSplitOptions.None) // Split performers by " - " because some roles include '-'
                    .Select(performer => performer.Split(',')) // Split name & roles in best effort by ',', first part is name, next parts roles
                    .GroupBy(parts => parts[0].Trim()) // Group performers by name since they can occure multiple times
                    .ToDictionary(group => group.Key,
                                  group => group.SelectMany(parts => parts.Skip(1).Select(role => role.Trim())).Distinct().ToList()); // Flatten roles by performer and remove duplicates
            }
        }

        public string[] GetPerformersWithRole(InvolvedPersonRoleType role)
        {
            var roleStrings = InvolvedPersonRoleMapping.GetStringsByRole(role);
            return _performers.Keys
                .Where(key => _performers[key].Exists(value => roleStrings.Contains(value, StringComparer.OrdinalIgnoreCase)))
                .ToArray();
        }
    }

}