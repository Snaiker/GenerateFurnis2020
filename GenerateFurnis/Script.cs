﻿#region
using GenerateFurnis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
#endregion

namespace GerarMobis
{
    class Script
    {
        private static readonly List<string> languages = new List<string>();
        private static readonly List<string> newFurnis = new List<string>();
        private static readonly Dictionary<string, Furnis> fixedFurnis = new Dictionary<string, Furnis>();

        private static string actualProductdata;
        private const string PATH_FILE_ERRORS = "/errors/logs.txt";
        private static readonly string[] files = Directory.GetFiles(@"swfs\");

        public static void initialize()
        {
            CreateFolders();

            try
            {
                #region Languages productdata
                languages.Add("com");
                languages.Add("com.br");
                languages.Add("tr");
                languages.Add("es");
                languages.Add("nl");
                languages.Add("fi");
                languages.Add("de");
                #endregion

                setTitle("Furnis Generator v2");
                writeLine("################################", ConsoleColor.Blue);
                writeLine("#                              #", ConsoleColor.White);
                writeLine("#   Furnis Generator v2.1      #", ConsoleColor.Blue);
                writeLine("#                              #", ConsoleColor.White);
                writeLine("#   Developed by Snaiker       #", ConsoleColor.Blue);
                writeLine("#                              #", ConsoleColor.White);
                writeLine("#   Discord: Pollak#5428       #", ConsoleColor.Blue);
                writeLine("#                              #", ConsoleColor.White);
                writeLine("################################\n", ConsoleColor.Blue);
                writeLine("Welcome! Bem-vind@! Bienvenid@! :)\n", ConsoleColor.White);

                Thread.Sleep(250);

                if (files.Length == 0)
                {
                    writeLine("Directory <SWFs> is empty. Please insert some files (.swf) to start.", ConsoleColor.Red);
                    readKeyExit();
                    return;
                }

                deleteFilesSQL();

                if (!File.Exists("extras/productdata_" + actualProductdata + ".txt") && !existsProductdata())
                {
                    Console.WriteLine("Type production? (com / br / tr / es / nl / fi / de)");
                    string typeProduction = Convert.ToString(Console.ReadLine());

                    string newPathProduct = typeProduction.Equals("com") ? ".com" : typeProduction.Equals("br") ? ".br" : typeProduction.Equals("tr") ? ".com.tr" : typeProduction.Equals("es") ? ".es" : typeProduction.Equals("nl") ? ".nl" : typeProduction.Equals("fi") ? ".fi" : typeProduction.Equals("de") ? ".de" : ".com";

                    writeLine("Oops, missing the file productdata.txt! Starting download...\n", ConsoleColor.Red);
                    downloadProductdata(newPathProduct);
                }

                try
                {
                    readProductda("/extras/productdata_" + actualProductdata + ".txt");

                    setTitle("Furnis Generator");

                    writeLine("Initial Item ID: ", ConsoleColor.White);
                    string item = Convert.ToString(Console.ReadLine());
                    if (!int.TryParse(item, out int itemIdInicial))
                    {
                        readKeyExit();
                        return;
                    }

                    int idOriginal = itemIdInicial;

                    writeLine("Page ID: ", ConsoleColor.White);
                    if (!int.TryParse(Convert.ToString(Console.ReadLine()), out int pageId))
                    {
                        readKeyExit();
                        return;
                    }

                    writeLine("Parent ID: ", ConsoleColor.White);
                    if (!int.TryParse(Convert.ToString(Console.ReadLine()), out int parentId))
                    {
                        readKeyExit();
                        return;
                    }

                    writeLine("Caption page: ", ConsoleColor.White);
                    string pageName = Console.ReadLine();

                    writeLine("Type emulator? Plus or Arcturus (write the name):", ConsoleColor.White);
                    string typeEmulator = Console.ReadLine();

                    loadingSwfs();

                    bool typeEmu = typeEmulator.Equals("plus", StringComparison.CurrentCultureIgnoreCase);

                    generatePage(pageId, parentId, pageName, typeEmu);
                    generateItems(itemIdInicial, pageId, typeEmu);
                    generateFurniture(itemIdInicial, typeEmu);
                    generateFurnidata(itemIdInicial);
                }
                catch (Exception e)
                {
                    saveLogs(Environment.CurrentDirectory + PATH_FILE_ERRORS, e.ToString());
                }
            }
            catch
            {
                readKeyExit();
            }
        }

        #region Generate files

        #region Pages
        private static void generatePage(int pageId, int parentId, string pageName, bool isPlus)
        {
            setTitle("Generate SQL - Catalog Pages");
            StringBuilder stringBuilder = new StringBuilder();

            if (isPlus)
                stringBuilder.AppendLine(@"INSERT INTO `catalog_pages` (`id`,`parent_id`, `caption`, `icon_image`, `min_rank`, `order_num`) VALUES (" + pageId + ", " + parentId + ", '" + pageName + "' , 13, 1, '0');");
            else
                stringBuilder.AppendLine(@"INSERT INTO `catalog_pages` (`id`, `parent_id`, `caption_save`, `caption`, `page_layout`, `icon_color`, `icon_image`, `min_rank`, `order_num`, `visible`, `enabled`, `club_only`, `vip_only`, `page_headline`, `page_teaser`, `page_special`, `page_text1`, `page_text2`, `page_text_details`, `page_text_teaser`, `room_id`, `includes`) VALUES (" + pageId + ", " + parentId + ", '" + pageName.ToLower().Replace(' ', '_') + "', '" + pageName + "', 'default_3x3', 1, 1, 1, 1, '1', '1', '0', '0', '', '', '', '', '', '', '', 0, '');");

            using (StreamWriter sw = File.CreateText(@"sqls/catalog_pages.sql"))
            {
                sw.Write(stringBuilder.ToString());
                sw.Close();
            }

            writeLine("[SQL] -> Catalog Page created!", ConsoleColor.Green);
        }
        #endregion

        #region Items
        private static void generateItems(int itemIdInicial, int pageId, bool isPlus)
        {
            setTitle("Generate SQL - Catalog Items");
            bool notExists = false;

            if (newFurnis.Count > 0)
            {
                StringBuilder stringBuilder = new StringBuilder();

                foreach (var actualItem in newFurnis)
                {
                    if (!tryGetInfo(actualItem, out Furnis furni))
                        notExists = true;

                    itemIdInicial++;
                    if (isPlus)
                        stringBuilder.AppendLine(@"INSERT INTO `catalog_items` (id, page_id, item_id, catalog_name, cost_credits, cost_diamonds, offer_id) VALUES (" + itemIdInicial + ", " + pageId + ", " + itemIdInicial + ", '" + (!notExists ? furni.publicName : actualItem + " name") + "', 3, 0, " + itemIdInicial + ");");
                    else
                        stringBuilder.AppendLine(@"INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_stack`, `limited_sells`, `order_number`, `offer_id`, `song_id`, `extradata`, `have_offer`, `club_only`) VALUES (" + itemIdInicial + ", '" + itemIdInicial + "', " + pageId + ", '" + actualItem + "', 3, 0, 0, 1, 0, 0, 1, " + itemIdInicial + ", 0, '', '1', '0');");
                    notExists = false;
                }

                using (StreamWriter sw = File.CreateText(@"sqls/catalog_items.sql"))
                {
                    sw.Write(stringBuilder.ToString());
                    sw.Close();
                }
            }

            writeLine("[SQL] -> Catalog Items created!", ConsoleColor.Green);
        }
        #endregion

        #region Furniture
        private static void generateFurniture(int itemIdInicial, bool isPlus)
        {
            bool notExists = false;
            string typeEmu = isPlus ? "furniture" : "items_base";

            setTitle("Generate SQL - " + typeEmu);

            if (newFurnis.Count > 0)
            {
                StringBuilder stringBuilder = new StringBuilder();

                foreach (var actualItem in newFurnis)
                {
                    if (!tryGetInfo(actualItem, out Furnis furni))
                        notExists = true;

                    itemIdInicial++;
                    if (isPlus)
                        stringBuilder.AppendLine(@"INSERT INTO `furniture` (`id`, `item_name`, `public_name`, `type`, `width`, `length`, `stack_height`, `can_stack`, `can_sit`, `is_walkable`, `sprite_id`, `allow_recycle`, `allow_trade`, `allow_marketplace_sell`, `allow_gift`, `allow_inventory_stack`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `height_adjustable`, `effect_id`, `wired_id`, `is_rare`, `clothing_id`, `extra_rot`) VALUES (" + itemIdInicial + ", '" + actualItem + "', '" + (!notExists ? furni.publicName : actualItem + " name") + "', 's', 1, 1, 0, '1', '0', '0', " + itemIdInicial + ", '1', '1', '1', '1', '1', 'default', 1, '0', '0', 0, 0, '0', 0, '0');");
                    else
                        stringBuilder.AppendLine(@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES (" + itemIdInicial + ", " + itemIdInicial + ", '" + actualItem + "', '" + actualItem + "', 1, 1, 0.0, '1', '0', '0', '0', '1', '1', '1', '1', '1', 's', 'default', 1, '', '', '', 0, 0, '');");
                    notExists = false;
                }

                using (StreamWriter sw = File.CreateText(@"sqls/" + typeEmu + ".sql"))
                {
                    sw.Write(stringBuilder.ToString());
                    sw.Close();
                }
            }

            typeEmu = char.ToUpper(typeEmu[0]) + typeEmu.Substring(1);
            writeLine("[SQL] -> " + typeEmu.Replace('_', ' ') + " created!", ConsoleColor.Green);
        }
        #endregion

        #region Furnidata
        private static void generateFurnidata(int itemIdInicial)
        {
            setTitle("Genereate SQL - Furnidata");

            if (newFurnis.Count > 0)
            {
                bool notExists = false;

                StringBuilder stringBuilder = new StringBuilder();

                foreach (var actualItem in newFurnis)
                {
                    if (!tryGetInfo(actualItem, out Furnis furni))
                        notExists = true;

                    stringBuilder.AppendLine("<furnitype id=\"" + (++itemIdInicial) + "\" classname=\"" + actualItem + "\">");
                    stringBuilder.AppendLine("  <revision>0</revision>");
                    stringBuilder.AppendLine("  <defaultdir>0</defaultdir>");
                    stringBuilder.AppendLine("  <xdim>1</xdim>");
                    stringBuilder.AppendLine("  <ydim>1</ydim>");
                    stringBuilder.AppendLine("  <partcolors />");
                    stringBuilder.AppendLine("  <name>" + (!notExists ? furni.publicName : actualItem + " name") + "</name>");
                    stringBuilder.AppendLine("  <description>" + (!notExists ? furni.descName : actualItem + " desc") + "</description>");
                    stringBuilder.AppendLine("  <adurl />");
                    stringBuilder.AppendLine("  <offerid>" + itemIdInicial + "</offerid>");
                    stringBuilder.AppendLine("  <buyout>1</buyout>");
                    stringBuilder.AppendLine("  <rentofferid>-1</rentofferid>");
                    stringBuilder.AppendLine("  <rentbuyout>0</rentbuyout>");
                    stringBuilder.AppendLine("  <bc>0</bc>");
                    stringBuilder.AppendLine("  <excludeddynamic>0</excludeddynamic>");
                    stringBuilder.AppendLine("  <customparams>0</customparams>");
                    stringBuilder.AppendLine("  <specialtype>1</specialtype>");
                    stringBuilder.AppendLine("  <canstandon>0</canstandon>");
                    stringBuilder.AppendLine("  <cansiton>0</cansiton>");
                    stringBuilder.AppendLine("  <canlayon>0</canlayon>");
                    stringBuilder.AppendLine("</furnitype>");
                    notExists = false;
                }

                using (StreamWriter sw = File.CreateText(@"sqls/furnidata.xml"))
                {
                    sw.Write(stringBuilder.ToString());
                    sw.Close();
                }
            }

            writeLine("[XML] -> Furnidata created!", ConsoleColor.Green);
        }

        #endregion

        #region Add Items Text
        private static void readProductda(string path)
        {
            if (!File.Exists(Environment.CurrentDirectory + path))
            {
                writeLine(path + " don't exists!", ConsoleColor.Red);
                return;
            }

            setTitle("Read productdata...");

            string getText = File.ReadAllText(Environment.CurrentDirectory + path);
            getText = Regex.Replace(getText, @"\n", "");
            getText = getText.Replace("]][[", "¦").Replace("],[", "¦");

            foreach (var texto in getText.Split('¦'))
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
                    saveLogs(Environment.CurrentDirectory + PATH_FILE_ERRORS, e.ToString());
                    break;
                }
            }

            writeLine("Loaded " + fixedFurnis.Count + " furnis from productdata!\n", ConsoleColor.Green);
        }
        #endregion

