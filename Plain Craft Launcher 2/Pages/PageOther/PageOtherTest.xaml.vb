Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Ookii.Dialogs.Wpf

Public Class PageOtherTest

    '今日人品

    ''' <summary>当天已测算的结果缓存，-1 代表未测算。</summary>
    Private Shared JrrpResult As Integer = -1
    ''' <summary>当前页面实例（若已打开），用于同步更新页面显示。</summary>
    Private Shared FrmInstance As PageOtherTest

    Public Sub New()
        InitializeComponent()
        FrmInstance = Me
    End Sub

    ''' <summary>
    ''' 计算并展示今日人品。同一天内多次测算结果相同。
    ''' </summary>
    Public Shared Sub Jrrp()
        If JrrpResult < 0 Then
            '以日期与用户名作为种子，确保同一天内结果稳定、不同人不同天结果不同
            Dim Seed As String = Now.ToString("yyyy-MM-dd") & "|" & Environment.UserName & "|PCL-ReBorn"
            Using Md5 As MD5 = MD5.Create()
                Dim Hash As Byte() = Md5.ComputeHash(Encoding.UTF8.GetBytes(Seed))
                '取前两个字节组合，映射到 0~100
                JrrpResult = CInt((CUInt(Hash(0)) Or (CUInt(Hash(1)) << 8)) Mod 101UI)
            End Using
        End If
        Dim Comment As String
        Select Case JrrpResult
            Case 0
                Comment = "0！今天的你被世界针对了，建议直接睡觉，明天再战。"
            Case 1 To 20
                Comment = "运气很低……开荒需谨慎，别怪 PCL 没提醒你。"
            Case 21 To 40
                Comment = "运气一般，苟着发育比莽一波更靠谱。"
            Case 41 To 60
                Comment = "平平淡淡的一天，说不定稳扎稳打反而有意外的收获。"
            Case 61 To 80
                Comment = "运气不错！该挖的矿该下的 Mod 都可以安排上了。"
            Case 81 To 99
                Comment = "运气爆棚！适合开新档、装整合包、挑战高难度任务！"
            Case Else
                Comment = "100！天选之人！今天连苦力怕都会给你让路（并不会）！"
        End Select
        Dim Message As String = $"你今天的运气指数为 {JrrpResult} / 100。" & vbCrLf & Comment
        '若页面已打开，同步显示到页面上
        RunInUi(Sub()
                    Try
                        If FrmInstance IsNot Nothing AndAlso FrmInstance.LabJrrp IsNot Nothing Then
                            FrmInstance.LabJrrp.Text = $"今日人品：{JrrpResult} / 100 —— {Comment}"
                        End If
                    Catch ex As Exception
                        '页面尚未加载完成时忽略
                    End Try
                End Sub)
        MyMsgBox(Message, "今日人品")
    End Sub

    '自定义下载

    ''' <summary>
    ''' 新建自定义下载：向用户询问链接与保存位置，后台使用多线程下载引擎下载。
    ''' </summary>
    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Try
            If String.IsNullOrWhiteSpace(Url) Then
                Url = MyMsgBoxInput("自定义下载", "请输入要下载的文件链接：", HintText:="支持任意直链，例如 https://example.com/file.zip")
                If String.IsNullOrWhiteSpace(Url) Then Return '用户取消
            End If
            Url = Url.Trim

            '根据链接推断文件名，并让用户确认
            If String.IsNullOrWhiteSpace(FileName) OrElse FileName = "未知" Then
                Try
                    FileName = Uri.UnescapeDataString(Url.BeforeLast("?").AfterLast("/"))
                Catch ex As Exception
                    FileName = ""
                End Try
                If String.IsNullOrWhiteSpace(FileName) OrElse FileName.Contains("?") OrElse FileName.Contains("=") Then
                    FileName = "PCL-Download-" & Now.ToString("HHmmss")
                End If
            End If
            Dim InputFileName As String = MyMsgBoxInput("自定义下载", "请确认文件名（可修改）：", FileName)
            If String.IsNullOrWhiteSpace(InputFileName) Then Return '用户取消
            FileName = InputFileName

            '选择保存文件夹
            If String.IsNullOrWhiteSpace(Folder) Then
                RunInUiWait(Sub()
                                Dim Dialog As New VistaFolderBrowserDialog With {
                                    .Description = "选择文件保存位置",
                                    .ShowNewFolderButton = True
                                }
                                If Dialog.ShowDialog() = True Then Folder = Dialog.SelectedPath
                            End Sub)
                If String.IsNullOrWhiteSpace(Folder) Then Return '用户取消
            End If
            If Not Folder.EndsWithF("\", True) AndAlso Not Folder.EndsWithF("/", True) Then Folder &= "\"
            Dim LocalPath As String = Folder & FileName

            '重复任务检查
            Dim LoaderName As String = "自定义下载：" & FileName
            If LoaderTaskbar.Any(Function(l) l.Name = LoaderName) Then
                Hint("该文件已在下载中！", HintType.Red)
                Return
            End If

            '启动下载并显示在任务栏中
            Hint($"已开始下载：{FileName}", HintType.Blue)
            RunInThread(Sub()
                            Try
                                Dim InnerLoader As New LoaderDownload("下载 " & FileName, New List(Of NetFile) From {New NetFile({Url}, LocalPath)})
                                Dim Loader As New LoaderCombo(Of Integer)(LoaderName, New List(Of LoaderBase) From {InnerLoader})
                                Loader.OnStateChanged =
                                Sub(l As LoaderBase)
                                    If l.State = LoadState.Finished Then
                                        Hint($"下载完成：{LocalPath}", HintType.Green)
                                        '打开文件所在文件夹
                                        RunInUi(Sub() OpenExplorer(LocalPath))
                                        LoaderTaskbar.Remove(l)
                                    ElseIf l.State = LoadState.Failed OrElse l.State = LoadState.Canceled Then
                                        Hint($"下载失败：{FileName}，详情请查看日志", HintType.Red)
                                        LoaderTaskbar.Remove(l)
                                    End If
                                End Sub
                                Loader.Start()
                                LoaderTaskbarAdd(Loader)
                                FrmMain.BtnExtraDownload.ShowRefresh()
                                FrmMain.BtnExtraDownload.Ribble()
                            Catch ex As Exception
                                Logger.Error(ex, "自定义下载失败")
                            End Try
                        End Sub)
        Catch ex As Exception
            Logger.Error(ex, "开始自定义下载失败")
        End Try
    End Sub

    '页面按钮事件

    Private Sub BtnJrrp_Click(sender As Object, e As EventArgs) Handles BtnJrrp.Click
        Jrrp()
    End Sub

    Private Sub BtnCustomDownload_Click(sender As Object, e As EventArgs) Handles BtnCustomDownload.Click
        StartCustomDownload(Nothing, Nothing)
    End Sub

    '以下百宝箱功能暂未开放

    Public Shared Sub RubbishClear()
        Hint("该功能暂未开放，敬请期待……")
    End Sub
    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        If ShowHint Then Hint("该功能暂未开放，敬请期待……")
    End Sub
    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        If ShowHint Then Hint("该功能暂未开放，敬请期待……")
    End Sub
    Public Shared Function GetRandomCave() As String
        Return "百宝箱的其他功能还在施工中……"
    End Function
    Public Shared Function GetRandomHint() As String
        Return "百宝箱的其他功能还在施工中……"
    End Function
    Public Shared Function GetRandomPresetHint() As String
        Return "百宝箱的其他功能还在施工中……"
    End Function

End Class
