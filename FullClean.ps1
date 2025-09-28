Get-ChildItem -Path . -Include bin,obj,objNetFramework -Recurse -Directory -Force |
	Where-Object { $_.FullName -notlike "*FRBDK\AnimationEditor\PreviewProject\bin*" } |
	ForEach-Object {
		Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
	}
