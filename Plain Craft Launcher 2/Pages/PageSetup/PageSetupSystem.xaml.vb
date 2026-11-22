Public Class PageSetupSystem


    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        Static IsLoaded As Boolean = False
        If IsLoaded Then Return
        IsLoaded = True

        AniControlEnabled += 1
        Reload()
        SliderLoad()
        AniControlEnabled -= 1

    End Sub
    Public Sub Reload()
        SettingService.RefreshSettings(Me)
        AiInit()
    End Sub
    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            '密钥框不绑定设置值，需要手动清空显示
            IsUpdatingAiUi = True
            TextAiApiKey.Password = ""
            IsUpdatingAiUi = False
            Logger.Info("已初始化启动器页设置")
            Hint("已初始化启动器页设置！", HintType.Green, False)
        Catch ex As Exception
            IsUpdatingAiUi = False
            Logger.Error(ex, "初始化启动器页设置失败", LogBehavior.Alert)
        End Try
        Reload()
    End Sub

    '滑动条
    Private Sub SliderLoad()
        SliderDownloadThread.GetHintText = Function(v) v + 1
        SliderDownloadSpeed.GetHintText =
        Function(v)
            Select Case v
                Case Is <= 14
                    Return (v + 1) * 0.1 & " M/s"
                Case Is <= 31
                    Return (v - 11) * 0.5 & " M/s"
                Case Is <= 41
                    Return (v - 21) & " M/s"
                Case Else
                    Return "无限制"
            End Select
        End Function
        SliderDebugAnim.GetHintText = Function(v) If(v > 29, "关闭", (v / 10 + 0.1) & "x")
    End Sub
    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Return
        If Not Settings.Get(Of Boolean)("HintDownloadThread") Then
            Settings.Set("HintDownloadThread", True)
            MyMsgBox("如果设置过多的下载线程，可能会导致下载时出现非常严重的卡顿。" & vbCrLf &
                     "一般设置 64 线程即可满足大多数下载需求，除非你知道你在干什么，否则不建议设置更多的线程数！", "警告", "我知道了", IsWarn:=True)
        End If
    End Sub

    '调试模式
    Private Sub CheckDebugMode_Change() Handles CheckDebugMode.Change
        If AniControlEnabled = 0 Then Hint("部分调试信息将在刷新或启动器重启后切换显示！",, False)
    End Sub

    '自动更新
    Private Sub ComboSystemActivity_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemActivity.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        If ComboSystemActivity.SelectedIndex <> 2 Then Return
        If MyMsgBox("若选择此项，即使在将来出现严重问题时，你也无法获取相关通知。" & vbCrLf &
                    "例如，如果发现某个版本游戏存在严重 Bug，你可能就会因为无法得到通知而导致无法预知的后果。" & vbCrLf & vbCrLf &
                    "一般选择 仅在有重要通知时显示公告 就可以让你尽量不受打扰了。" & vbCrLf &
                    "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
            If e.RemovedItems.Count > 0 Then ComboSystemActivity.SelectedItem = e.RemovedItems(0)
        End If
    End Sub
    Private Sub ComboSystemUpdate_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemUpdate.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        If ComboSystemUpdate.SelectedIndex <> 3 Then Return
        If MyMsgBox("若选择此项，即使在启动器将来出现严重问题时，你也无法获取更新并获得修复。" & vbCrLf &
                    "例如，如果官方修改了登录方式，从而导致现有启动器无法登录，你可能就会因为无法更新而无法开始游戏。" & vbCrLf & vbCrLf &
                    "一般选择 仅在有重大漏洞更新时显示提示 就可以让你尽量不受打扰了。" & vbCrLf &
                    "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
            If e.RemovedItems.Count > 0 Then ComboSystemUpdate.SelectedItem = e.RemovedItems(0)
        End If
    End Sub
    Private Sub BtnSystemUpdate_Click(sender As Object, e As EventArgs) Handles BtnSystemUpdate.Click
        UpdateCheckByButton()
    End Sub
    ''' <summary>
    ''' 启动器是否已经是最新版？
    ''' 若返回 Nothing，则代表无更新缓存文件或出错。
    ''' </summary>
    Public Shared Function IsLauncherNewest() As Boolean?
        Try
            '确认服务器公告是否正常
            Dim ServerContent As String = If(FileUtils.TryReadAsString(PathTemp & "Cache\Notice.cfg"), "")
            If ServerContent.Split("|").Count < 3 Then Return Nothing
            '确认是否为最新
            Return ServerContent.Split("|")(If(BuildType = BuildTypes.Release, 2, 1)) <= VersionCode
        Catch ex As Exception
            Logger.Error(ex, "确认启动器更新失败")
            Return Nothing
        End Try
    End Function

#Region "AI 日志分析"

    '是否正在由程序改动 AI 相关控件（避免触发保存或重复刷新）
    Private IsUpdatingAiUi As Boolean = False
    '是否正在获取模型列表
    Private IsAiModelLoading As Boolean = False

    ''' <summary>
    ''' 初始化 AI 设置界面：先用缓存的模型列表填充，再按密钥模式刷新可用状态。
    ''' </summary>
    Private Sub AiInit()
        Dim CachedModels As List(Of String) = Nothing
        Try
            '出于安全考虑，已保存的密钥不会回填到输入框，只显示保存状态
            IsUpdatingAiUi = True
            TextAiApiKey.Password = ""
            IsUpdatingAiUi = False
            CachedModels = ParseAiModelList(Settings.Get(Of String)("AiModelList"))
            FillAiModelCombo(CachedModels)
            RefreshAiKeyState()
            RefreshAiUiState()
        Catch ex As Exception
            IsUpdatingAiUi = False
            Logger.Warn(ex, "初始化 AI 设置失败")
        End Try
        '使用自定义密钥且还没有模型列表时，后台自动获取一次
        If AiIsCustomMode() AndAlso (CachedModels Is Nothing OrElse Not CachedModels.Any()) Then RefreshAiModelList(IsAuto:=True)
    End Sub

    ''' <summary>
    ''' 当前是否选择了自定义密钥。
    ''' </summary>
    Private Function AiIsCustomMode() As Boolean
        Return ComboAiKeyMode.SelectedIndex = 1
    End Function

    ''' <summary>
    ''' 是否已经保存了自定义密钥。
    ''' </summary>
    Private Function AiHasSavedKey() As Boolean
        Try
            Return Not String.IsNullOrWhiteSpace(Settings.Get(Of String)("AiApiKey"))
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 当前应当使用的自定义密钥：输入框里有内容就用输入框的，否则用已保存的。
    ''' </summary>
    Private Function AiGetApiKey() As String
        Dim Typed As String = If(TextAiApiKey.Password, "").Trim()
        If Typed.Length > 0 Then Return Typed
        Try
            Return If(Settings.Get(Of String)("AiApiKey"), "").Trim()
        Catch ex As Exception
            Logger.Warn(ex, "读取 AI 密钥失败")
            Return ""
        End Try
    End Function

    ''' <summary>
    ''' 刷新 API Key 输入框右侧的状态提示。
    ''' </summary>
    Private Sub RefreshAiKeyState()
        If LabAiKeyState Is Nothing Then Return
        If Not String.IsNullOrEmpty(TextAiApiKey.Password) Then
            LabAiKeyState.Text = "已保存"
        ElseIf AiHasSavedKey() Then
            LabAiKeyState.Text = "已保存，留空则不修改"
        Else
            LabAiKeyState.Text = "未填写"
        End If
        LabAiKeyState.Opacity = If(AiIsCustomMode(), 0.6, 0.3)
    End Sub

    ''' <summary>
    ''' 输入密钥时即时保存（输入框只显示圆点，不显示原文）。
    ''' </summary>
    Private Sub TextAiApiKey_PasswordChanged(sender As Object, e As RoutedEventArgs) Handles TextAiApiKey.PasswordChanged
        If IsUpdatingAiUi Then Return
        Dim Key As String = If(TextAiApiKey.Password, "").Trim()
        '留空代表沿用已保存的密钥，避免误清空
        If Key.Length > 0 Then Settings.Set("AiApiKey", Key)
        RefreshAiKeyState()
    End Sub

    ''' <summary>
    ''' 离开本页时清空密钥输入框，避免下次进入时还能看到上次输入的内容。
    ''' </summary>
    Private Sub PageSetupSystem_PageExit() Handles Me.PageExit
        If TextAiApiKey Is Nothing Then Return
        IsUpdatingAiUi = True
        TextAiApiKey.Password = ""
        IsUpdatingAiUi = False
        RefreshAiKeyState()
    End Sub

    ''' <summary>
    ''' 按当前密钥模式刷新控件可用状态与提示文本。
    ''' </summary>
    Private Sub RefreshAiUiState()
        If IsUpdatingAiUi Then Return
        Dim IsCustom As Boolean = AiIsCustomMode()
        Dim CanEdit As Boolean = IsCustom AndAlso Not IsAiModelLoading
        '公共密钥下无需填写这些内容
        TextAiApiBaseUrl.IsEnabled = CanEdit
        TextAiApiKey.IsEnabled = CanEdit
        ComboAiModel.IsEnabled = CanEdit
        BtnAiModelRefresh.IsEnabled = CanEdit
        Dim ControlOpacity As Double = If(CanEdit, 1, 0.5)
        TextAiApiBaseUrl.Opacity = ControlOpacity
        TextAiApiKey.Opacity = ControlOpacity
        ComboAiModel.Opacity = ControlOpacity
        BtnAiModelRefresh.Opacity = ControlOpacity
        BtnAiModelRefresh.Text = If(IsAiModelLoading, "正在获取……", "刷新模型列表")
        LabAiHint.Text = If(IsCustom,
            "填写接口地址与 API Key 后会自动获取可用模型列表，请从中选择一个模型。",
            "当前使用启动器内置的公共密钥，无需额外配置。")
        RefreshAiKeyState()
    End Sub

    ''' <summary>
    ''' 切换密钥来源。
    ''' </summary>
    Private Sub ComboAiKeyMode_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAiKeyMode.SelectionChanged
        If IsUpdatingAiUi OrElse AniControlEnabled <> 0 Then Return
        RefreshAiUiState()
        If AiIsCustomMode() Then RefreshAiModelList(IsAuto:=True)
    End Sub

    ''' <summary>
    ''' 选择模型后保存到设置。
    ''' </summary>
    Private Sub ComboAiModel_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAiModel.SelectionChanged
        If IsUpdatingAiUi OrElse AniControlEnabled <> 0 Then Return
        Dim Model As String = TryCast(ComboAiModel.SelectedItem, MyComboBoxItem)?.Content?.ToString()
        If String.IsNullOrWhiteSpace(Model) Then Return
        Settings.Set("AiModel", Model)
    End Sub

    Private Sub BtnAiModelRefresh_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAiModelRefresh.Click
        RefreshAiModelList()
    End Sub

    ''' <summary>
    ''' 填完接口地址或密钥后（焦点离开），若还没有模型列表则自动获取一次。
    ''' </summary>
    Private Sub AiConfigBox_LostFocus(sender As Object, e As RoutedEventArgs) Handles TextAiApiBaseUrl.LostFocus, TextAiApiKey.LostFocus
        If IsUpdatingAiUi OrElse AniControlEnabled <> 0 Then Return
        If Not AiIsCustomMode() Then Return
        If ComboAiModel.Items.Count > 0 Then Return
        RefreshAiModelList(IsAuto:=True)
    End Sub

    ''' <summary>
    ''' 从接口获取可用模型列表。
    ''' </summary>
    ''' <param name="IsAuto">是否为自动刷新。自动刷新时如果缺少信息则静默跳过。</param>
    Private Sub RefreshAiModelList(Optional IsAuto As Boolean = False)
        If IsAiModelLoading OrElse Not AiIsCustomMode() Then Return
        '检查填写内容
        Dim BaseUrl As String = If(TextAiApiBaseUrl.Text, "").Trim()
        Dim ApiKey As String = AiGetApiKey()
        If BaseUrl.Length = 0 OrElse ApiKey.Length = 0 Then
            If Not IsAuto Then Hint("请先填写接口地址与 API Key，再获取模型列表！", HintType.Red)
            Return
        End If
        If Not BaseUrl.RegexCheck("^(?i)https?://") Then
            Hint("接口地址需要以 http:// 或 https:// 开头！", HintType.Red)
            Return
        End If
        '开始获取
        IsAiModelLoading = True
        RefreshAiUiState()
        If Not IsAuto Then Hint("正在获取模型列表……")
        RunInNewThread(
        Sub()
            Try
                Dim Models As List(Of String) = ModAi.FetchModelList(BaseUrl, ApiKey)
                RunInUi(
                Sub()
                    Try
                        FillAiModelCombo(Models)
                        Settings.Set("AiModelList", Models.Join(vbLf))
                        Hint($"已获取 {Models.Count} 个可用模型", HintType.Green)
                    Catch ex As Exception
                        Logger.Warn(ex, "显示 AI 模型列表失败")
                    End Try
                End Sub)
            Catch ex As Exception
                Logger.Warn(ex, "获取 AI 模型列表失败")
                Hint("获取模型列表失败：" & ex.Message, HintType.Red)
            Finally
                IsAiModelLoading = False
                RunInUi(Sub() RefreshAiUiState())
            End Try
        End Sub, "AI Model List")
    End Sub

    ''' <summary>
    ''' 把模型列表填充到下拉框，并尽量保留之前选择的模型。
    ''' </summary>
    Private Sub FillAiModelCombo(Models As IEnumerable(Of String))
        Dim Saved As String = If(Settings.Get(Of String)("AiModel"), "").Trim()
        IsUpdatingAiUi = True
        Try
            ComboAiModel.Items.Clear()
            If Models IsNot Nothing Then
                For Each Model In Models
                    If String.IsNullOrWhiteSpace(Model) Then Continue For
                    ComboAiModel.Items.Add(New MyComboBoxItem With {.Content = Model.Trim()})
                Next
            End If
            '找出之前保存的模型
            Dim Target As MyComboBoxItem = Nothing
            For Each Item As MyComboBoxItem In ComboAiModel.Items
                If Item.Content?.ToString() = Saved Then Target = Item
            Next
            '之前保存的模型不在列表中（例如换了接口），补一项，让用户知道自己原本选的是什么
            If Target Is Nothing AndAlso Saved <> "" Then
                Target = New MyComboBoxItem With {.Content = Saved}
                ComboAiModel.Items.Insert(0, Target)
            End If
            If Target Is Nothing AndAlso ComboAiModel.Items.Count > 0 Then Target = TryCast(ComboAiModel.Items(0), MyComboBoxItem)
            ComboAiModel.SelectedItem = Target
        Finally
            IsUpdatingAiUi = False
        End Try
        '把最终选中的模型写回设置
        Dim SelectedModel As String = TryCast(ComboAiModel.SelectedItem, MyComboBoxItem)?.Content?.ToString()
        If Not String.IsNullOrWhiteSpace(SelectedModel) Then Settings.Set("AiModel", SelectedModel)
    End Sub

    ''' <summary>
    ''' 解析缓存中的模型列表。
    ''' </summary>
    Private Function ParseAiModelList(Text As String) As List(Of String)
        Dim Result As New List(Of String)
        If String.IsNullOrWhiteSpace(Text) Then Return Result
        For Each Line In Text.Split(New String() {vbCrLf, vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
            Dim Model As String = Line.Trim()
            If Model.Length > 0 AndAlso Not Result.Contains(Model) Then Result.Add(Model)
        Next
        Return Result
    End Function

#End Region

#Region "导出 / 导入设置"

    Private Sub BtnSystemSettingExp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingExp.Click
        Hint("该功能尚在开发中！")
    End Sub
    Private Sub BtnSystemSettingImp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingImp.Click
        Hint("该功能尚在开发中！")
    End Sub

#End Region

End Class
