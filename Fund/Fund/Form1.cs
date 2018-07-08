using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using MySql.Data;
using MySql.Data.MySqlClient;
using CCWin;


namespace Fund
{
    enum Condition { INFORMATION, COMPARISON }
    public partial class Form1 : CCSkinMain
    {
        Thread td;
        //初始化页数
        int page = 1;
        //初始化开始界面，并把页数转到第一页

        List<string> fundsID = new List<string>();
        List<Stock> stocks = new List<Stock>();

        List<string> fundsID2 = new List<string>();
        List<Stock> stocks2 = new List<Stock>();

        //初始化数据库
        public static MySqlConnection conn;
        string mysqlUser = "root", mysqlPassword = "123456";
        private BackgroundWorker bgworker;
        private BackgroundWorker bgworker1;
        private BackgroundWorker bgworker2;
        string[] all_timeChange = null;

        public Form1()
        {
            InitializeComponent();
            ThreadStart ts = new ThreadStart(GetIntroduction);
            td = new Thread(ts);
            td.SetApartmentState(ApartmentState.STA);
            td.Start();

        }
        public Form1(string userID, string pw)
        {
            mysqlUser = userID;
            mysqlPassword = pw;

            InitializeComponent();
            ThreadStart ts = new ThreadStart(GetIntroduction);
            td = new Thread(ts);
            td.SetApartmentState(ApartmentState.STA);
            td.Start();
        }
        ~Form1()
        {
            conn.Dispose();
        }

