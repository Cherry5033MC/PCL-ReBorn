namespace PCLCS;

/// <summary>
/// MC 百科条目。
/// <para/>使用 <see cref="WikiEntry.All"/> 获取所有条目。
/// </summary>
public class WikiEntry {

    /// <summary>
    /// 在 MC 百科中的对应 ID。
    /// </summary>
    public int Id;
    /// <summary>
    /// 中文译名。
    /// <c>null</c> 代表没有翻译。
    /// </summary>
    public string? ChineseName;
    /// <summary>
    /// 各个 Mod 平台的 Slug（例如 advanced-solar-panels）。
    /// 若没有对应键则为不在该平台上。
    /// </summary>
    public Dictionary<ResourcePlatforms, string> Slugs = [];
    /// <summary>
    /// MC 百科的浏览量逆序排行，1 代表浏览量最低。
    /// </summary>
    public int Popularity;

    /// <summary>
    /// 内置数据库中的所有 MC 百科条目。
    /// </summary>
    public static readonly Lazy<List<WikiEntry>> All = new(() => {
        var dataLines = GetDataLines();

        // 没有任何数据时直接返回空列表，避免后续除零 / 越界异常
        if (dataLines.Count == 0) return [];

        // 读取最后一行的浏览量
        Queue<int> popularities = [];
        for (int i = 0; i < dataLines[^1].Length; i += 3) { // 将每 3 个字符切割成一个元素
            popularities.Enqueue(int.Parse(dataLines[^1].Substring(i, 3).ConvertRadix(86, 10)));
        }
        dataLines.RemoveAt(dataLines.Count - 1);

        // 解析每一行
        List<WikiEntry> results = [];
        var lineNumber = 0;
        foreach (var lineData in dataLines) {
            lineNumber++;
            if (lineData == "") continue;

            var popularity = popularities.Dequeue();
            foreach (var entryData in lineData.Split('¨')) {
                var entry = new WikiEntry();
                var parts = entryData.Split('|');
                var slugs = parts[0];
                if (slugs.StartsWithF("@")) {
                    entry.Slugs.Add(ResourcePlatforms.Modrinth, slugs.Replace("@", ""));
                } else if (slugs.EndsWithF("@")) {
                    entry.Slugs.Add(ResourcePlatforms.CurseForge, slugs.TrimEnd('@'));
                    entry.Slugs.Add(ResourcePlatforms.Modrinth, entry.Slugs[ResourcePlatforms.CurseForge]);
                } else if (slugs.Contains("@")) {
                    var splitSlugs = slugs.Split('@');
                    entry.Slugs.Add(ResourcePlatforms.CurseForge, splitSlugs[0]);
                    entry.Slugs.Add(ResourcePlatforms.Modrinth, splitSlugs[1]);
                } else {
                    entry.Slugs.Add(ResourcePlatforms.CurseForge, slugs);
                }

                entry.Id = lineNumber;
                entry.Popularity = popularity;
                if (parts.Length >= 2) {
                    entry.ChineseName = parts[^1]; // 最后一项
                    if (entry.ChineseName.Contains("*")) { // 处理 *
                        var englishName = entry.Slugs.Values.First().Replace("-", " ").Capitalize();
                        entry.ChineseName = entry.ChineseName.Replace("*", $" ({englishName})");
                    }
                }
                results.Add(entry);
            }
        }
        return results;
    });

    /// <summary>
    /// 读取内置的 MC 百科数据。
    /// <para/>依次尝试：PCLCS 自身的内嵌资源 → 启动器主程序的内嵌资源（Resources.resx 中的 WikiEntries）。
    /// <para/>这样即使 PCLCS 的内嵌资源在某些构建方式下丢失，也不会让整个启动器崩溃。
    /// </summary>
    private static List<string> GetDataLines() {
        try {
            return FileUtils.ReadAsLines(@"Resource\WikiEntries.txt", type: typeof(WikiEntry)).ToList();
        } catch (Exception ex) {
            Logger.Warn(ex, "读取 PCLCS 内嵌的 WikiEntries.txt 失败，尝试使用启动器内置的副本");
        }

        try {
            var assembly = typeof(WikiEntry).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("WikiEntries", StringComparison.OrdinalIgnoreCase)
                                     || n.EndsWith("WikiEntries.txt", StringComparison.OrdinalIgnoreCase));
            if (resourceName is not null) {
                using var stream = assembly.GetManifestResourceStream(resourceName)!;
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd().SplitLines().ToList();
            }
        } catch (Exception ex) {
            Logger.Warn(ex, "读取启动器内置的 WikiEntries 资源失败");
        }

        Logger.Warn("未能读取内置的 MC 百科数据，模组中文名翻译将不可用");
        return [];
    }

    public override string ToString()
        => $"{Slugs.GetOrDefault(ResourcePlatforms.CurseForge)}&{Slugs.GetOrDefault(ResourcePlatforms.Modrinth)}|{Id}|{ChineseName}，浏览量 {Popularity}";
}
