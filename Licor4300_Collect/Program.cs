using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Web;
using System.IO;
using System.Threading;

namespace Licor4300_Collect
{
    class Program
    {

        private static void drawTextProgressBar(int progress, int total)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
        }


        static void Main(string[] args)
        {
            MessageBox.Show("This tool reads all Groups/Runs of an Licor 4300 and saves the to disk als .tiff", "Licor 4300", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string host = Interaction.InputBox("Please enter Hostname/IP of the 4300", "Enter Hostname/IP", "http://", -1, -1);
            string user = Interaction.InputBox("Please enter Username for login", "Enter Username", "service", -1, -1);
            string pass = Interaction.InputBox("Please enter Password for login", "Enter Password", "service", -1, -1);

            WebRequest request = WebRequest.Create(host);

            try
            {
                
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException webex)
            {
                MessageBox.Show("Sorry, the entered hostname/ip is not valid! " + webex.Message, "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

           

            WebRequest request2 = WebRequest.Create(host + "/scanapp/imaging/nonjava/open.pl");
            string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(user + ":" + pass));
            request2.Headers.Add("Authorization", "Basic " + encoded);

            HttpWebResponse response2 = null;

            try
            {
                response2 = (HttpWebResponse)request2.GetResponse();

            }
            catch(WebException webex2)
            {
                MessageBox.Show("Sorry, the entered username/password is not valid! " + webex2.Message, "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            string selectedPath = null;
            var t = new Thread((ThreadStart)(() => {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                //fbd.RootFolder = System.Environment.SpecialFolder.MyDocuments;
                fbd.ShowNewFolderButton = true;
                //fbd.
                if (fbd.ShowDialog() == DialogResult.Cancel)
                {
                    MessageBox.Show("No folder specified!", "Environment Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

                selectedPath = fbd.SelectedPath;
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            //Console.WriteLine(selectedPath);
            


            var encoding = ASCIIEncoding.ASCII;
            string responseText = null;
            using (var reader = new System.IO.StreamReader(response2.GetResponseStream(), encoding))
            {
                responseText = reader.ReadToEnd();
            }

            var doc = new HtmlAgilityPack.HtmlDocument();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
            doc.LoadHtml(responseText);
            

            //Debug.Write(responseText);

            var node = doc.DocumentNode.SelectSingleNode("//select[@name='group']");
            string group = null;

            WebRequest tmpReq = null;
            WebResponse tmpRes = null;
            Encoding tmpEncoding = null;
            string tmpResponseText = null;

            var doc2 = new HtmlAgilityPack.HtmlDocument();

            int groupCount = 0;
            int allGroups = node.Descendants().Count();

            foreach (var nNode in node.Descendants())
            {
                if (nNode.NodeType == HtmlNodeType.Element)
                {
                    
                    group = Regex.Replace(nNode.InnerText, @"\s+", string.Empty);

                    if (group == "SelectGroup")
                    {
                        allGroups--;
                        continue;
                    }

                    groupCount++;

                    Console.WriteLine("Processing group: " + group + "("+groupCount+"/"+allGroups+")");
                    Directory.CreateDirectory(selectedPath + @"\" + group);
                    


                    

                    tmpReq = WebRequest.Create(host + "/scanapp/imaging/nonjava/open.pl");
                    tmpReq.Headers.Add("Authorization", "Basic " + encoded);
                    tmpReq.Method = "POST";
                    tmpReq.ContentType = "application/x-www-form-urlencoded";
                    NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
                    outgoingQueryString.Add("group", group);
                    // outgoingQueryString.Add("field2", "value2");
                    string postdata = outgoingQueryString.ToString();
                    byte[] byteArray = Encoding.UTF8.GetBytes(postdata);
                    tmpReq.ContentLength = byteArray.Length;

                    Stream dataStream = tmpReq.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);

                    tmpRes = tmpReq.GetResponse();

                    tmpEncoding = ASCIIEncoding.ASCII;
                    tmpResponseText = null;
                    using (var reader = new System.IO.StreamReader(tmpRes.GetResponseStream(), tmpEncoding))
                    {
                        tmpResponseText = reader.ReadToEnd();
                    }

                    tmpRes.Close();

                    // Debug.Write(tmpResponseText);





                    ////////////////////////////// Run
                    ///


                    HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
                    doc2.LoadHtml(tmpResponseText);



                    var node2 = doc2.DocumentNode.SelectSingleNode("//select[@name='scan']");
                    string scan = null;

                    WebRequest tmpReq2 = null;
                    WebResponse tmpRes2 = null;
                    Encoding tmpEncoding2 = null;
                    string tmpResponseText2 = null;

                    int runCount = 0;
                    int allRuns = node2.Descendants().Count();

                    foreach (var nNode2 in node2.Descendants())
                    {
                        if (nNode2.NodeType == HtmlNodeType.Element)
                        {
                            
                            scan = Regex.Replace(nNode2.InnerText, @"\s+", string.Empty);

                            if (scan == "SelectRun")
                            {
                                allRuns--;
                                continue;
                            }
                            runCount++;

                            //Console.WriteLine("        Processing scan: " + scan);
                            drawTextProgressBar(runCount, node2.Descendants().Count());
                            //Console.WriteLine("             Processing 700");

                            //Directory.CreateDirectory(selectedPath + @"LiCor4300\" + group + @"\" + scan);


                            tmpReq2 = WebRequest.Create(host + "/scan/image/" + scan + ".tif?xml=%3Cimage%3E%3Cin%3E%3Cscangroup%3E" + group + "%3C/scangroup%3E%3Cscan%3E" + scan + "%3C/scan%3E%3Cformat%3Etiff%3C/format%3E%3Cchannel%3E700%3C/channel%3E%3C/in%3E%3C/image%3E");
                            tmpReq2.Headers.Add("Authorization", "Basic " + encoded);

                            tmpRes2 = tmpReq2.GetResponse();

                            byte[] lnByte = null;
                            using (var reader2 = new System.IO.BinaryReader(tmpRes2.GetResponseStream()))
                            {
                              lnByte = reader2.ReadBytes(1 * 1024 * 1024 * 50);
                           
                            }

                            if(lnByte.Length <= 54)
                            {
                                System.IO.File.AppendAllText(selectedPath + @"\errors.txt", group + ": Could not load " + scan + "_700" + Environment.NewLine);
                                continue;
                            }

                            tmpRes2.Close();

                            

                            System.IO.FileStream lxFS = new FileStream(selectedPath + @"\" + group + @"\" + scan + @"_700.tif", FileMode.Create);
                            lxFS.Write(lnByte, 0, lnByte.Length);
                            lxFS.Close();












                            tmpReq2 = WebRequest.Create(host + "/scan/image/" + scan + ".tif?xml=%3Cimage%3E%3Cin%3E%3Cscangroup%3E" + group + "%3C/scangroup%3E%3Cscan%3E" + scan + "%3C/scan%3E%3Cformat%3Etiff%3C/format%3E%3Cchannel%3E900%3C/channel%3E%3C/in%3E%3C/image%3E");
                            tmpReq2.Headers.Add("Authorization", "Basic " + encoded);

                            tmpRes2 = tmpReq2.GetResponse();

                            byte[] lnByte2 = null;
                            using (var reader2 = new System.IO.BinaryReader(tmpRes2.GetResponseStream()))
                            {
                                lnByte2 = reader2.ReadBytes(1 * 1024 * 1024 * 50);

                            }

                            if (lnByte2.Length <= 54)
                            {
                                System.IO.File.AppendAllText(selectedPath + @"\errors.txt", group + ": Could not load " + scan + "_800" + Environment.NewLine);
                                continue;
                            }

                            tmpRes2.Close();



                            System.IO.FileStream lxFS2 = new FileStream(selectedPath + @"\" + group + @"\" + scan + @"_800.tif", FileMode.Create);
                            lxFS2.Write(lnByte2, 0, lnByte2.Length);
                            lxFS2.Close();

                        }
                    }

                }









                
            }


        }
    }
}
