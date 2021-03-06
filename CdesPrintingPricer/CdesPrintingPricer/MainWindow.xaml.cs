﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using iTextSharp.text.pdf;
using org.pdfclown.files;
using org.pdfclown.objects;
using org.pdfclown.tools;
using org.pdfclown.documents;
using io = System.IO;
// TO DO
// Add Pdf Portfolio Support
// Add functionality to manually enter data, a calculator
// Add functionality for multiple file selection, place in own container for auto population
// Add functionality to populate individual containers for each file
// Add functionality to select different papers for different files
// Add functionality for laser printer prices
// Add functionality to save file as reduced size, rasterized and x1-a compatible
// ** possible page viewer
// ** Possible functionality to submit plots/prints
// ** Possible functionality to charge themselves for plots/prints

namespace CdesPrintingPricer
{

    public partial class MainWindow : Window
    {

        int i;
        string nameId;
        int numPages;
        decimal decPageCost;
        string strPageCost;
        decimal decTotalCost;
        double costSatin = 3.00;
        double costMatte = 3.00;
        double costBond = 1.00;
        double totalPageCost;
        string[] listItems;
        string fileState;
        int documentPageCount;
        decimal finalPageCost;
        const double postScriptPoints = 72.00;
        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

        public MainWindow()
        {
            InitializeComponent();


        }

        private void browseForFile_Click(object sender, RoutedEventArgs e)
        {
            OpenNewFile(true);
            if (documentPageCount == 1)
            {
                if (fileNameDisplay.Text == null)
                {

                    CalculateCost();
                }
                else if (fileState != null)
                {
                    CalculateCost();
                    i++;
                }
            }
        }


