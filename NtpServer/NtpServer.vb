Imports System.Net
Imports System.Net.Sockets

Public Class NtpServer
    Private Const NTP_EPOCH_TICKS As Long = 599266080000000000L

    Private _port As Integer
    Private _client As UdpClient
    Private _running As Boolean
    Private _thread As Threading.Thread

    Public Sub New(Optional port As Integer = 123)
        _port = port
    End Sub

    Public Sub Start()
        If _running Then Return
        _running = True
        _thread = New Threading.Thread(AddressOf Run)
        _thread.IsBackground = True
        _thread.Start()
    End Sub

    Public Sub [Stop]()
        _running = False
        If _client IsNot Nothing Then
            Try
                _client.Close()
            Catch
            End Try
        End If
        If _thread IsNot Nothing Then
            _thread.Join(1000)
        End If
    End Sub

    Public ReadOnly Property IsRunning As Boolean
        Get
            Return _running
        End Get
    End Property

    Public ReadOnly Property Port As Integer
        Get
            Return _port
        End Get
    End Property

    Private _requestCount As Long
    Public ReadOnly Property RequestCount As Long
        Get
            Return _requestCount
        End Get
    End Property

    Public Function TestClient() As Boolean
        Try
            Dim ep As New IPEndPoint(IPAddress.Loopback, _port)
            Dim client As New UdpClient()
            client.Client.SendTimeout = 3000
            client.Client.ReceiveTimeout = 3000

            Dim pkt(47) As Byte
            pkt(0) = (0 << 6) Or (4 << 3) Or 3
            pkt(40) = 1

            client.Send(pkt, pkt.Length, ep)
            Dim resp = client.Receive(ep)
            client.Close()
            Return resp.Length >= 48 AndAlso (resp(0) And &H7) = 4
        Catch
            Return False
        End Try
    End Function

    Private Sub Run()
        Try
            _client = New UdpClient(_port)
            Dim ep As New IPEndPoint(IPAddress.Any, 0)

            While _running
                Try
                    Dim data = _client.Receive(ep)
                    If data.Length >= 48 AndAlso (data(0) And &H7) = 3 Then
                        Dim originateTs As ULong = 0
                        For i As Integer = 0 To 7
                            originateTs = (originateTs << 8) Or data(40 + i)
                        Next
                        Dim resp = BuildResponse(data, originateTs)
                        _client.Send(resp, resp.Length, ep)
                        _requestCount += 1
                    End If
                Catch ex As ObjectDisposedException
                    Exit While
                Catch ex As Exception
                End Try
            End While
        Catch ex As Exception
        Finally
            If _client IsNot Nothing Then
                _client.Close()
                _client = Nothing
            End If
            _running = False
        End Try
    End Sub

    Private Function ToNtpTimestamp(t As DateTime) As ULong
        Dim ticks As Long = t.Ticks - NTP_EPOCH_TICKS
        Dim secs As UInteger = CType(ticks \ 10000000L, UInteger)
        Dim frac As UInteger = CType((ticks Mod 10000000L) * 4294967296L \ 10000000L, UInteger)
        Return (CULng(secs) << 32) Or frac
    End Function

    Private Sub WriteBE64(buf As Byte(), offset As Integer, value As ULong)
        Dim v As ULong = value
        For i As Integer = 7 To 0 Step -1
            buf(offset + i) = v Mod 256
            v >>= 8
        Next
    End Sub

    Private Function BuildResponse(packet As Byte(), originateTs As ULong) As Byte()
        Dim vn As Byte = (packet(0) >> 3) And &H7
        Dim now As DateTime = DateTime.UtcNow
        Dim refTs As ULong = ToNtpTimestamp(now)
        Dim recvTs As ULong = ToNtpTimestamp(now)
        Dim xmitTs As ULong = ToNtpTimestamp(now)

        Dim resp(47) As Byte
        resp(0) = (0 << 6) Or (vn << 3) Or 4
        resp(1) = 3
        resp(2) = 4
        resp(3) = &HE9

        WriteBE64(resp, 16, refTs)
        WriteBE64(resp, 24, originateTs)
        WriteBE64(resp, 32, recvTs)
        WriteBE64(resp, 40, xmitTs)

        Return resp
    End Function
End Class
