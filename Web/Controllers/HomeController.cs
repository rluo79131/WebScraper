using AngleSharp;
using AutoMapper;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Web.Models;

namespace Web.Controllers
{
    /// <summary>
    /// 首頁控制器
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// 映照器
        /// </summary>
        IMapper Mapper { get; }

        /// <summary>
        /// eventsContext
        /// </summary>
        /// <param name="configuration"></param>
        public HomeController(IMapper mapper)
        {
            Mapper = mapper;
        }

        /// <summary>
        /// 首頁頁面
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var playLists = new List<PlayerList>();

            for (var idx = 'A'; idx <= 'Z'; idx++)
            {
                var letter = Convert.ToChar(idx).ToString();
                var filePath = $"{Directory.GetCurrentDirectory()}/CsvFiles/{letter}.csv";

                if (System.IO.File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                        {
                            var playerInfos = csv.GetRecords<PlayerInfo>();
                            foreach (var playerInfo in playerInfos)
                            {
                                var playerList = Mapper.Map<PlayerList>(playerInfo);
                                playerList.Letter = letter;

                                playLists.Add(playerList);
                            }
                        }
                    }
                }
            }

            return View(playLists);
        }

        /// <summary>
        /// 錯誤頁面
        /// </summary>
        /// <returns></returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// 下載-球員資訊資料
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Download()
        {
            try
            {
                var domain = "https://www.basketball-reference.com";
                var url = $"{domain}/players/";
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                var html = response.Content.ReadAsStringAsync().Result;
                var browsingContext = BrowsingContext.New(Configuration.Default);
                var document = await browsingContext.OpenAsync(res => res.Content(html));
                var collections = document.QuerySelectorAll(".page_index > li").ToList();

                for (var idx = 'A'; idx <= 'Z'; idx++)
                {
                    var letter = Convert.ToChar(idx).ToString();
                    var collection = collections.FirstOrDefault(x => x.Children.Length > 0 && x.Children[0].TextContent == letter);
                    var playerInfos = new List<PlayerInfo>();

                    if (collection != null)
                    {
                        foreach (var element in collection.Children[1].Children)
                        {
                            url = $"{domain}{element.GetAttribute("href")}";
                            response = await httpClient.GetAsync(url);
                            html = response.Content.ReadAsStringAsync().Result;
                            document = await browsingContext.OpenAsync(res => res.Content(html));

                            var p1Datas = document.QuerySelector(".stats_pullout > .p1");
                            var p2Datas = document.QuerySelector(".stats_pullout > .p2").Children.ToList();
                            var fgNode = p2Datas.FirstOrDefault(x => x.Children[0].TextContent == "FG%");
                            var fg3Node = p2Datas.FirstOrDefault(x => x.Children[0].TextContent == "FG3%");
                            var ftNode = p2Datas.FirstOrDefault(x => x.Children[0].TextContent == "FT%");
                            var efgNode = p2Datas.FirstOrDefault(x => x.Children[0].TextContent == "eFG%");
                            var p3Datas = document.QuerySelector(".stats_pullout > .p3");

                            playerInfos.Add(new PlayerInfo()
                            {
                                Player = element.TextContent,
                                G = p1Datas.Children[0].Children[2].TextContent,
                                PTS = p1Datas.Children[1].Children[2].TextContent,
                                TRB = p1Datas.Children[2].Children[2].TextContent,
                                AST = p1Datas.Children[3].Children[2].TextContent,
                                FG = fgNode == null ? "" : fgNode.Children[2].TextContent,
                                FG3 = fg3Node == null ? "" : fg3Node.Children[2].TextContent,
                                FT = ftNode == null ? "" : ftNode.Children[2].TextContent,
                                EFG = efgNode == null ? "" : efgNode.Children[2].TextContent,
                                PER = p3Datas.Children[0].Children[2].TextContent,
                                WS = p3Datas.Children[1].Children[2].TextContent,
                            });
                        }

                        playerInfos = playerInfos.OrderBy(x => x.Player).ToList();
                    }

                    var path = $"{Directory.GetCurrentDirectory()}/CsvFiles";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    using (var stream = new FileStream($"{path}/{letter}.csv", FileMode.OpenOrCreate))
                    {
                        using (var file = new StreamWriter(stream, leaveOpen: true))
                        {
                            var csv = new CsvWriter(file, CultureInfo.CurrentCulture);
                            csv.WriteRecords(playerInfos);
                        }
                    }
                }

                var result = new ResultViewModel()
                {
                    Result = true,
                    Message = "下載成功"
                };
                return Json(result);
            }
            catch (Exception ex)
            {
                var result = new ResultViewModel()
                {
                    Result = false,
                    Message = ex.ToString()
                };
                return Json(result);
            }
        }
    }
}
