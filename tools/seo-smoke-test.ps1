param(
    [string]$BaseUrl = "http://127.0.0.1:5230"
)

$ErrorActionPreference = "Stop"

function Get-Page([string]$Path) {
    (Invoke-WebRequest -UseBasicParsing "$BaseUrl$Path").Content
}

function Assert-Contains([string]$Content, [string]$Expected, [string]$Message) {
    if (-not $Content.Contains($Expected)) {
        throw $Message
    }
}

$sitemapResponse = Invoke-WebRequest -UseBasicParsing "$BaseUrl/sitemap.xml"
$sitemap = [xml]$sitemapResponse.Content
$namespaceManager = [System.Xml.XmlNamespaceManager]::new($sitemap.NameTable)
$namespaceManager.AddNamespace("sm", "http://www.sitemaps.org/schemas/sitemap/0.9")
$sitemapUrls = $sitemap.SelectNodes("//sm:url", $namespaceManager)
$schoolUrls = $sitemap.SelectNodes('//sm:url/sm:loc[contains(text(), "/schools/wk_")]', $namespaceManager)

if ($sitemapUrls.Count -lt 4900) { throw "Sitemap URL count is unexpectedly low: $($sitemapUrls.Count)" }
if ($schoolUrls.Count -lt 4900) { throw "School URL count is unexpectedly low: $($schoolUrls.Count)" }
if (-not $sitemapResponse.Content.StartsWith('<?xml version="1.0" encoding="utf-8"?>')) {
    throw "Sitemap XML declaration is not UTF-8."
}

$indexHtml = Get-Page "/"
$filteredSchoolsHtml = Get-Page "/schools?q=東京"
$savedSchoolsHtml = Get-Page "/schools/saved"
$schoolDetailHtml = Get-Page "/schools/wk_d113299901022"

Assert-Contains $indexHtml "高校受験の学習コーチ" "The home title does not include the primary search intent."
Assert-Contains $filteredSchoolsHtml 'name="robots" content="noindex,follow"' "Filtered school results must be noindex,follow."
Assert-Contains $savedSchoolsHtml 'name="robots" content="noindex,follow"' "Saved schools must be noindex,follow."
Assert-Contains $schoolDetailHtml "EducationalOrganization" "School structured data is missing."
Assert-Contains $schoolDetailHtml "BreadcrumbList" "Breadcrumb structured data is missing."
Assert-Contains $indexHtml 'property="og:image"' "Open Graph image is missing."

$decodedSchoolDetailHtml = [System.Net.WebUtility]::HtmlDecode($schoolDetailHtml)
$jsonLdMatch = [regex]::Match($decodedSchoolDetailHtml, '<script type="application/ld\+json">(.*?)</script>', 'Singleline')
if (-not $jsonLdMatch.Success) { throw "JSON-LD script is missing." }
$jsonLd = $jsonLdMatch.Groups[1].Value | ConvertFrom-Json
$structuredDataTypes = @($jsonLd.'@graph' | ForEach-Object { $_.'@type' })
foreach ($requiredType in @("Organization", "WebSite", "BreadcrumbList", "EducationalOrganization")) {
    if ($requiredType -notin $structuredDataTypes) { throw "JSON-LD type is missing: $requiredType" }
}

$mainCount = [regex]::Matches($schoolDetailHtml, "<main[ >]").Count
if ($mainCount -ne 1) { throw "Expected one main landmark; found $mainCount." }

Write-Output "SEO smoke test passed: $($sitemapUrls.Count) sitemap URLs, $($schoolUrls.Count) schools."
