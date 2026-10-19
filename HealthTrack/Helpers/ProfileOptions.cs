// Owner: Person A (Auth / Profiles / Onboarding)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using HealthTrack.Data;
using HealthTrack.Models;

namespace HealthTrack.Helpers
{
    public enum OptionKind { Goal, Specialisation }

    /// <summary>One option shown in the multi-select (a predefined option or the user's own custom one).</summary>
    public class OptionItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsCustom { get; set; }
        public string Token { get { return ProfileOptions.IdToken(Id); } }
    }

    /// <summary>One validated choice: either an existing option (Id) or a brand-new custom name (Id == null).</summary>
    public class OptionPick
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Token { get { return Id.HasValue ? ProfileOptions.IdToken(Id.Value) : ProfileOptions.NewToken(Name); } }
    }

    /// <summary>
    /// Everything about the goal / specialisation lists in one place: the predefined values,
    /// what a user may pick, validation of a submitted selection and turning it into rows.
    ///
    /// The form posts "tokens": "id:12" for an existing option, "new:Muay Thai" for a custom one.
    /// Custom options are saved immediately (no admin approval), are owned by the user who typed
    /// them, and are only suggested back to that user. Typing a name that already exists
    /// (any capitalisation) re-uses the existing row instead of creating a duplicate.
    /// </summary>
    public static class ProfileOptions
    {
        public const int MinSelections = 1;
        public const int MaxSelections = 15;
        public const int MaxNewCustom = 5;      // new custom options per save (stops the lookup tables being spammed)
        public const int NameMinLength = 2;
        public const int NameMaxLength = 50;    // = column length
        public const int CustomSortOrder = 1000;

        public static readonly string[] PredefinedGoals =
        {
            "Build muscle", "Lose weight", "Gain weight", "Increase strength", "Improve endurance",
            "Improve flexibility", "General fitness", "Sports performance", "Boxing conditioning"
        };

        public static readonly string[] PredefinedSpecialisations =
        {
            "Strength training", "Bodybuilding", "Weight management", "Cardio and endurance", "Functional training",
            "Mobility and flexibility", "General fitness", "Sports conditioning", "Boxing and combat fitness", "Nutrition coaching"
        };

        // Letters (any language), digits, spaces and a few joining characters. No commas: the
        // display summary is comma-separated. Must start and end with a letter or digit.
        private static readonly Regex NamePattern =
            new Regex(@"^[\p{L}\p{N}](?:[\p{L}\p{N} &'\-/+.()]*[\p{L}\p{N})])?$", RegexOptions.Compiled);
        private static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);

        public static string IdToken(int id) { return "id:" + id.ToString(CultureInfo.InvariantCulture); }
        public static string NewToken(string name) { return "new:" + name; }

        /// <summary>The numeric ids inside "id:N" tokens (other tokens are ignored).</summary>
        public static List<int> IdsIn(IEnumerable<string> tokens)
        {
            var ids = new List<int>();
            foreach (var t in tokens ?? Enumerable.Empty<string>())
            {
                int id;
                if (t != null && t.StartsWith("id:", StringComparison.Ordinal) &&
                    int.TryParse(t.Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out id)) ids.Add(id);
            }
            return ids;
        }

        /// <summary>Trim and collapse inner whitespace. Returns null for blank input.</summary>
        public static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return Spaces.Replace(name.Trim(), " ");
        }

        public static string NameError(string normalized)
        {
            if (normalized == null) return "Enter a name.";
            if (normalized.Length < NameMinLength || normalized.Length > NameMaxLength)
                return "“" + Shorten(normalized) + "” must be " + NameMinLength + "–" + NameMaxLength + " characters.";
            if (!NamePattern.IsMatch(normalized))
                return "“" + Shorten(normalized) + "” can only use letters, numbers, spaces and & ' - / + . ( )";
            return null;
        }

        private static string Shorten(string s) { return s.Length > 30 ? s.Substring(0, 30) + "…" : s; }

        /// <summary>Predefined options + the user's own custom ones + any extra ids already linked to them.</summary>
        public static List<OptionItem> Available(HealthTrackContext db, OptionKind kind, int userId, IEnumerable<int> alsoInclude = null)
        {
            var extra = (alsoInclude ?? Enumerable.Empty<int>()).Distinct().ToList();
            List<OptionItem> rows;
            if (kind == OptionKind.Goal)
            {
                rows = db.FitnessGoals
                    .Where(o => o.IsPredefined || o.CreatedByUserId == userId || extra.Contains(o.FitnessGoalId))
                    .OrderBy(o => o.SortOrder).ThenBy(o => o.Name)
                    .Select(o => new OptionItem { Id = o.FitnessGoalId, Name = o.Name, IsCustom = !o.IsPredefined })
                    .ToList();
            }
            else
            {
                rows = db.Specialisations
                    .Where(o => o.IsPredefined || o.CreatedByUserId == userId || extra.Contains(o.SpecialisationId))
                    .OrderBy(o => o.SortOrder).ThenBy(o => o.Name)
                    .Select(o => new OptionItem { Id = o.SpecialisationId, Name = o.Name, IsCustom = !o.IsPredefined })
                    .ToList();
            }
            return rows;
        }

        /// <summary>Ids currently linked to a client (ClientGoals) or trainer (TrainerSpecialisations), in saved order.</summary>
        public static List<int> LinkedIds(HealthTrackContext db, OptionKind kind, int profileId)
        {
            if (kind == OptionKind.Goal)
                return db.ClientGoals.Where(x => x.ClientId == profileId).OrderBy(x => x.Position).Select(x => x.FitnessGoalId).ToList();
            return db.TrainerSpecialisations.Where(x => x.TrainerId == profileId).OrderBy(x => x.Position).Select(x => x.SpecialisationId).ToList();
        }

        private static OptionItem FindByName(HealthTrackContext db, OptionKind kind, string name)
        {
            // SQL Server's default collation compares case-insensitively, matching the unique index.
            if (kind == OptionKind.Goal)
                return db.FitnessGoals.Where(o => o.Name == name)
                    .Select(o => new OptionItem { Id = o.FitnessGoalId, Name = o.Name, IsCustom = !o.IsPredefined }).FirstOrDefault();
            return db.Specialisations.Where(o => o.Name == name)
                .Select(o => new OptionItem { Id = o.SpecialisationId, Name = o.Name, IsCustom = !o.IsPredefined }).FirstOrDefault();
        }

        /// <summary>
        /// Validates a submitted selection. Adds errors to <paramref name="modelState"/> under
        /// <paramref name="errorKey"/> and returns the cleaned, de-duplicated picks in the user's order.
        /// Nothing is written to the database here.
        /// </summary>
        /// <param name="tokens">"id:N" / "new:Name" values posted by the picker.</param>
        /// <param name="plainCustom">Extra custom names typed into the no-JavaScript fallback boxes.</param>
        /// <param name="alreadyLinked">Ids already linked to this user's profile (allowed even if owned by someone else).</param>
        public static List<OptionPick> Validate(HealthTrackContext db, OptionKind kind, int userId,
            IEnumerable<string> tokens, IEnumerable<string> plainCustom, IEnumerable<int> alreadyLinked,
            ModelStateDictionary modelState, string errorKey)
        {
            var noun = kind == OptionKind.Goal ? "goal" : "specialisation";
            var picks = new List<OptionPick>();
            var errors = new List<string>();

            var allowed = Available(db, kind, userId, alreadyLinked);
            var byId = allowed.ToDictionary(o => o.Id);
            var byName = new Dictionary<string, OptionItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in allowed) if (!byName.ContainsKey(o.Name)) byName[o.Name] = o;

            var seenIds = new HashSet<int>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var all = (tokens ?? Enumerable.Empty<string>()).Where(t => t != null)
                .Concat((plainCustom ?? Enumerable.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(NewToken));

            foreach (var raw in all.Take(MaxSelections * 3))   // hard cap on work done for a tampered post
            {
                if (raw.StartsWith("id:", StringComparison.Ordinal))
                {
                    int id;
                    if (!int.TryParse(raw.Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out id) || !byId.ContainsKey(id))
                    {
                        errors.Add("One of the selected " + noun + "s is no longer available. Please choose again.");
                        continue;
                    }
                    if (seenIds.Add(id)) { picks.Add(new OptionPick { Id = id, Name = byId[id].Name }); seenNames.Add(byId[id].Name); }
                }
                else if (raw.StartsWith("new:", StringComparison.Ordinal))
                {
                    var name = Normalize(raw.Substring(4));
                    if (name == null) continue;                       // empty custom entry: ignore
                    var err = NameError(name);
                    if (err != null) { errors.Add(err); continue; }
                    if (seenNames.Contains(name)) continue;          // duplicate of something already picked

                    OptionItem existing;
                    if (byName.TryGetValue(name, out existing))
                    {
                        // Same as a predefined option or one of the user's own: select that one instead.
                        if (seenIds.Add(existing.Id)) picks.Add(new OptionPick { Id = existing.Id, Name = existing.Name });
                        seenNames.Add(existing.Name);
                    }
                    else
                    {
                        // Either genuinely new, or another user's private custom option with the same name.
                        // Kept as a name; GoalEntities/SpecialisationEntities re-use the existing row if there is one.
                        var other = FindByName(db, kind, name);
                        var finalName = other != null ? other.Name : name;
                        picks.Add(new OptionPick { Id = null, Name = finalName });
                        seenNames.Add(finalName);
                    }
                }
                else
                {
                    errors.Add("The selection could not be read. Please choose again.");
                }
            }

            if (picks.Count < MinSelections)
                errors.Add("Choose at least " + MinSelections + " " + noun + ".");
            if (picks.Count > MaxSelections)
                errors.Add("Choose no more than " + MaxSelections + " " + noun + "s (you chose " + picks.Count + ").");
            if (picks.Count(p => !p.Id.HasValue) > MaxNewCustom)
                errors.Add("Add no more than " + MaxNewCustom + " new custom " + noun + "s at a time.");

            foreach (var e in errors.Distinct()) modelState.AddModelError(errorKey, e);
            return picks;
        }

        /// <summary>
        /// Resolves picks to entities: existing rows are re-used (also when another user already created a
        /// custom option with the same name), genuinely new names become new rows. Call SaveChanges afterwards
        /// (the caller does it together with the profile, so everything is saved or nothing is).
        /// Returns one entity per pick, in order.
        /// </summary>
        public static List<FitnessGoal> GoalEntities(HealthTrackContext db, IEnumerable<OptionPick> picks, int userId)
        {
            var result = new List<FitnessGoal>();
            foreach (var p in picks)
            {
                if (p.Id.HasValue) { result.Add(db.FitnessGoals.Find(p.Id.Value)); continue; }
                var name = p.Name;
                var existing = db.FitnessGoals.FirstOrDefault(o => o.Name == name);   // created meanwhile / owned by someone else
                if (existing != null) { result.Add(existing); continue; }
                var row = new FitnessGoal { Name = p.Name, IsPredefined = false, SortOrder = CustomSortOrder, CreatedByUserId = userId, CreatedAt = DateTime.Now };
                db.FitnessGoals.Add(row);
                result.Add(row);
            }
            return result;
        }

        public static List<Specialisation> SpecialisationEntities(HealthTrackContext db, IEnumerable<OptionPick> picks, int userId)
        {
            var result = new List<Specialisation>();
            foreach (var p in picks)
            {
                if (p.Id.HasValue) { result.Add(db.Specialisations.Find(p.Id.Value)); continue; }
                var name = p.Name;
                var existing = db.Specialisations.FirstOrDefault(o => o.Name == name);   // created meanwhile / owned by someone else
                if (existing != null) { result.Add(existing); continue; }
                var row = new Specialisation { Name = p.Name, IsPredefined = false, SortOrder = CustomSortOrder, CreatedByUserId = userId, CreatedAt = DateTime.Now };
                db.Specialisations.Add(row);
                result.Add(row);
            }
            return result;
        }

        /// <summary>
        /// Backward-compatible display text for the old single-string columns
        /// (ClientProfile.Goal = 200 chars, TrainerProfile.Specialisation = 100 chars).
        /// </summary>
        public static string Summary(IEnumerable<string> names, int maxLength)
        {
            var text = string.Join(", ", names.Where(n => !string.IsNullOrWhiteSpace(n)));
            if (text.Length == 0) return null;
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 1).TrimEnd(' ', ',') + "…";
        }
    }
}
