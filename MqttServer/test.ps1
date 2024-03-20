# Define the API endpoint URL
$apiUrl = "http://localhost:5000/"

# Define the API token
$apiToken = "6dc3017d-0458-4312-ac46-43bc4d137561"

# Define the headers with the API token
$headers = @{
    "Authorization" = "Bearer $apiToken"
}

# Make the HTTP GET request with the API token as a header
$response = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers

# Output the response
$response

Read-Host
