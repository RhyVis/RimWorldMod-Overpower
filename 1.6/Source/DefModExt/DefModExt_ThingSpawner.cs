namespace Rhynia.Overpower;

#nullable disable

public class DefModExt_ThingSpawner : DefModExtension
{
    [MustTranslate]
    public string name;
    public List<ThingCategoryDef> categories;
    public List<Search> searches;
    public List<ThingDef> spawnableDefs;
    public ThingDef defaultDef;

    public enum SearchType : byte
    {
        DefName,
        Label,
    }

    public class Search
    {
        public SearchType type;

        [MayTranslate]
        public string what;

        public List<ThingCategoryDef> categoryRestricts;
    }

    public bool Valid => spawnableDefs is not null && spawnableDefs.Any() && defaultDef is not null;

    public override void ResolveReferences(Def parentDef)
    {
        Debug($"Resolving DefModExt_ThingSpawner for {parentDef.defName}", this);
        using var _ = TimingScope.Start(
            (t) =>
                Debug(
                    $"Finished resolving DefModExt_ThingSpawner for {parentDef.defName} in {t.TotalMilliseconds} ms",
                    this
                )
        );

        spawnableDefs ??= [];
        spawnableDefs.RemoveAll(def => def is null);

        if (!categories.NullOrEmpty())
        {
            foreach (var category in categories)
            {
                Debug($"Resolving category: {category.defName}", this);
                var defs = DefDatabase<ThingDef>
                    .AllDefsListForReading.Where(def =>
                        def.thingCategories?.Contains(category) ?? false
                    )
                    .ToHashSet();
                spawnableDefs.AddRangeUnique(defs);
            }
            Debug($"Resolved spawnableDefs from categories: {spawnableDefs.Count} total", this);
        }

        if (!searches.NullOrEmpty())
        {
            Debug($"Resolving searches for {parentDef.defName}", this);
            foreach (var search in searches)
            {
                var categoryRestrict = search.categoryRestricts;
                var categoryRestrictExists = categoryRestrict is not null && categoryRestrict.Any();

                Debug(
                    $"Processing search: {search.what} ({search.type}, {categoryRestrictExists})",
                    this
                );

                HashSet<ThingDef> defs =
                [
                    .. search.type switch
                    {
                        SearchType.DefName => DefDatabase<ThingDef>.AllDefsListForReading.Where(
                            def =>
                                def.defName.ContainsIgnoreCase(search.what)
                                && (
                                    !categoryRestrictExists
                                    || (
                                        def.thingCategories?.Any(cat =>
                                            categoryRestrict.Contains(cat)
                                        ) ?? false
                                    )
                                )
                        ),
                        SearchType.Label => DefDatabase<ThingDef>.AllDefsListForReading.Where(def =>
                            def.label.ContainsIgnoreCase(search.what)
                            && (
                                !categoryRestrictExists
                                || (
                                    def.thingCategories?.Any(cat => categoryRestrict.Contains(cat))
                                    ?? false
                                )
                            )
                        ),
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(search.type),
                            search.type,
                            $"Unknown search type: {search.type}"
                        ),
                    },
                ];

                Debug($"Found {defs.Count} defs for search: {search.what}", this);
                spawnableDefs.AddRangeUnique(defs);
            }
        }

        defaultDef ??= spawnableDefs.FirstOrDefault();
        Debug($"Default def resolved: {defaultDef?.defName ?? "null!"}", this);

        if (!Valid)
        {
            var err = $"Invalid 'DefModExt_ThingSpawner' found for {parentDef.defName}";
            Error(err, this);
            throw new ArgumentException(err);
        }
    }
}
