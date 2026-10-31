'由于包含加解密等安全信息，本文件中的部分代码已被删除

Friend Module ModSecret

    '标注 PCL 的不同分支，仅用于替换标记
    Public Const VersionBranchMain As String = "ReBorn"
    '在开源版的注册表与常规版的注册表隔离，以防数据冲突
    Public Const RegFolder As String = "PCLDebug"
    '用于微软登录的 ClientId（从源码构建时默认为空，不内置）
    Public OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "")
    'CurseForge API Key
    Public CurseForgeAPIKey As String = If(Environment.GetEnvironmentVariable("PCL_CURSEFORGE_API_KEY"), "")
    '用于匿名数据收集的腾讯云日志服务上报 URL，形如 https://{region}.cls.tencentcs.com/track?topic_id={topic_id}
    Public Const ClsBaseUrl As String = ""

#Region "AI 日志分析"

    '====================================================================
    ' 崩溃窗口的「分析日志」按钮使用这里的配置，把日志发给 OpenAI 兼容接口。
    ' 只有下面三项需要你自己填，其余代码不用动。
    '====================================================================

    '接口请求地址（域名部分），需要带上版本号前缀，结尾不要加斜杠。示例：
    '  https://api.openai.com/v1
    '  https://api.deepseek.com/v1
    '  https://dashscope.aliyuncs.com/compatible-mode/v1
    '  https://你的中转域名/v1
    Public Const AiApiBaseUrl As String = ""

    '接口密钥，形如 sk-xxxxxxxx。留空时会改为读取环境变量 PCL_AI_API_KEY
    Public Const AiApiKey As String = ""

    '使用的模型名称，示例：gpt-4o-mini、gpt-4.1-mini、deepseek-chat、qwen-plus
    Public Const AiModel As String = "gpt-4o-mini"

    '单次请求的最长等待时间（毫秒）。日志较长或模型较慢时可适当调大
    Public Const AiTimeout As Integer = 100000

    ''' <summary>
    ''' 实际使用的请求地址，已去掉结尾的斜杠。
    ''' </summary>
    Friend ReadOnly Property AiApiBaseUrlNow As String
        Get
            Return If(AiApiBaseUrl, "").Trim().TrimEnd("/"c)
        End Get
    End Property

    ''' <summary>
    ''' 实际使用的密钥：优先取代码内填写的值，其次取环境变量 PCL_AI_API_KEY。
    ''' </summary>
    Friend ReadOnly Property AiApiKeyNow As String
        Get
            Dim Key As String = If(AiApiKey, "").Trim()
            If Key.Length > 0 Then Return Key
            Return If(Environment.GetEnvironmentVariable("PCL_AI_API_KEY"), "").Trim()
        End Get
    End Property

    ''' <summary>
    ''' 实际使用的模型名称，未填写时回退到 gpt-4o-mini。
    ''' </summary>
    Friend ReadOnly Property AiModelNow As String
        Get
            Dim ModelName As String = If(AiModel, "").Trim()
            If ModelName.Length > 0 Then Return ModelName
            Return "gpt-4o-mini"
        End Get
    End Property

#End Region

#Region "仓库地址"

    '====================================================================
    ' 双平台仓库：GitHub 为主站，Gitee 作为国内备选。
    ' 两边的地址都写死在这里，更新与反馈会自动同时查询两个平台。
    '====================================================================

    'GitHub 仓库
    Friend Const RepoOwnerGitHub As String = "Cherry5033MC"
    Friend Const RepoNameGitHub As String = "PCL-ReBorn"

    'Gitee 仓库
    Friend Const RepoOwnerGitee As String = "Cherry5033MC_gitee"
    Friend Const RepoNameGitee As String = "PCL-ReBorn"

    'GitHub 相关地址
    Friend ReadOnly Property RepoHomeGitHub As String
        Get
            Return "https://github.com/" & RepoOwnerGitHub & "/" & RepoNameGitHub
        End Get
    End Property
    Friend ReadOnly Property RepoIssueNewGitHub As String
        Get
            Return RepoHomeGitHub & "/issues/new/choose"
        End Get
    End Property
    Friend ReadOnly Property RepoIssueListGitHub As String
        Get
            Return RepoHomeGitHub & "/issues"
        End Get
    End Property
    Friend ReadOnly Property RepoReleaseLatestGitHub As String
        Get
            Return RepoHomeGitHub & "/releases/latest"
        End Get
    End Property
    Friend ReadOnly Property RepoReleaseApiGitHub As String
        Get
            Return "https://api.github.com/repos/" & RepoOwnerGitHub & "/" & RepoNameGitHub & "/releases/latest"
        End Get
    End Property
    Friend ReadOnly Property RepoLogRawGitHub As String
        Get
            Return "https://raw.githubusercontent.com/" & RepoOwnerGitHub & "/" & RepoNameGitHub & "/main/UpdateLog.txt"
        End Get
    End Property

    'Gitee 相关地址
    Friend ReadOnly Property RepoHomeGitee As String
        Get
            Return "https://gitee.com/" & RepoOwnerGitee & "/" & RepoNameGitee
        End Get
    End Property
    Friend ReadOnly Property RepoIssueNewGitee As String
        Get
            Return RepoHomeGitee & "/issues/new"
        End Get
    End Property
    Friend ReadOnly Property RepoIssueListGitee As String
        Get
            Return RepoHomeGitee & "/issues"
        End Get
    End Property
    Friend ReadOnly Property RepoReleaseLatestGitee As String
        Get
            Return RepoHomeGitee & "/releases/latest"
        End Get
    End Property
    Friend ReadOnly Property RepoReleaseApiGitee As String
        Get
            Return "https://gitee.com/api/v5/repos/" & RepoOwnerGitee & "/" & RepoNameGitee & "/releases/latest"
        End Get
    End Property
    Friend ReadOnly Property RepoLogRawGitee As String
        Get
            Return "https://gitee.com/" & RepoOwnerGitee & "/" & RepoNameGitee & "/raw/main/UpdateLog.txt"
        End Get
    End Property

#End Region

#Region "网络鉴权"

    '已开启下载功能（ReBron 版）
    Public Const RestrictMcDownload As Boolean = False
    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")  '#4951
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.pcl2.server/")
        If Url.Contains("api.curseforge.com") OrElse Url.Contains("forgecdn.net") Then Req.Headers.Add("x-api-key", CurseForgeAPIKey)
    End Sub

#End Region

#Region "主题"

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = 0
    Public ThemeDontClick As Integer = 0

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Try
            If ThemeNow = NewTheme AndAlso NewTheme >= 0 Then Return
            If NewTheme >= 0 Then ThemeNow = NewTheme

            Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
            Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
            Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
            Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
            Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
            Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
            Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
            Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
            ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
            ColorBg1 = New MyColor(190, Color7)

            ColorSemiTransparent = New MyColor(1, Color8)
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(Color1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(Color2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(Color3)
            Application.Current.Resources("ColorBrush4") = New SolidColorBrush(Color4)
            Application.Current.Resources("ColorBrush5") = New SolidColorBrush(Color5)
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(Color6)
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(Color7)
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(Color8)
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorBg0)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(ColorBg1)
            Application.Current.Resources("ColorObject1") = CType(Color1, Color)
            Application.Current.Resources("ColorObject2") = CType(Color2, Color)
            Application.Current.Resources("ColorObject3") = CType(Color3, Color)
            Application.Current.Resources("ColorObject4") = CType(Color4, Color)
            Application.Current.Resources("ColorObject5") = CType(Color5, Color)
            Application.Current.Resources("ColorObject6") = CType(Color6, Color)
            Application.Current.Resources("ColorObject7") = CType(Color7, Color)
            Application.Current.Resources("ColorObject8") = CType(Color8, Color)
            Application.Current.Resources("ColorObjectBg0") = CType(ColorBg0, Color)
            Application.Current.Resources("ColorObjectBg1") = CType(ColorBg1, Color)
            ThemeRefreshMain()
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub
    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If Not FrmMain.IsLoaded Then Return
            '顶部条背景
            Dim Brush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            Dim Deltas = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + Deltas(0), ColorSat, 48 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + Deltas(1), ColorSat, 54 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + Deltas(2), ColorSat, 48 + ColorLightAdjust)})
            FrmMain.PanTitle.Background = Brush
            FrmMain.PanTitle.Background.Freeze()
            '主页面背景
            If Settings.Get(Of Boolean)("UiBackgroundColorful") Then
                Brush = New LinearGradientBrush With {.EndPoint = New Point(0.1, 1), .StartPoint = New Point(0.9, 0)}
                Brush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 15, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 15, ColorSat * 0.8, 91)})
                FrmMain.PanForm.Background = Brush
            Else
                FrmMain.PanForm.Background = New MyColor(245, 245, 245)
            End If
            FrmMain.PanForm.Background.Freeze()
        End Sub)
    End Sub
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function

#End Region

#Region "更新"

    '基于 GitHub / Gitee Releases 的自动更新：两站同时查询，取版本号较高的一边
    '开源版本（源码构建）不包含自动更新，仅作者发布的成品提供
    Friend IsUpdateWaitingRestart As Boolean = False
    '更新源标识，与 UpdateGetLatest 返回的 Item3 对应
    Friend Const UpdateSourceGitHub As Integer = 0
    Friend Const UpdateSourceGitee As Integer = 1

    ''' <summary>当前版本的完整标签，如 2.99.0.0-reborn.1.0.0.0(1.b)。</summary>
    Private ReadOnly Property CurrentVersionTag As String
        Get
            Return VersionFullTag
        End Get
    End Property

    ''' <summary>
    ''' 解析 ReBorn 版本标签（如 2.99.0.0-reborn.1.0.0.0(1.b)）。
    ''' 结构：主版本号(-ReBorn修订版本号(次版本号.补丁字母))。
    ''' </summary>
    Private Function ParseVersionTag(Tag As String, ByRef BaseVer As Version, ByRef RebornVer As Version, ByRef PatchNum As Integer, ByRef PatchLetter As Integer) As Boolean
        Dim T As String = Tag.Trim().TrimStart("v"c, "V"c)
        Dim BasePart As String = T, Rest As String = ""
        Dim Idx As Integer = T.IndexOf("-reborn", StringComparison.OrdinalIgnoreCase)
        If Idx >= 0 Then
            BasePart = T.Substring(0, Idx)
            Rest = T.Substring(Idx + 7).TrimStart("."c)
        End If
        Dim RebornPart As String = Rest
        PatchNum = 0 : PatchLetter = 0
        Dim Paren As Integer = Rest.IndexOf("("c)
        If Paren >= 0 Then
            RebornPart = Rest.Substring(0, Paren)
            Dim Inner As String = Rest.Substring(Paren).Trim("("c, ")"c)
            Dim Segs As String() = Inner.Split("."c)
            If Segs.Length > 0 Then Integer.TryParse(Segs(0).Trim(), PatchNum)
            If Segs.Length > 1 AndAlso Segs(1).Trim().Length > 0 Then
                PatchLetter = Asc(Char.ToLower(Segs(1).Trim()(0))) - Asc("a"c) + 1
                If PatchLetter < 0 Then PatchLetter = 0
            End If
        End If
        BaseVer = Nothing
        RebornVer = New Version(0, 0, 0, 0)
        If Not Version.TryParse(BasePart, BaseVer) Then Return False
        Version.TryParse(RebornPart, RebornVer)
        Return True
    End Function

    ''' <summary>TagA 是否比 TagB 更新？比较顺序：主版本号 → ReBorn 版本 → 次版本号 → 补丁字母。</summary>
    Private Function IsVersionTagNewer(TagA As String, TagB As String) As Boolean
        Dim A1 As Version = Nothing
        Dim B1 As Version = Nothing
        Dim A2 As Version = Nothing
        Dim B2 As Version = Nothing
        Dim A3 As Integer = 0
        Dim B3 As Integer = 0
        Dim A4 As Integer = 0
        Dim B4 As Integer = 0
        If Not ParseVersionTag(TagA, A1, A2, A3, A4) Then Return False
        If Not ParseVersionTag(TagB, B1, B2, B3, B4) Then Return False
        If A1 <> B1 Then Return A1 > B1
        If A2 <> B2 Then Return A2 > B2
        If A3 <> B3 Then Return A3 > B3
        Return A4 > B4
    End Function

    ''' <summary>
    ''' 查询单个更新源。IsGitee 为 True 时查 Gitee，否则查 GitHub。
    ''' 返回 (完整 Tag, 下载地址, 源标识)，失败时抛出异常。
    ''' </summary>
    Private Function UpdateQuerySource(IsGitee As Boolean) As Tuple(Of String, String, Integer)
        Dim ApiUrl As String = If(IsGitee, RepoReleaseApiGitee, RepoReleaseApiGitHub)
        'Gitee 在国内通常更快，给更短的超时；GitHub 留一点余量
        Dim TimeoutMs As Integer = If(IsGitee, 8000, 10000)
        Dim Raw As String = NetRequestByLoader(ApiUrl, Timeout:=TimeoutMs, IsJson:=True)
        Dim Json As JObject = JObject.Parse(Raw)
        '版本号
        Dim TagToken As JToken = Json("tag_name")
        Dim Tag As String = If(TagToken Is Nothing, "", TagToken.ToString().Trim())
        If Tag = "" Then Throw New Exception("返回内容中没有版本号")
        '在附件中寻找 .exe
        Dim DownloadUrl As String = ""
        Dim Assets As JToken = Json("assets")
        If Assets IsNot Nothing Then
            For Each Asset As JToken In Assets
                Dim NameToken As JToken = Asset("name")
                Dim Name As String = If(NameToken Is Nothing, "", NameToken.ToString().Trim())
                If Name.EndsWithF(".exe", True) Then
                    Dim UrlToken As JToken = Asset("browser_download_url")
                    Dim Url As String = If(UrlToken Is Nothing, "", UrlToken.ToString().Trim())
                    If Url <> "" Then
                        DownloadUrl = Url
                        Exit For
                    End If
                End If
            Next
        End If
        If DownloadUrl = "" Then Throw New Exception("最新版本中没有找到 .exe 附件")
        Return New Tuple(Of String, String, Integer)(Tag, DownloadUrl, If(IsGitee, UpdateSourceGitee, UpdateSourceGitHub))
    End Function

    ''' <summary>单个源查询的结果容器，用于并行查询时不让异常跨线程抛出。</summary>
    Private Class UpdateQueryOutcome
        Public Result As Tuple(Of String, String, Integer) = Nothing
        Public ErrorMessage As String = ""
    End Class

    ''' <summary>查询单个源并把异常转成错误信息，方便并行调用。</summary>
    Private Function UpdateQuerySafe(IsGitee As Boolean) As UpdateQueryOutcome
        Dim Outcome As New UpdateQueryOutcome
        Try
            Outcome.Result = UpdateQuerySource(IsGitee)
        Catch ex As Exception
            Outcome.ErrorMessage = ex.Message
            Logger.Info(If(IsGitee, "Gitee", "GitHub") & " 更新源查询失败：" & ex.Message)
        End Try
        Return Outcome
    End Function

    ''' <summary>
    ''' 同时查询 GitHub 与 Gitee 的最新版本，返回版本号较高的一边。
    ''' 两边版本号相同时优先 Gitee（国内访问更快）。
    ''' 两个请求并行发出，避免国内访问 GitHub 超时拖慢 Gitee。
    ''' 返回 (完整 Tag, 下载地址, 源标识)。
    ''' </summary>
    Private Function UpdateGetLatest() As Tuple(Of String, String, Integer)
        '并行查询
        Dim TaskGithub As Task(Of UpdateQueryOutcome) = Task.Run(Function() UpdateQuerySafe(False))
        Dim TaskGitee As Task(Of UpdateQueryOutcome) = Task.Run(Function() UpdateQuerySafe(True))
        '等待结果（NetRequest 自身有超时，这里不会无限等待）
        Dim OutcomeGithub As UpdateQueryOutcome = Nothing
        Dim OutcomeGitee As UpdateQueryOutcome = Nothing
        Try
            OutcomeGithub = TaskGithub.Result
        Catch ex As Exception
            Logger.Info("等待 GitHub 查询结果失败：" & ex.Message)
        End Try
        Try
            OutcomeGitee = TaskGitee.Result
        Catch ex As Exception
            Logger.Info("等待 Gitee 查询结果失败：" & ex.Message)
        End Try
        '取出结果与错误信息
        Dim GithubResult As Tuple(Of String, String, Integer) = Nothing
        Dim GiteeResult As Tuple(Of String, String, Integer) = Nothing
        Dim GithubError As String = "未返回结果"
        Dim GiteeError As String = "未返回结果"
        If OutcomeGithub IsNot Nothing Then
            GithubResult = OutcomeGithub.Result
            If GithubResult Is Nothing Then GithubError = If(OutcomeGithub.ErrorMessage = "", "未知错误", OutcomeGithub.ErrorMessage)
        End If
        If OutcomeGitee IsNot Nothing Then
            GiteeResult = OutcomeGitee.Result
            If GiteeResult Is Nothing Then GiteeError = If(OutcomeGitee.ErrorMessage = "", "未知错误", OutcomeGitee.ErrorMessage)
        End If
        '两边都失败
        If GithubResult Is Nothing AndAlso GiteeResult Is Nothing Then
            Throw New Exception("无法连接更新服务器。" & vbCrLf &
                                "GitHub：" & GithubError & vbCrLf &
                                "Gitee：" & GiteeError)
        End If
        '只有一边成功
        If GithubResult Is Nothing Then Return GiteeResult
        If GiteeResult Is Nothing Then Return GithubResult
        '两边都成功：版本号相同优先 Gitee，否则取更高的
        If GiteeResult.Item1 = GithubResult.Item1 Then Return GiteeResult
        If IsVersionTagNewer(GiteeResult.Item1, GithubResult.Item1) Then Return GiteeResult
        Return GithubResult
    End Function

    ''' <summary>
    ''' 获取 UpdateLog.txt 的更新日志内容。优先从指定源取，失败时尝试另一个源。
    ''' 都失败时返回空字符串。
    ''' </summary>
    Private Function GetUpdateLog(Source As Integer) As String
        Dim Urls As New List(Of String)
        If Source = UpdateSourceGitee Then
            Urls.Add(RepoLogRawGitee)
            Urls.Add(RepoLogRawGitHub)
        Else
            Urls.Add(RepoLogRawGitHub)
            Urls.Add(RepoLogRawGitee)
        End If
        For Each Url As String In Urls
            Try
                Dim Text As String = NetRequestByLoader(Url, Timeout:=15000).Trim()
                If Text <> "" Then Return Text
            Catch ex As Exception
                Logger.Info("获取更新日志失败（" & Url & "）：" & ex.Message)
            End Try
        Next
        Return ""
    End Function

    ''' <summary>根据源标识返回对应的 Release 页面地址。</summary>
    Friend Function UpdateReleaseUrlBySource(Source As Integer) As String
        If Source = UpdateSourceGitee Then Return RepoReleaseLatestGitee
        Return RepoReleaseLatestGitHub
    End Function

    Friend Sub UpdateCheckByButton()
        '非作者成品（如自行从源码构建）：不提供在线更新
        If Not IsAuthorBuild Then
            Dim Message As String = "你正在使用 PCL ReBorn 开源版本，不包含在线更新功能。" & vbCrLf &
                                    "请前往发布页面手动下载最新版本。"
            Select Case MyMsgBox(Message, "检查更新", "GitHub", "Gitee", "取消")
                Case 1
                    OpenWebsite(RepoReleaseLatestGitHub)
                Case 2
                    OpenWebsite(RepoReleaseLatestGitee)
            End Select
            Return
        End If
        RunInThread(Sub()
                        Try
                            Hint("正在检查更新……")
                            Dim Latest = UpdateGetLatest()
                            If Not IsVersionTagNewer(Latest.Item1, CurrentVersionTag) Then
                                MyMsgBox($"你的 PCL 已经是最新版本（{CurrentVersionTag}）！", "检查更新")
                                Return
                            End If
                            Dim LogText As String = GetUpdateLog(Latest.Item3)
                            If LogText.Length > 300 Then LogText = LogText.Substring(0, 300) & "……"
                            Dim SourceName As String = If(Latest.Item3 = UpdateSourceGitee, "Gitee", "GitHub")
                            If MyMsgBox($"发现新版本 {Latest.Item1}（当前版本 {CurrentVersionTag}，来自 {SourceName}）！{vbCrLf}{vbCrLf}{LogText}{vbCrLf}{vbCrLf}是否立即下载更新？",
                                        "更新", "立即下载", "取消") = 2 Then Return
                            UpdateDownload()
                        Catch ex As Exception
                            Logger.Error(ex, "检查更新失败", LogBehavior.Alert)
                        End Try
                    End Sub)
    End Sub

    ''' <summary>
    ''' 启动时自动检查更新。发现新版本时按设置决定是否自动下载。
    ''' 非作者成品（如自行从源码构建）仅提示一次开源信息，不进行更新。
    ''' </summary>
    Friend Sub UpdateCheckAuto()
        If Not IsAuthorBuild Then
            Hint("你正在使用 PCL ReBorn 开源版本，更新与反馈请前往仓库页面", HintType.Blue)
            Return
        End If
        Try
            Dim Latest = UpdateGetLatest()
            If Not IsVersionTagNewer(Latest.Item1, CurrentVersionTag) Then Return
            Dim SourceName As String = If(Latest.Item3 = UpdateSourceGitee, "Gitee", "GitHub")
            Hint("发现新版本 " & Latest.Item1 & "（来自 " & SourceName & "），可在设置或关于页面中更新", HintType.Blue)
            '设置为“有新版本时自动下载”时自动开始下载
            If Settings.Get(Of Integer)("SystemSystemUpdate") = 3 Then UpdateDownload()
        Catch ex As Exception
            Logger.Info("自动检查更新失败：" & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' 下载最新版本到缓存目录，完成后提示重启以完成更新。
    ''' </summary>
    Private Sub UpdateDownload()
        RunInThread(Sub()
                        Try
                            Dim Latest = UpdateGetLatest()
                            Dim LocalPath As String = PathTemp & "Update\ReBorn-New.exe"
                            DirectoryUtils.Create(PathTemp & "Update\")
                            '记录待更新版本、来源与仓库更新日志，供替换成功后展示“更新完成”窗口
                            Dim PendingText As String = Latest.Item1 & vbCrLf & GetUpdateLog(Latest.Item3) & vbCrLf & "#source=" & Latest.Item3
                            File.WriteAllText(PathTemp & "Update\Pending.cfg", PendingText, Encoding.UTF8)
                            Hint("已开始下载新版本……")
                            Dim InnerLoader As New LoaderDownload("下载新版本", New List(Of NetFile) From {New NetFile({Latest.Item2}, LocalPath)})
                            Dim Loader As New LoaderCombo(Of Integer)("ReBorn 更新", New List(Of LoaderBase) From {InnerLoader})
                            Loader.OnStateChanged =
                            Sub(l As LoaderBase)
                                If l.State = LoadState.Finished Then
                                    IsUpdateWaitingRestart = True
                                    RunInUi(Sub()
                                                If MyMsgBox("新版本已下载完成，需要重启启动器以完成更新。", "更新", "立即重启", "稍后") = 1 Then
                                                    FormMain.EndProgramForce()
                                                End If
                                            End Sub)
                                ElseIf l.State = LoadState.Failed OrElse l.State = LoadState.Canceled Then
                                    Hint("更新下载失败，详情请查看日志", HintType.Red)
                                End If
                            End Sub
                            Loader.Start()
                            LoaderTaskbarAdd(Loader)
                            FrmMain.BtnExtraDownload.ShowRefresh()
                            FrmMain.BtnExtraDownload.Ribble()
                        Catch ex As Exception
                            Logger.Error(ex, "下载更新失败", LogBehavior.Alert)
                        End Try
                    End Sub)
    End Sub

    ''' <summary>
    ''' 程序退出前，若已下载新版本，则启动替换进程。
    ''' 替换器是缓存目录中的新 exe 本身，它会在旧进程退出后覆盖旧文件，然后重新启动旧文件。
    ''' </summary>
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
        Try
            Dim NewFile As String = PathTemp & "Update\ReBorn-New.exe"
            If Not File.Exists(NewFile) Then Return
            '参数：--update 旧进程ID 旧文件路径 新文件路径 是否重启
            Dim Args As String = $"--update {Process.GetCurrentProcess().Id} ""{PathExe}"" ""{NewFile}"" {If(TriggerRestartAndByEnd, 1, 0)}"
            Process.Start(New ProcessStartInfo With {
                .FileName = NewFile,
                .Arguments = Args,
                .UseShellExecute = False,
                .WorkingDirectory = PathUtils.RemoveLastPart(PathExe)
            })
        Catch ex As Exception
            Logger.Error(ex, "启动更新替换进程失败")
        End Try
    End Sub

    ''' <summary>
    ''' 由 --update 参数调用：等待旧进程退出后，用新文件替换旧文件，并重启旧文件。
    ''' </summary>
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
        Try
            '1. 等待旧进程完全退出
            Try
                Dim OldProcess = Process.GetProcessById(ProcessId)
                If Not OldProcess.WaitForExit(60000) Then Return '旧进程长时间未退出，放弃替换
            Catch
                '旧进程可能已经退出
            End Try
            '2. 再等待一会儿，确保文件句柄释放
            Thread.Sleep(1500)

            '3. 记录“更新完成”标记（在替换前写，避免路径失效）
            Try
                Dim PendingPath As String = PathTemp & "Update\Pending.cfg"
                If File.Exists(PendingPath) Then
                    File.WriteAllText(PathTemp & "Update\Updated.cfg", File.ReadAllText(PendingPath, Encoding.UTF8), Encoding.UTF8)
                End If
            Catch ex As Exception
                Logger.Warn(ex, "写入更新完成标记失败")
            End Try

            '4. 覆盖旧文件（重试，防止被占用）
            Dim Replaced As Boolean = False
            For i As Integer = 1 To 15
                Try
                    File.Copy(NewFileName, OldFileName, True)
                    Replaced = True
                    Exit For
                Catch ex As Exception
                    If i = 15 Then
                        Logger.Error(ex, "替换启动器文件失败")
                    Else
                        Thread.Sleep(1000)
                    End If
                End Try
            Next

            '5. 重新启动启动器
            If TriggerRestart AndAlso Replaced Then
                '给文件系统一点时间，避免新进程加载到残留内容
                Thread.Sleep(800)
                Process.Start(New ProcessStartInfo With {
                    .FileName = OldFileName,
                    .UseShellExecute = True,
                    .WorkingDirectory = PathUtils.RemoveLastPart(OldFileName)
                })
            End If
        Catch ex As Exception
            Logger.Error(ex, "自动更新失败")
        End Try
    End Sub

    ''' <summary>
    ''' 启动时检查是否刚完成过一次更新，是则展示“更新完成”窗口。
    ''' </summary>
    Friend Sub UpdateDoneShow()
        Try
            Dim FlagPath As String = PathTemp & "Update\Updated.cfg"
            If Not File.Exists(FlagPath) Then Return
            Dim Content As String = File.ReadAllText(FlagPath, Encoding.UTF8)
            File.Delete(FlagPath) '只展示一次
            Dim Lines As String() = Content.Split(New String() {vbCrLf, vbLf, vbCr}, StringSplitOptions.None)
            Dim NewVersion As String = If(Lines.Length > 0, Lines(0).Trim(), "")
            '末尾可能带有 #source= 标记，取出后从日志正文里剔除
            Dim Source As Integer = UpdateSourceGitHub
            Dim LogLines As New List(Of String)
            For i As Integer = 1 To Lines.Length - 1
                Dim Line As String = Lines(i).Trim()
                If Line.StartsWith("#source=") Then
                    Integer.TryParse(Line.Substring(8).Trim(), Source)
                Else
                    LogLines.Add(Lines(i))
                End If
            Next
            Dim LogText As String = String.Join(vbCrLf, LogLines).Trim()
            If LogText.Length > 600 Then LogText = LogText.Substring(0, 600) & "……"
            Dim Message As String = "PCL ReBorn 已成功更新到 " & NewVersion & "！" & vbCrLf
            If LogText <> "" Then Message &= vbCrLf & "本次更新内容（点击下方按钮可打开 Release 页面查看完整日志）：" & vbCrLf & LogText & vbCrLf
            Message &= vbCrLf & "感谢你对 PCL ReBorn 的支持！"
            Select Case MyMsgBox(Message, "更新完成", "太好了", "查看完整日志")
                Case 2
                    OpenWebsite(UpdateReleaseUrlBySource(Source))
            End Select
        Catch ex As Exception
            Logger.Error(ex, "展示更新完成窗口失败")
        End Try
    End Sub

    ''' <summary>
    ''' 确保 PathTemp/Latest.exe 是最新正式版的 PCL，它会被用于整合包打包。
    ''' </summary>
    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)
        '注意：ReBorn 版不提供此功能，整合包导出时将跳过 Latest.exe 的下载
    End Sub

#End Region

#Region "联网配置"

    ''' <summary>
    ''' 联网获取的配置信息。
    ''' 若获取失败或仍在获取中，可能为 Nothing。
    ''' </summary>
    Public ServerConfig As JObject

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 配置更新", Sub() Logger.Info("该版本中不包含更新通知功能……"), Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60} '超时 1 小时

#End Region

#Region "赞助等级"

    Public ReadOnly Property CurrentRank As DonationRank
        Get
            Return DonationRank.None
        End Get
    End Property

    ''' <summary>
    ''' 获取设备识别码。
    ''' </summary>
    Friend Function GetIdentify() As String
        Return "0000-0000-0000-0000"
    End Function

#End Region

End Module
