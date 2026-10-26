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

    '基于 GitHub Releases 的自动更新
    '开源版本（正式版构建）不包含自动更新，仅快照版/开发版提供
    Friend IsUpdateWaitingRestart As Boolean = False
    Private Const UpdateApiUrl As String = "https://api.github.com/repos/Cherry5033MC/PCL-ReBorn/releases/latest"
    Private Const UpdateLogUrl As String = "https://raw.githubusercontent.com/Cherry5033MC/PCL-ReBorn/main/UpdateLog.txt"
    Private Const UpdateReleaseUrl As String = "https://github.com/Cherry5033MC/PCL-ReBorn/releases/latest"

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
    ''' 获取 GitHub Releases 上最新版本的信息。获取失败时抛出异常。
    ''' 返回 (完整 Tag, 下载地址)。
    ''' </summary>
    Private Function UpdateGetLatest() As Tuple(Of String, String)
        Dim Json As JObject = JObject.Parse(NetRequestByLoader(UpdateApiUrl, Timeout:=45000, IsJson:=True))
        Dim Tag As String = Json("tag_name").ToString()
        '寻找 .exe 附件
        Dim DownloadUrl As String = Nothing
        For Each Asset In Json("assets")
            Dim Name As String = Asset("name").ToString()
            If Name.EndsWithF(".exe", True) Then
                DownloadUrl = Asset("browser_download_url").ToString()
                Exit For
            End If
        Next
        If DownloadUrl Is Nothing Then Throw New Exception("最新 Release 中未找到可执行文件")
        Return New Tuple(Of String, String)(Tag, DownloadUrl)
    End Function

    ''' <summary>
    ''' 获取仓库中 UpdateLog.txt 的更新日志内容。获取失败时返回空字符串。
    ''' </summary>
    Private Function GetUpdateLog() As String
        Try
            Return NetRequestByLoader(UpdateLogUrl, Timeout:=15000).Trim()
        Catch ex As Exception
            Logger.Info("获取更新日志失败：" & ex.Message)
            Return ""
        End Try
    End Function

    Friend Sub UpdateCheckByButton()
        '非作者成品（如自行从源码构建）：不提供在线更新
        If Not IsAuthorBuild Then
            If MyMsgBox("你正在使用 PCL ReBorn 开源版本，不包含在线更新功能。" & vbCrLf &
                        "请前往 GitHub Releases 页面手动下载最新版本。", "检查更新", "前往 GitHub", "取消") = 1 Then
                OpenWebsite(UpdateReleaseUrl)
            End If
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
                            Dim LogText As String = GetUpdateLog()
                            If LogText.Length > 300 Then LogText = LogText.Substring(0, 300) & "……"
                            If MyMsgBox($"发现新版本 {Latest.Item1}（当前版本 {CurrentVersionTag}）！{vbCrLf}{vbCrLf}{LogText}{vbCrLf}{vbCrLf}是否立即下载更新？",
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
            Hint("你正在使用 PCL ReBorn 开源版本，更新与反馈请前往 GitHub 仓库", HintType.Blue)
            Return
        End If
        Try
            Dim Latest = UpdateGetLatest()
            If Not IsVersionTagNewer(Latest.Item1, CurrentVersionTag) Then Return
            Hint("发现新版本 " & Latest.Item1 & "，可在设置或关于页面中更新", HintType.Blue)
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
                            '记录待更新版本与仓库更新日志，供替换成功后展示“更新完成”窗口
                            File.WriteAllText(PathTemp & "Update\Pending.cfg", Latest.Item1 & vbCrLf & GetUpdateLog(), Encoding.UTF8)
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
    ''' </summary>
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
        Try
            Dim NewFile As String = PathTemp & "Update\ReBorn-New.exe"
            If Not File.Exists(NewFile) Then Return
            Dim Args As String = $"--update {Process.GetCurrentProcess().Id} ""{PathExe}"" ""{NewFile}"" {If(TriggerRestartAndByEnd, 1, 0)}"
            Process.Start(New ProcessStartInfo With {.FileName = NewFile, .Arguments = Args, .UseShellExecute = False})
        Catch ex As Exception
            Logger.Error(ex, "启动更新替换进程失败")
        End Try
    End Sub

    ''' <summary>
    ''' 由 --update 参数调用：等待旧进程退出后，用新文件替换旧文件，并可选重启。
    ''' </summary>
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
        Try
            Try
                Dim OldProcess = Process.GetProcessById(ProcessId)
                If Not OldProcess.WaitForExit(60000) Then Return '旧进程长时间未退出，放弃替换
            Catch
                '旧进程可能已经退出
            End Try
            Thread.Sleep(500)
            For i As Integer = 1 To 10 '重试，防止旧文件被占用
                Try
                    File.Copy(NewFileName, OldFileName, True)
                    Exit For
                Catch ex As Exception
                    If i = 10 Then
                        Logger.Error(ex, "替换启动器文件失败")
                        Return
                    End If
                    Thread.Sleep(1000)
                End Try
            Next
            '标记更新完成，下次启动时展示“更新完成”窗口
            Try
                Dim PendingPath As String = PathTemp & "Update\Pending.cfg"
                If File.Exists(PendingPath) Then File.WriteAllText(PathTemp & "Update\Updated.cfg", File.ReadAllText(PendingPath, Encoding.UTF8), Encoding.UTF8)
            Catch
            End Try
            If TriggerRestart Then Process.Start(New ProcessStartInfo With {.FileName = OldFileName, .UseShellExecute = False})
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
            Dim LogText As String = If(Lines.Length > 1, String.Join(vbCrLf, Lines.Skip(1)).Trim(), "")
            If LogText.Length > 600 Then LogText = LogText.Substring(0, 600) & "……"
            Dim Message As String = "PCL ReBorn 已成功更新到 " & NewVersion & "！" & vbCrLf
            If LogText <> "" Then Message &= vbCrLf & "本次更新内容（点击下方按钮可打开 Release 页面查看完整日志）：" & vbCrLf & LogText & vbCrLf
            Message &= vbCrLf & "感谢你对 PCL ReBorn 的支持！"
            Select Case MyMsgBox(Message, "更新完成", "太好了", "查看完整日志")
                Case 2
                    OpenWebsite(UpdateReleaseUrl)
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
