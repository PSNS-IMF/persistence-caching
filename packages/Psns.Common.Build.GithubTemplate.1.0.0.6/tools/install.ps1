param($installPath, $toolsPath, $package, $project)

$path = [System.IO.Path]
$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
$solutionRoot = $path::GetDirectoryName($solution.FileName)
$projectRoot = $path::GetDirectoryName($project.FileName)

$solutionFolder = $solution.Projects | where-object { $_.ProjectName -eq "Solution Items" } | select -first 1
if(!$solutionFolder)
{ 
	$solutionFolder = $solution.AddSolutionFolder("Solution Items") 
}

$folderItems = Get-Interface $solutionFolder.ProjectItems ([EnvDTE.ProjectItems])

foreach($fileName in "README.md", "LICENSE.md", "template.nuspec")
{
	$filePath = "$projectRoot\$fileName"
	$targetPath = "$solutionRoot\$fileName"

	if($fileName -eq "template.nuspec")
	{
		$targetPath = $solutionRoot + "\" + $project.Name + ".nuspec"
	}

	$project.ProjectItems.Item($fileName).Remove()
	move-item -Force $filePath $targetPath
	$folderItems.AddFromFile($targetPath)
}

Add-Content -Force -Path ".\.gitignore" -Value "`r`n`r`n#HP Fortify Files`r`n`r`n*.fpr"