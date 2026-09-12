# CSFF Better Ice Pit

给 **Card Survival: Fantasy Forest** 使用的 BepInEx Mod。

## 功能

- 将冰窖 `Snow Cold`、`Ice Cold` 被动效果的液体含量上限改为 **8100**（原为 2700），即约 3 天再排一次融水。
- 冰窖会向同一地点的冰块 (`IceBlock`) 与雪堆 (`SnowPile`) 提供 `SpoilageTime +2` 的远程被动效果，抵消其每回合的融化消耗。
- 当冰块 (`IceBlock`) 与雪堆 (`SnowPile`) 离开冰窖所在地点后，远程被动效果自动取消，恢复游戏原本的融化规则。
- 卡牌属性修改仅在游戏数据库初始化完成后执行一次。

## 构建

项目默认引用需求中指定的游戏目录及：

`E:\SteamLibrary\steamapps\common\Card Survival Fantasy Forest\BepInEx\core`

执行：

```powershell
dotnet build -c Release
```

产物：`bin\Release\net472\CSFFBetterIcePit.dll`

## 安装

将 DLL 放入：

`E:\SteamLibrary\steamapps\common\Card Survival Fantasy Forest\BepInEx\plugins\`

启动游戏即可。BepInEx 日志中出现 `CSFF Better Ice Pit loaded.` 即表示插件已加载。
