''' <summary>
''' Mod 前置（依赖项）的自动补全。
''' 基于 Modrinth 与 CurseForge 提供的前置信息，找出目标版本缺失的前置，并为它们选择合适的版本。
''' </summary>
Public Module ModDependency

#Region "安全声明"

    ''' <summary>
    ''' 前置自动补全的安全声明。
    ''' PCL 只能依靠游戏版本与 Mod 加载器推断前置的版本，无法判断 Mod 之间的特殊版本绑定，
    ''' 因此凡是展示或执行前置补全的位置，都应当同时展示该文本。
    ''' </summary>
    Public Const SafetyNotice As String = "一些特殊的模组需要特殊版本的前置（如MTR的附属需要对应的MTR模组版本），PCL ReBorn暂时无法判断版本，请注意"

#End Region

#Region "数据结构"

    ''' <summary>
    ''' 一项待补全的前置。
    ''' </summary>
    Public Class DependencyItem
        ''' <summary>
        ''' 前置对应的工程。
        ''' </summary>
        Public ReadOnly Project As ResourceProject
        ''' <summary>
        ''' 为该前置选定的版本。
        ''' </summary>
        Public ReadOnly Version As ResourceVersion
        ''' <summary>
        ''' 需要该前置的 Mod 的名称。
        ''' </summary>
        Public ReadOnly RequiredBy As String

        Public Sub New(Project As ResourceProject, Version As ResourceVersion, RequiredBy As String)
            Me.Project = Project
            Me.Version = Version
            Me.RequiredBy = RequiredBy
        End Sub

        Public Overrides Function ToString() As String
            Dim Result As String = If(Project?.TranslatedName, "未知前置")
            Dim DisplayVersion As String = If(Version?.Version, Version?.Display)
            If Not String.IsNullOrEmpty(DisplayVersion) Then Result &= " " & DisplayVersion
            Return Result
        End Function
    End Class

    ''' <summary>
    ''' 一项待检查的前置工程。
    ''' </summary>
    Private Class DependencyRequest
        Public ReadOnly ProjectId As String
        Public ReadOnly RequiredBy As String
        Public Sub New(ProjectId As String, RequiredBy As String)
            Me.ProjectId = ProjectId
            Me.RequiredBy = RequiredBy
        End Sub
    End Class

#End Region

