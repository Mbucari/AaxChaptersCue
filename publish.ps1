<# You must enable running powershell scripts.

	Set-ExecutionPolicy -Scope CurrentUser Unrestricted
#>

$pubDir = "bin\Publish"
Remove-Item $pubDir -Recurse -Force

dotnet publish -c Release AaxChaptersCue\AaxChaptersCue.csproj -p:PublishProfile=AaxChaptersCue\Properties\PublishProfiles\FolderProfile.pubxml


$verMatch = Select-String -Path 'AaxChaptersCue\AaxChaptersCue.csproj' -Pattern '<Version>(\d{0,3}\.\d{0,3}\.\d{0,3})</Version>'
$archiveName = "bin\AaxChaptersCue."+$verMatch.Matches.Groups[1].Value+".zip"
Get-ChildItem -Path $pubDir -Recurse |
	Compress-Archive -DestinationPath $archiveName -Force