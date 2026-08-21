param(
    [string]$ApiBaseUrl = 'http://localhost:8006/api'
)

$ErrorActionPreference = 'Stop'

function Assert-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "ASSERTION FAILED: $Message"
    }
}

function Invoke-Api {
    param(
        [ValidateSet('GET', 'POST', 'PUT', 'DELETE')]
        [string]$Method,
        [string]$Path,
        [string]$Token,
        [object]$Body
    )

    $params = @{
        Method = $Method
        Uri = "$ApiBaseUrl$Path"
        ContentType = 'application/json'
    }

    if ($Token) {
        $params.Headers = @{ Authorization = "Bearer $Token" }
    }

    if ($null -ne $Body) {
        $params.Body = $Body | ConvertTo-Json -Depth 10
    }

    Invoke-RestMethod @params
}

$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$email = "leave-rejoin-$suffix@playpredict.test"
$password = 'TestDemo123!'

Write-Output "Registering isolated PLAYER $email"
$auth = Invoke-Api -Method POST -Path '/auth/register' -Body @{
    firstName = 'Leave'
    lastName = 'Rejoin'
    email = $email
    password = $password
}
$token = $auth.token
Assert-Condition ($null -ne $token) 'Registration must return a JWT.'

$officials = Invoke-Api -Method GET -Path '/leagues/officials' -Token $token
$official = $officials | Where-Object { $_.leagueType -eq 'Official' } | Select-Object -First 1
Assert-Condition ($null -ne $official) 'An active Official League must be available.'
Write-Output ("Selected Official: id={0}, isActive={1}, isParticipant={2}" -f $official.id, $official.isActive, $official.isParticipant)
Assert-Condition ([bool]$official.isActive) 'Selected Official League must be active.'
Assert-Condition (-not [bool]($official.isParticipant)) 'Isolated PLAYER must not already participate in the selected Official League.'

Write-Output "Joining Official League $($official.id)"
Invoke-Api -Method POST -Path "/leagues/$($official.id)/join" -Token $token -Body @{} | Out-Null

$mineAfterJoin = Invoke-Api -Method GET -Path '/leagues/mine' -Token $token
Assert-Condition (($mineAfterJoin.id -contains $official.id)) 'Official League must appear in Mis Ligas after joining.'

$matches = Invoke-Api -Method GET -Path "/leagues/$($official.id)/matches" -Token $token
$match = $matches | Where-Object { $_.canPredict } | Select-Object -First 1
Assert-Condition ($null -ne $match) 'Official League must have a match open for predictions.'

Write-Output "Saving prediction for match $($match.id)"
$prediction = Invoke-Api -Method POST -Path '/predictions' -Token $token -Body @{
    leagueId = [int]$official.id
    matchId = [int]$match.id
    predictedHomeScore = 2
    predictedAwayScore = 1
}
Assert-Condition ($prediction.predictedHomeScore -eq 2 -and $prediction.predictedAwayScore -eq 1) 'Prediction must be saved as 2-1.'

Write-Output 'Leaving Official League with an existing prediction'
Invoke-Api -Method DELETE -Path "/leagues/$($official.id)/leave" -Token $token | Out-Null

$mineAfterLeave = Invoke-Api -Method GET -Path '/leagues/mine' -Token $token
Assert-Condition (-not ($mineAfterLeave.id -contains $official.id)) 'Official League must disappear from Mis Ligas after leaving.'

$officialsAfterLeave = Invoke-Api -Method GET -Path '/leagues/officials' -Token $token
$officialAfterLeave = $officialsAfterLeave | Where-Object { $_.id -eq $official.id } | Select-Object -First 1
Assert-Condition ($null -ne $officialAfterLeave) 'Official League must remain available in Explorar.'
Assert-Condition (-not [bool]($officialAfterLeave.isParticipant)) 'Explorar must offer Participar after leaving.'

Write-Output 'Rejoining the same Official League'
Invoke-Api -Method POST -Path "/leagues/$($official.id)/join" -Token $token -Body @{} | Out-Null

$mineAfterRejoin = Invoke-Api -Method GET -Path '/leagues/mine' -Token $token
Assert-Condition (($mineAfterRejoin.id -contains $official.id)) 'Official League must reappear in Mis Ligas after rejoining.'

$matchesAfterRejoin = Invoke-Api -Method GET -Path "/leagues/$($official.id)/matches" -Token $token
$sameMatch = $matchesAfterRejoin | Where-Object { $_.id -eq $match.id } | Select-Object -First 1
Assert-Condition ($null -ne $sameMatch.myPrediction) 'Previous prediction must still exist after rejoining.'
Assert-Condition ($sameMatch.myPrediction.id -eq $prediction.id) 'Rejoin must expose the same persisted prediction.'
Assert-Condition ($sameMatch.myPrediction.predictedHomeScore -eq 2 -and $sameMatch.myPrediction.predictedAwayScore -eq 1) 'Persisted prediction values must remain 2-1.'

Write-Output 'PASS: join -> predict -> leave -> explore -> rejoin preserves prediction.'
