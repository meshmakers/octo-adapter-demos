param (
    [string]$tenantId = "meshtest"
)
octo-cli -c Config -asu "https://localhost:5001/" -isu "https://localhost:5003/" -bsu "https://localhost:5009/" -csu "https://localhost:5015/" -tid $tenantId
octo-cli -c Login -i