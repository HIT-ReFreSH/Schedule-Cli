using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HitRefresh.HitGeneralServices.Jwts;
using HitRefresh.HitGeneralServices.WeChatServiceHall;
using HitRefresh.Schedule.ScheduleResource;
using Ical.Net.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PlasticMetal.MobileSuit;
using PlasticMetal.MobileSuit.Core;
using static HitRefresh.Schedule.ScheduleResource.ResourceProvider;

namespace HitRefresh.Schedule
{
    /// <summary>
    ///     核心类
    /// </summary>
    [SuitInfo("ReFreSH-Schedule")]
    public class Program
    {
        private readonly Persistence _persistence;


        public Program(IIOHub io, JwtsService jwts, Persistence persistence)
        {
            _persistence = persistence;
            IO = io;
            Jwts = jwts;
        }

        public IIOHub IO { get; }
        public JwtsService Jwts { get; }

        /// <summary>
        ///     切换到深圳校区模式
        /// </summary>
        [SuitInfo("切换到深圳校区模式: SZ")]
        [SuitAlias("Sz")]
        public void UseSz()
        {
            _persistence.Region = ScheduleRegion.Shenzhen;
            Resource = IScheduleResource.Create(_persistence.Region);

            IO.WriteLine("已切换到深圳模式！");
        }

        /// <summary>
        ///     切换到哈尔滨校区模式
        /// </summary>
        [SuitInfo("切换到哈尔滨校区模式: HR")]
        [SuitAlias("Hr")]
        public void UseHrb()
        {
            _persistence.Region = ScheduleRegion.Harbin;
            Resource = IScheduleResource.Create(_persistence.Region);
            IO.WriteLine("已切换到本部模式！");
        }

        private static async Task Main(string[] args)
        {
            var builder = Suit.CreateBuilder()
                //.UseLog(ISuitLogger.CreateFileByPath("D:\\HSMX.log"))
                .UsePowerLine()
                .MapClient<Program>();
            builder.Services.AddSingleton<Persistence>()
                .AddScoped<JwtsService>();
            await builder.Build().RunAsync();
        }

        /// <summary>
        ///     修改通知时间
        /// </summary>
        /// <param name="m"></param>
        [SuitInfo("修改通知时间：ChNt <时间-分钟数>")]
        [SuitAlias("ChNt")]
        public void ChangeNotificationTime(int m)
        {
            _persistence.Schedule.NotificationTime = m;
        }

