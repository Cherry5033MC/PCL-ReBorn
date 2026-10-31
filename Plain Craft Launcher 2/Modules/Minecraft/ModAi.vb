''' <summary>
''' AI 崩溃日志分析。
''' 把崩溃日志与提示词发送到 OpenAI 兼容的 Chat Completions 接口，并把结果展示给用户。
''' 接口地址、密钥与模型名称在 ModSecret.vb 的「AI 日志分析」区域中填写。
''' </summary>
Public Module ModAi

#Region "提示词"

    ''' <summary>
    ''' 单次发送的日志最大字符数，超出时只保留末尾部分（崩溃信息通常在末尾）。
    ''' </summary>
    Private Const MaxLogLength As Integer = 50000

    ''' <summary>
    ''' 系统提示词，约束 AI 的角色、回答语言与输出格式。
    ''' </summary>
    Private ReadOnly AiSystemPrompt As String = String.Join(vbCrLf, New String() {
        "你是一名 Minecraft 崩溃日志分析专家，熟悉原版日志、Forge / NeoForge / Fabric / Quilt 的加载流程与堆栈信息。",
        "用户会提供游戏环境、启动器自动分析出的初步结论，以及崩溃日志原文。请据此给出诊断。",
        "回答要求：",
        "1. 使用简体中文，语气直接务实，不要寒暄，不要复述用户提供的日志。",
        "2. 第一句话就给出最可能的崩溃原因。",
        "3. 然后分条列出判断依据，每条不超过两行。",
        "4. 若判断是某个模组导致的，直接写出模组文件名或 Mod ID。",
        "5. 最后给出可操作的解决步骤，按优先级排序，一步一条。",
        "6. 信息不足时直接说明还缺少什么，不要编造日志中不存在的内容。",
        "7. 只输出纯文本，不要使用 Markdown 标题、代码块或表格。"
    })

#End Region

#Region "对外入口"

    ''' <summary>
    ''' 分析崩溃日志并展示结果。可在任意线程调用，内部已捕获所有异常。
    ''' </summary>
    ''' <param name="LogText">崩溃日志原文。</param>
    ''' <param name="AnalyzeResult">启动器自动分析出的结论，可为空。</param>
    ''' <param name="Instance">崩溃的版本实例，可为空。</param>
    Public Sub AnalyzeCrash(LogText As String, Optional AnalyzeResult As String = Nothing, Optional Instance As McInstance = Nothing)
        Try
            '检查配置
            If AiApiBaseUrlNow.Length = 0 Then
                ShowTip("尚未填写接口地址，无法进行 AI 分析。" & vbCrLf & vbCrLf &
                        "请打开 Modules\ModSecret.vb，找到「AI 日志分析」区域，填写 AiApiBaseUrl" & vbCrLf &
                        "（例如 https://api.openai.com/v1），然后重新编译。", True)
                Return
            End If
            If AiApiKeyNow.Length = 0 Then
                ShowTip("尚未填写接口密钥，无法进行 AI 分析。" & vbCrLf & vbCrLf &
                        "请打开 Modules\ModSecret.vb，找到「AI 日志分析」区域，填写 AiApiKey（形如 sk-xxxx），" & vbCrLf &
                        "或者设置环境变量 PCL_AI_API_KEY，然后重新编译。", True)
                Return
            End If
            If String.IsNullOrWhiteSpace(LogText) Then
                ShowTip("没有找到可用于分析的日志内容，无法进行 AI 分析。", True)
                Return
            End If

            '开始请求
            Hint("正在请求 AI 分析日志，请稍候……", HintType.Blue)
            Dim IsFinished As Integer = 0
            '等待较久时补一条提示，避免用户以为没有反应
            RunInThread(
            Sub()
                Thread.Sleep(25000)
                If IsFinished = 0 Then Hint("AI 仍在分析中，日志较长时耗时会明显增加……", HintType.Blue)
            End Sub)
            RunInThread(
            Sub()
                Dim Answer As String = Nothing
                Dim Failure As Exception = Nothing
                Try
                    Answer = RequestAi(LogText, AnalyzeResult, Instance)
                Catch ex As Exception
                    Failure = ex
                End Try
                IsFinished = 1
                If Failure IsNot Nothing Then
                    Logger.Error(Failure, "AI 日志分析失败")
                    ShowError(Failure)
                Else
                    ShowResult(Answer)
                End If
            End Sub)
        Catch ex As Exception
            Logger.Error(ex, "启动 AI 日志分析失败")
            ShowTip("启动 AI 分析时出现意外错误：" & vbCrLf & vbCrLf & ex.Message, True)
        End Try
    End Sub

#End Region

#Region "请求与解析"

    ''' <summary>
    ''' 组装提示词并请求接口，返回 AI 的回答正文。失败时抛出异常。
    ''' </summary>
    Private Function RequestAi(LogText As String, AnalyzeResult As String, Instance As McInstance) As String
        Dim UserMessage As String = BuildUserMessage(LogText, AnalyzeResult, Instance)
        Logger.Info($"AI 日志分析：模型 {AiModelNow}，请求内容 {UserMessage.Length} 字符")

        '系统消息
        Dim SystemMessage As New JObject
        SystemMessage("role") = "system"
        SystemMessage("content") = AiSystemPrompt
        '用户消息
        Dim UserNode As New JObject
        UserNode("role") = "user"
        UserNode("content") = UserMessage
        '消息数组
        Dim Messages As New JArray
        Messages.Add(SystemMessage)
        Messages.Add(UserNode)
        '请求体
        Dim Body As New JObject
        Body("model") = AiModelNow
        Body("messages") = Messages
        Body("temperature") = 0.3
        Body("stream") = False

        '发送请求
        Dim Url As String = AiApiBaseUrlNow & "/chat/completions"
        Dim Headers As String(,) = {{"Authorization", "Bearer " & AiApiKeyNow}}
        Dim Raw As String = NetRequestByClient(Url, HttpMethod.Post, Body.ToString(), "application/json", "application/json",
                                               AiTimeout, Headers, RequireJson:=True)

        '解析回答
        Dim Json As JObject = JObject.Parse(Raw)
        Dim Choices As JToken = Json("choices")
        If Choices Is Nothing OrElse Not Choices.HasValues Then
            Throw New Exception("接口返回的内容中没有 choices 字段：" & vbCrLf & ClipText(Raw, 800))
        End If
        Dim Choice As JToken = Choices.Children().FirstOrDefault()
        If Choice Is Nothing OrElse Choice.Type = JTokenType.Null Then
            Throw New Exception("接口返回的 choices 为空：" & vbCrLf & ClipText(Raw, 800))
        End If
        Dim MessageNode As JToken = Choice("message")
        If MessageNode Is Nothing Then
            Throw New Exception("接口返回的内容中没有 message 字段：" & vbCrLf & ClipText(Raw, 800))
        End If
        Dim ContentNode As JToken = MessageNode("content")
        If ContentNode Is Nothing Then
            Throw New Exception("接口返回的内容中没有 message.content 字段：" & vbCrLf & ClipText(Raw, 800))
        End If
        Dim Answer As String = ContentNode.ToString()
        If String.IsNullOrWhiteSpace(Answer) Then Throw New Exception("接口返回的分析结果为空，请稍后重试。")
        Return Answer
    End Function

    ''' <summary>
    ''' 把游戏环境、启动器结论与日志拼成一段用户消息。
    ''' </summary>
    Private Function BuildUserMessage(LogText As String, AnalyzeResult As String, Instance As McInstance) As String
        Dim Sb As New StringBuilder

        '游戏环境
        Sb.AppendLine("【游戏环境】")
        Sb.AppendLine("启动器：PCL ReBorn " & VersionFullTag)
        Sb.AppendLine("操作系统：" & Environment.OSVersion.ToString() & If(Environment.Is64BitOperatingSystem, "（64 位）", "（32 位）"))
        If Instance IsNot Nothing Then
            Try
                Sb.AppendLine("版本名称：" & Instance.Name)
            Catch ex As Exception
                Logger.Warn(ex, "获取版本名称失败")
            End Try
            Try
                Dim DisplayName As String = Instance.VersionDisplayName()
                If Not String.IsNullOrWhiteSpace(DisplayName) Then Sb.AppendLine("版本类型：" & DisplayName)
            Catch ex As Exception
                Logger.Warn(ex, "获取版本显示名称失败")
            End Try
            'Mod 列表
            Try
                Dim Mods As New List(Of String)
                For Each ModFile In DirectoryUtils.EnumerateFiles(Instance.PathIndie & "mods\", True)
                    If PathUtils.GetExtension(ModFile) = "jar" Then Mods.Add(PathUtils.GetLastPart(ModFile))
                Next
                If Mods.Any() Then
                    Mods.Sort()
                    Sb.AppendLine("已安装模组（" & Mods.Count & " 个）：" & Mods.Join("、"))
                End If
            Catch ex As Exception
                Logger.Warn(ex, "获取模组列表失败")
            End Try
        End If

        '启动器的初步结论
        Dim CleanResult As String = CleanValue(AnalyzeResult)
        If Not String.IsNullOrWhiteSpace(CleanResult) Then
            Sb.AppendLine()
            Sb.AppendLine("【启动器自动分析出的初步结论（仅供参考，可能有误）】")
            Sb.AppendLine(CleanResult)
        End If

        '日志正文
        Sb.AppendLine()
        Sb.AppendLine("【崩溃日志】")
        Sb.AppendLine(BuildLogPart(LogText))
        Return Sb.ToString()
    End Function

    ''' <summary>
    ''' 截断过长的日志，只保留末尾部分。
    ''' </summary>
    Private Function BuildLogPart(LogText As String) As String
        Dim Text As String = LogText.Replace(vbCrLf, vbLf)
        If Text.Length <= MaxLogLength Then Return Text
        Dim Removed As Integer = Text.Length - MaxLogLength
        Return "（日志过长，已省略前 " & Removed & " 个字符）" & vbLf & Text.Substring(Removed)
    End Function

    ''' <summary>
    ''' 清理启动器分析结论里的换行标记，避免把 \n、\h 这类符号原样发给 AI。
    ''' </summary>
    Private Function CleanValue(Text As String) As String
        If String.IsNullOrWhiteSpace(Text) Then Return ""
        Dim Result As String = Text.Replace("\n", vbLf)
        Result = Result.Replace("\h", "")
        Result = Result.Replace("  ", " ")
        Return Result.Trim()
    End Function

#End Region

#Region "展示结果"

    ''' <summary>
    ''' 在 UI 线程弹出提示窗。
    ''' </summary>
    Private Sub ShowTip(Message As String, IsWarn As Boolean)
        RunInUi(Sub() MyMsgBox(Message, "AI 日志分析", IsWarn:=IsWarn))
    End Sub

    ''' <summary>
    ''' 展示分析结果，并附上复制与打开文件功能。
    ''' </summary>
    Private Sub ShowResult(Answer As String)
        Dim Text As String = CleanAiText(Answer)
        Logger.Info($"AI 分析结果：{vbCrLf}{Text}")

        '顺手存一份到临时文件夹，方便事后查看
        Dim SavedPath As String = Nothing
        Try
            SavedPath = PathTemp & "AI-Analysis-" & Now.ToString("yyyyMMdd-HHmmss") & ".txt"
            FileUtils.Write(SavedPath, Text, Encoding.UTF8)
        Catch ex As Exception
            Logger.Warn(ex, "保存 AI 分析结果失败")
            SavedPath = Nothing
        End Try

        Dim Caption As String = Text
        If SavedPath IsNot Nothing Then Caption &= vbCrLf & vbCrLf & "（分析结果已保存到 " & SavedPath & "）"

        RunInUi(
        Sub()
            MyMsgBox(Caption, "AI 分析结果", "确定", "复制结果", "打开结果文件",
                     Button2Action:=Sub() ClipboardSet(Text),
                     Button3Action:=Sub() OpenAiResultFile(SavedPath))
        End Sub)
    End Sub

    ''' <summary>
    ''' 打开已保存的分析结果文件。
    ''' </summary>
    Private Sub OpenAiResultFile(FilePath As String)
        Try
            If FilePath Is Nothing OrElse Not FileUtils.Exists(FilePath) Then
                Hint("结果文件不存在，可能已被清理", HintType.Red)
                Return
            End If
            OpenExplorer(FilePath)
        Catch ex As Exception
            Logger.Error(ex, "打开 AI 分析结果文件失败")
        End Try
    End Sub

    ''' <summary>
    ''' 根据异常类型给出更容易理解的失败原因。
    ''' </summary>
    Private Sub ShowError(Failure As Exception)
        Dim Message As String = Failure.Message
        If TypeOf Failure Is HttpRequestCodeException Then
            Dim Code As Integer = CInt(CType(Failure, HttpRequestCodeException).StatusCode)
            Select Case Code
                Case 400
                    Message = "接口认为请求参数有误（HTTP 400）。" & vbCrLf &
                              "常见原因：AiModel 填写的模型名不存在，或日志太长超出了该模型的上下文长度。" & vbCrLf & vbCrLf & Message
                Case 401, 403
                    Message = "接口拒绝了这次请求（HTTP " & Code & "）。" & vbCrLf &
                              "常见原因：AiApiKey 无效、已过期，或该密钥没有调用此模型的权限。" & vbCrLf & vbCrLf & Message
                Case 404
                    Message = "接口地址不存在（HTTP 404）。" & vbCrLf &
                              "请检查 ModSecret.vb 里的 AiApiBaseUrl，一般需要以 /v1 结尾，程序会自动拼上 /chat/completions。" & vbCrLf & vbCrLf & Message
                Case 429
                    Message = "请求过于频繁或额度不足（HTTP 429），请稍后再试，或检查账户余额。" & vbCrLf & vbCrLf & Message
                Case Else
                    Message = "接口返回错误（HTTP " & Code & "）：" & vbCrLf & vbCrLf & Message
            End Select
        ElseIf TypeOf Failure Is WebException Then
            Message = "网络请求失败，请检查网络连接、代理设置，或更换接口地址后重试。" & vbCrLf & vbCrLf & Message
        ElseIf TypeOf Failure Is FormatException Then
            Message = "接口返回的内容不是有效的 JSON，可能是地址填写有误，或中间被代理拦截。" & vbCrLf & vbCrLf & Message
        End If
        RunInUi(Sub() MyMsgBox("AI 分析失败。" & vbCrLf & vbCrLf & Message, "AI 日志分析", IsWarn:=True))
    End Sub

    ''' <summary>
    ''' 去掉 AI 输出里的 Markdown 装饰，让弹窗里显示的文本更整洁。
    ''' </summary>
    Private Function CleanAiText(Text As String) As String
        If String.IsNullOrWhiteSpace(Text) Then Return ""
        Dim Lines As String() = Text.Replace(vbCrLf, vbLf).Split(CChar(vbLf))
        Dim Result As New List(Of String)
        For Each Line In Lines
            Dim NewLine As String = Line.TrimEnd()
            '去掉标题符号
            Dim Trimmed As String = NewLine.TrimStart()
            If Trimmed.StartsWithF("#") Then
                NewLine = Trimmed.TrimStart("#"c).Trim()
            End If
            '去掉粗体与行内代码标记
            NewLine = NewLine.Replace("**", "")
            NewLine = NewLine.Replace("`", "")
            Result.Add(NewLine)
        Next
        Return Result.Join(vbCrLf).Trim()
    End Function

    ''' <summary>
    ''' 截取字符串的开头部分，用于把接口原文塞进报错信息。
    ''' </summary>
    Private Function ClipText(Text As String, Length As Integer) As String
        If Text Is Nothing Then Return ""
        If Text.Length <= Length Then Return Text
        Return Text.Substring(0, Length) & "……"
    End Function

#End Region

End Module
