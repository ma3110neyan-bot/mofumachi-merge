$ErrorActionPreference='Stop'
. '/workspace/mofumachi-merge/scripts/visual-lock.ps1'
try {Get-BasePolishPatch '/tmp/noah-review-scope-check-hufw6n_e';throw 'REVIEW FAILURE: candidate scope downgrade accepted'} catch {if($_.Exception.Message -ne 'Accepted North checkpoint requires the Noah-only scope; previous boundary scope cannot reopen it.'){throw};Write-Output ('REVIEW PASS: '+$_.Exception.Message)}
