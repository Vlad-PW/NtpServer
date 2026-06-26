Imports NtpServer

Module Program
    Sub Main(args As String())
        Dim port As Integer = If(args.Length > 0, Integer.Parse(args(0)), 123)
        Dim server As New NtpServer.NtpServer(port)

        AddHandler Console.CancelKeyPress,
            Sub(s, e)
                server.Stop()
                Console.WriteLine("Server stopped")
            End Sub

        server.Start()
        Console.WriteLine($"NTP server started on port {port} (press Ctrl+C to stop)")

        While server.IsRunning
            Threading.Thread.Sleep(500)
        End While
    End Sub
End Module
