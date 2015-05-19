﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Net;
using System.Web;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Xml;
using System.Threading;

namespace EVELauncher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string saveFileJson;
        saveFile userSaveFile = new saveFile();
        string temp = System.IO.Path.GetTempPath();
        netConnect eveConnection = new netConnect();
        public MainWindow()
        {
            InitializeComponent();
            updateServerStatus();
            if (!File.Exists(temp + @"\fakeEveLauncher.json"))
            {
                userSaveFile.path = "";
                userSaveFile.userName = "";
                userSaveFile.userPass = "";
                userSaveFile.isCloseAfterLaunch = false;
                userSaveFile.Write(temp + @"\fakeEveLauncher.json", JsonConvert.SerializeObject(userSaveFile));
            }
            else
            {
                saveFileJson = userSaveFile.Read(temp + @"\fakeEveLauncher.json",Encoding.UTF8);
                userSaveFile = JsonConvert.DeserializeObject<saveFile>(saveFileJson);
                gameExePath.Text = userSaveFile.path;
                userName.Text = userSaveFile.userName;
                userPass.Password = userSaveFile.userPass;
                exitAfterLaunch.IsChecked = userSaveFile.isCloseAfterLaunch;
                if (!String.IsNullOrEmpty(userSaveFile.userName))
                {
                    saveUserName.IsChecked = true;
                }
                if (!String.IsNullOrEmpty(userSaveFile.userPass))
                {
                    savePassword.IsChecked = true;
                }
            }
        }

        private void loginClearClick(object sender, RoutedEventArgs e)
        {
            userName.Text = "";
            userPass.Password = "";
        }

        private void loginButtonClick(object sender, RoutedEventArgs e)
        {
            string accessToken;
            if (String.IsNullOrEmpty(userName.Text) == false || String.IsNullOrEmpty(userPass.Password) == false)
            {
                if (saveUserName.IsChecked == true)
                {
                    userSaveFile.userName = userName.Text;
                    if (savePassword.IsChecked == true)
                    {
                        userSaveFile.userPass = userPass.Password;
                    }
                }
                if (String.IsNullOrEmpty(gameExePath.Text) == false)
                {
                    userSaveFile.path = gameExePath.Text;
                    File.WriteAllText(temp + @"\fakeEveLauncher.json", JsonConvert.SerializeObject(userSaveFile));
                        accessToken = eveConnection.getAccessToken(userName.Text, userPass.Password);
                        if (accessToken == "netErr")
                        {
                            MessageBox.Show("登录失败，网络错误","错误",MessageBoxButton.OK,MessageBoxImage.Error);
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(accessToken) == false)
                            {
                                Process.Start(gameExePath.Text, "/noconsole /ssoToken=" + accessToken);
                                if (exitAfterLaunch.IsChecked == true)
                                {
                                    userSaveFile.isCloseAfterLaunch = true;
                                    File.WriteAllText(temp + @"\fakeEveLauncher.json", JsonConvert.SerializeObject(userSaveFile));
                                    this.Close();
                                }
                            }
                            else
                            {
                                MessageBox.Show("登录失败，用户名或密码错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                }
                else
                {
                    MessageBox.Show("请指定主执行程序路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("请填写用户名和密码", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void choosePathClick(object sender, RoutedEventArgs e)
        {
            string exePath;
            OpenFileDialog chooseExeFile = new OpenFileDialog();
            chooseExeFile.InitialDirectory = @"C:\";
            chooseExeFile.Filter = "CCP-EVE执行主程序|exefile.exe";
            if (chooseExeFile.ShowDialog() == true)
            {
                exePath = chooseExeFile.FileName;
                gameExePath.Text = exePath;
            }
        }

        private void serverStateRefresh(object sender, RoutedEventArgs e)
        {
            updateServerStatus();
        }

        private void aboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("EVE山寨启动器，无广告~ \n协议：MIT License \n作者：@imi415_ \n更新：https://blog.imi.moe/?p=288 \nGitHub主页：https://github.com/imi415/EVELauncher ","关于",MessageBoxButton.OK,MessageBoxImage.Asterisk);
        }

        private void checkSavePass(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("警告：密码明文存储，请勿在非本人电脑上勾选此项！用户信息存储在临时文件夹的fakeEveLauncher.json里，请注意删除！！", "安全警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
            {
                savePassword.IsChecked = false;
            }
            saveUserName.IsChecked = true;
        }

        //异步发送更新请求，委托更新状态控件
        public async void updateServerStatus()
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => refreshStatus.Content = "正在刷新，请稍等..."));
                string XMLString;
                XMLString = eveConnection.getApiXML("https://api.eve-online.com.cn/server/ServerStatus.xml.aspx");
                XmlDocument XML = new XmlDocument();
                XML.LoadXml(XMLString);
                string JSON = JsonConvert.SerializeXmlNode(XML);
                JSON = JSON.Replace("@", "");
                eveServerStatus status = JsonConvert.DeserializeObject<eveServerStatus>(JSON);
                if (status.eveApi.result.serverOpen == "True")
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => serverStatusLabel.Content = "开启"));
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => serverStatusLabel.Content = "关闭"));
                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() => playerNumberLabel.Content = status.eveApi.result.onlinePlayers));
                Application.Current.Dispatcher.BeginInvoke(new Action(() => lastUpdateLabel.Content = status.eveApi.cachedUntil + " UTC+08:00"));
                Application.Current.Dispatcher.BeginInvoke(new Action(() => refreshStatus.Content = "刷新完成"));
            });
        }
    }
}