        private void OpenNewFile(bool openNewFile)
        {
            try
            {
                stackPageLayout.Children.Clear();
                stackButtonLayout.Children.Clear();
                stackCostLayout.Children.Clear();
                stackTotalCost.Children.Clear();
                fileNameDisplay.Clear();
                fileSizeDisplay.Clear();
                numPagesDisplay.Clear();
                dlg.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dlg.Filter = "PDF Files (*.pdf)|*.pdf|All files (*.*)|*.*";
                dlg.FilterIndex = 1;
                dlg.RestoreDirectory = true;
                Nullable<bool> result = dlg.ShowDialog();
                if (result == true)
                {
                    GetFileName(true);
                    GetNumPages(true);
                    GetFileSize(true);
                    GetPageSizes(true);
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was a problem writing the current files data. Please verify file still exists and try again.");
            }
        }

        private void GetFileName(bool getFileName)
        {
            try
            {
                string fileName = dlg.SafeFileName;
                fileNameDisplay.Clear();
                fileNameDisplay.Text += fileName;
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was a problem reading the selectd file. Please check the file and try again.");
            }
        }

        private void GetFileSize(bool getFileSize)
        {
            try
            {
                double sizeKB = 0;
                double sizeMB = 0;
                double sizeGB = 0;
                long text = new io::FileInfo(dlg.FileName).Length;
                double sizeBytes = Convert.ToDouble(text);
                sizeKB = sizeBytes / 1024;
                sizeMB = sizeKB / 1024;
                sizeGB = sizeMB / 1024;
                sizeKB = Math.Round(sizeKB, 2);
                sizeGB = Math.Round(sizeGB, 2);
                sizeMB = Math.Round(sizeMB, 2);

                if (sizeGB >= 1)
                {
                    fileSizeDisplay.Clear();
                    fileSizeDisplay.TextAlignment = TextAlignment.Center;
                    fileSizeDisplay.Text += sizeGB + " GB";
                }
                else if (sizeMB >= 1)
                {
                    fileSizeDisplay.Clear();
                    fileSizeDisplay.TextAlignment = TextAlignment.Center;
                    fileSizeDisplay.Text += sizeMB + " MB";
                }
                else
                {
                    fileSizeDisplay.Clear();
                    fileSizeDisplay.TextAlignment = TextAlignment.Center;
                    fileSizeDisplay.Text += sizeKB + " KB";
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was an error reading the file size. Please check the file and try again.");
            }
        }

        private void GetNumPages(bool getNumPages)
        {
            try
            {
                string filePages = dlg.FileName;
                using (File currentFile = new File(filePages))
                {
                    HashSet<PdfReference> visitedReferences = new HashSet<PdfReference>();
                    org.pdfclown.documents.Document documentName = currentFile.Document;
                    Pages documentPages = documentName.Pages;
                    numPages = documentPages.Count;
                    numPagesDisplay.Clear();
                    numPagesDisplay.TextAlignment = TextAlignment.Right;
                    numPagesDisplay.Text += numPages;
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was an error reading the number of pages. Please check the file and try again");
            }
        }

        private void GetPageSizes(bool getPageSizes)
        {
            int pageNum = 0;
            long incrementalDataSize = 0;
            string filePageSize = dlg.FileName;
            using (File currentFile = new File(dlg.FileName))
                try
                {

                    {
                        HashSet<PdfReference> visitedReferences = new HashSet<PdfReference>();
                        PdfReader reader = new PdfReader(filePageSize);
                        org.pdfclown.documents.Document documentName = currentFile.Document;
                        Pages documentPages = documentName.Pages;
                        documentPageCount = documentPages.Count;
                        foreach (org.pdfclown.documents.Page page in documentPages)
                        {
                            pageNum++;
                            iTextSharp.text.Rectangle mediabox = reader.GetPageSize(pageNum);
                            long pageFullDataSize = PageManager.GetSize(page);
                            long pageDifferentialDataSize = PageManager.GetSize(page, visitedReferences);
                            incrementalDataSize += pageDifferentialDataSize;
                            double pageLength = mediabox.Height / postScriptPoints;
                            double pageWidth = mediabox.Width / postScriptPoints;
                            PageSizeBoxes(pageLength, pageWidth);
                            if (documentPageCount > 1)
                            {
                                PageSelectButtons(true);
                            }
                            PageCostBoxes();
                        }
                    }
                    if (fileState != null)
                    {
                        TotalCostBox();
                    }
                }

                catch (System.IO.IOException)
                {
                    MessageBox.Show("There was a problem reading the page sizes. Please check the file and try again.");
                }

        }

        private void PageSizeBoxes(double pageLength, double pageWidth)
        {
            try
            {
                TextBox pageSizeBox = new TextBox();
                pageSizeBox.IsReadOnly = true;
                pageSizeBox.Name = "pageSizeBox_" + stackPageLayout.Children.Count;
                pageSizeBox.Width = 75;
                pageSizeBox.Height = 25;
                pageSizeBox.TextWrapping = TextWrapping.NoWrap;
                pageSizeBox.Margin = new Thickness(-25, 10, 0, 0);
                pageSizeBox.TextAlignment = TextAlignment.Center;
                if (FindName("pageSizeBox_" + stackPageLayout.Children.Count) != null)
                {
                    UnregisterName("pageSizeBox_" + stackPageLayout.Children.Count);
                }
                if (pageLength < pageWidth)
                {
                    if (pageWidth <= 42)
                    {
                        pageSizeBox.Text = pageLength + " x " + pageWidth;
                        stackPageLayout.Children.Add(pageSizeBox);
                        this.RegisterName(string.Format("pageSizeBox_" + stackCostLayout.Children.Count), pageSizeBox);
                        fileState = "name";
                    }
                }
                else if (pageWidth < pageLength)
                {
                    if (pageLength <= 42)
                    {
                        pageSizeBox.Text = pageWidth + " x " + pageLength;
                        stackPageLayout.Children.Add(pageSizeBox);
                        this.RegisterName(string.Format("pageSizeBox_" + stackCostLayout.Children.Count), pageSizeBox);
                        fileState = "name";
                    }
                    else if (pageWidth <= 42)
                    {
                        pageSizeBox.Text = pageLength + " x " + pageWidth;
                        stackPageLayout.Children.Add(pageSizeBox);
                        this.RegisterName(string.Format("pageSizeBox_" + stackCostLayout.Children.Count), pageSizeBox);
                        fileState = "name";
                    }
                }

                else
                {
                    fileState = null;
                    MessageBox.Show("One or more pages is too large. Currently the maximum printable size is 42 inches. Please check the file and try again.");
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was a problem calculating the page Sizes. Please check the file and try again.");
            }
        }


        private void TotalCostBox()
        {
            TextBox totalCost = new TextBox();
            totalCost.IsReadOnly = true;
            totalCost.HorizontalAlignment = HorizontalAlignment.Center;
            totalCost.Name = "totalCost";
            totalCost.Width = 75;
            totalCost.Height = 25;
            totalCost.TextWrapping = TextWrapping.NoWrap;
            totalCost.Margin = new Thickness(-25, 10, 0, 0);
            totalCost.TextAlignment = TextAlignment.Center;
            stackTotalCost.Children.Add(totalCost);
            if (FindName("totalCost") != null)
            {
                UnregisterName("totalCost");
            }
            this.RegisterName(string.Format("totalCost"), totalCost);

        }

        private void PageSelectButtons(bool pageSelectButton)
        {
            CheckBox pageSelect = new CheckBox();
            pageSelect.Name = "pageSelectButton_" + stackButtonLayout.Children.Count;
            string pageID = pageSelect.Name;
            pageSelect.HorizontalAlignment = HorizontalAlignment.Center;
            pageSelect.Margin = new Thickness(0, 18.5, 0, 0);
            pageSelect.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(pageCost_Checked));
            pageSelect.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(pageCost_Unchecked));
            stackButtonLayout.Children.Add(pageSelect);

        }

        private void PageCostBoxes()
        {
            if (documentPageCount >= 1)
            {
                foreach (TextBox pageSizeBox in stackPageLayout.Children.OfType<TextBox>().Where(t => t.Name.Equals("pageSizeBox_" + stackCostLayout.Children.Count)))
                {
                    TextBox pageCostBox = new TextBox();
                    pageCostBox.IsReadOnly = true;
                    pageCostBox.Name = "pageCostBox_" + stackCostLayout.Children.Count;
                    pageCostBox.HorizontalAlignment = HorizontalAlignment.Center;
                    pageCostBox.TextAlignment = TextAlignment.Center;
                    pageCostBox.Width = 75;
                    pageCostBox.Height = 25;
                    pageCostBox.TextWrapping = TextWrapping.NoWrap;
                    pageCostBox.Margin = new Thickness(-25, 10, 0, 0);
                    pageCostBox.Text = "";
                    stackCostLayout.Children.Add(pageCostBox);
                    if (FindName("pageCostBox_" + stackCostLayout.Children.Count) != null)
                    {
                        UnregisterName("pageCostBox_" + stackCostLayout.Children.Count);
                    }
                    this.RegisterName(string.Format("pageCostBox_" + stackCostLayout.Children.Count), pageCostBox);

                }
            }
        }


        public void pageCost_Checked(object sender, RoutedEventArgs e)
        {
            TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
            TextBox totalCost = (TextBox)this.FindName("totalCost");
            var pageBox = sender as CheckBox;
            var pageLayout = pageBox.Parent as FrameworkElement;
            var name = (((CheckBox)sender).Name);
            nameId = Regex.Match(name, @"\d+").Value;
            i = Convert.ToInt32(nameId);
            CalculateCost();
            ArrayList list = new ArrayList(1000);
            foreach (TextBox pageCost in stackCostLayout.Children)
            {
                strPageCost = pageCost.Text.Trim(new char[] { '$' });
                if (pageCost.Text != "")
                {
                    decPageCost = Convert.ToDecimal(strPageCost);
                    list.Add(decPageCost);
                }
            }
            for (int x = 0; x < list.Count; x++)
            {
                CalcTotalCost(list);
                totalCost.Text = "$" + Convert.ToString(decTotalCost);
            }
            UpdateTotalCost();
        }


        decimal CalcTotalCost(ArrayList list)
        {

            decimal[] decSubCost = new decimal[stackCostLayout.Children.Count];
            for (int x = 0; x < list.Count; x++)
            {
                decSubCost[x] = Convert.ToDecimal(list[x]);
                decTotalCost = decSubCost.Sum();
            }


            return

            decTotalCost;

        }

        private void pageCost_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBox totalCost = (TextBox)this.FindName("totalCost");
            var pageBox = sender as CheckBox;
            var pageLayout = pageBox.Parent as FrameworkElement;
            var name = (((CheckBox)sender).Name);
            nameId = Regex.Match(name, @"\d+").Value;
            i = Convert.ToInt32(nameId);
            ClearCost();
            ArrayList list = new ArrayList(1000);
            foreach (TextBox pageCost in stackCostLayout.Children)
            {
                strPageCost = pageCost.Text.Trim(new char[] { '$' });
                if (pageCost.Text != "")
                {
                    decPageCost = Convert.ToDecimal(strPageCost);
                    list.Add(decPageCost);
                }
            }
            for (int x = 0; x < list.Count; x++)
            {
                CalcTotalCost(list);
                totalCost.Text = "$" + Convert.ToString(decTotalCost);
            }
            UpdateTotalCost();
        }


        private void CalculateCost()
        {

            TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
            i--;
            TextBox pageSizeBox = (TextBox)this.FindName(string.Format("pageSizeBox_{0}", i + 1));
            {
                string pageDimension = pageSizeBox.Text;
                string chargeDimension = pageDimension.Substring(0, pageDimension.LastIndexOf(" x") + 1);

                if (chooseBond.IsChecked == true)
                {
                    totalPageCost = costBond * (Convert.ToDouble(chargeDimension) / 12);
                    totalPageCost = Math.Round(totalPageCost, 2);
                    finalPageCost = Convert.ToDecimal(totalPageCost);
                    Convert.ToString(totalPageCost);
                    pageCostBox.Text = "$ " + string.Format("{0:f2}", totalPageCost);
                }

                else if (chooseMatte.IsChecked == true)
                {
                    totalPageCost = costMatte * (Convert.ToDouble(chargeDimension) / 12);
                    totalPageCost = Math.Round(totalPageCost, 2);
                    finalPageCost = Convert.ToDecimal(totalPageCost);
                    Convert.ToString(totalPageCost);
                    pageCostBox.Text = "$ " + string.Format("{0:f2}", totalPageCost);
                }

                else if (chooseSatin.IsChecked == true)
                {
                    totalPageCost = costSatin * (Convert.ToDouble(chargeDimension) / 12);
                    totalPageCost = Math.Round(totalPageCost, 2);
                    finalPageCost = Convert.ToDecimal(totalPageCost);
                    Convert.ToString(totalPageCost);
                    pageCostBox.Text = "$ " + string.Format("{0:f2}", totalPageCost);
                }
            }
        }

        private void UpdateCost()
        {
            {
                TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
                i--;
                TextBox pageSizeBox = (TextBox)this.FindName(string.Format("pageSizeBox_{0}", i + 1));
                i++;
                if (pageCostBox.Text != "")
                {
                    string pageDimension = pageSizeBox.Text;
                    string chargeDimension = pageDimension.Substring(0, pageDimension.LastIndexOf(" x") + 1);

                    if (chooseBond.IsChecked == true)
                    {
                        totalPageCost = costBond * (Convert.ToDouble(chargeDimension) / 12);
                        totalPageCost = Math.Round(totalPageCost, 2);
                        Convert.ToString(totalPageCost);
                        pageCostBox.Text = "$ " + string.Format("{0:f2}", totalPageCost);
                    }

                    else if (chooseMatte.IsChecked == true)
                    {
                        totalPageCost = costMatte * (Convert.ToDouble(chargeDimension) / 12);
                        totalPageCost = Math.Round(totalPageCost, 2);
                        Convert.ToString(totalPageCost);
                        pageCostBox.Text = "$ " + string.Format("{0:f2}", totalPageCost);
                    }

                    else if (chooseSatin.IsChecked == true)
                    {
                        totalPageCost = costSatin * (Convert.ToDouble(chargeDimension) / 12);
                        totalPageCost = Math.Round(totalPageCost, 2);
                        Convert.ToString(totalPageCost);
                        pageCostBox.Text = "$ " + string.Format("{0:f2}", totalPageCost);
                    }
                }
            }
        }




        private void ClearCost()
        {
            TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
            TextBox pageSizeBox = (TextBox)this.FindName(string.Format("pageSizeBox_{0}", i + 1));
            TextBox totalCost = (TextBox)this.FindName(string.Format("totalCost"));
            {
                pageCostBox.Clear();
                totalCost.Clear();
            }
        }


        private void UpdateTotalCost()
        {
            TextBox totalCost = (TextBox)this.FindName("totalCost");
            i = Convert.ToInt32(nameId);
            TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
            TextBox pageSizeBox = (TextBox)this.FindName(string.Format("pageSizeBox_{0}", i + 1));

        }


        private void chooseBond_Checked(object sender, RoutedEventArgs e)
        {
            i = 0;
            if (numPages != 0)
            {
                TextBox totalCost = (TextBox)this.FindName("totalCost");
                TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
                foreach (TextBox box in stackCostLayout.Children)
                {
                    UpdateCost();
                    ArrayList list = new ArrayList(1000);
                    foreach (TextBox pageCost in stackCostLayout.Children)
                    {
                        strPageCost = pageCost.Text.Trim(new char[] { '$' });
                        if (pageCost.Text != "")
                        {
                            decPageCost = Convert.ToDecimal(strPageCost);
                            list.Add(decPageCost);
                        }
                    }
                    for (int x = 0; x < list.Count; x++)
                    {
                        CalcTotalCost(list);
                        totalCost.Text = "$" + Convert.ToString(decTotalCost);
                    }
                    UpdateTotalCost();
                }
                i++;
            }
        }



        private void chooseMatte_Checked(object sender, RoutedEventArgs e)
        {
            i = 0;
            if (numPages != 0)
            {
                TextBox totalCost = (TextBox)this.FindName("totalCost");
                TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
                foreach (TextBox box in stackCostLayout.Children)
                {
                    UpdateCost();
                    ArrayList list = new ArrayList(1000);
                    foreach (TextBox pageCost in stackCostLayout.Children)
                    {
                        strPageCost = pageCost.Text.Trim(new char[] { '$' });
                        if (pageCost.Text != "")
                        {
                            decPageCost = Convert.ToDecimal(strPageCost);
                            list.Add(decPageCost);
                        }
                    }
                    for (int x = 0; x < list.Count; x++)
                    {
                        CalcTotalCost(list);
                        totalCost.Text = "$" + Convert.ToString(decTotalCost);
                    }
                    UpdateTotalCost();
                }
                i++;
            }
        }

        private void chooseSatin_Checked(object sender, RoutedEventArgs e)
        {
            i = 0;
            if (numPages != 0)
            {
                TextBox totalCost = (TextBox)this.FindName("totalCost");
                TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
                foreach (TextBox box in stackCostLayout.Children)
                {
                    UpdateCost();
                    ArrayList list = new ArrayList(1000);
                    foreach (TextBox pageCost in stackCostLayout.Children)
                    {
                        strPageCost = pageCost.Text.Trim(new char[] { '$' });
                        if (pageCost.Text != "")
                        {
                            decPageCost = Convert.ToDecimal(strPageCost);
                            list.Add(decPageCost);
                        }
                    }
                    for (int x = 0; x < list.Count; x++)
                    {
                        CalcTotalCost(list);
                        totalCost.Text = "$ " + Convert.ToString(decTotalCost);
                    }
                    UpdateTotalCost();
                }
                i++;
            }
        }



        private void fileNameDisplay_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
