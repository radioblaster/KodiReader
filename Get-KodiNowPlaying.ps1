<#
.SYNOPSIS
    Shows what Kodi is currently playing.
.DESCRIPTION
    Uses Kodi JSON-RPC over WebSocket (port 9090), which needs no username or password.
    Requires Kodi setting: Services > Control > "Allow remote control from applications on this system".
#>
param(
    [string]$KodiHost = "127.0.0.1",
    [int]$Port = 9090,
    [int]$TimeoutMs = 5000
)

$ErrorActionPreference = "Stop"
$script:RequestId = 0

# Sends one text message over the WebSocket.
function Send-WsText([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$Text) {
    $segment = [ArraySegment[byte]]::new([Text.Encoding]::UTF8.GetBytes($Text))
    $type = [System.Net.WebSockets.WebSocketMessageType]::Text
    [void]$Socket.SendAsync($segment, $type, $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
}

# Receives one complete text message (it may span several frames).
function Receive-WsText([System.Net.WebSockets.ClientWebSocket]$Socket) {
    $buffer = New-Object byte[] 8192
    $stream = New-Object System.IO.MemoryStream
    $timeout = [Threading.CancellationTokenSource]::new($TimeoutMs)
    do {
        $result = $Socket.ReceiveAsync([ArraySegment[byte]]::new($buffer), $timeout.Token).GetAwaiter().GetResult()
        if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
            throw "Kodi closed the connection."
        }
        $stream.Write($buffer, 0, $result.Count)
    } while (-not $result.EndOfMessage)
    [Text.Encoding]::UTF8.GetString($stream.ToArray())
}

# Calls a Kodi JSON-RPC method and returns its "result" part.
function Invoke-KodiRpc($Socket, [string]$Method, [hashtable]$Params = @{}) {
    $script:RequestId++
    $request = @{ jsonrpc = "2.0"; id = $script:RequestId; method = $Method; params = $Params }
    Send-WsText $Socket ($request | ConvertTo-Json -Depth 5 -Compress)

    while ($true) {
        $message = Receive-WsText $Socket | ConvertFrom-Json
        if ($message.id -ne $script:RequestId) { continue }   # skip notifications (e.g. Player.OnPause)
        if ($message.error) {
            throw "Kodi error in ${Method}: $($message.error.message) $($message.error.data | ConvertTo-Json -Depth 5 -Compress)"
        }
        return $message.result
    }
}

# Formats a Kodi time object {hours, minutes, seconds} as h:mm:ss.
function Format-KodiTime($Time) {
    "{0}:{1:D2}:{2:D2}" -f $Time.hours, $Time.minutes, $Time.seconds
}

# Prints the playing item and the playback position.
function Show-NowPlaying($Item, $State) {
    $title = if ($Item.title) { $Item.title } else { $Item.label }
    if ($Item.showtitle) {
        $title = "{0} - S{1:D2}E{2:D2} - {3}" -f $Item.showtitle, $Item.season, $Item.episode, $title
    }

    Write-Host "Type    : $($Item.type)"
    Write-Host "Title   : $title"
    if ($Item.year) { Write-Host "Year    : $($Item.year)" }
    Write-Host "File    : $($Item.file)"
    Write-Host ("Position: {0} / {1} ({2:N0} %)" -f (Format-KodiTime $State.time), (Format-KodiTime $State.totaltime), $State.percentage)
    Write-Host "Paused  : $($State.speed -eq 0)"
}

$socket = [System.Net.WebSockets.ClientWebSocket]::new()
try {
    [void]$socket.ConnectAsync([Uri]"ws://${KodiHost}:${Port}/jsonrpc", [Threading.CancellationToken]::None).GetAwaiter().GetResult()

    $players = @(Invoke-KodiRpc $socket "Player.GetActivePlayers")
    if ($players.Count -eq 0) {
        Write-Host "Nothing is playing."
        return
    }

    $playerId = $players[0].playerid
    $item = (Invoke-KodiRpc $socket "Player.GetItem" @{
        playerid   = $playerId
        properties = @("title", "showtitle", "season", "episode", "year", "file")
    }).item
    $state = Invoke-KodiRpc $socket "Player.GetProperties" @{
        playerid   = $playerId
        properties = @("time", "totaltime", "percentage", "speed")
    }

    Show-NowPlaying $item $state
}
finally {
    $socket.Dispose()
}
