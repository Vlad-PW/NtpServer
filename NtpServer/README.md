# NtpServer — библиотека NTP-сервера на VB.NET

## Что делает

Поднимает локальный NTP-сервер (Network Time Protocol v4, stratum 3) в любом .NET-приложении. Сервер слушает UDP-порт, принимает NTP-запросы от клиентов (например, `w32tm`, Linux `ntpd`, любые NTP-клиенты) и отвечает текущим системным временем.

Используется, когда нужно синхронизировать время нескольких машин в локальной сети без доступа к внешним NTP-серверам.

## Как подключить

Скопируйте файл `NtpServer.dll` в папку вашего проекта.

**Через CLI:**
```bash
dotnet add reference путь\к\NtpServer.dll
```

**Или вручную в .vbproj:**
```xml
<ItemGroup>
  <Reference Include="NtpServer">
    <HintPath>путь\к\NtpServer.dll</HintPath>
  </Reference>
</ItemGroup>
```

**NuGet не требуется** — это локальная DLL.

## Как работать

### Создать и запустить

```vb
Imports NtpServer

Dim server As New NtpServer.NtpServer(123)  ' порт
server.Start()                                ' запуск в фоновом потоке
```

### Остановить

```vb
server.Stop()
```

### Свойства

| Свойство | Тип | Описание |
|---|---|---|
| `IsRunning` | `Boolean` | `True` пока сервер работает |
| `Port` | `Integer` | Порт, который слушает сервер |

### Полный пример (консоль)

```vb
Imports NtpServer

Module Program
    Sub Main(args As String())
        Dim port As Integer = If(args.Length > 0, CInt(args(0)), 123)
        Dim server As New NtpServer.NtpServer(port)

        AddHandler Console.CancelKeyPress,
            Sub(s, e)
                server.Stop()
                Console.WriteLine("Stopped")
            End Sub

        server.Start()
        Console.WriteLine($"NTP server on port {port}")

        While server.IsRunning
            Threading.Thread.Sleep(500)
        End While
    End Sub
End Module
```

### Пример из WinForms

```vb
Imports NtpServer

Public Class Form1
    Private _ntp As NtpServer.NtpServer

    Private Sub btnStart_Click(...) Handles btnStart.Click
        _ntp = New NtpServer.NtpServer(123)
        _ntp.Start()
        lblStatus.Text = "Running"
    End Sub

    Private Sub btnStop_Click(...) Handles btnStop.Click
        If _ntp IsNot Nothing Then _ntp.Stop()
        lblStatus.Text = "Stopped"
    End Sub
End Class
```

### Настройка клиентов Windows для синхронизации с вашим сервером

На клиентских машинах (от администратора):

```powershell
w32tm /config /manualpeerlist:192.168.x.x /syncfromflags:MANUAL
w32tm /update
w32tm /resync
```

Где `192.168.x.x` — IP машины, на которой запущен ваш NTP-сервер.

## Важно

- Порт **123** требует прав администратора. Для тестирования используйте порт > 1024.
- Перед запуском на порту 123 остановите штатную службу времени Windows:
  ```powershell
  net stop w32time
  ```
- Сервер не хранит состояние между запросами. Подходит для небольших локальных сетей.
- Точность соответствует точности системных часов.
