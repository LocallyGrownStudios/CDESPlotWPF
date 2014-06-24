using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
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

// TO DO
// Add Radial Buttons for Pricing
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
                stackPageSize.Children.Clear();
                fileNameDisplay.Clear();
                fileSizeDisplay.Clear();
                numPagesDisplay.Clear();
                filePageSizes.Clear();
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
                string fileName = dlg.FileName;
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
                    fileSizeDisplay.TextAlignment = TextAlignment.Right;
                    fileSizeDisplay.Text += sizeGB;
                    fileSizeDisplay.Text += " GB";
                }
                else if (sizeMB >= 1)
                {
                    fileSizeDisplay.Clear();
                    fileSizeDisplay.TextAlignment = TextAlignment.Right;
                    fileSizeDisplay.Text += sizeMB;
                    fileSizeDisplay.Text += " MB";

                }
                else
                {
                    fileSizeDisplay.Clear();
                    fileSizeDisplay.TextAlignment = TextAlignment.Right;
                    fileSizeDisplay.Text += sizeKB;
                    fileSizeDisplay.Text += " KB";
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
                                    filePageSizes.Text += pageLength / postScriptPoints;
                                    filePageSizes.Text += " x ";
                                    filePageSizes.Text += pageWidth / postScriptPoints;
                                    filePageSizes.Text += " , ";
                                    double lengthToCharge = mediabox.Height / postScriptPoints;
                                    PageSizeBoxes(pageLength, pageWidth);
                                    CalculateCost(lengthToCharge);
                                }
                            }
                            else if (mediabox.Width / postScriptPoints <= 42)
                            {
                                {

                                    filePageSizes.Text += mediabox.Width / postScriptPoints;
                                    filePageSizes.Text += " x ";
                                    filePageSizes.Text += mediabox.Height / postScriptPoints;
                                    filePageSizes.Text += " , ";
                                    double lengthToCharge = mediabox.Height / postScriptPoints;
                                    PageSizeBoxes(pageLength, pageWidth);
                                    CalculateCost(lengthToCharge);
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

        private void CalculateCost(double lengthToCharge)
        {
            try
            {
                double pageCostBond = ((lengthToCharge / 12) * costBond);
                pageCostBond = Math.Round(pageCostBond, 2);
                filePageSizes.Text += " Bond $ ";
                filePageSizes.Text += string.Format("{0:f2}", pageCostBond);
                filePageSizes.Text += " , ";
                double pageCostMatte = ((lengthToCharge / 12) * costMatte);
                pageCostMatte = Math.Round(pageCostMatte, 2);
                filePageSizes.Text += " Matte $ ";
                filePageSizes.Text += string.Format("{0:f2}", pageCostMatte);
                filePageSizes.Text += " , ";
                double pageCostSatin = ((lengthToCharge / 12) * costSatin);
                pageCostSatin = Math.Round(pageCostSatin, 2);
                filePageSizes.Text += " Satin $ ";
                filePageSizes.Text += string.Format("{0:f2}", pageCostSatin);
                filePageSizes.Text += " , ";
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was a problem calculating the page cost. Please check the file and try again.");
            }
        }

        private void PageSizeBoxes(double pageLength, double pageWidth)
        {
            try
            {
                int pageBoxNum = 0;
                pageBoxNum++;
                TextBox pageSizeBox = new TextBox();
                pageSizeBox.IsReadOnly = true;
                pageSizeBox.Name = "pageSizeBox_" + pageBoxNum;
                pageSizeBox.Width = 75;
                pageSizeBox.Height = 25;
                pageSizeBox.Margin = new Thickness(-25, 10, 0, 0);
                pageSizeBox.TextAlignment = TextAlignment.Center;
                if (pageLength <= 42)
                {
                    if (pageWidth <= 42)
                    {
                        if (pageLength < pageWidth)
                        {
                            pageSizeBox.Text = pageLength + " x " + pageWidth;
                        }
                        else if (pageWidth <= 42)
                        {
                            pageSizeBox.Text = pageWidth + " x " + pageLength;
                        }
                    }
                    stackPageSize.Children.Add(pageSizeBox);
                }
                else if (pageLength > 42)
                {
                    if (pageWidth > 42)
                    {
                        MessageBox.Show("One or more pages is too large. Currently the maximum printable size is 42 inches. Please check the file and try again.");
                    }
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("There was a problem calculating the page Sizes. Please check the file and try again.");
            }
        }
    }
}
