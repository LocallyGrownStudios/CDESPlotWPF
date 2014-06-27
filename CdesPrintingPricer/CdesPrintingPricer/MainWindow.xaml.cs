using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using org.pdfclown.files;
using org.pdfclown.objects;
using org.pdfclown.tools;
using org.pdfclown.documents;
using org.pdfclown.documents.contents;
using org.pdfclown.documents.contents.objects;
using org.pdfclown.documents.interaction;
using org.pdfclown.documents.interchange.metadata;
using org.pdfclown.documents.interaction.viewer;
using System.ComponentModel;

// TO DO
// Add Pdf Portfolio Support
// Add Price Boxes
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

        double costSatin = 3.00;
        double costMatte = 3.00;
        double costBond = 1.00;
        const double postScriptPoints = 72.00;
        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
        string lengthToCharge;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void browseForFile_Click(object sender, RoutedEventArgs e)
        {
            OpenNewFile(true);
        }

        private void OpenNewFile(bool openNewFile)
        {
            try
            {
                stackPageLayout.Children.Clear();
                stackButtonLayout.Children.Clear();
                stackCostLayout.Children.Clear();
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
                string text = System.IO.File.ReadAllText(dlg.FileName);
                int size = text.Length;
                double sizeKB = text.Length / 1024;
                double sizeMB = sizeKB / 1024;
                double sizeGB = sizeMB / 1024;
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
                int numPages = 0;
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
                        int documentPageCount = documentPages.Count;
                        foreach (org.pdfclown.documents.Page page in documentPages)
                        {
                            pageNum++;
                            iTextSharp.text.Rectangle mediabox = reader.GetPageSize(pageNum);
                            long pageFullDataSize = PageManager.GetSize(page);
                            long pageDifferentialDataSize = PageManager.GetSize(page, visitedReferences);
                            incrementalDataSize += pageDifferentialDataSize;
                            double pageLength = mediabox.Height / postScriptPoints;
                            double pageWidth = mediabox.Width / postScriptPoints;
                            if (mediabox.Height / postScriptPoints < mediabox.Width / postScriptPoints)
                            {
                                if (mediabox.Height / postScriptPoints <= 42)
                                {
                                    lengthToCharge = Convert.ToString(mediabox.Height / postScriptPoints);
                                    PageSizeBoxes(pageLength, pageWidth);
                                    if (documentPageCount > 1)
                                    {
                                        PageSelectButtons(true);
                                    }
                                    PageCostBoxes(documentPageCount);
                                }
                            }
                            else if (mediabox.Width / postScriptPoints <= 42)
                            {
                                {
                                    lengthToCharge = Convert.ToString(mediabox.Height / postScriptPoints);
                                    PageSizeBoxes(pageLength, pageWidth);
                                    if (documentPageCount > 1)
                                    {
                                        PageSelectButtons(true);
                                    }
                                    PageCostBoxes(documentPageCount);
                                }
                            }

                            else if (mediabox.Height / postScriptPoints > 42)
                            {
                                if (mediabox.Width / postScriptPoints > 42)
                                {
                                    MessageBox.Show("One or more pages is too large. Currently the maximum printable size is 42 inches.");
                                }
                            }
                        }
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
                this.RegisterName(string.Format("pageSizeBox_" + stackCostLayout.Children.Count), pageSizeBox);
                if (pageLength < pageWidth)
                {
                    if (pageLength <= 42)
                    {
                        pageSizeBox.Text = pageLength + " x " + pageWidth;
                        stackPageLayout.Children.Add(pageSizeBox);
                    }
                }
                else if (pageWidth <= 42)
                {
                    pageSizeBox.Text = pageWidth + " x " + pageLength;
                    stackPageLayout.Children.Add(pageSizeBox);
                }
                else
                {
                    MessageBox.Show("One or more pages is too large. Currently the maximum printable size is 42 inches. Please check the file and try again.");
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was a problem calculating the page Sizes. Please check the file and try again.");
            }
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

        private void PageCostBoxes(int documentPageCount)
        {
            if (documentPageCount >= 1)
            {
                foreach (TextBox pageSizeBox in stackPageLayout.Children.OfType<TextBox>().Where(t => t.Name.Equals("pageSizeBox_" + stackCostLayout.Children.Count)))
                {
                    TextBox pageCostBox = new TextBox();
                    pageCostBox.Name = "pageCostBox_" + stackCostLayout.Children.Count;
                    pageCostBox.HorizontalAlignment = HorizontalAlignment.Center;
                    pageCostBox.TextAlignment = TextAlignment.Center;
                    pageCostBox.Width = 75;
                    pageCostBox.Height = 25;
                    pageCostBox.TextWrapping = TextWrapping.NoWrap;
                    pageCostBox.Margin = new Thickness(-25, 10, 0, 0);
                    pageCostBox.Text = "";
                    stackCostLayout.Children.Add(pageCostBox);
                    this.RegisterName(string.Format("pageCostBox_" + stackCostLayout.Children.Count), pageCostBox);
                }
            }
        }


        private void pageCost_Checked(object sender, RoutedEventArgs e)
        {
            var pageBox = sender as CheckBox;
            var pageLayout = pageBox.Parent as FrameworkElement;
            var name = (((CheckBox)sender).Name);
            string nameId;
            nameId = Regex.Match(name, @"\d+").Value;
            int i = Convert.ToInt32(nameId);
            CalculateCost(nameId, i);
        }


        private void CalculateCost  (string nameId, int i)
        {
                TextBox pageCostBox = (TextBox)this.FindName(string.Format("pageCostBox_{0}", i + 1));
                i--;
                TextBox pageSizeBox = (TextBox)this.FindName(string.Format("pageSizeBox_{0}", i + 1));
                {
                    string costboxName = pageCostBox.Name;
                    string pageBoxName = pageSizeBox.Name;

                    if (chooseBond.IsChecked == true)
                    {
                        double totalPageCost = costBond * (Convert.ToDouble(lengthToCharge) / 12);
                        pageCostBox.Text = Convert.ToString(totalPageCost);
                    }

                    if (chooseMatte.IsChecked == true)
                    {
                        double totalPageCost = costMatte * (Convert.ToDouble(lengthToCharge) / 12);
                        pageCostBox.Text = Convert.ToString(totalPageCost);
                    }

                    if (chooseSatin.IsChecked == true)
                    {
                        double totalPageCost = costSatin * (Convert.ToDouble(lengthToCharge) / 12);
                        pageCostBox.Text = Convert.ToString(totalPageCost);
                    }
                
            }
        }

        private void pageCost_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void chooseBond_Checked(object sender, RoutedEventArgs e)
        {
            pageCost_Checked(sender, e);
        }

        private void chooseMatte_Checked(object sender, RoutedEventArgs e)
        {
            pageCost_Checked(sender, e);
        }

        private void chooseSatin_Checked(object sender, RoutedEventArgs e)
        {
            pageCost_Checked(sender, e);
        }

        private void fileNameDisplay_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