        #region Download Productdata
        private static void downloadProductdata(string path)
        {
            setTitle("Download productdata...");
            try
            {
                WebClient webClient = new WebClient();

                path = path.Replace(".com.tr", "tr").Replace(".", string.Empty).Replace(".de", "de").Replace("br", "com.br");
                webClient.Headers.Add("User-Agent: Other");
                webClient.DownloadFile(new Uri("https://www.habbo." + path + "/gamedata/productdata/68a94a97ea90183f76a6950e5b360211450aa904"), Environment.CurrentDirectory + "/extras/productdata_" + path + ".txt");
                writeLine("Download completed!\n", ConsoleColor.Green);
                actualProductdata = path;
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region Exists file Productdata
        private static bool existsProductdata()
        {
            foreach (var language in languages)
                if (File.Exists("extras/productdata_" + language + ".txt"))
                {
                    actualProductdata = language;
                    return true;
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

            string actualFile = "";

            foreach (var file in files)
            {
                actualFile = file.Replace(".swf", "").Replace(@"swfs\", "");
                dateStart = DateTime.Now;

                date = DateTime.Now - dateStart;

                newFurnis.Add(actualFile);
                writeLine("Loading SWF name <" + actualFile + "> in " + date.Seconds + " s, " + date.Milliseconds + " ms", ConsoleColor.White);
            }
        }
        #endregion

        #region Save logs
        private static void saveLogs(string path, string content)
        {
            try
            {
                FileStream Writer = new FileStream(path, FileMode.Append, FileAccess.Write);
                byte[] Msg = Encoding.ASCII.GetBytes(Environment.NewLine + content);
                Writer.Write(Msg, 0, Msg.Length);
                Writer.Dispose();
            }
            catch (Exception e)
            {
                writeLine("Could not write to file: " + e + ":" + content, ConsoleColor.Red);
            }
        }
        #endregion

        #region Read key error
        private static void readKeyExit()
        {
            writeLine("\nPress any key to exit...", ConsoleColor.White);
            Console.ReadKey();
        }
        #endregion

        #region Delete files
        private static void deleteFilesSQL()
        {
            setTitle("Delete files (Folder SQLs)");
            try
            {
                string[] files = Directory.GetFiles(@"sqls\");

                string name = "";

                if (files.Length > 0)
                    foreach (var file in files)
                    {
                        try
                        {
                            name = file.Replace(@"sqls\", "");
                            if (File.Exists(Environment.CurrentDirectory + "/sqls/" + name))
                                File.Delete(Environment.CurrentDirectory + "/sqls/" + name);
                        }
                        catch (Exception e)
                        {
                            saveLogs(Environment.CurrentDirectory + PATH_FILE_ERRORS, e.ToString());
                        }
                    }
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(ex);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex);
            }
        }
        #endregion

        #region Create folders
        private static void CreateFolders()
        {
            if (!Directory.Exists("errors"))
                Directory.CreateDirectory("errors");
            if (!Directory.Exists("sqls"))
                Directory.CreateDirectory("sqls");
            if (!Directory.Exists("extras"))
                Directory.CreateDirectory("extras");
            if (!Directory.Exists("swfs"))
                Directory.CreateDirectory("swfs");
        }
        #endregion

        #region Set Title Console
        private static void setTitle(string title) => Console.Title = title;
        #endregion

        #region Console Writeline
        private static void writeLine(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text);
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion

        #endregion
    }
}
