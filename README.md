# MusicFree

## 打包

```shell
# Android
dotnet build -f:net7.0-android -c:release /p:CreatePackage=true
```

## Android 权限

> permission

```xml
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.VIBRATE" />
```

> sdk

```
<uses-sdk android:minSdkVersion="26" android:targetSdkVersion="33" />
```

## Mac 配置
> Info.plist

```
# dict
-NSAppTransportSecurity
# bool
--NSAllowsArbitraryLoads Yes
```

```
<key>NSAppTransportSecurity</key>
<dict>
	<key>NSAllowsArbitraryLoads</key>
	<true/>
</dict>
```