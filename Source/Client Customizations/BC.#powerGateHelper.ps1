#==============================================================================#
# (c) 2022 coolOrange s.r.l.                                                   #
#                                                                              #
# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER    #
# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES  #
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.   #
#==============================================================================#

if ($processName -notin @('Connectivity.VaultPro', 'Inventor')) {
    return
}

#Remark: this function is used to identify differences between the current ERP Item as fetched from the ERP sytstem and the new
#        ERP Item, computed based on the mapping from the Vault entity (see powerGateMappingFiles.ps1 and powerGAteMappingItems.ps1)
function GetErpObjectDifferences($erpItem, $newErpItem, $fieldsToExclude = @()) {

    $differences = @()
    $properties = $erpItem | Get-Member -MemberType Properties | Select-Object Name
    foreach ($property in $properties) {
        if ($property.Name -eq "_Keys" -or $property.Name -eq "_Properties") { continue }
        if ($fieldsToExclude -contains $property.Name) { continue }

        if ([string]$erpItem.$($property.Name) -ne [string]$newErpItem.$($property.Name)) {
            if (([string]$erpItem.$($property.Name)).Length -gt 20) {
                $currentValue = ([string]$erpItem.$($property.Name)).Substring(0, 20).Trim() + "..."
            }
            else {
                $currentValue = ([string]$erpItem.$($property.Name))
            }
            if (([string]$newErpItem.$($property.Name)).Length -gt 20) {
                $newValue = ([string]$newErpItem.$($property.Name)).Substring(0, 20).Trim() + "..."
            }
            else {
                $newValue = ([string]$newErpItem.$($property.Name))
            }

            $differences += "$($property.Name): $currentValue -> $newValue"
        }
    }

    return $differences
}

#Remark: this function is used to display an Image in a WPF control (see Vault-Tab-Item-ErpItem.ps1 and Vault-Tab-File-ErpItem.ps1)
function GetImageFromByteArray($thumbnail) {
    if (-not $thumbnail) { return $null }
    try {
        $bmp = [System.Drawing.Bitmap]::FromStream((New-Object System.IO.MemoryStream (@(, $thumbnail))))
        $memory = New-Object System.IO.MemoryStream
        $null = $bmp.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
        $memory.Position = 0
        $img = New-Object System.Windows.Media.Imaging.BitmapImage
        $img.BeginInit()
        $img.StreamSource = $memory
        $img.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
        $img.EndInit()
        $img.Freeze()
        $memory.Close()
        $memory.Dispose()
        return $img        
    }
    catch {
        return $null
    }
}

function FindVisualChildren([System.Windows.DependencyObject] $parent, [Type] $type) {
    $results = @()
    for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($Parent); $i++) {
        $child = [System.Windows.Media.VisualTreeHelper]::GetChild($Parent, $i)
        if ($child -is $Type) {
            $results += $child
        }
        $results += FindVisualChildren -Parent $child -Type $Type
    }
    return $results
}

function FindLogicalChildren([System.Windows.DependencyObject] $parent, [Type] $type) {
    $results = @()
    foreach ($child in [System.Windows.LogicalTreeHelper]::GetChildren($Parent)) {
        if ($child -is [System.Windows.DependencyObject]) {
            if ($child -is $Type) {
                $results += $child
            }
            $results += FindLogicalChildren -Parent $child -Type $Type
        }
    }
    return $results
}

function ApplyVaultTheme($control) {
    $currentTheme = [Autodesk.DataManagement.Client.Framework.Forms.SkinUtils.WinFormsTheme]::Instance.CurrentTheme
    $md = $control.Resources.MergedDictionaries[0]
    if (-not $md) { return }

    # Add the current theme to the resource dictionary
    $td = [System.Management.Automation.PSSerializer]::Deserialize([System.Management.Automation.PSSerializer]::Serialize($md, 20))
    $td.Source = New-Object Uri("pack://application:,,,/Autodesk.DataManagement.Client.Framework.Forms;component/SkinUtils/WPF/Themes/$($currentTheme)Theme.xaml", [System.UriKind]::Absolute)
    $control.Resources.MergedDictionaries.Clear()
    $control.Resources.MergedDictionaries.Add($td);
    $control.Resources.MergedDictionaries.Add($md);

    if ($control -is [Autodesk.DataManagement.Client.Framework.Forms.Controls.WPF.ThemedWPFWindow]) {
        # Set Vault to be the owner of the window
        $interopHelper = New-Object System.Windows.Interop.WindowInteropHelper($control)
        $interopHelper.Owner = (Get-Process -Id $PID).MainWindowHandle

        # Set the window style, depending on the current theme
        $styleKey = if ($currentTheme -eq "Default") { "DefaultThemedWindowStyle" } else { "DarkLightThemedWindowStyle" }
        $control.Style = $control.Resources.MergedDictionaries[0][$styleKey]    
    }
    elseif ($control -is [System.Windows.Controls.ContentControl]) {
        # powerEvents to reload the tab?!
    }
    else {
        return
    }

    # Workaround to fix the DataGrid colors in light theme
    <#
    if ($currentTheme -eq "Light") {
        $dataGrids = FindLogicalChildren -Parent $control -Type ([System.Windows.Controls.DataGrid])
        foreach ($dataGrid in $dataGrids) {
            $cellStyle = $dataGrid.CellStyle
            $trigger = New-Object Windows.Trigger
            $trigger.Property = [Windows.Controls.DataGridCell]::IsSelectedProperty
            $trigger.Value = $true
            $color = [System.Windows.Media.ColorConverter]::ConvertFromString("#e1f2fa")
            $brush = New-Object System.Windows.Media.SolidColorBrush $color
            $brush.Freeze()
            $trigger.Setters.Add((New-Object Windows.Setter([Windows.Controls.Control]::BackgroundProperty, $brush)))
            $trigger.Setters.Add((New-Object Windows.Setter([Windows.Controls.Control]::ForegroundProperty, [Windows.Media.Brushes]::Black)))
            $cellStyle.Triggers.Add($trigger)
            $dataGrid.CellStyle = $cellStyle            
        }
    }
    #>
}