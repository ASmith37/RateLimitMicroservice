# PowerShell script to send requests to the MessageRateLimiter server

# Function to generate a random phone number
function Generate-RandomPhoneNumber {
    $random = Get-Random -Minimum 1000000000 -Maximum 9999999999
    return $random.ToString()
}

# Function to generate a random account number
function Generate-RandomAccountNumber {
    $random = Get-Random -Minimum 100000 -Maximum 999999
    return $random.ToString()
}

# Prompt for server address
$serverAddress = Read-Host "Enter the server address (default: localhost:5000)"
if (-not $serverAddress) {
    $serverAddress = "localhost:5000"
}

# Prompt for account number
$useRandomAccount = Read-Host "Use a random account number? (y/N)"
if ($useRandomAccount -eq 'y') {
    $accountNumber = Generate-RandomAccountNumber
    Write-Host "Generated Account Number: $accountNumber"
} else {
    $accountNumber = Read-Host "Enter the account number"
}

# Prompt for phone number
$useRandomPhone = Read-Host "Use a random phone number? (y/N)"
if ($useRandomPhone -eq 'y' -and $useRandomAccount -eq 'y') {
    $phoneNumber = Generate-RandomPhoneNumber
    Write-Host "Generated Phone Number: $phoneNumber"
} elseif ($useRandomPhone -eq 'y') {
    $phoneNumber = Generate-RandomPhoneNumber
    Write-Host "Generated Phone Number: $phoneNumber"
} else {
    $phoneNumber = Read-Host "Enter the phone number"
}

# Prompt for message rate
$messageRate = Read-Host "Enter the request rate (integer of messages per second)"
$messageRate = [int]$messageRate

# Initialize counters
$acceptedCount = 0
$rejectedCount = 0

# Function to send requests
function Send-Requests {
    param (
        [string]$url,
        [string]$accountNumber,
        [string]$phoneNumber,
        [int]$rate
    )

    $interval = 1000 / $rate
    $stop = $false
    $requestCounter = 0  # Counter for requests sent

    # Start sending requests
    while (-not $stop) {
        $startTime = Get-Date

        $body = @{
            AccountNumber = $accountNumber
            PhoneNumber = $phoneNumber
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop

        if ($response.CanSend) {
            $script:acceptedCount++
        } else {
            $script:rejectedCount++
        }

        # Increment the request counter
        $requestCounter++

        # Check if we've reached the specified rate
        if ($requestCounter -ge $rate) {
            Write-Host "Accepted: $acceptedCount, Rejected: $rejectedCount. Press Control-C to stop."
            $requestCounter = 0  # Reset the counter
        }

        # Calculate elapsed time and adjust sleep
        $elapsedTime = (Get-Date) - $startTime
        $sleepTime = $interval - $elapsedTime.TotalMilliseconds

        # Ensure we only sleep if there's time left
        if ($sleepTime -gt 0) {
            Start-Sleep -Milliseconds $sleepTime
        }
    }

    # Display final counts
    Write-Host "Total Accepted: $acceptedCount"
    Write-Host "Total Rejected: $rejectedCount"
}

# Start sending requests
$url = "http://$serverAddress/api/message/check-sendability"
Send-Requests -url $url -accountNumber $accountNumber -phoneNumber $phoneNumber -rate $messageRate