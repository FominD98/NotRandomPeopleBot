$data = Import-Excel -Path "C:\pet\NotRandomPeople\AiTelegramBot\pub_4765546.xlsx"
$cols = $data[0].PSObject.Properties.Name

# Column indices:
# 0 - № п/п
# 2 - Наименование ОКН
# 3 - Категория
# 5 - Местонахождение
# 6 - Район
# 20 - Регистрационный номер

$objects = @()
$id = 1

foreach ($row in $data) {
    $props = $row.PSObject.Properties
    $propsArray = @($props)

    $name = $propsArray[2].Value
    $category = $propsArray[3].Value
    $address = $propsArray[5].Value
    $district = $propsArray[6].Value
    $regNumber = $propsArray[20].Value

    if ([string]::IsNullOrWhiteSpace($name)) {
        continue
    }

    # Extract year from name (e.g., "1796 г." or just "1796")
    $yearBuilt = $null
    if ($name -match '(\d{4})') {
        $yearBuilt = [int]$Matches[1]
    }

    # Map protection category
    $catTrimmed = if ($category) { $category.ToString().Trim() } else { "" }
    $protectionCategory = switch ($catTrimmed) {
        "Ф" { "federal" }
        "Р" { "regional" }
        "М" { "local" }
        default { "regional" }
    }

    $obj = [ordered]@{
        id = $id.ToString()
        name = $name.ToString().Trim()
        latitude = 0.0
        longitude = 0.0
        category = ""
        shortDescription = ""
        history = ""
        interestingFacts = @()
        yearBuilt = $yearBuilt
        isUnescoSite = $false
        imageUrl = ""
        district = if ($district) { $district.ToString().Trim() } else { "" }
        address = if ($address) { $address.ToString().Trim() } else { "" }
        protectionCategory = $protectionCategory
        registrationNumber = if ($regNumber) { $regNumber.ToString() } else { "" }
    }

    $objects += $obj
    $id++
}

$result = @{
    objects = $objects
}

$json = $result | ConvertTo-Json -Depth 4
$jsonPath = "C:\pet\NotRandomPeople\AiTelegramBot\Data\heritage_objects_full.json"
[System.IO.File]::WriteAllText($jsonPath, $json, [System.Text.Encoding]::UTF8)

Write-Host "Converted $($objects.Count) objects to $jsonPath"
