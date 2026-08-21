param(
    [string]$ApiBaseUrl = 'http://localhost:8006/api'
)

$ErrorActionPreference = 'Stop'

function Assert-Condition {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERTION FAILED: $Message" }
}

function Invoke-Api {
    param(
        [ValidateSet('GET', 'POST', 'DELETE')][string]$Method,
        [string]$Path,
        [string]$Token,
        [object]$Body
    )
    $params = @{ Method = $Method; Uri = "$ApiBaseUrl$Path"; ContentType = 'application/json' }
    if ($Token) { $params.Headers = @{ Authorization = "Bearer $Token" } }
    if ($null -ne $Body) { $params.Body = $Body | ConvertTo-Json -Depth 10 }
    Invoke-RestMethod @params
}

$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$auth = Invoke-Api -Method POST -Path '/auth/register' -Body @{
    firstName = 'Prediction'
    lastName = 'Delete'
    email = "prediction-delete-$suffix@playpredict.test"
    password = 'TestDemo123!'
}
$token = $auth.token
Assert-Condition ($null -ne $token) 'Registration must return a JWT.'

$officials = Invoke-Api -Method GET -Path '/leagues/officials' -Token $token
$official = $officials | Where-Object { $_.leagueType -eq 'Official' -and $_.isActive } | Select-Object -First 1
Assert-Condition ($null -ne $official) 'An active Official League must exist.'
Invoke-Api -Method POST -Path "/leagues/$($official.id)/join" -Token $token -Body @{} | Out-Null

$matches = Invoke-Api -Method GET -Path "/leagues/$($official.id)/matches" -Token $token
$match = $matches | Where-Object { $_.canPredict -and $null -eq $_.myPrediction } | Select-Object -First 1
Assert-Condition ($null -ne $match) 'A never-predicted open match must exist.'

$prediction = Invoke-Api -Method POST -Path '/predictions' -Token $token -Body @{
    leagueId = [int]$official.id
    matchId = [int]$match.id
    predictedHomeScore = 0
    predictedAwayScore = 0
}
Assert-Condition ($prediction.predictedHomeScore -eq 0 -and $prediction.predictedAwayScore -eq 0) '0-0 must be persisted.'

$afterCreate = Invoke-Api -Method GET -Path "/leagues/$($official.id)/matches" -Token $token
$createdMatch = $afterCreate | Where-Object { $_.id -eq $match.id } | Select-Object -First 1
Assert-Condition ($createdMatch.myPrediction.id -eq $prediction.id) 'Reload must expose the persisted prediction.'

Invoke-Api -Method DELETE -Path "/predictions/$($prediction.id)" -Token $token | Out-Null

$afterDelete = Invoke-Api -Method GET -Path "/leagues/$($official.id)/matches" -Token $token
$deletedMatch = $afterDelete | Where-Object { $_.id -eq $match.id } | Select-Object -First 1
Assert-Condition ($null -eq $deletedMatch.myPrediction) 'Reload after DELETE must expose no Prediction.'

Write-Output 'PASS: never predicted -> save 0-0 -> reload -> delete -> reload remains without Prediction.'
