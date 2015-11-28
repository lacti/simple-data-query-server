using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DataQueryServer;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            typeof(User).PrepareCsvBinParserWriter();

            var data = new List<User>().LoadFromCsv(@"c:\Users\lacti\Documents\user.txt");
            foreach (var each in data)
            {
                Console.WriteLine("{0} {1} {2}", each.Id, each.Name, each.Money);
            }

            data.SaveToCsv(@"c:\Users\lacti\Documents\user2.txt");
            data.SaveToBin(@"c:\Users\lacti\Documents\user.bin");

            var data2 = new List<User>().LoadFromBin(@"c:\Users\lacti\Documents\user.bin");
            foreach (var each in data2)
            {
                Console.WriteLine("{0} {1} {2}", each.Id, each.Name, each.Money);
            }
            */

            typeof(Yes24Raw).PrepareCsvBinParserWriter();
            typeof(Yes24Bin).PrepareCsvBinParserWriter();
            /*
            var data = new List<Yes24Raw>().LoadFromCsv(@"c:\Users\lacti\Documents\TDS_yes24_UTF8.csv");
            Console.WriteLine(data.Count);

            data.SaveToCsv(@"c:\Users\lacti\Documents\TDS_yes24_UTF82.csv");
            data.SaveToBin(@"c:\Users\lacti\Documents\TDS_yes24_UTF8.bin");
            */
            /*
            var data2 = new List<Yes24Raw>().LoadFromBin(@"c:\Users\lacti\Documents\TDS_yes24_UTF8.bin");
            Console.WriteLine(data2.Count);
            var data = data2.Select(e => new Yes24Bin
            {
                일자 = Parse(e.일자),
                구분 = e.구분,
                회원번호 = Parse(e.회원번호.Trim()),
                책제목 = e.책제목,
                카테고리 = e.카테고리,
                작가 = e.작가,
                ISBN = e.ISBN,
                출판사 = e.출판사,
                출판일자 = Parse(e.출판일자.Trim()),
                주문시간 = Parse(e.주문시간.Trim()),
                수량 = Parse(e.수량.Trim()),
                카트적재여부 = e.카트적재여부,
                카트적재일자 = e.카트적재일자,
                모바일구분 = e.모바일구분,
                배송주소1 = e.배송주소1,
                배송주소2 = e.배송주소2,
            }).ToList();
            Console.WriteLine(data.Count);
            data.SaveToBin(@"c:\Users\lacti\Documents\TDS_yes24_UTF8_typed.bin");
            Console.WriteLine(data.Count);
            */
            /*
            using (var _ = new Watch())
            {
                var data = new List<Yes24Bin>().LoadFromBin(@"c:\Users\lacti\Documents\TDS_yes24_UTF8_typed.bin");
                Console.WriteLine(data.Count);
            }
            Console.ReadKey();
            */
            using (var _ = new Watch())
            {
                var data = new List<Yes24Raw>().LoadFromCsv(@"c:\Users\lacti\Documents\TDS_yes24_UTF8.csv");
                Console.WriteLine(data.Count);
            }
        }

        private static int Parse(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return 0;
            var trimmed = data.Trim();
            if (trimmed.All(char.IsNumber))
                return int.Parse(trimmed);
            return 0;
        }
    }

    class Watch : IDisposable
    {
        private readonly Stopwatch _watch = new Stopwatch();

        public Watch()
        {
            _watch.Start();
        }

        public void Dispose()
        {
            _watch.Stop();
            Console.WriteLine("Elapsed: {0}", _watch.Elapsed);
        }
    }


    public class User : IData
    {
        public long Id;
        public string Name;
        public long Money;
    }

    public class Yes24Raw : IData
    {
        public string 일자;
        public string 구분;
        public string 회원번호;
        public string 책제목;
        public string 카테고리;
        public string 작가;
        public string ISBN;
        public string 출판사;
        public string 출판일자;
        public string 주문시간;
        public string 수량;
        public string 카트적재여부;
        public string 카트적재일자;
        public string 모바일구분;
        public string 배송주소1;
        public string 배송주소2;
    }

    public class Yes24Bin : IData
    {
        public int 일자;
        public string 구분;
        public int 회원번호;
        public string 책제목;
        public string 카테고리;
        public string 작가;
        public string ISBN;
        public string 출판사;
        public int 출판일자;
        public int 주문시간;
        public int 수량;
        public string 카트적재여부;
        public string 카트적재일자;
        public string 모바일구분;
        public string 배송주소1;
        public string 배송주소2;
    }

}
;