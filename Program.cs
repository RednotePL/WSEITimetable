using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace WSEITimetable
{
    internal class Program
    {
        private static string startup_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static void Main(string[] args)
        {
            //load a file. TODO: drag and drop/input only student ID
            string[] html = File.ReadAllLines(Path.Combine(startup_path, "NAVIGATOR - archman.pl.html"));
            int table_start = -1;

            //search for table start
            for (int i = 0; i < html.Length; i++)
            {
                if (html[i].Contains("style=\"width:45px;min-width:45px;white-space:nowrap;\""))
                {
                    table_start = i + 1;
                    break;
                }
            }

            //search for table end
            int table_end = -1;
            for (int i = table_start; i < html.Length; i++)
            {
                if (html[i].Contains("<tr class=\"grid-footer\">"))
                {
                    table_end = i + 1;
                    break;
                }
            }

            //copy only timetable
            string[] table = new string[table_end - table_start];
            for (int i = table_start; i < table_end; i++)
            {
                table[i - table_start] = html[i];
            }

            //clean unneeded attributes
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = table[i].Replace("align=\"center\"", "");
                table[i] = table[i].Replace("class=\"optional optional1\"", "");
                table[i] = table[i].Replace("class=\"optional optional4\"", "");
                table[i] = table[i].Replace("class=\"grid-row-alternating\"", "");
                table[i] = table[i].Replace("style=\"width:45px;min-width:45px;\"", "");
                table[i] = table[i].Replace("class=\"grid-row\"", "");
                table[i] = Regex.Replace(table[i], "(value=\")(\\d*)(\")", "");
            }

            //to be proper XML, we need to start and end document
            table[0] = "<table><tr>";
            table[table.Length - 1] = "</tr></table>";

            //parse as XML and convert to JSON
            XmlDocument doc = new XmlDocument();
            string xml = "";
            for (int i = 0; i < table.Length; i++)
            {
                xml += table[i];
            }
            doc.LoadXml(xml);
            string jsonText = JsonConvert.SerializeXmlNode(doc, Formatting.Indented);

            //File.WriteAllText(Path.Combine(startup_path, "table.json"), jsonText);

            //JSON PROCESSING
            List<Przedmiot> przedmioty = new List<Przedmiot>();
            RootObject ro = JsonConvert.DeserializeObject<RootObject>(jsonText);

            //Clean and reorganize
            foreach (var e in ro.table.tr)
            {
                przedmioty.Add(new Przedmiot()
                {
                    data = e.td[1],
                    //dzien = e.td[2],
                    g_rozp = e.td[3],
                    g_zak = e.td[4],
                    //licz_g = e.td[5],
                    nazwa = e.td[6],
                    sala = e.td[7],
                    wykladowca = e.td[8],
                    //grupa = e.td[9]
                });
            }

            //File.WriteAllText(Path.Combine(startup_path, "plan_cleaned.json"), JsonConvert.SerializeObject(przedmioty, Formatting.Indented));

            //Begin CSV document
            string[] csv_plan = new string[przedmioty.Count + 1];
            csv_plan[0] = "Subject,Start date,Start time,End date,End time,Location,Description";

            //Build each line
            for (int i = 1; i <= przedmioty.Count; i++)
            {
                csv_plan[i] = przedmioty[i - 1].nazwa + "," +
                              przedmioty[i - 1].data + "," +
                              przedmioty[i - 1].g_rozp + "," +
                              przedmioty[i - 1].data + "," +
                              przedmioty[i - 1].g_zak + "," +
                              przedmioty[i - 1].sala + "," +
                              przedmioty[i - 1].wykladowca;
            }

            //Save as csv file
            File.WriteAllLines(Path.Combine(startup_path, "plan.csv"), csv_plan);
        }
    }

    public class Przedmiot
    {
        public string data { get; set; }

        //public string dzien { get; set; }
        public string g_rozp { get; set; }

        public string g_zak { get; set; }

        //public string licz_g { get; set; }
        public string nazwa { get; set; }

        public string sala { get; set; }
        public string wykladowca { get; set; }
        //public string grupa { get; set; }
    }

    public class Tr
    {
        public List<string> td { get; set; }
    }

    public class Table
    {
        public List<Tr> tr { get; set; }
    }

    public class RootObject
    {
        public Table table { get; set; }
    }
}