        /// <summary>
        ///     向课表添加一个课程
        /// </summary>
        [SuitInfo("向课表添加一个课程条目:add <课程名称>")]
        public string Add(string courseName)
        {
            _ = _persistence.Schedule[courseName];
            if (!int.TryParse(IO.ReadLine("输入星期(0-6,0表示周日)"), out var week)
                || week < 0 || week > 6) return "时间不合法";
            for (var i = 0; i < 6; i++)
                IO.WriteLine(Suit.CreateContentArray(
                    ($"{i}.", ConsoleColor.Yellow),
                    (Resource.CourseTimeToFriendlyName((CourseTime) i), null)));
            if (!int.TryParse(IO.ReadLine("输入节次(0-5)"), out var startTime) || startTime < 0 || startTime > 5)
                return "节次不合法";
            var isLong = IO.ReadLine("这是两节连上的课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) == "n";
            var isLab = IO.ReadLine("这是实验课课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) == "n";

            var weekExpression =
                IO.ReadLine("输入教师-周数-教室表达式(正则：教师[周数|起始-截至[单|双]?](|[周数|起始-截至[单|双]?])*教室, 如张三[10]|李四[1|2-9单]正心11)");
            if (weekExpression is null) return "格式不合法";
            _persistence.Schedule[courseName]
                .AddContent((DayOfWeek) week, (CourseTime) startTime, isLong, isLab, weekExpression);
            return "添加成功";
        }

        /// <summary>
        ///     从课表移除一个课程(或其子条目)
        /// </summary>
        /// <param name="course"></param>
        /// <param name="subId"></param>
        [SuitAlias("Rm")]
        [SuitInfo("从课表移除一个课程(或其子条目)：Remove <课程名>[ <子条目从0序号>]")]
        public void Remove(string course,
            int subId = -1)
        {
            if (_persistence.Schedule is null) return;
            if (subId == -1)
                _persistence.Schedule.Remove(course);
            else
                _persistence.Schedule[course].RemoveSubEntryAt(subId);
        }

        /// <summary>
        ///     编辑课表中的课程(子条目)
        /// </summary>
        /// <param name="course"></param>
        /// <param name="subId"></param>
        /// <returns></returns>
        [SuitAlias("Ed")]
        [SuitInfo("编辑课表中的课程(子条目)：Edit <课程名>[ <子条目从0序号>]")]
        public string Edit(string course,
            int subId = -1)
        {
            if (_persistence.Schedule is null) return "课本未初始化";
            if (subId == -1)
            {
                _persistence.Schedule[course].CourseName
                    = IO.ReadLine($"输入课程名称={_persistence.Schedule[course].CourseName}",
                        _persistence.Schedule[course].CourseName,
                        null) ?? "新课程";
                return "编辑成功";
            }

            var targetSub = _persistence.Schedule[course][subId];
            if (!int.TryParse(IO.ReadLine($"输入星期(0-6,0表示周日)={(int) targetSub.DayOfWeek}",
                    ((int) targetSub.DayOfWeek).ToString()), out var week)
                || week < 0 || week > 6) return "时间不合法";
            for (var i = 0; i < 6; i++)
                IO.WriteLine(Suit.CreateContentArray(
                    ($"{i}.", ConsoleColor.Yellow),
                    (Resource.CourseTimeToFriendlyName((CourseTime) i), null)));
            if (!int.TryParse(IO.ReadLine($"输入节次(0-5)=${(int) targetSub.CourseTime}",
                    ((int) targetSub.CourseTime).ToString()), out var startTime) || startTime < 0 || startTime > 5)
                return "时间不合法";
            var isLong = IO.ReadLine("这是两节连上的课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) != "n";
            var isLab = IO.ReadLine("这是实验课课吗？(N|else)", "n")?.ToLower(CultureInfo.CurrentCulture) != "n";

            var weekExpression =
                IO.ReadLine("输入教师-周数-教室表达式(正则：教师[周数|起始-截至[单|双]?](|[周数|起始-截至[单|双]?])*教室, 如张三[10]|李四[1|2-9单]正心11)");
            if (weekExpression is null) return "格式不合法";
            _persistence.Schedule[course].RemoveSubEntry(targetSub);
            _persistence.Schedule[course]
                .AddContent((DayOfWeek) week, (CourseTime) startTime, isLong, isLab, weekExpression);
            return "编辑成功";
        }

        /// <summary>
        ///     导出整张课表
        /// </summary>
        /// <param name="path"></param>
        [SuitAlias("Ex")]
        [SuitInfo("导出整张课表：Export <.ics>")]
        public void Export(string path = "")
        {
            ScheduleCheck();
            if (_persistence.Schedule is null) return;
            if (path == "")
                path = IO.ReadLine("输入保存文件位置") ?? "";
            try
            {
                var calendar = _persistence.Schedule.ToCalendar();
                //calendar.Name = IO.ReadLine($"输入课表名称(默认:{calendar.Name})", calendar.Name, null);
                File.WriteAllText(path,
                    new CalendarSerializer().SerializeToString(calendar),
                    new UTF8Encoding(false));
            }
            catch
            {
                IO.WriteLine("写入错误。", OutputType.Error);
            }
        }

        /// <summary>
        ///     禁用指定课程/所有课程通知
        /// </summary>
        /// <param name="course"></param>
        [SuitAlias("DsNtf")]
        [SuitInfo("启用指定课程/所有课程通知：EnNtf[ <课程名>]")]
        public void DisableNotification(
            string course = "")
        {
            if (course == "")
                _persistence.Schedule.EnableNotification = false;
            else
                _persistence.Schedule[course].EnableNotification = false;
        }

        /// <summary>
        ///     禁用周编号
        /// </summary>
        [SuitAlias("DsWi")]
        [SuitInfo("禁用周编号：DsWi")]
        public void DisableWeekIndex()
        {
            _persistence.Schedule.DisableWeekIndex = true;
        }

        /// <summary>
        ///     启用周编号
        /// </summary>
        [SuitAlias("EnWi")]
        [SuitInfo("启用周编号：EnWi")]
        public void EnableWeekIndex()
        {
            _persistence.Schedule.DisableWeekIndex = false;
        }

        /// <summary>
        ///     启用指定课程/所有课程通知
        /// </summary>
        /// <param name="course"></param>
        [SuitAlias("EnNtf")]
        [SuitInfo("启用指定课程/所有课程通知：EnNtf[ <课程名称>]")]
        public void EnableNotification(string course = "")
        {
            if (course == "")
                _persistence.Schedule.EnableNotification = true;
            else
                _persistence.Schedule[course].EnableNotification = true;
        }

        /// <summary>
        ///     显示整张课表
        /// </summary>
        [SuitAlias("V")]
        [SuitInfo("显示整张课表：V")]
        public void Show()
        {
            ScheduleCheck();
            if (_persistence.Schedule is null) return;
            var maxWeek = _persistence.Schedule.MaxWeek;
            IO.WriteLine("课表：");
            IO.WriteLine(Suit.CreateContentArray(
                ("通知:\t", null),
                ($"{(_persistence.Schedule.EnableNotification == null ? $"?\t提前{_persistence.Schedule.NotificationTime}分钟" : (bool) _persistence.Schedule.EnableNotification ? $"启用\t提前{_persistence.Schedule.NotificationTime}分钟" : "关闭")}",
                    _persistence.Schedule.EnableNotification == null ? IO.ColorSetting.InformationColor :
                    (bool) _persistence.Schedule.EnableNotification ? IO.ColorSetting.OkColor :
                    IO.ColorSetting.ErrorColor))
            );
            var region = _persistence.Region switch
            {
                ScheduleRegion.Harbin => "哈尔滨",
                ScheduleRegion.Shenzhen => "深圳",
                _ => ""
            };
            IO.WriteLine($"校区:\t{region}");
            IO.WriteLine(Suit.CreateContentArray(
                ("周数:\t", null),
                ($"{(_persistence.Schedule.DisableWeekIndex ? "关闭" : "开启")}",
                    _persistence.Schedule.DisableWeekIndex ? IO.ColorSetting.ErrorColor : IO.ColorSetting.OkColor)));
            IO.AppendWriteLinePrefix();
            var outList = new List<PrintUnit>
            {
                ("\t星期\t节次\t类别\t", null)
            };

            for (var i = 1; i <= maxWeek; i++) outList.Add(($"{i} ".PadLeft(3, '0').PadLeft(4, ' '), null));

            IO.WriteLine(outList);
            foreach (var course in _persistence.Schedule.EnumerateCourseEntries())
            {
                IO.WriteLine(course.CourseName, OutputType.Info);
                IO.AppendWriteLinePrefix();
                foreach (var item in course.EnumerateContents())
                {
                    var subOutList = new List<PrintUnit>
                    {
                        ($"{CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(item.DayOfWeek)}\t",
                            IO.ColorSetting.InformationColor, null),
                        ($"{Resource.CourseTimeToFriendlyName(item.CourseTime)}\t", IO.ColorSetting.InformationColor,
                            null),
                        ($"{(item.IsLab ? "实验" : "    ")}\t", IO.ColorSetting.InformationColor, null)
                    };
                    for (var i = 1; i <= maxWeek; i++)
                    {
                        subOutList.Add((" ", null, null));
                        if (string.IsNullOrEmpty(item[i].Name))
                            subOutList.Add(("  ", null, IO.ColorSetting.OkColor));
                        else
                            subOutList.Add(("  ", null, IO.ColorSetting.ErrorColor));
                        subOutList.Add((" ", null, null));
                    }

                    IO.WriteLine(subOutList);
                }

                IO.SubtractWriteLinePrefix();
            }

            IO.SubtractWriteLinePrefix();
            IO.WriteLine("日期映射：", OutputType.Title);
            IO.AppendWriteLinePrefix();
            foreach (var item in _persistence.Schedule.DateMap)
                IO.WriteLine(Suit.CreateContentArray(
                    (item.Key.ToShortDateString(), IO.ColorSetting.InformationColor),
                    (" -> ", null),
                    (item.Value == null ? "停课" : ((DateTime) item.Value).ToShortDateString(),
                        IO.ColorSetting.InformationColor)));
            IO.SubtractWriteLinePrefix();
        }

        /// <summary>
        ///     从xls导入整个课表
        /// </summary>
        /// <param name="path"></param>
        [SuitAlias("Lds")]
        [SuitInfo("从xls导入深圳课表：LoadXlsSz <.xls>")]
        public void LoadXlsSz(string path)
        {
            UseSz();
            LoadXls(path);
        }

        /// <summary>
        ///     从Jwts导入整个课表
        /// </summary>
        /// <param name="path"></param>
        [SuitAlias("fc")]
        [SuitInfo("从Jwts导入整个课表：Fetch <username> <year> <au|sp|su>")]
        public async Task Fetch(string username, int year, string semester)
        {
            var sw = new Stopwatch();
            sw.Start();
            var seme = semester switch
            {
                "sp" => JwtsSemester.Spring,
                "su" => JwtsSemester.Summer,
                _ => JwtsSemester.Autumn
            };
            var seme2 = semester switch
            {
                "sp" => Semester.Spring,
                "su" => Semester.Summer,
                _ => Semester.Autumn
            };
            var data = await WeChatServices.GetScheduleAnonymousAsync((uint) year, seme, username);
            _persistence.Schedule =
                ScheduleEntity.FromWeb(year, seme2, data);
            sw.Stop();
            Console.WriteLine($"用时：{sw.Elapsed.TotalSeconds}s.");
        }

        /// <summary>
        ///     从xls导入整个课表
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
            _persistence.Schedule = ScheduleEntity.FromXls(fs);
        }

        /// <summary>
        ///     从json导入整个课表
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

            _persistence.Schedule = JsonSerializer.Deserialize<ScheduleEntity>(File.ReadAllText(path)) ??
                                    throw new NullReferenceException();
        }

        /// <summary>
        ///     创建新课表
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

            IO.WriteLine("选择学期：", OutputType.Title);
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

            _persistence.Schedule = new ScheduleEntity(year, (Semester) s);
        }

