using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PlasticMetal.MobileSuit;
using PlasticMetal.MobileSuit.ObjectModel;
using PlasticMetal.MobileSuit.Parsing;
using PlasticMetal.MobileSuit.ObjectModel.Future;
using System.Globalization;
using static HitRefresh.Schedule.ScheduleStatic;
using Ical.Net.Serialization;

namespace HitRefresh.Schedule
{
    /// <summary>
    /// 核心类
    /// </summary>
    [SuitInfo("ReFreSH-Schedule")]
    public class Program : CommandLineApplication<StartUpArgs>
    {
        static void Main(string[] args)
        {
            Suit.GetBuilder()
                .UsePrompt<PowerLineThemedPromptServer>()
                .UseLog(PlasticMetal.MobileSuit.Core.ILogger.OfFile(@"D:\SCM.log"))//Logger)
                .Build<Program>()
                .Run(args);


        }
        private ScheduleEntity Schedule { get; set; } = new ScheduleEntity();
        /// <summary>
        /// 构造函数
        /// </summary>
        public Program()
        {
            Text = "ReFreSH-Schedule";
        }
        /// <summary>
        /// 修改通知时间
        /// </summary>
        /// <param name="m"></param>
        [SuitInfo("修改通知时间：ChNt <时间-分钟数>")]
        [SuitAlias("ChNt")]
        public void ChangeNotificationTime([SuitParser(typeof(Parsers), nameof(Parsers.ParseInt))] int m)
        {
            Schedule.NotificationTime = m;
        }
        /// <summary>
        /// 向课表添加一个课程
        /// </summary>
        [SuitInfo("向课表添加一个课程条目:add <课程名称>")]
        public TraceBack Add(string courseName)
        {
            _ = Schedule[courseName];
            if (!int.TryParse(IO.ReadLine("输入星期(0-6,0表示周日)"), out var week)
            || week < 0 || week > 6) return TraceBack.InvalidCommand;
            for (int i = 0; i < 6; i++)
            {
                IO.WriteLine(Suit.CreateContentArray(
                    ($"{i}.", ConsoleColor.Yellow),
                    (((CourseTime)i).ToFriendlyName(), null)));
            }
            if (!int.TryParse(IO.ReadLine("输入节次(0-5)"), out var startTime) || startTime < 0 || startTime > 5)
                return TraceBack.InvalidCommand;
            var isLong = IO.ReadLine("这是两节连上的课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) == "n";
            var isLab = IO.ReadLine("这是实验课课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) == "n";

            var weekExpression = IO.ReadLine("输入教师-周数-教室表达式(正则：教师[周数|起始-截至[单|双]?](|[周数|起始-截至[单|双]?])*教室, 如张三[10]|李四[1|2-9单]正心11)", true);

            Schedule[courseName].AddSubEntry((DayOfWeek)week, (CourseTime)startTime, isLong, isLab, weekExpression);
            return TraceBack.AllOk;
        }
        /// <summary>
        /// 向课表导入一个JSON描述的课程
        /// </summary>
        /// <param name="path"></param>
        [SuitAlias("ImpCs")]
        [SuitInfo("向课表导入一个JSON描述的课程: ImpCs <.json>")]
        public void ImportCourse(string path)
        {
            if (!File.Exists(path))
            {
                IO.WriteLine("未找到文件。", OutputType.Error);
                return;
            }
            Schedule.Add(File.ReadAllText(path).AsCourse());
        }
        /// <summary>
        /// 从课表导出一个JSON描述的课程
        /// </summary>
        /// <param name="courseName"></param>
        /// <param name="path"></param>
        [SuitAlias("ExpCs")]
        [SuitInfo("从课表导出一个JSON描述的课程：ExpCs <课程名> <.json>")]
        public void ExportCourse(string courseName, string path = "")
        {
            if (Schedule is null) return;
            if (path == "")
                path = IO.ReadLine("输入保存文件位置") ?? "";
            try
            {
                File.WriteAllText(path, Schedule[courseName].ToJson());
            }
            catch
            {
                IO.WriteLine("写入错误。", OutputType.Error);
                //Environment.Exit(0);
            }
        }
        /// <summary>
        /// 从课表移除一个课程(或其子条目)
        /// </summary>
        /// <param name="course"></param>
        /// <param name="subId"></param>
        [SuitAlias("Rm")]
        [SuitInfo("从课表移除一个课程(或其子条目)：Remove <课程名>[ <子条目从0序号>]")]
        public void Remove(string course,
            [SuitParser(typeof(Parsers), nameof(Parsers.ParseInt))] int subId = -1)
        {
            if (Schedule is null) return;
            if (subId == -1)
            {
                Schedule.Remove(course);
            }
            else
            {
                Schedule[course].RemoveSubEntry(subId);
            }
        }
        /// <summary>
        /// 编辑课表中的课程(子条目)
        /// </summary>
        /// <param name="course"></param>
        /// <param name="subId"></param>
        /// <returns></returns>
        [SuitAlias("Ed")]
        [SuitInfo("编辑课表中的课程(子条目)：Edit <课程名>[ <子条目从0序号>]")]
        public TraceBack Edit(string course,
            [SuitParser(typeof(Parsers), nameof(Parsers.ParseInt))] int subId = -1)
        {
            if (Schedule is null) return TraceBack.InvalidCommand;
            if (subId == -1)
            {
                Schedule[course].CourseName
                    = IO.ReadLine($"输入课程名称={Schedule[course].CourseName}", Schedule[course].CourseName,
                    null);
                return TraceBack.AllOk;
            }
            else
            {
                var targetSub = Schedule[course][subId];
                if (!int.TryParse(IO.ReadLine($"输入星期(0-6,0表示周日)={(int)targetSub.DayOfWeek}",
                    ((int)targetSub.DayOfWeek).ToString()), out var week)
            || week < 0 || week > 6) return TraceBack.InvalidCommand;
                for (int i = 0; i < 6; i++)
                {
                    IO.WriteLine(Suit.CreateContentArray(
                        ($"{i}.", ConsoleColor.Yellow),
                        (((CourseTime)i).ToFriendlyName(), null)));
                }
                if (!int.TryParse(IO.ReadLine($"输入节次(0-5)=${(int)targetSub.CourseTime}",
                    ((int)targetSub.CourseTime).ToString()), out var startTime) || startTime < 0 || startTime > 5)
                    return TraceBack.InvalidCommand;
                var isLong = IO.ReadLine("这是两节连上的课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) != "n";
                var isLab = IO.ReadLine("这是实验课课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) != "n";

                var weekExpression = IO.ReadLine("输入教师-周数-教室表达式(正则：教师[周数|起始-截至[单|双]?](|[周数|起始-截至[单|双]?])*教室, 如张三[10]|李四[1|2-9单]正心11)", true);
                Schedule[course].RemoveSubEntry(targetSub);
                Schedule[course].AddSubEntry((DayOfWeek)week, (CourseTime)startTime, isLong, isLab, weekExpression);
                return TraceBack.AllOk;
            }

        }
        /// <summary>
        /// 导出整张课表
        /// </summary>
        /// <param name="path"></param>
        [SuitAlias("Ex")]
        [SuitInfo("导出整张课表：Export <.ics>")]
        public void Export(string path = "")
        {
            ScheduleCheck();
            if (Schedule is null) return;
            if (path == "")
                path = IO.ReadLine("输入保存文件位置") ?? "";
            try
            {
                var calendar = Schedule.ToCalendar();
                //calendar.Name = IO.ReadLine($"输入课表名称(默认:{calendar.Name})", calendar.Name, null);
                File.WriteAllText(path,
                    new CalendarSerializer().SerializeToString(calendar),
                    new UTF8Encoding(false));
            }
            catch
            {
                IO.WriteLine("写入错误。", OutputType.Error);
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// 禁用指定课程/所有课程通知
        /// </summary>
        /// <param name="course"></param>
        [SuitAlias("DsNtf")]
        [SuitInfo("启用指定课程/所有课程通知：EnNtf[ <课程名>]")]
        public void DisableNotification(
            string course = "")
        {

            if (course == "")
            {
                Schedule.EnableNotification = false;
            }
            else
            {
                Schedule[course].EnableNotification = false;

            }
        }
        /// <summary>
        /// 禁用周编号
        /// </summary>
        [SuitAlias("DsWi")]
        [SuitInfo("禁用周编号：DsWi")]
        public void DisableWeekIndex()
        {
            Schedule.DisableWeekIndex = true;
        }
        /// <summary>
        /// 启用周编号
        /// </summary>
        [SuitAlias("EnWi")]
        [SuitInfo("启用周编号：EnWi")]
        public void EnableWeekIndex()
        {
            Schedule.DisableWeekIndex = false;
        }

        /// <summary>
        /// 启用指定课程/所有课程通知
        /// </summary>
        /// <param name="course"></param>
        [SuitAlias("EnNtf")]
        [SuitInfo("启用指定课程/所有课程通知：EnNtf[ <课程名称>]")]
        public void EnableNotification(string course = "")
        {
            if (course == "")
            {
                Schedule.EnableNotification = true;
            }
            else
            {
                Schedule[course].EnableNotification = true;

            }
        }
        /// <summary>
        /// 显示整张课表
        /// </summary>
        [SuitAlias("V")]
        [SuitInfo("显示整张课表：V")]
        public void Show()
        {

            ScheduleCheck();
            if (Schedule is null) return;
            var maxWeek = Schedule.MaxWeek;
            IO.WriteLine("课表：");
            IO.WriteLine(Suit.CreateContentArray(
                ("通知:\t", null),
                ($"{(Schedule.EnableNotification == null ? $"?\t提前{Schedule.NotificationTime}分钟" : (bool)Schedule.EnableNotification ? $"启用\t提前{Schedule.NotificationTime}分钟" : "关闭")}",
                Schedule.EnableNotification == null ? IO.ColorSetting.CustomInformationColor : (bool)Schedule.EnableNotification ? IO.ColorSetting.AllOkColor : IO.ColorSetting.ErrorColor))
                , OutputType.Prompt);
            IO.WriteLine(Suit.CreateContentArray(
                ("周数:\t", null),

                ($"{(Schedule.DisableWeekIndex ? "关闭" : "开启")}", Schedule.DisableWeekIndex ? IO.ColorSetting.ErrorColor : IO.ColorSetting.AllOkColor)), OutputType.Prompt);
            IO.AppendWriteLinePrefix();
            var outList = new List<(string, ConsoleColor?)>
            {

                ("\t周二\t三四节\t实验\t", Console.BackgroundColor)


            };

            for (var i = 1; i <= maxWeek; i++)
            {
                outList.Add(($"{i} ".PadLeft(3, '0').PadLeft(4, ' '), null));
            }

            IO.WriteLine(outList, OutputType.ListTitle);
            foreach (var course in Schedule)
            {
                IO.WriteLine(course.CourseName, OutputType.CustomInfo);
                IO.AppendWriteLinePrefix();
                foreach (var item in course)
                {
                    var subOutList = new List<(string, ConsoleColor?, ConsoleColor?)>
                    {
                        ($"{item.DayOfWeek.ToFriendlyName()}\t",IO.ColorSetting.InformationColor,null),
                        ($"{item.CourseTime.ToFriendlyName()}\t",IO.ColorSetting.InformationColor,null),
                        ($"{(item.IsLab?"实验":"    ")}\t",IO.ColorSetting.InformationColor,null)
                    };
                    for (var i = 1; i <= maxWeek; i++)
                    {
                        subOutList.Add((" ", null, null));
                        if (string.IsNullOrEmpty(item[i].Name))
                        {
                            subOutList.Add(("  ", null, IO.ColorSetting.AllOkColor));
                        }
                        else
                        {
                            subOutList.Add(("  ", null, IO.ColorSetting.ErrorColor));
                        }
                        subOutList.Add((" ", null, null));
                    }
                    IO.WriteLine(subOutList);
                }
                IO.SubtractWriteLinePrefix();
            }
            IO.SubtractWriteLinePrefix();
            IO.WriteLine("日期映射：", OutputType.Prompt);
            IO.AppendWriteLinePrefix();
            foreach (var item in Schedule.DateMap)
            {
                IO.WriteLine(Suit.CreateContentArray(
                    (item.Key.ToShortDateString(), IO.ColorSetting.InformationColor),
                    (" -> ", null),
                    (item.Value == null ? "停课" : ((DateTime)item.Value).ToShortDateString(), IO.ColorSetting.InformationColor)));
            }
            IO.SubtractWriteLinePrefix();
        }
        /// <summary>
        /// 从xls导入整个课表
        /// </summary>
        /// <param name="path"></param>
        [SuitAlias("Ld")]
        [SuitInfo("从xls导入整个课表：LoadXls <.xls>")]
        public void LoadXls(string path)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (!File.Exists(path))
            {
                IO.WriteLine("未找到文件。", OutputType.Error);
                return;
            }

            using var fs = File.OpenRead(path);
            Schedule = ScheduleEntity.FromXls(fs);
        }
        /// <summary>
        /// 从json导入整个课表
        /// </summary>
        /// <param name="path"></param>
        [SuitInfo("从json导入整个课表：LoadJson <.json>")]
        [SuitAlias("Lj")]
        public void LoadJson(string path)
        {
            if (!File.Exists(path))
            {
                IO.WriteLine("未找到文件。", OutputType.Error);
                return;
            }
            Schedule = File.ReadAllText(path).AsSchedule();
        }
        /// <summary>
        /// 创建新课表
        /// </summary>
        [SuitInfo("创建新课表")]
        public void New()
        {
            if (!(int.TryParse(
                    IO.ReadLine("输入年份", "1", null), out var year)
                && year >= 2020 && year <= 2021))
            {
                IO.WriteLine("无效输入。", OutputType.Error);
                return;
            }
            IO.WriteLine("选择学期：", OutputType.ListTitle);
            IO.AppendWriteLinePrefix();
            IO.WriteLine("0. 春(默认)");
            IO.WriteLine("1. 夏");
            IO.WriteLine("2. 秋");
            IO.SubtractWriteLinePrefix();
            if (!int.TryParse(
                    IO.ReadLine("", "0", null), out var s) || s > 2 || s < 0)
            {
                IO.WriteLine("无效输入。", OutputType.Error);
                return;
            }

            Schedule = new ScheduleEntity(year, (Semester)s);
        }
        /// <summary>
        /// 打开浏览器下载课表
        /// </summary>
        [SuitAlias("Dl")]
        [SuitInfo("打开浏览器下载课表")]
        public void Download()
        {
            Process.Start(new ProcessStartInfo("http://jwts-hit-edu-cn.ivpn.hit.edu.cn/") { UseShellExecute = true });
        }
        /// <summary>
        /// 初始化课表
        /// </summary>
        [SuitInfo("初始化课表")]
        public void Init()
        {
            IO.WriteLine("课表尚未初始化，您可以：", OutputType.ListTitle);
            IO.AppendWriteLinePrefix();
            //IO.WriteLine("0. 自动导入(默认)");
            IO.WriteLine("1. 手动导入XLS(默认)");
            IO.WriteLine("2. 手动导入JSON");
            IO.WriteLine("3. 创建新的课表");
            IO.WriteLine("其它. 退出");
            IO.SubtractWriteLinePrefix();
            if (int.TryParse(IO.ReadLine("选择", "1", null), out var o))
            {
                switch (o)
                {
                    //case 0:
                    //    Auto();
                    //    break;
                    case 1:
                        LoadXls(IO.ReadLine("Xls位置") ?? "");
                        break;
                    case 2:
                        LoadJson(IO.ReadLine("Json位置") ?? "");
                        break;
                    case 3:
                        New();
                        break;
                    default:
                        Environment.Exit(0);
                        break;
                }
            }
            else
            {
                Environment.Exit(0);
            }

        }
        /// <summary>
        /// 保存整张课表到Json
        /// </summary>
        /// <param name="path"></param>
        [SuitInfo("保存整张课表到Json：Save <.json>")]
        public void Save(string path = "")
        {
            ScheduleCheck();
            if (Schedule is null) return;
            if (path == "")
                path = IO.ReadLine("输入保存JSON文件位置") ?? "";
            try
            {
                File.WriteAllText(path, Schedule.ToJson());
            }
            catch
            {
                IO.WriteLine("写入错误。", OutputType.Error);
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// Parser
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static object ParseShortDate(string s)
        {
            return DateTime.ParseExact(s, "d", CultureInfo.CurrentCulture);
        }
        /// <summary>
        /// 进行/解除日期映射
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        [SuitAlias("dmap")]
        [SuitInfo("日期映射(目的为空则此日停课): dmap <源>[ <目的>]")]
        public void DateMap(
            [SuitParser(typeof(Program), nameof(ParseShortDate))] DateTime from, [SuitParser(typeof(Program), nameof(ParseShortDate))] DateTime? to = null)
        {
            if (from == to)
            {
                Schedule.DateMap.Remove(from);
            }
            else
            {
                if (Schedule.DateMap.ContainsKey(from))
                    Schedule.DateMap.Remove(from);
                Schedule.DateMap.Add(from, to);
            }
        }
        private void ScheduleCheck()
        {
            if (Schedule is null) Init();
        }
        /// <summary>
        /// 显示课程详情
        /// </summary>
        /// <param name="courseName"></param>
        [SuitAlias("Detail")]
        [SuitAlias("Det")]
        [SuitInfo("显示课程详情: Det <课程名称>")]
        public void ShowCourseEntry(string courseName)
        {
            ScheduleCheck();
            if (!Schedule.Contains(courseName))
            {
                IO.WriteLine($"课程 {courseName} 不存在！", OutputType.Error);
                return;
            }

            IO.WriteLine(Suit.CreateContentArray((courseName, null), ($"\t{(Schedule[courseName].EnableNotification ? "启用通知" : "关闭通知")}", IO.ColorSetting.ErrorColor)));
            IO.AppendWriteLinePrefix();
            foreach (var item in Schedule[courseName])
            {
                IO.WriteLine(Suit.CreateContentArray(($"{item.DayOfWeek.ToFriendlyName()}\t", IO.ColorSetting.InformationColor, null),
                        ($"{item.CourseTime.ToFriendlyName()}\t", IO.ColorSetting.InformationColor, null),
                        ($"{(item.IsLab ? "实验" : "    ")}\t", IO.ColorSetting.InformationColor, null),
                        ($"{(item.IsLongCourse ? "两届连上" : "        ")}\t", IO.ColorSetting.InformationColor, null)
                        ));
                IO.AppendWriteLinePrefix();
                IO.WriteLine("周\t教室\t老师", OutputType.ListTitle);
                foreach (var (i, cell) in item)
                {
                    IO.WriteLine($"{i}\t{cell.Location}\t{cell.Teacher}");
                }

                IO.SubtractWriteLinePrefix();
            }

            IO.SubtractWriteLinePrefix();
        }
        /// <inheritdoc/>
        public override void SuitShowUsage()
        {
            IO.WriteLine("用法:");
            IO.AppendWriteLinePrefix();
            IO.WriteLine("快速使用:");
            IO.AppendWriteLinePrefix();
            IO.WriteLine(Suit.CreateContentArray(
                ("HRSchedule ", IO.ColorSetting.InformationColor),
                ("-i <输入xls> ", IO.ColorSetting.AllOkColor),
                ("-o <输出ics>", IO.ColorSetting.ErrorColor)
                ));
            IO.WriteLine("可用选项:", OutputType.Prompt);
            IO.AppendWriteLinePrefix();
            IO.WriteLine(Suit.CreateContentArray(
                ("--disable-week-index", IO.ColorSetting.InformationColor),
                ("\t关闭周数显示", IO.ColorSetting.ErrorColor)
                ));
            IO.WriteLine(Suit.CreateContentArray(
                ("--enable-notification", IO.ColorSetting.InformationColor),
                ("\t启用通知", IO.ColorSetting.AllOkColor)
                ));
            IO.WriteLine(Suit.CreateContentArray(
                ("-t <提醒提前的分钟数>", IO.ColorSetting.InformationColor),
                ("\t设定通知时间(默认25)", IO.ColorSetting.AllOkColor)
                ));
            IO.WriteLine(Suit.CreateContentArray(
    ("-m <源日期>[:<目标>]", IO.ColorSetting.InformationColor),
    ("\t日期映射，支持多个-m", IO.ColorSetting.AllOkColor)
    ));
            IO.SubtractWriteLinePrefix();

            IO.WriteLine("一般使用:");
            IO.AppendWriteLinePrefix();
            IO.WriteLine(Suit.CreateContentArray(
                ("输入\"", null),
                ("HRSchedule", IO.ColorSetting.InformationColor),
                ("\"启动程序，之后输入\"", null),
                ("Ls", IO.ColorSetting.InformationColor),
                ("\"查看所有可用指令", null)));
            IO.SubtractWriteLinePrefix();
            IO.SubtractWriteLinePrefix();
            IO.SubtractWriteLinePrefix();
            Environment.Exit(0);
        }
        /// <inheritdoc/>
        public override int SuitStartUp(StartUpArgs arg)
        {
            try
            {
                LoadXls(arg.Input);
                Schedule.EnableNotification = arg.EnableNotification;
                Schedule.DisableWeekIndex = arg.DisableWeekIndex;
                Schedule.NotificationTime = arg.NotificationTime;
                foreach (var item in arg.DateMap)
                {
                    Schedule.DateMap.Add(item);
                }
                Export(arg.Output);
                Environment.Exit(0);
                return 0;
            }
            catch (Exception e)
            {
                IO.WriteLine(e.Message);
                Environment.Exit(-1);
                return -1;
            }

        }
    }
}
