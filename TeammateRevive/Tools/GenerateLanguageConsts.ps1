$LangPath = [System.IO.Path]::Combine($PSScriptRoot, '..\Localization\Languages\en\tokens.json')
$ResultPath = [System.IO.Path]::Combine($PSScriptRoot, '..\Localization\LanguageConsts.cs')

$dict = (Get-Content $LangPath | ConvertFrom-Json).strings

function GenerateSummary($value) {
    # escaping opening tag
    $value = $value -replace '<','&lt;'
    $result = "    /// <summary>`n    /// [EN] `"$(($value -replace '(?<NL>\n)','${NL}    /// '))`"`n    /// </summary>"
    return  $result
}

$fileContent = "// This file is auto-generated from GenerateLanguageConsts.ps1 script, do not edit by hand.

// ReSharper disable InconsistentNaming
namespace TeammateRevive.Localization;

public class LanguageConsts
{$($dict.PSObject.Properties | % { "`n$(GenerateSummary $_.Value)`n    public static string $($_.Name) = nameof($($_.Name));`n" })
}
"
Write-Host $fileContent
[System.IO.File]::WriteAllText($ResultPath, $fileContent)