        /// <summary>
        ///     打开浏览器下载课表
        /// </summary>
        [SuitAlias("Dl")]
        [SuitInfo("打开浏览器下载课表")]
        public void Download()
        {
            Process.Start(new ProcessStartInfo("http://jwts-hit-edu-cn.ivpn.hit.edu.cn/") {UseShellExecute = true});
        }

        /// <summary>
        ///     初始化课表
        /// </summary>
        [SuitInfo("初始化课表")]
        public void Init()
        {
            IO.WriteLine("课表尚未初始化，您可以：", OutputType.Title);
            IO.AppendWriteLinePrefix();
            //IO.WriteLine("0. 自动导入(默认)");
            IO.WriteLine("1. 手动导入XLS(默认)");
            IO.WriteLine("2. 手动导入JSON");
            IO.WriteLine("3. 创建新的课表");
            IO.WriteLine("其它. 退出");
            IO.SubtractWriteLinePrefix();
            if (int.TryParse(IO.ReadLine("选择", "1", null), out var o))
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
            else
                Environment.Exit(0);
        }

        /// <summary>
        ///     保存整张课表到Json
        /// </summary>
        /// <param name="path"></param>
        [SuitInfo("保存整张课表到Json：Save <.json>")]
        public void Save(string path = "")
        {
            ScheduleCheck();
            if (_persistence.Schedule is null) return;
            if (path == "")
                path = IO.ReadLine("输入保存JSON文件位置") ?? "";
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(_persistence.Schedule));
            }
            catch
            {
                IO.WriteLine("写入错误。", OutputType.Error);
                Environment.Exit(0);
            }
        }

        /// <summary>
        ///     Parser
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static object ParseShortDate(string s)
        {
            return DateTime.ParseExact(s, "d", CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     进行/解除日期映射
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        [SuitAlias("dmap")]
        [SuitInfo("日期映射(目的为空则此日停课): dmap <源>[ <目的>]")]
        public void DateMap(
            [SuitParser(nameof(ParseShortDate), typeof(Program))]
            DateTime from,
            [SuitParser(nameof(ParseShortDate), typeof(Program))]
            DateTime? to = null)
        {
            if (from == to)
            {
                _persistence.Schedule.DateMap.Remove(from);
            }
            else
            {
                if (_persistence.Schedule.DateMap.ContainsKey(from))
                    _persistence.Schedule.DateMap.Remove(from);
                _persistence.Schedule.DateMap.Add(from, to);
            }
        }

        private void ScheduleCheck()
        {
            if (_persistence.Schedule is null) Init();
        }

        /// <summary>
        ///     显示课程详情
        /// </summary>
        /// <param name="courseName"></param>
        [SuitAlias("Detail")]
        [SuitAlias("Det")]
        [SuitInfo("显示课程详情: Det <课程名称>")]
        public void ShowCourseEntry(string courseName)
        {
            ScheduleCheck();
            if (!_persistence.Schedule.Contains(courseName))
            {
                IO.WriteLine($"课程 {courseName} 不存在！", OutputType.Error);
                return;
            }

            IO.WriteLine(Suit.CreateContentArray((courseName, null),
                ($"\t{(_persistence.Schedule[courseName].EnableNotification ? "启用通知" : "关闭通知")}",
                    IO.ColorSetting.ErrorColor)));
            IO.AppendWriteLinePrefix();
            foreach (var item in _persistence.Schedule[courseName].EnumerateContents())
            {
                IO.WriteLine(Suit.CreateContentArray(
                    ($"{CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(item.DayOfWeek)}\t",
                        IO.ColorSetting.InformationColor, null),
                    ($"{Resource.CourseTimeToFriendlyName(item.CourseTime)}\t", IO.ColorSetting.InformationColor, null),
                    ($"{(item.IsLab ? "实验" : "    ")}\t", IO.ColorSetting.InformationColor, null),
                    ($"{(item.IsLongCourse ? "两届连上" : "        ")}\t", IO.ColorSetting.InformationColor, null)
                ));
                IO.AppendWriteLinePrefix();
                IO.WriteLine("周\t教室\t老师", OutputType.Title);
                foreach (var (i, cell) in item.EnumerateInformation())
                    IO.WriteLine($"{i}\t{cell.Location}\t{cell.Teacher}");

                IO.SubtractWriteLinePrefix();
            }

            IO.SubtractWriteLinePrefix();
        }

        public class Persistence
        {
            public ScheduleEntity? Schedule { get; set; } = new ScheduleEntity();
            public ScheduleRegion Region { get; set; } = ScheduleRegion.Harbin;
        }
        //    /// <inheritdoc/>
        //    public override void SuitShowUsage()
        //    {
        //        IO.WriteLine("用法:");
        //        IO.AppendWriteLinePrefix();
        //        IO.WriteLine("快速使用:");
        //        IO.AppendWriteLinePrefix();
        //        IO.WriteLine(Suit.CreateContentArray(
        //            ("HRSchedule ", IO.ColorSetting.InformationColor),
        //            ("-i <输入xls> ", IO.ColorSetting.OkColor),
        //            ("-o <输出ics>", IO.ColorSetting.ErrorColor)
        //            ));
        //        IO.WriteLine("可用选项:", OutputType.Prompt);
        //        IO.AppendWriteLinePrefix();
        //        IO.WriteLine(Suit.CreateContentArray(
        //            ("--disable-week-index", IO.ColorSetting.InformationColor),
        //            ("\t关闭周数显示", IO.ColorSetting.ErrorColor)
        //            ));
        //        IO.WriteLine(Suit.CreateContentArray(
        //            ("--enable-notification", IO.ColorSetting.InformationColor),
        //            ("\t启用通知", IO.ColorSetting.OkColor)
        //            ));
        //        IO.WriteLine(Suit.CreateContentArray(
        //            ("-t <提醒提前的分钟数>", IO.ColorSetting.InformationColor),
        //            ("\t设定通知时间(默认25)", IO.ColorSetting.OkColor)
        //            ));
        //        IO.WriteLine(Suit.CreateContentArray(
        //("-m <源日期>[:<目标>]", IO.ColorSetting.InformationColor),
        //("\t日期映射，支持多个-m", IO.ColorSetting.OkColor)
        //));
        //        IO.SubtractWriteLinePrefix();

        //        IO.WriteLine("一般使用:");
        //        IO.AppendWriteLinePrefix();
        //        IO.WriteLine(Suit.CreateContentArray(
        //            ("输入\"", null),
        //            ("HRSchedule", IO.ColorSetting.InformationColor),
        //            ("\"启动程序，之后输入\"", null),
        //            ("Ls", IO.ColorSetting.InformationColor),
        //            ("\"查看所有可用指令", null)));
        //        IO.SubtractWriteLinePrefix();
        //        IO.SubtractWriteLinePrefix();
        //        IO.SubtractWriteLinePrefix();
        //        Environment.Exit(0);
        //    }
        //    /// <inheritdoc/>
        //    public override int SuitStartUp(StartUpArgs arg)
        //    {
        //        try
        //        {
        //            LoadXls(arg.Input);
        //            Schedule.EnableNotification = arg.EnableNotification;
        //            Schedule.DisableWeekIndex = arg.DisableWeekIndex;
        //            Schedule.NotificationTime = arg.NotificationTime;
        //            foreach (var item in arg.DateMap)
        //            {
        //                Schedule.DateMap.Add(item);
        //            }
        //            Export(arg.Output);
        //            Environment.Exit(0);
        //            return 0;
        //        }
        //        catch (Exception e)
        //        {
        //            IO.WriteLine(e.Message);
        //            Environment.Exit(-1);
        //            return -1;
        //        }

        //    }
    }
}