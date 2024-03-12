# Define the API endpoint URL
$apiUrl = "http://localhost:5000/"

# Define the API token
$apiToken = "123456"

# Define the headers with the API token
$headers = @{
    "Authorization" = "Bearer $apiToken"
}

# Make the HTTP GET request with the API token as a header
$response = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers

# Output the response
$response

Read-Host