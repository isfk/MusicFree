.PHONY: build_andorid
build_andorid:
	dotnet build -f:net7.0-android -c:release /p:CreatePackage=true

.PHONY: build_mac
build_mac:
	dotnet build -f:net7.0-maccatalyst -c:Release /p:CreatePackage=true