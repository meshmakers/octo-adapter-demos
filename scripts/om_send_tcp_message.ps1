param (
    [int]$port = 8000
)
$server = "127.0.0.1"   
$message = '{"Key": "Value"}'  

$client = New-Object System.Net.Sockets.TcpClient
$client.Connect($server, $port)

# Convert message to byte array
$stream = $client.GetStream()
$writer = New-Object System.IO.StreamWriter $stream
$writer.AutoFlush = $true
$writer.WriteLine($message)

$writer.Close()
$stream.Close()
$client.Close()

Write-Host "Message sent: $message"
