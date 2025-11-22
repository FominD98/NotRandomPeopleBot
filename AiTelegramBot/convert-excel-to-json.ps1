[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$excelPath = "C:\pet\NotRandomPeople\AiTelegramBot\pub_4765546.xlsx"
$jsonPath = "C:\pet\NotRandomPeople\AiTelegramBot\Data\heritage_objects_full.json"

$data = Import-Excel -Path $excelPath

$objects = @()
$id = 1

foreach ($row in $data) {
    $name = $row.'Наименование ОКН с указанием объектов, входящих в его состав, в соответствии с актом органа государственной власти о его постановке на государственную охрану'
    $district = $row.'Район'
    $address = $row.'Местонахождение ОКН с указанием объектов, входящих в его состав, в соответствии с актом органа государственной власти о его постановке на государственную охрану'
    $category = $row.'Категория'
    $regNumber = $row.'Регистрация ОКН в Едином государственном реестре объектов культурного наследия (памятников истории и культуры) народов Российской Федерации '

    if ([string]::IsNullOrWhiteSpace($name)) {
        continue
    }

    # Extract year from name (e.g., "1796 г." or "XIX век")
    $yearBuilt = $null
    if ($name -match '(\d{4})\s*г') {
        $yearBuilt = [int]$Matches[1]
    }

    # Map protection category
    $protectionCategory = switch ($category.Trim()) {
        "Ф" { "federal" }
        "Р" { "regional" }
        "М" { "local" }
        default { "regional" }
    }

    $obj = [ordered]@{
        id = $id.ToString()
        name = $name.Trim()
        latitude = 0.0
        longitude = 0.0
        category = ""
        shortDescription = ""
        history = ""
        interestingFacts = @()
        yearBuilt = $yearBuilt
        isUnescoSite = $false
        imageUrl = ""
        district = if ($district) { $district.Trim() } else { "" }
        address = if ($address) { $address.Trim() } else { "" }
        protectionCategory = $protectionCategory
        registrationNumber = if ($regNumber) { $regNumber.ToString() } else { "" }
    }

    $objects += $obj
    $id++
}

$result = @{
    objects = $objects
}

$json = $result | ConvertTo-Json -Depth 4 -Compress:$false
[System.IO.File]::WriteAllText($jsonPath, $json, [System.Text.Encoding]::UTF8)

Write-Host "Converted $($objects.Count) objects to $jsonPath"
