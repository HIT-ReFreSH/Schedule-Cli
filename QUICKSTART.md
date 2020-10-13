# 使用说明

## 安装

## 从Github Release

前往[Release](https://github.com/HIT-ReFreSH/Schedule-Cli/releases)下载操作系统对应的压缩包，解压后即可运行。

### 从包管理系统

请确保您在电脑上安装了[.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/)
Schedule-Cli被打包为dotnet global tool，因此在终端中执行`dotnet tool install HitRefresh.Schedule-cli --global`即可完成安装

如果需要更新，请先运行`dotnet tool uninstall HitRefresh.Schedule-cli --global`

## 在教务处网站上获取课表

请登录jwts，然后在个人课表查询处下载xls格式的课表。

## 运行Schedule-Cli

### 快速使用

直接执行`HRSchedule -i <.xls> -o <.ics>`即可

除此之外，还支持以下选项：

![](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-11.png)

### 细节使用

执行`HRSchedule`命令来启动程序

输入'ls'即可获得所有可用命令:

![](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-6.png)

#### 一般用法

`Ld <.xls>`可以导入Xls格式的课表，然后可以使用`Save <.json>`将其保存到Json。

Json格式课表可以使用`Lj <.json>`来导入。New则是从头开始手动创建课表。

使用`V`指令，可以查看整张课表的课程情况

![](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-7.png)

使用`Ex <.ics>`指令把课表导出到ics

使用`det <课名>`可以查看某个课程详细信息

![](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-8.png)

`Ed <课程名>`则可以对课程进行编辑

`Rm <课程名>`删除课程。


`ImpCs <.json>`和`ExpCs <课名> <.json>`可以导入或者导出单个课程。
Add则是从头开始手动加入课程。

`dmap <源日期>[ <目标日期>]`可以进行日期映射，如：

- `dmap 2020/10/10`将2020/10/10停课
- `dmap 2020/10/1 2020/10/10`将2020/10/1的课程串到2020/10/10上课

使用`V`指令，也可以查看这些日期映射的情况

![](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-9.png)

更多可用指令，可输入`Ls`查看

## iCalendar格式的使用

### Windows日历 如何导入

**请注意，Windows版“日历”应用只能将事件导入到已经存在的日历中，这可能是不安全的，因此作者建议采用网页版Outlook，或者Google日历来完成事件导入。**

先使用您的**电子邮件账户**登录Windows日历程序，然后使用Windows日历打开生成的`ics`文件，自动显示导入。

根据提示，选择指定的日历即可完成导入。

![image1](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-1.png)

导入后，日历将与您登录的电子邮件账户同步，在移动端登录邮箱也会同步导入的日历。

### Outlook日历如何导入

**注：Outlook日历默认会针对没有提醒的时间会添加提前15分钟的提醒，这个可以在设置里改成开始时提醒，或者干脆在导入ics那步用本地的windows/手机日历导入即可。**

1. 首先登陆网页版[网页版Outlook日历](https://outlook.live.com/calendar/)进行导入。
2. 在左边栏中点击"添加日历"![image2](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-3.png)
3. 在弹出的窗口中，如图示完成新建日历。![image3](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-4.png)
4. 将ICS描述的事件导入到新建的日历中。![image4](https://github.com/HIT-ReFreSH/Schedule-Cli/raw/master/images/image-5.png)

### Google日历 如何导入

请参考[将活动导入到 Google 日历](https://support.google.com/calendar/answer/37118?hl=zh-Hans)进行导入。

在导入后，日历将于您的Gmail账户同步，在移动端登录Gmail账户，或者下载Google日历客户端就可以使用。

### iOS 如何导入

#### 方法一

在Windows下使用Windows日历，Outlook日历或者Google日历，在iOS的'邮件'应用中登录对应的电子邮件账户就可以导入日历到iOS设备。

#### 方法二

在Windows下使用电子邮件将`ics`文件通过QQ传到手机，或者作为附件发送到iOS`邮件`应用中登录的账户，按照提示即可完成导入。

### Android 如何导入

#### 方法一

在Windows下使用Windows日历，Outlook日历或者Google日历，在您使用系统的`日历`应用中登录对应的电子邮件账户就可以导入日历到Android设备。

#### 方法二

在Windows下使用电子邮件将`ics`文件通过QQ传到手机，选择使用`日历`打开。如果您的系统无法使用日历打开`ics`文件，建议您安装`Google 日历`（无需登录即可导入）或者其他支持的日历软件（欢迎在PR中提出）。
