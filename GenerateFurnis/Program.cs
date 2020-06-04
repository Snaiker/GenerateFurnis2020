#region
using GenerateFurnis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
#endregion

namespace GerarMobis
{
    class Program
    {
        private static readonly List<string> itemsNomes = new List<string>();
        private static readonly Dictionary<string, Furnis> fixedFurnis = new Dictionary<string, Furnis>();
        private static string actualProductdata;
        private static bool errorFinalSymbol = false;
        private static readonly string[] files = Directory.GetFiles(@"swfs\");

        private static void Main()
        {
            Console.Title = "Furnis Generator v1.0";
            Console.WriteLine("################################");
            Console.WriteLine("#                              #");
            Console.WriteLine("#        Furnis Generator v1.0 #");
            Console.WriteLine("#                              #");
            Console.WriteLine("#    Developed by Snaiker      #");
            Console.WriteLine("#    Discord: Pollak#5428      #");
            Console.WriteLine("################################\n");
            Console.WriteLine("Welcome! Bem-vind@! Bienvenid@! :)\n");

            Thread.Sleep(350);

            if (File.Exists("extras/items.txt"))
                File.Delete("extras/items.txt");

            if (files.Length == 0)
            {
                Console.WriteLine("Directory <SWFs> is empty. Please insert some files (.swf) to start.");
                readKeyExit();
                return;
            }

            if (!existsProductdata())
            {
                if (!File.Exists("extras/productdata_" + actualProductdata + ".txt"))
                {
                    Console.WriteLine("Type production? (com / br / tr / es / nl / fi)");
                    string typeProduction = Convert.ToString(Console.ReadLine());

                    string newPathProduct = typeProduction.Equals("com") ? ".com" : typeProduction.Equals("br") ? ".com.br" : typeProduction.Equals("tr") ? ".com.tr" : typeProduction.Equals("es") ? ".es" : typeProduction.Equals("nl") ? ".nl" : typeProduction.Equals("fi") ? ".fi" : ".com";

                    Console.WriteLine("Oops, missing the file productdata.txt! Starting download...");
                    downloadProductdata(newPathProduct);
                }
            }

            try
            {
                loadInfoFurnis();

                if (errorFinalSymbol)
                {
                    readKeyExit();
                    return;
                }

                Console.WriteLine("--");

                Console.Write("\nInitial Item ID: ");
                string item = Convert.ToString(Console.ReadLine());
                if (!int.TryParse(item, out int itemIdInicial))
                {
                    readKeyExit();
                    return;
                }

                int idOriginal = itemIdInicial;

                Console.Write("\nPage ID: ");
                if (!int.TryParse(Convert.ToString(Console.ReadLine()), out int pageId))
                {
                    readKeyExit();
                    return;
                }

                Console.Write("\nParent ID: ");
                if (!int.TryParse(Convert.ToString(Console.ReadLine()), out int parentId))
                {
                    readKeyExit();
                    return;
                }

                Console.Write("\nCaption page: ");
                string pageName = Console.ReadLine();

                Console.Write("\nType emulator? Press the key... PLUS [P] or Arcturus [A]: ");
                ConsoleKeyInfo keyPressed = Console.ReadKey();

                Console.WriteLine("\n");

                loadingSwfs();

                generatePages(pageId, parentId, pageName);
                generateItems(itemsNomes, itemIdInicial, pageId, keyPressed.Key == ConsoleKey.P ? true : keyPressed.Key == ConsoleKey.A ? false : true);
                generateFurniture(itemsNomes, itemIdInicial, keyPressed.Key == ConsoleKey.P ? true : keyPressed.Key == ConsoleKey.A ? false : true);
                generateFurnidata(itemsNomes, itemIdInicial);

                if (File.Exists("extras/items.txt"))
                    File.Delete("extras/items.txt");

                readKeyExit();
            }
            catch (Exception e)
            {
                insertErrors(e);
            }
        }

        #region Generate files

        #region Pages
        private static void generatePages(int pageId, int parentId, string pageName)
        {
            using (StreamWriter sw = File.CreateText(@"sqls/catalog_pages.sql"))
            {
                sw.WriteLine(@"INSERT INTO `catalog_pages` (`id`,`parent_id`, `caption`, `icon_image`, `min_rank`, `order_num`) VALUES (" + pageId + ", " + parentId + ", '" + pageName + "' , 13, 1, '0');");
                sw.Close();
            }

            Console.WriteLine("\n[SQL] -> CatalogPages created!\n");
        }
        #endregion

        #region Items
        private static void generateItems(List<string> itemsNomes, int itemIdInicial, int pageId, bool isPlus)
        {
            int idOriginal = itemIdInicial;
            bool isNull = false;

            using (StreamWriter sw = File.CreateText(@"sqls/catalog_items.sql"))
            {
                if (itemsNomes.Count > 0)
                {
                    foreach (var actualItem in itemsNomes)
                    {
                        if (!tryGetInfo(actualItem, out Furnis furni))
                            isNull = true;

                        idOriginal++;
                        if (isPlus)
                            sw.WriteLine(@"INSERT INTO `catalog_items` (id, page_id, item_id, catalog_name, cost_credits, cost_diamonds) VALUES (" + idOriginal + ", " + pageId + ", " + idOriginal + ", '" + (!isNull ? furni.publicName : actualItem + " name") + "', 3, 0);");
                        else
                            sw.WriteLine(@"INSERT INTO `catalog_items` VALUES ('" + idOriginal + "', '" + idOriginal + "', '" + pageId + "', '-1', '0', '99', '" + (!isNull ? furni.publicName : actualItem + " name") + "' ,'10', '0', '0', '1', '0', '0', '', '1', '0', 'none');");
                            isNull = false;
                    }
                }

                sw.Close();
            }

            Console.WriteLine("[SQL] -> CatalogItems created!\n");
        }
        #endregion

        #region Furniture
        private static void generateFurniture(List<string> itemsNomes, int itemIdInicial, bool isPlus)
        {
            int idOriginal = itemIdInicial;
            bool isNull = false;

            using (StreamWriter sw = File.CreateText(@"sqls/" + (isPlus ? "furniture" : "items_base") + ".sql"))
            {
                if (itemsNomes.Count > 0)
                {
                    foreach (var actualItem in itemsNomes)
                    {
                        if (!tryGetInfo(actualItem, out Furnis furni))
                            isNull = true;

                        idOriginal++;
                        if (isPlus)
                            sw.WriteLine(@"INSERT INTO `furniture` (`id`, `item_name`, `public_name`, `type`, `width`, `length`, `stack_height`, `can_stack`, `can_sit`, `is_walkable`, `sprite_id`, `allow_recycle`, `allow_trade`, `allow_marketplace_sell`, `allow_gift`, `allow_inventory_stack`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `height_adjustable`, `effect_id`, `wired_id`, `is_rare`, `clothing_id`, `extra_rot`) VALUES (" + idOriginal + ", '" + actualItem + "', '" + (!isNull ? furni.publicName : actualItem + " name") + "', 's', 1, 1, 0, '1', '0', '0', " + idOriginal + ", '1', '1', '1', '1', '1', 'default', 1, '0', '0', 0, 0, '0', 0, '0');");
                        else
                            sw.WriteLine(@"INSERT INTO `items_base` VALUES ('" + idOriginal + "', '" + idOriginal + "', '" + actualItem + "', '" + (!isNull ? furni.publicName : actualItem + " name") + "', '1', '1', '0', '1', '0', '0', '0', '1', '1', '1', '1', '1', 's', 'default', '0', '0','0','0','0','0','0');");
                        isNull = false;
                    }
                }

                sw.Close();
            }

            Console.WriteLine("[SQL] -> " + (isPlus ? "Furniture" : "Items Base") + " created!\n");
        }
        #endregion

        #region Furnidata
        private static void generateFurnidata(List<string> itemsNomes, int itemIdInicial)
        {
            int idOriginal = itemIdInicial;
            bool isNull = false;

            using (StreamWriter sw = File.CreateText(@"sqls/furnidata.xml"))
            {
                if (itemsNomes.Count > 0)
                {
                    foreach (var actualItem in itemsNomes)
                    {
                        if (!tryGetInfo(actualItem, out Furnis furni))
                            isNull = true;

                        sw.WriteLine("<furnitype id=\"" + (++idOriginal) + "\" classname=\"" + actualItem + "\">");
                        sw.WriteLine("  <revision>0</revision>");
                        sw.WriteLine("  <defaultdir>0</defaultdir>");
                        sw.WriteLine("  <xdim>1</xdim>");
                        sw.WriteLine("  <ydim>1</ydim>");
                        sw.WriteLine("  <partcolors />");
                        sw.WriteLine("  <name>" + (!isNull ? furni.publicName : actualItem + " name") + "</name>");
                        sw.WriteLine("  <description>" + (!isNull ? furni.publicName : actualItem + " desc") + "</description>");
                        sw.WriteLine("  <adurl />");
                        sw.WriteLine("  <offerid>-1</offerid>");
                        sw.WriteLine("  <buyout>0</buyout>");
                        sw.WriteLine("  <rentofferid>-1</rentofferid>");
                        sw.WriteLine("  <rentbuyout>0</rentbuyout>");
                        sw.WriteLine("  <bc>0</bc>");
                        sw.WriteLine("  <excludeddynamic>0</excludeddynamic>");
                        sw.WriteLine("  <customparams>0</customparams>");
                        sw.WriteLine("  <specialtype>1</specialtype>");
                        sw.WriteLine("  <canstandon>0</canstandon>");
                        sw.WriteLine("  <cansiton>0</cansiton>");
                        sw.WriteLine("  <canlayon>0</canlayon>");
                        sw.WriteLine("</furnitype>");
                        isNull = false;
                    }
                }

                sw.Close();
            }

            Console.WriteLine("[XML] -> Furnidata created!\n");
        }
        #endregion

        #region Add Items Text
        private static void addItemsText(string path)
        {
            if (File.Exists("extras/items.txt"))
                return;

            string getText = File.ReadAllText(path);
            getText = Regex.Replace(getText, @"\t|\n|\r", string.Empty).Replace("]][[", "],[");

            foreach (var texto in getText.Split("],["))
            {
                try
                {
                    string[] item = texto.Split(',');
                    string className = !string.IsNullOrEmpty(item[0]) ? item[0].Replace("[", string.Empty).Replace(@"""", string.Empty) : "className empty!";
                    string publicName = !string.IsNullOrEmpty(item[1]) ? item[1].Replace(@"""", string.Empty) : "publicName empty!";
                    string descName = !string.IsNullOrEmpty(item[2]) ? item[2].Replace("[", string.Empty).Replace("]", string.Empty).Replace(@"""", string.Empty) : "descName empty!";

                    if (!fixedFurnis.ContainsKey(className))
                        fixedFurnis.Add(className, new Furnis(className, publicName, descName));
                }
                catch (Exception e)
                {
                    insertErrors(e);
                }
            }

            using (StreamWriter sw = new StreamWriter("extras/items.txt"))
            {
                foreach (KeyValuePair<string, Furnis> f in fixedFurnis)
                    sw.Write(f.Key + "|'|" + f.Value.publicName + "|'|" + f.Value.descName + "@@");
                sw.Close();
            }

            loadInfoFurnis();
        }
        #endregion

        #region Load info Furnis
        private static void loadInfoFurnis()
        {
            if (!File.Exists("extras/items.txt"))
            {
                addItemsText("extras/productdata_" + actualProductdata + ".txt");
                return;
            }

            string readFile = File.ReadAllText("extras/items.txt");
            readFile = Regex.Replace(readFile, @"\t|\n|\r", string.Empty).Replace("]][[", "],[");

            if (readFile.EndsWith("@@"))
                readFile = readFile.Substring(0, readFile.Length - 2);

            foreach (var texto in readFile.Split("@@"))
            {
                try
                {
                    if (readFile.EndsWith("@@"))
                    {
                        Console.WriteLine("Error! Remove the end of the file @@");
                        errorFinalSymbol = true;
                        break;
                    }

                    string[] item = texto.Split("|'|");

                    if (!fixedFurnis.ContainsKey(item[0]))
                        fixedFurnis.Add(item[0], new Furnis(item[0], item[1], item[2]));
                }
                catch (Exception e)
                {
                    insertErrors(e);
                }
            }
        }
        #endregion

        #region Download Productdata
        private static void downloadProductdata(string path)
        {
            WebClient webClient = new WebClient();

            webClient.Headers.Add("User-Agent: Other");
            webClient.DownloadFile(new Uri("https://www.habbo" + path + "/gamedata/productdata/68a94a97ea90183f76a6950e5b360211450aa904"), Environment.CurrentDirectory + "/extras/productdata_" + path.Replace(".com.br", "br").Replace(".com.tr", "tr").Replace(".", string.Empty) + ".txt");
            Console.WriteLine("Download completed!");
            readKeyExit();
        }
        #endregion

        #region Exists file Productdata
        private static bool existsProductdata()
        {
            string[] options = { "com", "br", "tr", "es", "nl", "fi" };

            foreach (var option in options)
            {
                if (File.Exists("extras/productdata_" + option + ".txt"))
                {
                    actualProductdata = option;
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Try Get Infos and exists
        private static bool tryGetInfo(string className, out Furnis furni)
        {
            if (!fixedFurnis.TryGetValue(className, out furni))
                return false;
            return true;
        }
        #endregion

        #region Loading Swfs
        private static void loadingSwfs()
        {
            DateTime dateStart;
            TimeSpan date;

            int currentId = 0;
            string actualFile = "";

            foreach (var arquivo in files)
            {
                actualFile = arquivo.Replace(".swf", "").Replace(@"swfs\", "");
                dateStart = DateTime.Now;

                if (itemsNomes.Contains(actualFile))
                    continue;

                date = DateTime.Now - dateStart;

                itemsNomes.Add(actualFile);
                Console.WriteLine("[" + (++currentId) + "] Loading swf name <" + actualFile + "> -> " + date.Seconds + " s, " + date.Milliseconds + " ms");
            }
        }
        #endregion

        #endregion

        #region Logs errors
        private static void insertErrors(Exception e)
        {
            using (StreamWriter sw = File.CreateText(@"errors/file_errors.txt"))
                sw.WriteLine(e.ToString());
        }
        #endregion

        #region Read key error
        static void readKeyExit()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        #endregion
    }
}
