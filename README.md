# 课表-命令行版

![GitHub release (latest by date)](https://img.shields.io/github/v/release/HIT-ReFreSH/Schedule-Cli)

![GitHub issues](https://img.shields.io/github/issues/HIT-ReFreSH/Schedule-Cli)

本应用使用dotnet core编写，完全开放、开源

运行时下载：https://dotnet.microsoft.com/download/dotnet-core/

使用说明参见[此处](https://github.com/HIT-ReFreSH/Schedule-Cli/blob/master/QUICKSTART.md)

![GPL3orLater](https://www.gnu.org/graphics/gplv3-or-later.png)

如果您在使用本程序的时候遇到了BUG或者有什么好的建议，欢迎您在Issuses中提出。

如果您对本程序进行了改进，欢迎PR！


## 主要功能

- 可以将从jwts上下载的XLS格式课表导入
- 将课表储存为JSON，方便保存和打开
- 导出课表为`iCalendar (RFC 5545) `格式以便导入到日历软件中
- 可以手动增删和编辑课程，支持单个课程的导入导出到JSON，方便共享和蹭课

## 为什么要使用本程序？

- 本程序导出的` iCalendar (RFC 5545) `格式受世界上几乎所有的现代操作系统支持，实现了真正的跨平台
- 由于日历一般为系统自带应用，因此UI往往与系统原生UI相同，并且系统的日历应用往往有优化。而且若不喜欢系统的日历应用，还可以使用第三方的日历应用。
- **本程序导出的课表支持在开课前进行提醒，能够有效防止忘课。**
- 可以将课表或者单个课程储存为JSON，方便**共享课程(蹭课)**
- 支持手动增加/删除课程 **(可以追加考试时间记录)**
- 方便的共享课表和课程
- 日期映射，快速适应学校政策变化

## 一些特性

- 课表生成的日历，默认在课程开始前25分钟进行提醒

## 将来可能会实现的功能

- 自动完成从Jwts下载xls课表的功能

## 已知BUG

- 暂无

本应用仅适用于**哈尔滨工业大学**的课程导出，不兼容其他学校的系统