#Region "主流程"

    ''' <summary>
    ''' 找出目标版本缺失的前置。该方法不会进行任何用户交互。
    ''' </summary>
    ''' <param name="Sources">需要检查的 Mod 版本列表。</param>
    ''' <param name="Platform">这些 Mod 的来源平台，用于获取前置的工程信息。</param>
    ''' <param name="Instance">安装目标版本，用于确定前置应有的游戏版本与 Mod 加载器。</param>
    ''' <param name="ExcludedProjectIds">不应被视作前置的工程 ID，一般为正在安装的 Mod 自身。</param>
    Public Function FindMissingDependencies(Sources As IEnumerable(Of ResourceVersion), Platform As ResourcePlatforms,
                                            Instance As McInstance, Optional ExcludedProjectIds As IEnumerable(Of String) = Nothing) As List(Of DependencyItem)
        Dim Result As New List(Of DependencyItem)
        If Sources Is Nothing OrElse Instance Is Nothing Then Return Result
        Dim SourceList As List(Of ResourceVersion) = Sources.Where(Function(s) s IsNot Nothing).ToList()
        If Not SourceList.Any() Then Return Result

        '整理排除项
        Dim Excluded As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If ExcludedProjectIds IsNot Nothing Then
            For Each ProjectId In ExcludedProjectIds
                If Not String.IsNullOrWhiteSpace(ProjectId) Then Excluded.Add(ProjectId)
            Next
        End If

        '获取本地已经安装的 Mod 信息
        Dim ModsFolder As String = Instance.PathIndie & "mods\"
        Dim LocalNames As List(Of String) = GetLocalModNames(ModsFolder)
        Dim InstalledIds As List(Of String) = GetInstalledProjectIds(ModsFolder)

        '初始化待检查队列
        Dim Pending As New List(Of DependencyRequest)
        For Each Source In SourceList
            For Each ProjectId In Source.DependencyIds
                Pending.Add(New DependencyRequest(ProjectId, Source.Display))
            Next
        Next
        If Not Pending.Any() Then Return Result

        '逐层检查前置，包含前置的前置
        Dim Checked As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Do While Pending.Any()
            Dim Request As DependencyRequest = Pending(0)
            Pending.RemoveAt(0)
            If String.IsNullOrWhiteSpace(Request.ProjectId) Then Continue Do
            If Excluded.Contains(Request.ProjectId) Then Continue Do
            If Not Checked.Add(Request.ProjectId) Then Continue Do

            '获取前置的工程信息
            Dim TargetProject As ResourceProject = Nothing
            Try
                TargetProject = GetProject(Request.ProjectId, Platform)
            Catch ex As Exception
                Logger.Warn(ex, $"获取前置工程 {Request.ProjectId} 的信息失败")
            End Try
            If TargetProject Is Nothing Then Continue Do

            '获取前置的全部版本
            Dim Versions As List(Of ResourceVersion) = Nothing
            Try
                Versions = ResourceVersion.FromProjectId(TargetProject.Id, TargetProject.Platform, LoadDependencies:=False)
            Catch ex As Exception
                Logger.Warn(ex, $"获取前置 {TargetProject.RawName} 的版本列表失败")
            End Try
            If Versions Is Nothing OrElse Not Versions.Any() Then Continue Do

            '已经安装过的前置不再重复下载
            If IsInstalled(TargetProject, Versions, InstalledIds, LocalNames) Then
                Logger.Info($"前置 {TargetProject.RawName} 已经安装，跳过补全")
                Continue Do
            End If

            '选取与当前版本兼容的前置版本
            Dim TargetVersion As ResourceVersion = GetSuitableVersion(Versions, Instance)
            If TargetVersion Is Nothing Then
                Logger.Info($"前置 {TargetProject.RawName} 没有与 {Instance.Version.VanillaName}（{Instance.Version.ModLoaders}）兼容的版本，跳过补全")
                Continue Do
            End If

            Result.Add(New DependencyItem(TargetProject, TargetVersion, Request.RequiredBy))
            Logger.Info($"发现缺失的前置：{TargetProject.RawName}（{TargetVersion.FileName}），需要它的 Mod：{Request.RequiredBy}")

            '继续检查前置自己的前置
            For Each ProjectId In TargetVersion.DependencyIds
                Pending.Add(New DependencyRequest(ProjectId, TargetProject.TranslatedName))
            Next
        Loop
        Return Result
    End Function

#End Region

#Region "界面交互"

    ''' <summary>
    ''' 询问用户是否补全前置。
    ''' </summary>
    Public Function ConfirmDependencyDownload(Items As List(Of DependencyItem)) As Boolean
        If Items Is Nothing OrElse Not Items.Any() Then Return False
        Dim Text As New StringBuilder
        Text.AppendLine($"检测到 {Items.Count} 个缺失的前置，是否一并下载？")
        Text.AppendLine()
        For Each Item In Items.Take(10)
            Text.AppendLine("  · " & Item.ToString())
        Next
        If Items.Count > 10 Then Text.AppendLine($"  · 以及另外 {Items.Count - 10} 个前置")
        Text.AppendLine()
        Text.Append("注意：").Append(SafetyNotice)
        '可能在后台线程调用，需要确保弹窗在 UI 线程弹出
        Return RunInUiWait(Function() MyMsgBox(Text.ToString, "补全 Mod 前置", "一并下载前置", "跳过前置")) = 1
    End Function

    ''' <summary>
    ''' 将前置项转换为下载请求，下载到指定的 Mods 文件夹。
    ''' </summary>
    Public Function BuildDependencyNetFiles(Items As IEnumerable(Of DependencyItem), ModsFolder As String) As List(Of NetFile)
        Dim Result As New List(Of NetFile)
        If Items Is Nothing OrElse String.IsNullOrWhiteSpace(ModsFolder) Then Return Result
        If Not ModsFolder.EndsWithF("\") Then ModsFolder &= "\"
        For Each Item In Items
            If Item Is Nothing OrElse Item.Version Is Nothing Then Continue For
            If Not Item.Version.DownloadAvailable Then Continue For
            Try
                Result.Add(Item.Version.ToNetFile(ModsFolder, ResourceVersion.DownloadReason.Dependency))
            Catch ex As Exception
                Logger.Warn(ex, $"生成前置 {Item.Project?.RawName} 的下载信息失败")
            End Try
        Next
        Return Result
    End Function

#End Region

#Region "工具方法"

    ''' <summary>
    ''' 获取前置工程的信息，优先使用缓存。
    ''' </summary>
    Private Function GetProject(ProjectId As String, Platform As ResourcePlatforms) As ResourceProject
        If ResourceProject.Cache.ContainsKey(ProjectId) Then Return ResourceProject.Cache(ProjectId)
        Dim TargetPlatform As ResourcePlatforms = Platform
        If Not TargetPlatform.Flags.IsSingle() Then TargetPlatform = TargetPlatform.Flags.First()
        Return ResourceProject.FromProjectId(ProjectId, TargetPlatform)
    End Function

    ''' <summary>
    ''' 获取 Mods 文件夹中所有 Mod 的文件名，不含扩展名与 .disabled、.old 后缀。
    ''' </summary>
    Public Function GetLocalModNames(ModsFolder As String) As List(Of String)
        Dim Result As New List(Of String)
        Try
            If Not DirectoryUtils.Exists(ModsFolder) Then Return Result
            For Each FilePath In DirectoryUtils.EnumerateFiles(ModsFolder, True)
                Dim FileName As String = PathUtils.GetFileNameWithoutExtension(FilePath)
                If FileName.EndsWithF(".disabled") OrElse FileName.EndsWithF(".old") Then
                    FileName = PathUtils.GetFileNameWithoutExtension(FileName)
                End If
                If Not String.IsNullOrWhiteSpace(FileName) Then Result.Add(FileName)
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"读取 Mods 文件夹失败：{ModsFolder}")
        End Try
        Return Result
    End Function

    ''' <summary>
    ''' 获取 Mods 文件夹中已安装 Mod 对应的工程 ID。
    ''' 只有在 Mod 管理页面加载过该文件夹时才能读取到，否则返回空列表。
    ''' </summary>
    Public Function GetInstalledProjectIds(ModsFolder As String) As List(Of String)
        Dim Result As New List(Of String)
        Try
            If LocalResourceLoader.State <> LoadState.Finished Then Return Result
            If LocalResourceLoader.Input Is Nothing Then Return Result
            If Not PathUtils.IsSame(LocalResourceLoader.Input, ModsFolder) Then Return Result
            If LocalResourceLoader.Output Is Nothing Then Return Result
            For Each Entry In LocalResourceLoader.Output
                If Entry IsNot Nothing AndAlso Entry.Project IsNot Nothing Then Result.Add(Entry.Project.Id)
            Next
        Catch ex As Exception
            Logger.Warn(ex, "获取已安装 Mod 的工程信息失败")
        End Try
        Return Result
    End Function

    ''' <summary>
    ''' 判断某个前置是否已经安装在 Mods 文件夹中。
    ''' </summary>
    Private Function IsInstalled(Project As ResourceProject, Versions As IEnumerable(Of ResourceVersion),
                                 InstalledIds As IEnumerable(Of String), LocalNames As IEnumerable(Of String)) As Boolean
        If Project Is Nothing Then Return False
        If InstalledIds.Any(Function(Id) String.Equals(Id, Project.Id, StringComparison.OrdinalIgnoreCase)) Then Return True
        '按文件名判断：本地文件可能被 PCL 重命名过，所以使用包含判断
        For Each File In Versions
            If File Is Nothing OrElse String.IsNullOrEmpty(File.FileName) Then Continue For
            Dim BaseName As String = PathUtils.GetFileNameWithoutExtension(File.FileName)
            If BaseName.Length < 8 Then Continue For '文件名过短容易误判
            For Each LocalName In LocalNames
                If LocalName.ContainsIgnoreCase(BaseName) Then Return True
            Next
        Next
        Return False
    End Function

    ''' <summary>
    ''' 从版本列表中挑选与目标版本兼容的版本，优先正式版，其次发布时间最新的。
    ''' </summary>
    Private Function GetSuitableVersion(Versions As IEnumerable(Of ResourceVersion), Instance As McInstance) As ResourceVersion
        Dim GameVersion As String = Instance.Version.VanillaName
        Dim Loaders As ModLoaders = Instance.Version.ModLoaders
        Dim Candidates As New List(Of ResourceVersion)
        For Each File In Versions
            If File Is Nothing OrElse Not File.DownloadAvailable Then Continue For
            '游戏版本：允许缺少版本信息或标记为未知版本的情况
            If File.GameVersions IsNot Nothing AndAlso File.GameVersions.Any() AndAlso
               Not File.GameVersions.Contains(GameVersion) AndAlso Not File.GameVersions.Contains("未知版本") Then Continue For
            'Mod 加载器
            If File.ModLoaders <> ModLoaders.None AndAlso Loaders <> ModLoaders.None AndAlso
               Not File.ModLoaders.Flags.Intersect(Loaders.Flags).Any() Then Continue For
            Candidates.Add(File)
        Next
        If Not Candidates.Any() Then Return Nothing
        Return Candidates.OrderBy(Function(f) CInt(f.ReleaseType)).ThenByDescending(Function(f) f.ReleaseDate).First()
    End Function

#End Region

End Module
