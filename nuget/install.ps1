param($installPath, $toolsPath, $package, $project)

# set native dll copy options
$xmpir32 = $project.ProjectItems.Item("xmpir32.dll")
$xmpir32.Properties.Item("CopyToOutputDirectory").Value = 1 # CopyToOutputDirectory = Copy always

$xmpir64 = $project.ProjectItems.Item("xmpir64.dll")
$xmpir64.Properties.Item("CopyToOutputDirectory").Value = 1 # CopyToOutputDirectory = Copy always