        void bgworker_DoWork(object sender, DoWorkEventArgs e)
        {

            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(50);
            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                string[] st = e.Argument as string[];
                int year = Convert.ToInt32(st[0]);
                int season = Convert.ToInt32(st[1]);
                switch (season)
                {
                    case 1:
                        getFirstSeason(year, season);
                        break;
                    case 2:
                        getSecondSeason(year, season);
                        break;
                    case 3:
                        getThirdSeason(year, season);
                        break;
                    case 4:
                        getFourthSeason(year, season);
                        break;
                }

                worker.ReportProgress(100);
            }


        }
        void bgworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
          
        }
        void bgworker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 0;
            if (e.Cancelled)
            {
                MessageBox.Show("Background task has been canceled", "info");
            }
            else
            {
                String year = skinComboBox40.SelectedItem.ToString();
                String inputSeason = skinComboBox41.SelectedItem.ToString();
                String season = "";

                switch (inputSeason)
                {
                    case "第一季度":
                        season = "first";

                        break;
                    case "第二季度":
                        season = "second";

                        break;
                    case "第三季度":
                        season = "third";

                        break;
                    case "第四季度":
                        season = "fourth";

                        break;
                }

                StreamReader sr = new StreamReader("..\\..\\stock\\result\\result_" + year + "_" + season + ".txt", Encoding.Default);
                stocks.Clear();
                String str = sr.ReadToEnd();
                all_timeChange = str.Split(',');
                for (int i = 0; i < 100; i++)
                {
                    DataGridViewRow row = new DataGridViewRow();

                    //this.Invoke((EventHandler)delegate
                    //{
                    if (skinDataGridView40.RowCount < 100)
                    {
                        skinDataGridView40.Rows.Add(row);
                    }
                    skinDataGridView40.Rows[i].Cells[0].Value = i + 1;
                    skinDataGridView40.Rows[i].Cells[1].Value = all_timeChange[i * 2];

                    double d = Convert.ToDouble(all_timeChange[i * 2 + 1]);
                    d = Math.Round(d, 2);
                    skinDataGridView40.Rows[i].Cells[2].Value = d;
                    //});
                }

                skinDataGridView40.Visible = true;
            }
        }




        void bgworker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker1 = sender as BackgroundWorker;
            worker1.ReportProgress(20);
            if (worker1.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                getFundsID(true);
               
                string[] sts = e.Argument as string[];
                int year1 = Convert.ToInt32(sts[0]);
                int season1 = Convert.ToInt32(sts[1]);
                int year2 = Convert.ToInt32(sts[2]);
                int season2 = Convert.ToInt32(sts[3]);
                updateStock(year1, season1, 0, true);
                updateStock(year2, season2, 0, true);

                List<Stock> tmp = new List<Stock>();
               
                bool IN;
                
                foreach (Stock st in stocks2)
                {
                    IN = false;
                    for (int i = 0; i < tmp.Count; i++)
                    {
                        if (tmp.ElementAt(i).sum < st.sum)
                        {
                            tmp.Insert(i, st);
                            IN = true;
                            break;
                        }
                    }
                    if (!IN)
                    {
                        tmp.Add(st);
                    }
    
                }
                string result = "";
                for (int i = 0; i < 50; i++)
                {
                    result += tmp.ElementAt(i).name + "," + tmp.ElementAt(i).sum + ",";
                }
                inText(result, "result\\result_increase_" + year1 + "_" + season1 + "_" + year2 + "_" + season2, "Default");
                worker1.ReportProgress(50);
                stocks2.Clear();
                updateStock(year1, season1);
                updateStock(year2, season2, Condition.COMPARISON);
                List<Stock> tmp1 = new List<Stock>();
                bool IN1;
                foreach (Stock st in stocks2)
                {
                    IN1 = false;
                    for (int i = 0; i < tmp1.Count; i++)
                    {
                        if (tmp1.ElementAt(i).sum > st.sum)
                        {
                            tmp1.Insert(i, st);
                            IN1 = true;
                            break;
                        }
                    }

                    if (!IN1)
                    {
                        tmp1.Add(st);
                    }
                }
                worker1.ReportProgress(70);
                string result1 = "";
                for (int i = 0; i < 50; i++)
                {
                    result1 += tmp1.ElementAt(i).name + "," + tmp1.ElementAt(i).sum + ",";
                    worker1.ReportProgress(70+i/2);
                }
                inText(result1, "result\\result_decrease_" + year1 + "_" + season1 + "_" + year2 + "_" + season2, "Default");
                stocks2.Clear();
                worker1.ReportProgress(100);
            }


        }
        void bgworker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar2.Value = e.ProgressPercentage;
            
        }

        void bgworker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar2.Value = 0;
            if (e.Cancelled)
            {
                MessageBox.Show("Background task has been canceled", "info");
            }
            else
            {
                //bgworker1.ReportProgress(0);
                String firstYear = skinComboBox51.SelectedItem.ToString();
                int year1 = Convert.ToInt32(firstYear);
                String firstSelectSeason = skinComboBox52.SelectedItem.ToString();
                String secondYear = skinComboBox53.SelectedItem.ToString();
                int year2 = Convert.ToInt32(secondYear);
                String secondSelectSeason = skinComboBox54.SelectedItem.ToString();
                int season1 = 0;
                int season2 = 0;

                switch (firstSelectSeason)
                {
                    case "第一季度":
                        season1 = 1;
                        break;
                    case "第二季度":
                        season1 = 2;
                        break;
                    case "第三季度":
                        season1 = 3;
                        break;
                    case "第四季度":
                        season1 = 4;
                        break;
                }

                switch (secondSelectSeason)
                {
                    case "第一季度":
                        season2 = 1;
                        break;
                    case "第二季度":
                        season2 = 2;
                        break;
                    case "第三季度":
                        season2 = 3;
                        break;
                    case "第四季度":
                        season2 = 4;
                        break;
                }

                //填充前50名基金增持最多股票数据
                StreamReader incsr = new StreamReader("..\\..\\stock\\result\\result_increase_" + firstYear + "_" + season1
                    + "_" + secondYear + "_" + season2 + ".txt", Encoding.Default);
                String incstr = incsr.ReadToEnd();
                string[] incall = incstr.Split(',');
                for (int i = 0; i < 50; i++)
                {
                    DataGridViewRow row = new DataGridViewRow();

                    this.Invoke((EventHandler)delegate
                    {
                        if (skinDataGridView51.RowCount < 50)
                        {
                            skinDataGridView51.Rows.Add(row);
                        }
                        skinDataGridView51.Rows[i].Cells[0].Value = i + 1;
                        skinDataGridView51.Rows[i].Cells[1].Value = incall[i * 2];

                        double d = Convert.ToDouble(incall[i * 2 + 1]);
                        d = Math.Round(d, 2);
                        skinDataGridView51.Rows[i].Cells[2].Value = d;
                    });
                }

                //填充前50名基金减持最多股票数据
                StreamReader decsr = new StreamReader("..\\..\\stock\\result\\result_decrease_" + firstYear + "_" + season1
                    + "_" + secondYear + "_" + season2 + ".txt", Encoding.Default);
                String decstr = decsr.ReadToEnd();
                string[] decall = decstr.Split(',');
                for (int i = 0; i < 50; i++)
                {
                    DataGridViewRow row = new DataGridViewRow();

                    this.Invoke((EventHandler)delegate
                    {
                        if (skinDataGridView52.RowCount < 50)
                        {
                            skinDataGridView52.Rows.Add(row);
                        }
                        skinDataGridView52.Rows[i].Cells[0].Value = i + 1;
                        skinDataGridView52.Rows[i].Cells[1].Value = decall[i * 2];

                        double d = Convert.ToDouble(decall[i * 2 + 1]);
                        d = Math.Round(d, 2);
                        skinDataGridView52.Rows[i].Cells[2].Value = d;
                    });
                }

                skinLabel55.Visible = true;
                skinLabel56.Visible = true;
                skinDataGridView51.Visible = true;
                skinDataGridView52.Visible = true;
            }
                

        }






        private void button4_Click(object sender, EventArgs e)
        {
            
        }

        

        void GetIntroduction()
        {
            //运行Form1首先链接到数据库
            conn = new MySqlConnection("Data Source=127.0.0.1;User Id=" + mysqlUser + ";Password=" + mysqlPassword);
            conn.Open();
            //断是否存在目标数据库Fund，不存在则先创建数据库，然后链接Fund。
            MySqlCommand useDatabase = new MySqlCommand("use Fund;", conn);
            try
            {
                useDatabase.ExecuteNonQuery();
            }
            catch (Exception)
            {
                MySqlCommand newDataBase = new MySqlCommand("CREATE DATABASE Fund;", conn);
                newDataBase.ExecuteNonQuery();
                newDataBase.Dispose();
                useDatabase.ExecuteNonQuery();
            }
            useDatabase.Dispose();
            //判断是否存在当天的table，不存在则执行updateData操作，弹出正在更新数据对话框、进度条
            try
            {
                string dname = DateTime.Now.ToString("yyyy_MM_dd");
                MySqlCommand test0 = new MySqlCommand("select * from " + dname + " where id=2;", conn);
                using (MySqlDataReader test00 = test0.ExecuteReader())
                {
                    if (!test00.Read())
                    {
                        test00.Dispose();
                        test0.Dispose();
                        throw new Exception();
                    }
                    test00.Dispose();
                    test0.Dispose();
                    
                }
            }
            catch (Exception)
            {
                updateData();
                //bgworker2 = new BackgroundWorker();
                //bgworker2.WorkerReportsProgress = true;
                //bgworker2.WorkerSupportsCancellation = true;
                //bgworker2.DoWork += bgworker2_DoWork;
                //bgworker2.ProgressChanged += bgworker2_ProgressChanged;
                //bgworker2.RunWorkerCompleted += bgworker2_RunWorkerCompleted;
                //bgworker2.RunWorkerAsync();
            }
            turnTo(1);
        }

        //获取网页的内容，代码来自老师给的股票
        string GetContent(string url)
        {
            string html = "";
            // 发送查询请求
            WebRequest request = WebRequest.Create(url);
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
                // 获得流
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                html = sr.ReadToEnd();
                response.Close();
            }
            catch (Exception ex)
            {
                // 本机没有联网
                if (ex.GetType().ToString().Equals("System.Net.WebException"))
                {
                    MessageBox.Show("请检查你的计算机是否已连接上互联网。\n" + url, "提示");
                }
            }
            return html;
        }

        private void updateData()
        {
            //开始更新
            MessageBox.Show("正在更新，视网络情况大概需要2到5分钟时间，请稍后。");
            //创建新表储存数据
            string createTable = "create table " + "newtable" + " ("
                + "id " + "int not null primary key auto_increment,"
                + "code " + "char(6),"
                + "name " + "char(10),"
                + "date " + "char(10),"
                + "data1 " + "char(8),"
                + "data2 " + "char(8),"
                + "data3 " + "char(8),"
                + "data4 " + "char(8),"
                + "data5 " + "char(8),"
                + "data6 " + "char(8),"
                + "data7 " + "char(8),"
                + "data8 " + "char(8),"
                + "data9 " + "char(8),"
                + "data10 " + "char(8),"
                + "data11 " + "char(8),"
                + "data12 " + "char(8),"
                + "data13 " + "char(8)"
                + ");";
            //尝试删除已存在newtable表，防止上次未更新完中途退出
            string deleteNew = "drop table " + "newtable" + ";";
            MySqlCommand dropNew = new MySqlCommand(deleteNew, conn);
            try
            {
                dropNew.ExecuteNonQuery();
            }
            catch (Exception ex) { }
            dropNew.Dispose();

            MySqlCommand newTable = new MySqlCommand(createTable, conn);
            newTable.ExecuteNonQuery();
            newTable.Dispose();

            //爬取表格数据存入数据库
            for (int i = 1; i < 56; i++)
            {
                this.Invoke((EventHandler)delegate
                {
                    string url = "http://fund.eastmoney.com/data/rankhandler.aspx?op=ph&dt=kf&ft=all&rs=&gs=0&sc=zzf&st=desc&sd=2015-10-29&ed=2016-10-29&qdii=&tabSubtype=,,,,,&pi="
                        + i + "&pn=50&dx=1&v=0.10850418109563731";

                    string data = GetContent(url);
                    //正则表达式，提取每两个引号之间内容
                    Regex re = new Regex("(?<=\").*?(?=\")", RegexOptions.None);

                    //用正则表达式提取内容,并存入数据库
                    MatchCollection mc = re.Matches(data);
                    foreach (Match funds in mc)
                    {
                        string fund = funds.Value;
                        //把逗号之间的内容提取出来放进string数组里
                        string[] all = Regex.Split(fund, ",", RegexOptions.IgnoreCase);
                        if (all[0].Length == 0)
                            continue;

                        //存数据库
                        string insert = "insert into " + "newtable"
                            + "(code,name,date,data1,data2,data3,data4,data5,data6,data7,data8,data9,data10,data11,data12,data13) values("
                            + "'" + all[0] + "',"
                            + "'" + all[1].Substring(0, (all[1].Length > 6 ? 6 : all[1].Length)) + "',"
                            + "'" + (all[3].Length == 0 ? "---" : all[3].Substring(5)) + "',"
                            + "'" + (all[4].Length == 0 ? "---" : all[4]) + "',"
                            + "'" + (all[5].Length == 0 ? "---" : all[5]) + "',"
                            + "'" + getPecent(all[6]) + "',"
                            + "'" + getPecent(all[7]) + "',"
                            + "'" + getPecent(all[8]) + "',"
                            + "'" + getPecent(all[9]) + "',"
                            + "'" + getPecent(all[10]) + "',"
                            + "'" + getPecent(all[11]) + "',"
                            + "'" + getPecent(all[12]) + "',"
                            + "'" + getPecent(all[13]) + "',"
                            + "'" + getPecent(all[14]) + "',"
                            + "'" + getPecent(all[15]) + "',"
                            + "'" + all[20] + "'"
                            + ");";
                        MySqlCommand cmdInsert = new MySqlCommand(insert, conn);
                        cmdInsert.ExecuteNonQuery();
                        cmdInsert.Dispose();
                    }
                });
            }

            //以当天日期作为表格名（如2017-12-20）
            string tableName = DateTime.Now.ToString("yyyy_MM_dd");

            //删除已存在的当天的数据表
            string deleteTable = "drop table " + tableName + ";";
            MySqlCommand dropTable = new MySqlCommand(deleteTable, conn);
            try
            {
                dropTable.ExecuteNonQuery();
            }
            catch (Exception ex) { }
            dropTable.Dispose();

            //重命名newtable名字为当天日期
            string rename = "alter table newtable rename " + tableName + ";";
            MySqlCommand renameTable = new MySqlCommand(rename, conn);
            renameTable.ExecuteNonQuery();
            renameTable.Dispose();

            //弹出提示框提示更新完成
            MessageBox.Show("更新完成！");
        }

        //转到特定页
        void turnTo(int pi)
        {
            string tableName = DateTime.Now.ToString("yyyy_MM_dd");
            
            //清空之前内容
            this.skinDataGridView1.Rows.Clear();

            //从数据库读取数据
            string readData = "select * from " + tableName + " limit " + ((pi - 1) * 50) + "," + (pi * 50 - 1) + ";";
            
            using (MySqlCommand cmdRead = new MySqlCommand(readData, conn))
            {
                MySqlDataReader myReader = cmdRead.ExecuteReader();
                int index = 0;

                while (myReader.Read())
                {
                    if (myReader.HasRows)
                    {
                        //新建一行
                        DataGridViewRow row = new DataGridViewRow();
                        //之后的代码都是把string数组的内容放进每一行里
                        this.Invoke((EventHandler)delegate
                        {
                            skinDataGridView1.Rows.Add(row);
                            for (int i = 0; i < 16; i++)
                                skinDataGridView1.Rows[index].Cells[i].Value = myReader.GetString(i + 1);
                        });

                        index++;
                    }
                }

                myReader.Dispose();
                cmdRead.Dispose();
            }


            //改变页码
            page = pi;
            label1.Text = page + "/55";

            //清空文字框内容
            textBox1.Text = "";
        }

        //获得两位有效数字的字符串百分数
        string getPecent(string temp)
        {
            if (temp.Length == 0)
            {
                return "---";
            }
            double d = Math.Round(Convert.ToDouble(temp), 2);
            temp = d.ToString();
            if (Convert.ToInt32(d) - d == 0)
                temp += ".00%";
            else if (Convert.ToInt32(d*10) - d*10 == 0)
                temp += "0%";
            else
                temp += "%";
            return temp;
        }


        private void skinDataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        //下一页
        private void button2_Click(object sender, EventArgs e)
        {
            if (page == 55)
                return;
            turnTo(page + 1);
        }
        //上一页
        private void button1_Click(object sender, EventArgs e)
        {
            if (page == 1)
                return;
            turnTo(page - 1);
        }
        //转到特定页
        private void button3_Click(object sender, EventArgs e)
        {
            //使用try catch通过文字框内容转到特定页，并抛出错误输出
            try
            {
                int a = Convert.ToInt32(textBox1.Text);
                if (!(a > 0 && a < 56))
                    throw new Exception();
                turnTo(a);
            }
            catch(Exception ex)
            {
                MessageBox.Show("输入错误，请检查您输入的数字（1-55）。", "提示");
                textBox1.Text = "";
                textBox1.Enabled = true;
                
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                
            }
        }

        


        //查看某只基金详情
        private void skinDataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //Form2 f = new Form2(skinDataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString());
            //f.Show();
            skinTabControl1.SelectTab(1);
            singleMessageShow(skinDataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString());

        }

        /*
        void getFundsID()
        {
            for (int i = 1; i < 2; i++)
            {
                this.Invoke((EventHandler)delegate
                {
                    string url = "http://fund.eastmoney.com/data/rankhandler.aspx?op=ph&dt=kf&ft=all&rs=&gs=0&sc=zzf&st=desc&sd=2015-10-29&ed=2016-10-29&qdii=&tabSubtype=,,,,,&pi="
                        + i + "&pn=50&dx=1&v=0.10850418109563731";

                    string data = GetContent(url);
                    //正则表达式，提取每两个引号之间内容
                    Regex re = new Regex("(?<=\").*?(?=\")", RegexOptions.None);

                    //用正则表达式提取内容
                    MatchCollection mc = re.Matches(data);
                    foreach (Match funds in mc)
                    {
                        string fund = funds.Value;
                        //把逗号之间的内容提取出来放进string数组里
                        string[] all = Regex.Split(fund, ",", RegexOptions.IgnoreCase);
                        if (all[0].Length == 0)
                            continue;
                        fundsID.Add(all[0]);
                    }
                });
            }
        }
        */
        
        void getFundsID(bool flag = false)
        {
            string tableName = DateTime.Now.ToString("yyyy_MM_dd");
            MySqlCommand readID = new MySqlCommand("select code from " + tableName + ";", conn);
            using (MySqlDataReader idReader = readID.ExecuteReader())
            {
                while (idReader.Read())
                {
                    if (idReader.HasRows)
                    {
                        if(!flag)
                            fundsID.Add(idReader.GetString(0));
                        else
                            fundsID2.Add(idReader.GetString(0));
                    }
                        
                }
                idReader.Dispose();
                readID.Dispose();
            }
        }

        

        void updateStock(int year, int season, Condition condition = 0,bool flag = false)
        {
            string url2;
            string HTML;
            List<String> fundsIDTmp = new List<string>();
            if (!flag)
            {
                fundsIDTmp = fundsID;
            }
            else
            {
                fundsIDTmp = fundsID2;
            }
            foreach (string ID in fundsIDTmp)
            {
                url2 = "http://fund.eastmoney.com/f10/FundArchivesDatas.aspx?type=jjcc&code=" + ID + "&topline=100&year=" + year + "&month=";

                HTML = GetContent(url2);
                string content = HTML;
                string pecent;

                double sums = 0;
                switch (season)
                {
                    case 1:
                        
                        if (content.Length == 0)
                            continue;
                        if (content.Contains(year + "年1季度股票投资明细"))
                            content = content.Substring(content.IndexOf(year + "年1季度股票投资明细") + 25);
                        else
                            continue;
                        break;
                    case 2:
                        
                        if (content.Length == 0)
                            continue;
                        if (content.IndexOf(year + "年1季度股票投资明细") - content.IndexOf(year + "年2季度股票投资明细") - 25 > 0)
                            content = content.Substring(content.IndexOf(year + "年2季度股票投资明细") + 25, content.IndexOf(year + "年1季度股票投资明细") - content.IndexOf(year + "年2季度股票投资明细") + 25);
                        else
                            continue;
                        break;
                    case 3:
                        
                        if (content.Length == 0)
                            continue;
                        if (content.IndexOf(year + "年2季度股票投资明细") - content.IndexOf(year + "年3季度股票投资明细") - 25 > 0)
                            content = content.Substring(content.IndexOf(year + "年3季度股票投资明细") + 25, content.IndexOf(year + "年2季度股票投资明细") - content.IndexOf(year + "年3季度股票投资明细") + 25);
                        else
                            continue;
                        break;
                    case 4:
                        
                        if (content.Length == 0)
                            continue;
                        if (content.IndexOf(year + "年3季度股票投资明细") - content.IndexOf(year + "年4季度股票投资明细") - 25 > 0)
                            content = content.Substring(content.IndexOf(year + "年4季度股票投资明细") + 25, content.IndexOf(year + "年3季度股票投资明细") - content.IndexOf(year + "年4季度股票投资明细") + 25);
                        else
                            continue;
                        break;
                }

                while (content.Contains("<td class='tol'>") && content.IndexOf("<td class='tol'>") + 20 < content.Length)
                {

                    content = content.Substring(content.IndexOf("<td class='tol'>") + 20);
                    string stockName = null;
                    if (content.Contains("<"))
                        stockName = content.Substring(content.IndexOf(">") + 1, content.IndexOf("<") - content.IndexOf(">") - 1);
                    else
                        break;
                    content = content.Substring(content.IndexOf("档案") + 2);
                    if (content.Length <= 20)
                        break;
                    content = content.Substring(content.IndexOf("<td class='tor'>") + 16);
                    if (content.Length <= 20)
                        break;
                    content = content.Substring(content.IndexOf("<td class='tor'>") + 16);
                    if (content.Length <= 15)
                        break;
                    content = content.Substring(content.IndexOf("<td class='tor'>") + 16);

                    if (content.Contains("<"))
                        pecent = content.Substring(0, content.IndexOf("<"));
                    else
                        continue;
                    if (pecent.Length != 0)
                        try
                        {
                            sums = Convert.ToDouble(pecent);
                        }
                        catch (FormatException e)
                        {
                            continue;
                        }
                    else
                        continue;
                    bool matchs = false;
                    if (!flag)
                    {
                        foreach (Stock sto in stocks)
                        {
                            if (sto.name == stockName)
                            {
                                if (condition == 0)
                                    sto.sum += sums;
                                else
                                    sto.sum -= sums;
                                matchs = true;
                                sums = 0;
                                break;
                            }
                        }
                        if (!matchs && stockName != null)
                        {
                            Stock st = new Stock();
                            st.name = stockName;
                            st.sum = sums;
                            stocks.Add(st);
                            sums = 0;
                        }
                    }
                    else
                    {
                        foreach (Stock sto in stocks2)
                        {
                            if (sto.name == stockName)
                            {
                                if (condition == 0)
                                    sto.sum += sums;
                                else
                                    sto.sum -= sums;
                                matchs = true;
                                sums = 0;
                                break;
                            }
                        }
                        if (!matchs && stockName != null)
                        {
                            Stock st = new Stock();
                            st.name = stockName;
                            st.sum = sums;
                            stocks2.Add(st);
                            sums = 0;
                        }
                    }
                    
                }
                
            }
        }
        
        void getFirstSeason(int year, int season)
        {
            getFundsID();
            updateStock(year, season);
            List<Stock> tmp = new List<Stock>();
            bool IN;
            foreach (Stock st in stocks)
            {
                IN = false;
                for (int i = 0; i < tmp.Count; i++)
                {
                    if (tmp.ElementAt(i).sum < st.sum)
                    {
                        tmp.Insert(i, st);
                        IN = true;
                        break;
                    }
                }
                if (!IN)
                {
                    tmp.Add(st);
                }
            }
            string result = "";
            for (int i = 0; i < 100; i++)
            {
                result += tmp.ElementAt(i).name + "," + tmp.ElementAt(i).sum + ",";
            }
            inText(result, "result\\result_"+year+"_first", "Default");
        }
        
        void getFourthSeason(int year, int season)
        {
            getFundsID();
            updateStock(year, season);
            List<Stock> tmp = new List<Stock>();
            bool IN;
            foreach (Stock st in stocks)
            {
                IN = false;
                for (int i = 0; i < tmp.Count; i++)
                {
                    if (tmp.ElementAt(i).sum < st.sum)
                    {
                        tmp.Insert(i, st);
                        IN = true;
                        break;
                    }
                }
                if (!IN)
                {
                    tmp.Add(st);
                }
            }
            string result = "";
            for (int i = 0; i < 100; i++)
            {
                result += tmp.ElementAt(i).name + "," + tmp.ElementAt(i).sum + ",";
            }
            inText(result, "result\\result_"+year+"_fourth", "Default");
        }
        
        void getThirdSeason(int year, int season)
        {
            getFundsID();
            updateStock(year, season);
            List<Stock> tmp = new List<Stock>();
            bool IN;
            foreach (Stock st in stocks)
            {
                IN = false;
                for (int i = 0; i < tmp.Count; i++)
                {
                    if (tmp.ElementAt(i).sum < st.sum)
                    {
                        tmp.Insert(i, st);
                        IN = true;
                        break;
                    }
                }
                if (!IN)
                {
                    tmp.Add(st);
                }
            }
            string result = "";
            for (int i = 0; i < 100; i++)
            {
                result += tmp.ElementAt(i).name + "," + tmp.ElementAt(i).sum + ",";
            }
            inText(result, "result\\result_"+year+"_third", "Default");
        }
        
        void getSecondSeason(int year, int season)
        {
            getFundsID();
            updateStock(year, season);
            List<Stock> tmp = new List<Stock>();
            bool IN;
            foreach (Stock st in stocks)
            {
                IN = false;
                for (int i = 0; i < tmp.Count; i++)
                {
                    if (tmp.ElementAt(i).sum < st.sum)
                    {
                        tmp.Insert(i, st);
                        IN = true;
                        break;
                    }
                }
                if (!IN)
                {
                    tmp.Add(st);
                }
            }
            string result = "";
            for (int i = 0; i < 100; i++)
            {
                result += tmp.ElementAt(i).name + "," + tmp.ElementAt(i).sum + ",";
            }
            inText(result, "result\\result_"+year+"_second", "Default");
        }

        
        void inText(string data, string name,string str="UTF8")
        {
            FileStream fs = new FileStream("..\\..\\stock\\" + name + ".txt", FileMode.Create);
            //获得字节数组
            byte[] datas;
            if (str == "UTF8")
            {
                datas = System.Text.Encoding.UTF8.GetBytes(data);
                fs.Write(datas, 0, data.Length);
            }
            else
            {
                datas = System.Text.Encoding.Default.GetBytes(data);
                fs.Write(datas, 0, datas.Length);
            }
            //开始写入
            //清空缓冲区、关闭流
            fs.Flush();
            fs.Close();
        }

        List<string> GetAll(int year,int season)
        {
            List<string> b = new List<string>();
            if (season == 1)
            {
                DirectoryInfo folder = new DirectoryInfo("..\\..\\stock\\first" + "\\" + year);
                for (int i = 0; i < folder.GetFiles("*.txt").Count(); i++)
                {
                    b.Add(folder.GetFiles("*.txt")[i].Name);
                }

                return b;
            }
            else if (season == 2)
            {
                DirectoryInfo folder = new DirectoryInfo("..\\..\\stock\\second" + "\\" + year);
                for (int i = 0; i < folder.GetFiles("*.txt").Count(); i++)
                {
                    b.Add(folder.GetFiles("*.txt")[i].Name);
                }

                return b;
            }
            else if(season == 3)
            {
                DirectoryInfo folder = new DirectoryInfo("..\\..\\stock\\third" + "\\" + year);
                for (int i = 0; i < folder.GetFiles("*.txt").Count(); i++)
                {
                    b.Add(folder.GetFiles("*.txt")[i].Name);
                }

                return b;
            }
            else if(season == 4)
            {
                DirectoryInfo folder = new DirectoryInfo("..\\..\\stock\\fourth" + "\\" + year);
                for (int i = 0; i < folder.GetFiles("*.txt").Count(); i++)
                {
                    b.Add(folder.GetFiles("*.txt")[i].Name);
                }

                return b;
            }
            else
            {
                DirectoryInfo folder = new DirectoryInfo("..\\..\\stock\\result");
                for (int i = 0; i < folder.GetFiles("*.txt").Count(); i++)
                {
                    b.Add(folder.GetFiles("*.txt")[i].Name);
                }

                return b;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            bgworker2 = new BackgroundWorker();
            bgworker2.WorkerReportsProgress = true;
            bgworker2.WorkerSupportsCancellation = true;
            bgworker2.DoWork += bgworker2_DoWork;
            bgworker2.ProgressChanged += bgworker2_ProgressChanged;
            bgworker2.RunWorkerCompleted += bgworker2_RunWorkerCompleted;
            bgworker2.RunWorkerAsync();
            //updateData();
            //Form4 form4 = new Form4();
            //form4.Show();
        }
        void bgworker2_DoWork(object sender, DoWorkEventArgs e)
        {
            MySqlConnection conn2=new MySqlConnection("Data Source=127.0.0.1;User Id="+mysqlUser+";Password="+mysqlPassword);
            conn2.Open();
            MySqlCommand useDatabase = new MySqlCommand("use Fund;", conn2);
            useDatabase.ExecuteNonQuery();
            useDatabase.Dispose();


            BackgroundWorker worker2 = sender as BackgroundWorker;
            worker2.ReportProgress(20);
            if (worker2.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                //开始更新
                MessageBox.Show("正在更新，视网络情况大概需要2到5分钟时间，请稍后。");
                //创建新表储存数据
                string createTable = "create table " + "newtable" + " ("
                    + "id " + "int not null primary key auto_increment,"
                    + "code " + "char(6),"
                    + "name " + "char(10),"
                    + "date " + "char(10),"
                    + "data1 " + "char(8),"
                    + "data2 " + "char(8),"
                    + "data3 " + "char(8),"
                    + "data4 " + "char(8),"
                    + "data5 " + "char(8),"
                    + "data6 " + "char(8),"
                    + "data7 " + "char(8),"
                    + "data8 " + "char(8),"
                    + "data9 " + "char(8),"
                    + "data10 " + "char(8),"
                    + "data11 " + "char(8),"
                    + "data12 " + "char(8),"
                    + "data13 " + "char(8)"
                    + ");";
                //尝试删除已存在newtable表，防止上次未更新完中途退出
                string deleteNew = "drop table " + "newtable" + ";";
                MySqlCommand dropNew = new MySqlCommand(deleteNew, conn2);
                try
                {
                    dropNew.ExecuteNonQuery();
                }
                catch (Exception ex) { }
                dropNew.Dispose();

                MySqlCommand newTable = new MySqlCommand(createTable, conn2);
                newTable.ExecuteNonQuery();
                newTable.Dispose();

                //爬取表格数据存入数据库
                for (int i = 1; i < 56; i++)
                {
                    //this.Invoke((EventHandler)delegate
                    //{
                    worker2.ReportProgress(i + 20);

                    string url = "http://fund.eastmoney.com/data/rankhandler.aspx?op=ph&dt=kf&ft=all&rs=&gs=0&sc=zzf&st=desc&sd=2015-10-29&ed=2016-10-29&qdii=&tabSubtype=,,,,,&pi="
                        + i + "&pn=50&dx=1&v=0.10850418109563731";

                    string data = GetContent(url);
                    //正则表达式，提取每两个引号之间内容
                    Regex re = new Regex("(?<=\").*?(?=\")", RegexOptions.None);

                    //用正则表达式提取内容,并存入数据库
                    MatchCollection mc = re.Matches(data);
                    foreach (Match funds in mc)
                    {
                        string fund = funds.Value;
                        //把逗号之间的内容提取出来放进string数组里
                        string[] all = Regex.Split(fund, ",", RegexOptions.IgnoreCase);
                        if (all[0].Length == 0)
                            continue;

                        //存数据库
                        string insert = "insert into " + "newtable"
                            + "(code,name,date,data1,data2,data3,data4,data5,data6,data7,data8,data9,data10,data11,data12,data13) values("
                            + "'" + all[0] + "',"
                            + "'" + all[1].Substring(0, (all[1].Length > 6 ? 6 : all[1].Length)) + "',"
                            + "'" + (all[3].Length == 0 ? "---" : all[3].Substring(5)) + "',"
                            + "'" + (all[4].Length == 0 ? "---" : all[4]) + "',"
                            + "'" + (all[5].Length == 0 ? "---" : all[5]) + "',"
                            + "'" + getPecent(all[6]) + "',"
                            + "'" + getPecent(all[7]) + "',"
                            + "'" + getPecent(all[8]) + "',"
                            + "'" + getPecent(all[9]) + "',"
                            + "'" + getPecent(all[10]) + "',"
                            + "'" + getPecent(all[11]) + "',"
                            + "'" + getPecent(all[12]) + "',"
                            + "'" + getPecent(all[13]) + "',"
                            + "'" + getPecent(all[14]) + "',"
                            + "'" + getPecent(all[15]) + "',"
                            + "'" + all[20] + "'"
                            + ");";
                        MySqlCommand cmdInsert = new MySqlCommand(insert, conn2);
                        cmdInsert.ExecuteNonQuery();
                        cmdInsert.Dispose();
                    }
                    //});
                }

                //以当天日期作为表格名（如2017-12-20）
                string tableName = DateTime.Now.ToString("yyyy_MM_dd");

                //尝试删除已存在的当天的数据表
                string deleteTable = "drop table " + tableName + ";";
                MySqlCommand dropTable = new MySqlCommand(deleteTable, conn2);
                try
                {
                    dropTable.ExecuteNonQuery();
                }
                catch (Exception ex) { }
                dropTable.Dispose();

                //重命名newtable名字为当天日期
                string rename = "alter table newtable rename " + tableName + ";";
                MySqlCommand renameTable = new MySqlCommand(rename, conn2);
                renameTable.ExecuteNonQuery();
                renameTable.Dispose();

                conn2.Dispose();

                //弹出提示框提示更新完成
                worker2.ReportProgress(100);
            }


        }
        void bgworker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar3.Value = e.ProgressPercentage;
            label2.Text = ((e.ProgressPercentage>=75?75:e.ProgressPercentage)-20) + "/55";

        }
        void bgworker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar3.Value = 0;
            label2.Text = 0 + "/55";
            if (e.Cancelled)
            {
                MessageBox.Show("Background task has been canceled", "info");
            }
            else
            {
                MessageBox.Show("Background task has been finished", "info");
            }
        }






        private void skinButton20_Click(object sender, EventArgs e)
        {
            string str = skinTextBox20.Text;

            //通过try catch把输入的基金代码转到网页，若网页不存在，则抛出错误，
            //同时把输入的错误基金代号的格式抛出
            try
            {
                string x = GetContent(@"http://fund.eastmoney.com/" + str + ".html");
                
                x = x.Substring(x.IndexOf("<title>") + 7);
                x = x.Substring(0, x.IndexOf("</title>"));
                if (!(x.Contains(str)))
                {
                    throw new Exception();
                } else
                {
                    singleMessageShow(str);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("输入错误，不存编号为" + str + "的基金。", "提示");
            }
        }

        private void singleMessageShow(string id)
        {
            //获取图片内容
            skinPictureBox20.ImageLocation = @"http://j4.dfcfw.com/charts/pic6/" + id + ".png";

            //通过string的substring和IndexOf方法处理字符串，获取文字内容，并放入文字框
            string result = GetContent(@"http://fund.eastmoney.com/" + id + ".html");
            try
            {
                skinLabel202.Text = id;

                result = result.Substring(result.IndexOf("id=\"gz_gsz\">"));
                string tmp = result.Substring(result.IndexOf("\">") + 2);
                tmp = tmp.Substring(0, tmp.IndexOf("<"));
                skinTextBox21.Text = tmp;

                result = result.Substring(result.IndexOf("ui-font-large") + 1);
                tmp = result.Substring(result.IndexOf("\">") + 2);
                tmp = tmp.Substring(0, tmp.IndexOf("<"));
                skinTextBox22.Text = tmp;

                result = result.Substring(result.IndexOf("ui-font-large"));
                tmp = result.Substring(result.IndexOf("\">") + 2);
                tmp = tmp.Substring(0, tmp.IndexOf("<"));
                skinTextBox23.Text = tmp;

                result = result.Substring(result.IndexOf("<table>"));
                result = result.Substring(result.IndexOf("<td>") + 4);
                result = result.Substring(result.IndexOf(">") + 1);
                tmp = result.Substring(0, result.IndexOf("</a>"));
                result = result.Substring(result.IndexOf("|&nbsp;&nbsp;") + 13);
                tmp += " | " + result.Substring(0, result.IndexOf("<"));
                skinLabel26.Text = tmp;

                result = result.Substring(result.IndexOf("基金规模"));
                result = result.Substring(result.IndexOf("：") + 1);
                tmp = result.Substring(0, result.IndexOf("<"));
                skinLabel28.Text = tmp;

                result = result.Substring(result.IndexOf("基金经理"));
                result = result.Substring(result.IndexOf(">") + 1);
                tmp = result.Substring(0, result.IndexOf("<"));
                skinLabel210.Text = tmp;

                result = result.Substring(result.IndexOf("日"));
                result = result.Substring(result.IndexOf("：") + 1);
                tmp = result.Substring(0, result.IndexOf("<"));
                skinLabel212.Text = tmp;

                result = result.Substring(result.IndexOf("人"));
                result = result.Substring(result.IndexOf("：") + 1);
                result = result.Substring(result.IndexOf(">") + 1);
                tmp = result.Substring(0, result.IndexOf("<"));
                skinLabel214.Text = tmp;
            }
            catch (Exception)
            {
                MessageBox.Show("此基金处于认购期不存在信息", "提示");
            }

            skinTextBox21.Visible = true;
            skinTextBox22.Visible = true;
            skinTextBox23.Visible = true;
            skinLabel201.Visible = true;
            skinLabel21.Visible = true;
            skinLabel22.Visible = true;
            skinLabel23.Visible = true;
            skinLabel24.Visible = true;
            skinLabel25.Visible = true;
            skinLabel27.Visible = true;
            skinLabel29.Visible = true;
            skinLabel211.Visible = true;
            skinLabel213.Visible = true;
        }

        private void skinButton21_Click(object sender, EventArgs e)
        {
            string str = skinLabel202.Text;

            //通过try catch把输入的基金代码转到网页，若网页不存在，则抛出错误，
            //同时把输入的错误基金代号的格式抛出
            try
            {
                if (str == "")
                {
                    throw new Exception();
                }
                else
                {
                    skinTabControl1.SelectTab(2);
                    singlePositionsShow(str);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("当前基金为空！");
            }
        }

        private void skinButton30_Click(object sender, EventArgs e)
        {
            string str = skinTextBox30.Text;

            //通过try catch把输入的基金代码转到网页，若网页不存在，则抛出错误，
            //同时把输入的错误基金代号的格式抛出
            try
            {
                string x = GetContent(@"http://fund.eastmoney.com/" + str + ".html");

                x = x.Substring(x.IndexOf("<title>") + 7);
                x = x.Substring(0, x.IndexOf("</title>"));
                if (!(x.Contains(str)))
                {
                    throw new Exception();
                }
                else
                {
                    singlePositionsShow(str);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("输入错误，不存编号为" + str + "的基金。", "提示");
            }
        }

        private void singlePositionsShow(string id)
        {
            string url = @"http://fund.eastmoney.com/f10/FundArchivesDatas.aspx?type=jjcc&code=" + id + "&topline=10&year=&month=&rt=0.029766627475606988";

            string data = GetContent(url);
            //如果没有表格内容，则返回
            //string的contain方法是判断字符串是否有一段特定的字符
            if (!data.Contains("<tbody>"))
                return;
            skinLabel32.Text = id;
            if (!data.Contains("市场"))
            {

                data = data.Substring(data.IndexOf("<tbody>") + 7);
                data = data.Substring(0, data.IndexOf("</tbody>"));
                int index = 0;
                string tmp;
                //处理字符串，获取需要的信息
                while (data.Contains("<tr>"))
                {

                    DataGridViewRow row = new DataGridViewRow();

                    data = data.Substring(data.IndexOf("<td>") + 4);
                    tmp = data.Substring(0, data.IndexOf("<"));
                    this.Invoke((EventHandler)delegate
                    {
                        if(skinDataGridView30.RowCount < 10)
                        {
                            skinDataGridView30.Rows.Add(row);
                        }
                        skinDataGridView30.Rows[index].Cells[0].Value = tmp;
                    });

                    data = data.Substring(data.IndexOf("<td>") + 4);
                    tmp = data.Substring(data.IndexOf(">") + 1);
                    //tmp = tmp.Substring(0, tmp.IndexOf("</a"));
                    tmp = tmp.Substring(0, tmp.IndexOf("<"));
                    this.Invoke((EventHandler)delegate
                    {
                        skinDataGridView30.Rows[index].Cells[1].Value = tmp;
                    });

                    data = data.Substring(data.IndexOf("<td"));
                    data = data.Substring(data.IndexOf(">") + 1);
                    tmp = data.Substring(data.IndexOf(">") + 1);
                    tmp = tmp.Substring(0, tmp.IndexOf("<"));
                    this.Invoke((EventHandler)delegate
                    {
                        skinDataGridView30.Rows[index].Cells[2].Value = tmp;
                    });

                    //跳过无用数据
                    data = data.Substring(data.IndexOf("<td") + 3);
                    data = data.Substring(data.IndexOf("<td") + 3);
                    data = data.Substring(data.IndexOf("<td") + 3);


                    data = data.Substring(data.IndexOf("<td") + 3);
                    tmp = data.Substring(data.IndexOf(">") + 1);
                    tmp = tmp.Substring(0, tmp.IndexOf("<"));
                    this.Invoke((EventHandler)delegate
                    {
                        skinDataGridView30.Rows[index].Cells[3].Value = tmp;
                    });

                    data = data.Substring(data.IndexOf("<td") + 3);
                    tmp = data.Substring(data.IndexOf(">") + 1);
                    tmp = tmp.Substring(0, tmp.IndexOf("<"));
                    this.Invoke((EventHandler)delegate
                    {
                        skinDataGridView30.Rows[index].Cells[4].Value = tmp;
                    });

                    data = data.Substring(data.IndexOf("<td"));
                    tmp = data.Substring(data.IndexOf(">") + 1);
                    tmp = tmp.Substring(0, tmp.IndexOf("<"));
                    this.Invoke((EventHandler)delegate
                    {
                        skinDataGridView30.Rows[index].Cells[5].Value = tmp;
                    });
                    index++;
                }
            }
            else
            {
                data = data.Substring(data.IndexOf("<tbody>") + 7);
                data = data.Substring(0, data.IndexOf("</tbody>"));
                int index = 0;
                while (data.Contains("<tr>"))
                {

                    DataGridViewRow row = new DataGridViewRow();
                    this.Invoke((EventHandler)delegate
                    {
                        skinDataGridView30.Rows.Add(row);
                    });
                    for (int i = 0; i < 6; i++)
                    {
                        data = data.Substring(data.IndexOf("<td") + 3);
                        data = data.Substring(data.IndexOf(">") + 1);
                        string tmp = data.Substring(0, data.IndexOf("<"));
                        this.Invoke((EventHandler)delegate
                        {
                            skinDataGridView30.Rows[index].Cells[i].Value = tmp;
                        });
                    }
                    index++;
                }
            }
            skinLabel31.Visible = true;
            skinDataGridView30.Visible = true;
        }
        
        private void skinButton40_Click(object sender, EventArgs e)
        {
            
            if (skinComboBox40.SelectedIndex == -1)
            {
                MessageBox.Show("请选择年份");
                return;
            }
            if (skinComboBox41.SelectedIndex == -1)
            {
                MessageBox.Show("请选择季度");
                return;
            }
            String year = skinComboBox40.SelectedItem.ToString();
            String inputSeason = skinComboBox41.SelectedItem.ToString();
            String season = "";
            int season_int = 0;
            switch (inputSeason)
            {
                case "第一季度":
                    season = "first";
                    season_int = 1;
                    break;
                case "第二季度":
                    season = "second";
                    season_int = 2;
                    break;
                case "第三季度":
                    season = "third";
                    season_int = 3;
                    break;
                case "第四季度":
                    season = "fourth";
                    season_int = 4;
                    break;
            }
            List<string> srs = GetAll(2017, 0);
            string[] all = null;
            
            if(!srs.Contains("result_" + year + "_" + season + ".txt"))
            {
                
                DialogResult dr = MessageBox.Show("暂无此数据，需更新数据，可能需要部分等待时间，是否确定？", "更新数据", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.OK)
                {
                    //点确定的代码
                    //getInformation(Convert.ToInt32(year), season_int);
                    string[] st = new string[2];
                    st[0] = year.ToString();
                    st[1] = season_int.ToString();
                   
                    bgworker = new BackgroundWorker();
                    bgworker.WorkerReportsProgress = true;
                    bgworker.WorkerSupportsCancellation = true;
                    bgworker.DoWork += bgworker_DoWork;
                    bgworker.ProgressChanged += bgworker_ProgressChanged;
                    bgworker.RunWorkerCompleted += bgworker_RunWorkerCompleted;
                    bgworker.RunWorkerAsync(st);
                }
                
            }
            else
            {
                StreamReader sr = new StreamReader("..\\..\\stock\\result\\result_" + year + "_" + season + ".txt", Encoding.Default);
                String str = sr.ReadToEnd();
                all_timeChange = str.Split(',');
                for (int i = 0; i < 100; i++)
                {
                    DataGridViewRow row = new DataGridViewRow();

                    //this.Invoke((EventHandler)delegate
                    //{
                    if (skinDataGridView40.RowCount < 100)
                    {
                        skinDataGridView40.Rows.Add(row);
                    }
                    skinDataGridView40.Rows[i].Cells[0].Value = i + 1;
                    skinDataGridView40.Rows[i].Cells[1].Value = all_timeChange[i * 2];

                    double d = Convert.ToDouble(all_timeChange[i * 2 + 1]);
                    d = Math.Round(d, 2);
                    skinDataGridView40.Rows[i].Cells[2].Value = d;
                    //});
                }

                skinDataGridView40.Visible = true;
            }


        }

        private void skinButton1_Click(object sender, EventArgs e)
        {
            if (skinComboBox51.SelectedIndex == -1 || skinComboBox52.SelectedIndex == -1 ||
                skinComboBox53.SelectedIndex == -1 || skinComboBox54.SelectedIndex == -1)
            {
                MessageBox.Show("请选择要进行对比的年份和季度！");
                return;
            }

            String firstYear = skinComboBox51.SelectedItem.ToString();
            int year1 = Convert.ToInt32(firstYear);
            String firstSelectSeason = skinComboBox52.SelectedItem.ToString();
            String secondYear = skinComboBox53.SelectedItem.ToString();
            int year2 = Convert.ToInt32(secondYear);
            String secondSelectSeason = skinComboBox54.SelectedItem.ToString();
            int season1 = 0;
            int season2 = 0;

            switch (firstSelectSeason)
            {
                case "第一季度":
                    season1 = 1;
                    break;
                case "第二季度":
                    season1 = 2;
                    break;
                case "第三季度":
                    season1 = 3;
                    break;
                case "第四季度":
                    season1 = 4;
                    break;
            }

            switch (secondSelectSeason)
            {
                case "第一季度":
                    season2 = 1;
                    break;
                case "第二季度":
                    season2 = 2;
                    break;
                case "第三季度":
                    season2 = 3;
                    break;
                case "第四季度":
                    season2 = 4;
                    break;
            }

            if (year1 == year2 && season1 == season2)
            {
                MessageBox.Show("请选择不同的年份与季度！");
                return;
            }
            List<string> srs = GetAll(2017, 0);
            if (!srs.Contains("result_increase_" + firstYear + "_" + season1 + "_" + secondYear + "_" + season2 + ".txt"))
            {
                DialogResult dr = MessageBox.Show("暂无此数据，需更新数据，可能需要部分等待时间，是否确定？", "更新数据", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.OK)
                {
                    //点确定的代码
                    //getInformation(Convert.ToInt32(year), season_int);
                    string[] st = new string[4];
                    st[0] = year1.ToString();
                    st[1] = season1.ToString();
                    st[2] = year2.ToString();
                    st[3] = season2.ToString();


                    bgworker1 = new BackgroundWorker();
                    bgworker1.WorkerReportsProgress = true;
                    bgworker1.WorkerSupportsCancellation = true;
                    bgworker1.DoWork += bgworker1_DoWork;
                    bgworker1.ProgressChanged += bgworker1_ProgressChanged;
                    bgworker1.RunWorkerCompleted += bgworker1_RunWorkerCompleted;
                    bgworker1.RunWorkerAsync(st);
                }
              
            }
            else
            {
                //填充前50名基金增持最多股票数据
                StreamReader incsr = new StreamReader("..\\..\\stock\\result\\result_increase_" + firstYear + "_" + season1
                    + "_" + secondYear + "_" + season2 + ".txt", Encoding.Default);
                String incstr = incsr.ReadToEnd();
                string[] incall = incstr.Split(',');
                for (int i = 0; i < 50; i++)
                {
                    DataGridViewRow row = new DataGridViewRow();

                    this.Invoke((EventHandler)delegate
                    {
                        if (skinDataGridView51.RowCount < 50)
                        {
                            skinDataGridView51.Rows.Add(row);
                        }
                        skinDataGridView51.Rows[i].Cells[0].Value = i + 1;
                        skinDataGridView51.Rows[i].Cells[1].Value = incall[i * 2];

                        double d = Convert.ToDouble(incall[i * 2 + 1]);
                        d = Math.Round(d, 2);
                        skinDataGridView51.Rows[i].Cells[2].Value = d;
                    });
                }

                //填充前50名基金减持最多股票数据
                StreamReader decsr = new StreamReader("..\\..\\stock\\result\\result_decrease_" + firstYear + "_" + season1
                    + "_" + secondYear + "_" + season2 + ".txt", Encoding.Default);
                String decstr = decsr.ReadToEnd();
                string[] decall = decstr.Split(',');
                for (int i = 0; i < 50; i++)
                {
                    DataGridViewRow row = new DataGridViewRow();

                    this.Invoke((EventHandler)delegate
                    {
                        if (skinDataGridView52.RowCount < 50)
                        {
                            skinDataGridView52.Rows.Add(row);
                        }
                        skinDataGridView52.Rows[i].Cells[0].Value = i + 1;
                        skinDataGridView52.Rows[i].Cells[1].Value = decall[i * 2];

                        double d = Convert.ToDouble(decall[i * 2 + 1]);
                        d = Math.Round(d, 2);
                        skinDataGridView52.Rows[i].Cells[2].Value = d;
                    });
                }

                skinLabel55.Visible = true;
                skinLabel56.Visible = true;
                skinDataGridView51.Visible = true;
                skinDataGridView52.Visible = true;
            }



        }

        private void progressBar1_Click(object sender, EventArgs e)
        {
            
                bgworker.CancelAsync();
            
        }

        private void progressBar2_Click(object sender, EventArgs e)
        {

        }

        private void progressBar3_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if(e.KeyValue == 13)
            {
                this.button3.PerformClick();
            }
        }

        private void skinTextBox20_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                this.skinButton20.PerformClick();
            }
        }

        private void skinTextBox30_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                this.skinButton30.PerformClick();
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void skinTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void skinTabPage1_Click(object sender, EventArgs e)
        {

        }
    }

    public class Stock
    {
        public string name { get; set; }
        public double sum { get; set; }

    }
}
