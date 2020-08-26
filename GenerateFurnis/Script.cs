#region
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
            try
            {
                #region Languages productdata
                languages.Add("com");
                languages.Add("br");
                languages.Add("tr");
                languages.Add("es");
                languages.Add("nl");
                languages.Add("fi");
                languages.Add("de");
                #endregion

                setTitle("Furnis Generator v2");
                writeLine("################################", ConsoleColor.Blue);
                writeLine("#                              #", ConsoleColor.White);
                writeLine("#    Furnis Generator v2       #", ConsoleColor.Blue);
                writeLine("#                              #", ConsoleColor.White);
                writeLine("#    Developed by Snaiker      #", ConsoleColor.Blue);
                writeLine("#                              #", ConsoleColor.White);
                writeLine("#    Discord: Pollak#5428      #", ConsoleColor.Blue);
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

                deleteFiles();

                if (!File.Exists("extras/productdata_" + actualProductdata + ".txt") && !existsProductdata())
                {
                    Console.WriteLine("Type production? (com / br / tr / es / nl / fi / de)");
                    string typeProduction = Convert.ToString(Console.ReadLine());

                    string newPathProduct = typeProduction.Equals("com") ? ".com" : typeProduction.Equals("br") ? ".com.br" : typeProduction.Equals("tr") ? ".com.tr" : typeProduction.Equals("es") ? ".es" : typeProduction.Equals("nl") ? ".nl" : typeProduction.Equals("fi") ? ".fi" : typeProduction.Equals("de") ? ".de" : ".com";

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

                    generatePage(pageId, parentId, pageName);
                    generateItems(itemIdInicial, pageId, typeEmulator.Equals("plus", StringComparison.CurrentCultureIgnoreCase) ? true : typeEmulator.Equals("arcturus", StringComparison.CurrentCultureIgnoreCase) ? false : true);
                    generateFurniture(itemIdInicial, typeEmulator.Equals("plus", StringComparison.CurrentCultureIgnoreCase) ? true : typeEmulator.Equals("arcturus", StringComparison.CurrentCultureIgnoreCase) ? false : true);
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
        private static void generatePage(int pageId, int parentId, string pageName)
        {
            setTitle("Generate SQL - Catalog Pages");
            using (StreamWriter sw = File.CreateText(@"sqls/catalog_pages.sql"))
            {
                //sw.WriteLine(@"INSERT INTO `catalog_pages` (`id`, `parent_id`, `type`, `caption`, `icon_image`, `visible`, `enabled`,`min_rank`, `order_num`, `page_name`, `page_headline`, `page_teaser`, `page_special`, `page_text1`, `page_text2`, `page_text_details`, `page_text_teaser`, `page_layout`) VALUES(" + pageId + ", " + parentId + ", 'DEFAULT', '" + pageName + "', 999996, '1', '1', 1, 0, '', '', '', '', '', '', '', '', 'default_3x3');");
                sw.WriteLine(@"INSERT INTO `catalog_pages` (`id`,`parent_id`, `caption`, `icon_image`, `min_rank`, `order_num`) VALUES (" + pageId + ", " + parentId + ", '" + pageName + "' , 13, 1, '0');");
                sw.Close();
            }

            writeLine("[SQL] -> Catalog Page created!", ConsoleColor.Green);
        }
        #endregion

        #region Items
        private static void generateItems(int itemIdInicial, int pageId, bool isPlus)
        {
            //sw.WriteLine(@"INSERT INTO `catalog_items` (`id`, `page_id`, `item_ids`, `catalog_name`, `offer_active`,`cost_credits`, `cost_duckets`, `cost_diamonds`, `cost_moneyspecial`, `amount`, `club_level`, `extra_info`,`limited_sells`, `future_ltds`, `allow_gift`, `offer_id`, `order_number`, `predesigned_id`) VALUES(" + idOriginal + ", " + pageId + ", '" + idOriginal + "', '" + (!notExists ? furni.publicName : actualItem + " name") + "', '0', 3, 0, 0, 0, 1, 0, '', 0, '0', '1', -1, 0, '0');");

            setTitle("Generate SQL - Catalog Items");
            int idOriginal = itemIdInicial;
            bool notExists = false;

            if (newFurnis.Count > 0)
            {
                using (StreamWriter sw = File.CreateText(@"sqls/catalog_items.sql"))
                {
                    foreach (var actualItem in newFurnis)
                    {
                        if (!tryGetInfo(actualItem, out Furnis furni))
                            notExists = true;

                        idOriginal++;
                        if (isPlus)
                            sw.WriteLine(@"INSERT INTO `catalog_items` (id, page_id, item_id, catalog_name, cost_credits, cost_diamonds, offer_id) VALUES (" + idOriginal + ", " + pageId + ", " + idOriginal + ", '" + (!notExists ? furni.publicName : actualItem + " name") + "', 3, 0, " + idOriginal + ");");
                        else
                            sw.WriteLine(@"INSERT INTO `catalog_items` VALUES ('" + idOriginal + "', '" + idOriginal + "', '" + pageId + "', '-1', '0', '99', '" + (!notExists ? furni.publicName : actualItem + " name") + "' ,'10', '0', '0', '1', '0', '0', '', '1', '0', 'none');");
                        notExists = false;
                    }

                    sw.Close();
                }

                writeLine("[SQL] -> Catalog Items created!", ConsoleColor.Green);
            }
        }
        #endregion

        #region Furniture
        private static void generateFurniture(int itemIdInicial, bool isPlus, string furniline = "")
        {
            //sw.WriteLine(@"INSERT INTO `items_base` (`item_id`, `sprite_id`, `item_name`, `type`, `width`, `length`, `height`,`allow_stack`, `allow_walk`, `allow_sit`, `allow_lay`, `allow_recycle`, `allow_trade`, `allow_marketplace_sell`,`allow_inventory_stack`, `allow_rotation`, `interaction_type`, `cycle_count`, `vending_ids`, `maxLtdItems`, `multi_height`,`effectid`, `effect_type`) VALUES(" + idOriginal + ", " + idOriginal + ", '" + actualItem + "', 's', 1, 1, 0.00, 1, 0, 0, 0, 0, 1, 1, 1, 0, 'default', 1, '0', 0, '', 0, 'DEFAULT');");

            int idOriginal = itemIdInicial;
            bool notExists = false;
            string typeEmu = isPlus ? "furniture" : "items_base";

            setTitle("Generate SQL - " + typeEmu);

            if (newFurnis.Count > 0)
            {
                using (StreamWriter sw = File.CreateText(@"sqls/" + typeEmu + ".sql"))
                {
                    foreach (var actualItem in newFurnis)
                    {
                        if (!tryGetInfo(actualItem, out Furnis furni))
                            notExists = true;

                        idOriginal++;
                        if (isPlus)
                            sw.WriteLine(@"INSERT INTO `furniture` (`id`, `furniline`, `item_name`, `public_name`, `type`, `width`, `length`, `stack_height`, `can_stack`, `can_sit`, `is_walkable`, `sprite_id`, `allow_recycle`, `allow_trade`, `allow_marketplace_sell`, `allow_gift`, `allow_inventory_stack`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `height_adjustable`, `effect_id`, `wired_id`, `is_rare`, `clothing_id`, `extra_rot`) VALUES (" + idOriginal + ", '" + furniline + "', '" + actualItem + "', '" + (!notExists ? furni.publicName : actualItem + " name") + "', 's', 1, 1, 0, '1', '0', '0', " + idOriginal + ", '1', '1', '1', '1', '1', 'default', 1, '0', '0', 0, 0, '0', 0, '0');");
                        else
                            sw.WriteLine(@"INSERT INTO `items_base` VALUES ('" + idOriginal + "', '" + idOriginal + "', '" + actualItem + "', '" + (!notExists ? furni.publicName : actualItem + " name") + "', '1', '1', '0', '1', '0', '0', '0', '1', '1', '1', '1', '1', 's', 'default', '0', '0','0','0','0','0','0');");
                        notExists = false;
                    }

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
            bool notExists = false;

            setTitle("Genereate SQL - Furnidata");

            if (newFurnis.Count > 0)
            {
                using (StreamWriter sw = File.CreateText(@"sqls/furnidata.xml"))
                {
                    foreach (var actualItem in newFurnis)
                    {
                        if (!tryGetInfo(actualItem, out Furnis furni))
                            notExists = true;

                        sw.WriteLine("<furnitype id=\"" + (++itemIdInicial) + "\" classname=\"" + actualItem + "\">");
                        sw.WriteLine("  <revision>0</revision>");
                        sw.WriteLine("  <defaultdir>0</defaultdir>");
                        sw.WriteLine("  <xdim>1</xdim>");
                        sw.WriteLine("  <ydim>1</ydim>");
                        sw.WriteLine("  <partcolors />");
                        sw.WriteLine("  <name>" + (!notExists ? furni.publicName : actualItem + " name") + "</name>");
                        //sw.WriteLine("  <description>" + actualItem + " desc" + "</description>");
                        sw.WriteLine("  <description>" + (!notExists ? furni.descName : actualItem + " desc") + "</description>");
                        sw.WriteLine("  <adurl />");
                        sw.WriteLine("  <offerid>" + itemIdInicial + "</offerid>");
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
                        notExists = false;
                    }

                    sw.Close();
                }

                writeLine("[XML] -> Furnidata created!", ConsoleColor.Green);
            }
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
            WebClient webClient = new WebClient();

            path = path.Replace(".com.br", "br").Replace(".com.tr", "tr").Replace(".", string.Empty).Replace(".de", "de");

            webClient.Headers.Add("User-Agent: Other");
            webClient.DownloadFile(new Uri("https://www.habbo" + path + "/gamedata/productdata/68a94a97ea90183f76a6950e5b360211450aa904"), Environment.CurrentDirectory + "/extras/productdata_" + path + ".txt");
            writeLine("Download completed!\n", ConsoleColor.Green);
            actualProductdata = path;
        }
        #endregion

        #region Exists file Productdata
        private static bool existsProductdata()
        {
            foreach (var language in languages)
            {
                if (File.Exists("extras/productdata_" + language + ".txt"))
                {
                    actualProductdata = language;
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
        private static void deleteFiles()
        {
            setTitle("Delete files (Folder SQLs)");
            string[] files = Directory.GetFiles(@"sqls\");

            string name = "";

            if (files.Length > 0)
            {
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
